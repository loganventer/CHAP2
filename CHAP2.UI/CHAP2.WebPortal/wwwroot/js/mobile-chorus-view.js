/**
 * MobileChorusView - Mobile chorus companion experience.
 *
 * Single Responsibility: Orchestrates the mobile chorus card, lyrics panel, and navigation.
 * Composition over Inheritance: Composes DeviceDetector, LyricsPanel, TouchHandler, ChorusSyncService.
 *   Does NOT extend ChorusDisplay.
 * Dependency Inversion: Receives dependencies via constructor injection.
 *
 * @param {Object} deps
 * @param {IDeviceDetector} deps.deviceDetector
 * @param {IChorusSyncService} deps.syncService
 * @param {Object} deps.chorusData - { id, name, key, text }
 */
class MobileChorusView {
    constructor({ deviceDetector, syncService, chorusData }) {
        this._deviceDetector = deviceDetector;
        this._syncService = syncService;
        this._chorusData = chorusData;
        this._lyricsPanel = null;
        this._touchHandler = null;
        this._keyPicker = null;
        this._choruses = [];
        this._currentIndex = -1;
        this._visible = false;

        // DOM element references (set during init)
        this._titleEl = null;
        this._keyEl = null;
        this._positionEl = null;
        this._prevBtn = null;
        this._nextBtn = null;
        this._syncIndicator = null;
    }

    /** Initialize the mobile view: bind DOM, create composed services, register listeners. */
    init() {
        // Grab DOM elements
        this._titleEl = document.getElementById('mobileChorusTitle');
        this._keyEl = document.getElementById('mobileChorusKey');
        this._positionEl = document.getElementById('mobilePosition');
        this._prevBtn = document.getElementById('mobilePrevBtn');
        this._nextBtn = document.getElementById('mobileNextBtn');
        this._syncIndicator = document.getElementById('syncIndicator');

        const panelEl = document.getElementById('lyricsPanel');
        const toggleEl = document.getElementById('lyricsPanelToggle');

        if (!panelEl || !toggleEl) {
            debug('[MobileChorusView] Required DOM elements not found. Aborting init.');
            return;
        }

        // Compose LyricsPanel
        this._lyricsPanel = new window.CHAP2.LyricsPanel(panelEl, toggleEl);

        // Compose TouchHandler for swipe navigation
        const cardEl = document.getElementById('mobileChorusCard');
        if (cardEl) {
            this._touchHandler = new window.CHAP2.TouchHandler(cardEl, {
                swipeThreshold: 60,
                onSwipeLeft: () => this._navigateChorus(1),
                onSwipeRight: () => this._navigateChorus(-1)
            });
            this._touchHandler.enable();
        }

        // Load chorus list from sessionStorage
        this._loadChorusList();

        // Bind navigation buttons
        if (this._prevBtn) {
            this._prevBtn.addEventListener('click', () => this._navigateChorus(-1));
        }
        if (this._nextBtn) {
            this._nextBtn.addEventListener('click', () => this._navigateChorus(1));
        }

        // Compose KeyPicker for key changes
        if (window.CHAP2.KeyPicker) {
            this._keyPicker = new window.CHAP2.KeyPicker({
                onSelect: (newKey) => this._handleKeyChange(newKey)
            });
        }

        // Make key badge tappable to open picker
        if (this._keyEl) {
            this._keyEl.addEventListener('click', () => {
                if (this._keyPicker && this._chorusData) {
                    this._keyPicker.open(this._chorusData.key);
                }
            });
        }

        // Listen for real-time chorus changes from other clients
        if (this._syncService) {
            this._syncService.onChorusChanged((chorusId) => {
                this._handleRemoteChorusChange(chorusId);
            });

            this._syncService.onKeyChanged((chorusId, newKey) => {
                if (this._chorusData && this._chorusData.id === chorusId) {
                    this._chorusData.key = newKey;
                    this._updateDisplay();
                }
            });
        }

        // Update display with current data
        this._updateDisplay();
        this._updateNavigationState();

        // Default lyrics expanded on load
        if (this._lyricsPanel && this._chorusData) {
            this._lyricsPanel.expand();
        }

        debug('[MobileChorusView] Initialized.');
    }

    /** Show the mobile view. */
    show() {
        this._visible = true;
        const card = document.getElementById('mobileChorusCard');
        if (card) card.style.display = '';
    }

    /** Hide the mobile view. */
    hide() {
        this._visible = false;
        const card = document.getElementById('mobileChorusCard');
        if (card) card.style.display = 'none';
    }

    /**
     * Update the displayed chorus data.
     * @param {Object} chorusData - { id, name, key, text }
     */
    updateChorus(chorusData) {
        this._chorusData = chorusData;

        // If lyrics panel is expanded, collapse then update
        if (this._lyricsPanel && this._lyricsPanel.isExpanded()) {
            this._lyricsPanel.collapse();
        }

        this._updateDisplay();
        this._updateNavigationState();
    }

    /**
     * Update the sync connection status indicator.
     * @param {string} status - 'connected' | 'disconnected' | 'reconnecting'
     */
    updateSyncStatus(status) {
        if (!this._syncIndicator) return;

        this._syncIndicator.className = 'mobile-chorus-card__sync-indicator';
        if (status === 'connected') {
            this._syncIndicator.classList.add('mobile-chorus-card__sync-indicator--connected');
            this._syncIndicator.title = 'Connected - real-time sync active';
        } else if (status === 'reconnecting') {
            this._syncIndicator.classList.add('mobile-chorus-card__sync-indicator--reconnecting');
            this._syncIndicator.title = 'Reconnecting...';
        } else {
            this._syncIndicator.title = 'Disconnected';
        }
    }

    /** Clean up all composed services and listeners. */
    destroy() {
        if (this._lyricsPanel) this._lyricsPanel.destroy();
        if (this._touchHandler) this._touchHandler.destroy();
        if (this._keyPicker) this._keyPicker.destroy();
        this._lyricsPanel = null;
        this._touchHandler = null;
        this._keyPicker = null;
    }

    // ---- Private Methods ----

    /** @private Handle key change from the picker. Persists and broadcasts. */
    async _handleKeyChange(newKey) {
        if (!this._chorusData) return;

        const chorusId = this._chorusData.id;

        try {
            const response = await fetch(`/Home/UpdateChorusKey/${chorusId}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ key: newKey })
            });

            if (!response.ok) {
                console.error('[MobileChorusView] Failed to update key:', response.status);
                return;
            }

            // Update local state
            this._chorusData.key = newKey;
            this._updateDisplay();

            // Broadcast to other clients
            if (this._syncService) {
                this._syncService.broadcastKeyChange(chorusId, newKey);
            }
        } catch (err) {
            console.error('[MobileChorusView] Error updating key:', err);
        }
    }

    /** @private Update the title, key, and lyrics content in the DOM. */
    _updateDisplay() {
        if (!this._chorusData) return;

        if (this._titleEl) {
            this._titleEl.textContent = this._chorusData.name;
        }

        // Update key text (inside the key circle, there's a span for the text)
        const keyTextEl = document.getElementById('mobileChorusKeyText');
        if (keyTextEl) {
            keyTextEl.textContent = this._getKeyDisplay(this._chorusData.key);
        } else if (this._keyEl) {
            this._keyEl.textContent = this._getKeyDisplay(this._chorusData.key);
        }

        // Update lyrics panel content
        if (this._lyricsPanel && this._chorusData.text) {
            const lyricsHtml = this._formatLyricsHtml(this._chorusData.text);
            this._lyricsPanel.setContent(lyricsHtml);
        }
    }

    /** @private Load the chorus navigation list from sessionStorage. */
    _loadChorusList() {
        try {
            const stored = sessionStorage.getItem('chorusList');
            if (stored) {
                this._choruses = JSON.parse(stored);
            }
            const currentId = this._chorusData?.id;
            if (currentId && this._choruses.length > 0) {
                this._currentIndex = this._choruses.findIndex(c => c && c.id === currentId);
            }
        } catch (err) {
            debug('[MobileChorusView] Error loading chorus list:', err);
            this._choruses = [];
        }
    }

    /** @private Navigate to the previous or next chorus. */
    _navigateChorus(direction) {
        if (!this._choruses || this._choruses.length <= 1) {
            debug('[MobileChorusView] No choruses to navigate.');
            return;
        }

        let newIndex = this._currentIndex + direction;
        if (newIndex < 0) newIndex = this._choruses.length - 1;
        if (newIndex >= this._choruses.length) newIndex = 0;

        const chorus = this._choruses[newIndex];
        if (!chorus) return;

        this._currentIndex = newIndex;
        sessionStorage.setItem('currentChorusId', chorus.id);

        // Broadcast to other clients (desktop will navigate)
        if (this._syncService) {
            this._syncService.broadcastChorusChange(chorus.id);
        }

        // Fetch and display the new chorus locally
        this._fetchAndDisplayChorus(chorus.id);
    }

    /** @private Handle a chorus change event from another client. */
    async _handleRemoteChorusChange(chorusId) {
        debug('[MobileChorusView] Remote chorus change:', chorusId);

        // Update current index if we have the chorus in our list
        const idx = this._choruses.findIndex(c => c && c.id === chorusId);
        if (idx !== -1) {
            this._currentIndex = idx;
        }

        await this._fetchAndDisplayChorus(chorusId);
    }

    /** @private Fetch chorus data from the server and update the display. */
    async _fetchAndDisplayChorus(chorusId) {
        try {
            const response = await fetch(`/Home/ChorusData/${chorusId}`);
            if (!response.ok) {
                console.error('[MobileChorusView] Failed to fetch chorus:', response.status);
                return;
            }

            const data = await response.json();
            this.updateChorus({
                id: data.id,
                name: data.name,
                key: data.key,
                text: data.chorusText
            });
        } catch (err) {
            console.error('[MobileChorusView] Error fetching chorus:', err);
        }
    }

    /** @private Update prev/next button states and position indicator. */
    _updateNavigationState() {
        const hasMultiple = this._choruses.length > 1;

        if (this._prevBtn) this._prevBtn.disabled = !hasMultiple;
        if (this._nextBtn) this._nextBtn.disabled = !hasMultiple;

        if (this._positionEl) {
            if (hasMultiple && this._currentIndex >= 0) {
                this._positionEl.textContent = `${this._currentIndex + 1} / ${this._choruses.length}`;
            } else {
                this._positionEl.textContent = '';
            }
        }
    }

    /**
     * @private Convert raw chorus text to HTML lines.
     * @param {string} text
     * @returns {string}
     */
    _formatLyricsHtml(text) {
        if (!text) return '';

        // Strip [PAGE] markers for mobile (show all text in one scrollable view)
        const cleanText = text.replace(/\[PAGE\]/g, '');

        return cleanText.split('\n').map(line => {
            if (line.trim() === '') {
                return '<div class="lyrics-panel__line lyrics-panel__line--empty"></div>';
            }
            const escaped = window.CHAP2?.escapeHtml ? window.CHAP2.escapeHtml(line) : line;
            return `<div class="lyrics-panel__line">${escaped}</div>`;
        }).join('');
    }

    /**
     * @private Get a human-readable key display string.
     * @param {string|number} key
     * @returns {string}
     */
    _getKeyDisplay(key) {
        // If it's already a display string (e.g., from the JSON endpoint)
        if (typeof key === 'string' && key !== '') {
            return key.replace('Sharp', '#').replace('Flat', 'b');
        }

        // Numeric enum fallback
        const keyMap = {
            0: '-', 1: 'C', 2: 'C#', 3: 'D', 4: 'D#', 5: 'E',
            6: 'F', 7: 'F#', 8: 'G', 9: 'G#', 10: 'A',
            11: 'A#', 12: 'B', 13: 'Cb', 14: 'Db', 15: 'Eb',
            16: 'Fb', 17: 'Gb', 18: 'Ab', 19: 'Bb'
        };
        return keyMap[key] || String(key);
    }
}

// Register on namespace
window.CHAP2 = window.CHAP2 || {};
window.CHAP2.MobileChorusView = MobileChorusView;
