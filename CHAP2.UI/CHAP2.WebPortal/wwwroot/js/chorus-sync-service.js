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
        this._keyChangedCallbacks = [];
        this._setlistChangedCallbacks = [];
        this._connected = false;
        // Cache the most recent setlist broadcast so a listener that
        // registers AFTER the hub already pushed the list (which can
        // happen inside MobileChorusView.init on /sync) still gets it.
        this._lastSetlist = null;
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

        // Register server-to-client methods
        this._connection.on('ReceiveChorusChanged', (chorusId) => {
            debug('[ChorusSyncService] Received chorus change:', chorusId);
            for (const callback of this._chorusChangedCallbacks) {
                callback(chorusId);
            }
        });

        this._connection.on('ReceiveKeyChanged', (chorusId, newKey) => {
            debug('[ChorusSyncService] Received key change:', chorusId, newKey);
            for (const callback of this._keyChangedCallbacks) {
                callback(chorusId, newKey);
            }
        });

        this._connection.on('ReceiveSetlistUpdate', (setlistJson) => {
            let list = [];
            try { list = JSON.parse(setlistJson || '[]') || []; }
            catch (err) { debug('[ChorusSyncService] Bad setlist JSON:', err); }
            this._lastSetlist = list;
            debug('[ChorusSyncService] Received setlist update, length:', list.length);
            for (const callback of this._setlistChangedCallbacks) {
                callback(list);
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

    /**
     * Register a callback for when a remote client changes the key.
     * @param {function(string, string): void} callback - Receives (chorusId, newKey).
     */
    onKeyChanged(callback) {
        this._keyChangedCallbacks.push(callback);
    }

    /**
     * Broadcast a key change to all other connected clients.
     * @param {string} chorusId
     * @param {string} newKey
     */
    async broadcastKeyChange(chorusId, newKey) {
        if (!this._connection || !this._connected) return;
        try {
            await this._connection.invoke('SendKeyChanged', chorusId, newKey);
            debug('[ChorusSyncService] Broadcast key change:', chorusId, newKey);
        } catch (err) {
            console.error('[ChorusSyncService] Failed to broadcast key change:', err);
        }
    }

    /**
     * Register a callback for when a remote client broadcasts its setlist.
     * If a setlist has already been received before this listener was
     * registered, the cached value is replayed synchronously so late
     * subscribers (e.g. MobileChorusView.init running after OnConnected
     * already pushed the list) still get the current setlist.
     * @param {function(Array): void} callback - Receives the setlist array.
     */
    onSetlistChanged(callback) {
        this._setlistChangedCallbacks.push(callback);
        if (Array.isArray(this._lastSetlist)) {
            try { callback(this._lastSetlist); }
            catch (err) { console.error('[ChorusSyncService] Setlist replay failed:', err); }
        }
    }

    /**
     * Broadcast the current setlist so mobile sync clients can drive
     * their prev/next navigation from it.
     * @param {Array} setlist - Array of chorus summaries (id + name at minimum).
     */
    async broadcastSetlist(setlist) {
        if (!this._connection || !this._connected) return;
        try {
            const json = JSON.stringify(setlist || []);
            await this._connection.invoke('SendSetlistUpdate', json);
            debug('[ChorusSyncService] Broadcast setlist, items:', (setlist || []).length);
        } catch (err) {
            console.error('[ChorusSyncService] Failed to broadcast setlist:', err);
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
        this._keyChangedCallbacks = [];
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
