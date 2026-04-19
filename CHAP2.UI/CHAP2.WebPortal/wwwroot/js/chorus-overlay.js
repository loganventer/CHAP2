/**
 * ChorusOverlay - fullscreen iframe overlay for the chorus display page.
 *
 * Single Responsibility: show/hide a fullscreen iframe pointing at
 * /Home/ChorusDisplay/{id} so the user never leaves the home page.
 *
 * The inner ChorusDisplay page retains its own prev/next, SignalR
 * broadcasting, font sizing, fullscreen and print behaviour. Its Close
 * button posts `chorus-overlay:close` to window.parent, which this class
 * listens for.
 */
class ChorusOverlay {
    constructor(rootEl, iframeEl, closeBtnEl) {
        this._root = rootEl;
        this._frame = iframeEl;
        this._closeBtn = closeBtnEl;
        this._onKeydown = this._onKeydown.bind(this);
        this._onMessage = this._onMessage.bind(this);
    }

    /** Wire events. Called once at startup. */
    init() {
        if (!this._root || !this._frame) return;

        if (this._closeBtn) {
            this._closeBtn.addEventListener('click', () => this.close());
        }

        window.addEventListener('message', this._onMessage);
    }

    /**
     * Open the overlay pointing at the given chorus id. Focus is handed
     * to the iframe once it finishes loading so keyboard input (arrows,
     * Esc, font-size shortcuts, Ctrl+P) flows into the chorus display.
     * @param {string} chorusId
     */
    openChorus(chorusId) {
        if (!chorusId || !this._root || !this._frame) return;
        this._frame.addEventListener('load', this._focusFrame, { once: true });
        this._frame.src = `/Home/ChorusDisplay/${chorusId}`;
        this._root.hidden = false;
        document.body.classList.add('chorus-overlay-open');
        document.addEventListener('keydown', this._onKeydown);
    }

    /** @private Give the iframe's window keyboard focus. */
    _focusFrame = () => {
        try {
            this._frame.focus();
            if (this._frame.contentWindow) {
                this._frame.contentWindow.focus();
            }
        } catch (e) {
            // Cross-origin or transient; ignore.
        }
    }

    /** Close the overlay and tear down the iframe so SignalR disconnects. */
    close() {
        if (!this._root || !this._frame) return;
        this._root.hidden = true;
        this._frame.src = 'about:blank';
        document.body.classList.remove('chorus-overlay-open');
        document.removeEventListener('keydown', this._onKeydown);
    }

    /** @private Handle Esc to close the overlay from the parent page. */
    _onKeydown(e) {
        if (e.key === 'Escape') {
            this.close();
        }
    }

    /** @private Accept a close message posted from the iframe's Close button. */
    _onMessage(e) {
        if (e && e.data && e.data.type === 'chorus-overlay:close') {
            this.close();
        }
    }
}

document.addEventListener('DOMContentLoaded', () => {
    const root = document.getElementById('chorusOverlay');
    const frame = document.getElementById('chorusOverlayFrame');
    const closeBtn = document.getElementById('chorusOverlayClose');
    if (!root || !frame) return;
    const overlay = new ChorusOverlay(root, frame, closeBtn);
    overlay.init();
    window.chorusOverlay = overlay;
});
