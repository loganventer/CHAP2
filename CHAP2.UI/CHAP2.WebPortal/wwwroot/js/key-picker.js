/**
 * KeyPicker - Bottom sheet key selector for mobile.
 *
 * Single Responsibility: Only manages the key selection UI.
 * Shows a bottom sheet with musical keys in a grid layout.
 * Tapping a key fires the onSelect callback and closes.
 *
 * @interface IKeyPicker
 * @method open(currentKey: string): void
 * @method close(): void
 * @method destroy(): void
 */
class KeyPicker {
    /**
     * @param {Object} options
     * @param {function(string): void} options.onSelect - Called with the selected key enum name.
     */
    constructor(options = {}) {
        this._onSelect = options.onSelect || null;
        this._overlay = null;
        this._currentKey = null;
        this._isOpen = false;

        this._keys = [
            { label: 'C', value: 'C' },
            { label: 'C#', value: 'CSharp' },
            { label: 'D', value: 'D' },
            { label: 'Eb', value: 'EFlat' },
            { label: 'E', value: 'E' },
            { label: 'F', value: 'F' },
            { label: 'F#', value: 'FSharp' },
            { label: 'G', value: 'G' },
            { label: 'Ab', value: 'AFlat' },
            { label: 'A', value: 'A' },
            { label: 'Bb', value: 'BFlat' },
            { label: 'B', value: 'B' }
        ];

        this._buildDOM();
    }

    /**
     * Open the picker, highlighting the current key.
     * @param {string} currentKey - The current key enum name (e.g. 'CSharp').
     */
    open(currentKey) {
        this._currentKey = currentKey;
        this._highlightCurrent();
        this._overlay.classList.add('key-picker--open');
        this._overlay.setAttribute('aria-hidden', 'false');
        this._isOpen = true;

        // Trap focus inside picker
        const firstBtn = this._overlay.querySelector('.key-picker__key');
        if (firstBtn) firstBtn.focus();
    }

    /** Close the picker. */
    close() {
        this._overlay.classList.remove('key-picker--open');
        this._overlay.setAttribute('aria-hidden', 'true');
        this._isOpen = false;
    }

    /** Remove from DOM. */
    destroy() {
        if (this._overlay && this._overlay.parentNode) {
            this._overlay.parentNode.removeChild(this._overlay);
        }
        this._overlay = null;
    }

    /** @private */
    _buildDOM() {
        this._overlay = document.createElement('div');
        this._overlay.className = 'key-picker';
        this._overlay.setAttribute('aria-hidden', 'true');
        this._overlay.setAttribute('role', 'dialog');
        this._overlay.setAttribute('aria-label', 'Select musical key');

        this._overlay.innerHTML = `
            <div class="key-picker__backdrop"></div>
            <div class="key-picker__sheet">
                <div class="key-picker__header">
                    <span class="key-picker__title">Change Key</span>
                    <button class="key-picker__close" aria-label="Close">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
                <div class="key-picker__grid">
                    ${this._keys.map(k => `
                        <button class="key-picker__key" data-key="${k.value}" aria-label="${k.label}">
                            ${k.label}
                        </button>
                    `).join('')}
                </div>
            </div>
        `;

        // Bind events
        this._overlay.querySelector('.key-picker__backdrop').addEventListener('click', () => this.close());
        this._overlay.querySelector('.key-picker__close').addEventListener('click', () => this.close());

        this._overlay.querySelectorAll('.key-picker__key').forEach(btn => {
            btn.addEventListener('click', () => {
                const key = btn.dataset.key;
                if (this._onSelect && key !== this._currentKey) {
                    this._onSelect(key);
                }
                this.close();
            });
        });

        // Close on Escape
        this._overlay.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') this.close();
        });

        document.body.appendChild(this._overlay);
    }

    /** @private */
    _highlightCurrent() {
        this._overlay.querySelectorAll('.key-picker__key').forEach(btn => {
            btn.classList.toggle('key-picker__key--active', btn.dataset.key === this._currentKey);
        });
    }
}

// Register on namespace
window.CHAP2 = window.CHAP2 || {};
window.CHAP2.KeyPicker = KeyPicker;
