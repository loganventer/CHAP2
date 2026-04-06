/**
 * ChorusSyncService - Real-time chorus synchronization via SignalR.
 *
 * Single Responsibility: Only manages the SignalR connection and chorus change events.
 * Dependency Inversion: Consumers depend on the IChorusSyncService contract.
 *
 * @interface IChorusSyncService
 * @method connect(): Promise<void>
 * @method onChorusChanged(callback: (chorusId: string) => void): void
 * @method broadcastChorusChange(chorusId: string): Promise<void>
 * @method isConnected(): boolean
 * @method disconnect(): Promise<void>
 */
class ChorusSyncService {
    /**
     * @param {Object} [options]
     * @param {string} [options.hubUrl='/chorusHub'] - The SignalR hub endpoint.
     * @param {function} [options.onStatusChange] - Called with status: 'connected' | 'disconnected' | 'reconnecting'.
     */
    constructor(options = {}) {
        this._hubUrl = options.hubUrl || '/chorusHub';
        this._onStatusChange = options.onStatusChange || null;
        this._connection = null;
        this._chorusChangedCallbacks = [];
        this._connected = false;
    }

    /** Establish the SignalR connection with auto-reconnect. */
    async connect() {
        if (typeof signalR === 'undefined') {
            console.warn('[ChorusSyncService] SignalR library not loaded. Real-time sync disabled.');
            return;
        }

        this._connection = new signalR.HubConnectionBuilder()
            .withUrl(this._hubUrl)
            .withAutomaticReconnect([0, 1000, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        // Register the server-to-client method
        this._connection.on('ReceiveChorusChanged', (chorusId) => {
            debug('[ChorusSyncService] Received chorus change:', chorusId);
            for (const callback of this._chorusChangedCallbacks) {
                callback(chorusId);
            }
        });

        // Connection lifecycle events
        this._connection.onreconnecting(() => {
            this._connected = false;
            this._setStatus('reconnecting');
            debug('[ChorusSyncService] Reconnecting...');
        });

        this._connection.onreconnected(() => {
            this._connected = true;
            this._setStatus('connected');
            debug('[ChorusSyncService] Reconnected.');
        });

        this._connection.onclose(() => {
            this._connected = false;
            this._setStatus('disconnected');
            debug('[ChorusSyncService] Connection closed.');
        });

        try {
            await this._connection.start();
            this._connected = true;
            this._setStatus('connected');
            debug('[ChorusSyncService] Connected to hub.');
        } catch (err) {
            console.error('[ChorusSyncService] Failed to connect:', err);
            this._setStatus('disconnected');
        }
    }

    /**
     * Register a callback for when a remote client changes the chorus.
     * @param {function(string): void} callback - Receives the new chorus ID.
     */
    onChorusChanged(callback) {
        this._chorusChangedCallbacks.push(callback);
    }

    /**
     * Broadcast a chorus change to all other connected clients.
     * @param {string} chorusId - The ID of the new chorus.
     */
    async broadcastChorusChange(chorusId) {
        if (!this._connection || !this._connected) {
            debug('[ChorusSyncService] Not connected, cannot broadcast.');
            return;
        }

        try {
            await this._connection.invoke('SendChorusChanged', chorusId);
            debug('[ChorusSyncService] Broadcast chorus change:', chorusId);
        } catch (err) {
            console.error('[ChorusSyncService] Failed to broadcast:', err);
        }
    }

    /** @returns {boolean} Whether the connection is currently active. */
    isConnected() {
        return this._connected;
    }

    /** Disconnect from the hub. */
    async disconnect() {
        if (this._connection) {
            try {
                await this._connection.stop();
            } catch (err) {
                console.error('[ChorusSyncService] Error disconnecting:', err);
            }
            this._connection = null;
            this._connected = false;
        }
        this._chorusChangedCallbacks = [];
    }

    /** @private */
    _setStatus(status) {
        if (this._onStatusChange) {
            this._onStatusChange(status);
        }
    }
}

// Register on namespace
window.CHAP2 = window.CHAP2 || {};
window.CHAP2.ChorusSyncService = ChorusSyncService;
