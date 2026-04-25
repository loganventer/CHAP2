/**
 * ApiStatusIndicator - one responsibility: present a frosted-glass
 * overlay while a long-running fetch is in flight, so the user knows
 * "the app is starting up" instead of staring at a blank page during
 * a Render free-tier cold start.
 *
 * Decoupled from any specific caller via document events:
 *   chap2:api-wait-start   detail = { delayMs?, message? }
 *   chap2:api-wait-end     no detail
 *   chap2:api-fail         detail = { message }
 *
 * Callers (search-v2.js, browse-all, etc.) dispatch the events; the
 * indicator subscribes. No two-way coupling, no shared global -- the
 * indicator can be replaced with a different implementation that
 * listens for the same events without touching the callers.
 */
class ApiStatusIndicator {
    constructor() {
        this._el = null;
        this._timer = null;
    }

    init() {
        if (this._el) return;
        this._el = this._buildOverlay();
        document.body.appendChild(this._el);

        document.addEventListener('chap2:api-wait-start', (e) => {
            const delayMs = (e.detail && e.detail.delayMs) || 1500;
            const message = (e.detail && e.detail.message)
                || 'Waking the server up — this can take up to a minute on the free tier...';
            this._beginWaiting(delayMs, message);
        });
        document.addEventListener('chap2:api-wait-end', () => this._cancel());
        document.addEventListener('chap2:api-fail', (e) => {
            const message = (e.detail && e.detail.message)
                || "Couldn't reach the server. It may still be waking up — please try again in a moment.";
            this._fail(message);
        });
    }

    _buildOverlay() {
        const overlay = document.createElement('div');
        overlay.className = 'api-status-overlay';
        overlay.id = 'apiStatusOverlay';
        overlay.setAttribute('role', 'status');
        overlay.setAttribute('aria-live', 'polite');
        overlay.hidden = true;
        overlay.innerHTML = `
            <div class="api-status-overlay__panel">
                <div class="api-status-overlay__rings" aria-hidden="true">
                    <span class="api-status-overlay__ring"></span>
                    <span class="api-status-overlay__ring"></span>
                    <span class="api-status-overlay__ring"></span>
                    <i class="fas fa-cloud-arrow-up api-status-overlay__icon"></i>
                </div>
                <h2 class="api-status-overlay__title">Connecting to the server</h2>
                <p class="api-status-overlay__text"></p>
                <div class="api-status-overlay__dots" aria-hidden="true">
                    <span></span><span></span><span></span>
                </div>
            </div>
        `;
        return overlay;
    }

    _beginWaiting(delayMs, message) {
        this._cancel();
        this._timer = setTimeout(() => this._show(message), delayMs);
    }

    _cancel() {
        if (this._timer) { clearTimeout(this._timer); this._timer = null; }
        if (this._el) this._el.hidden = true;
    }

    _fail(message) {
        if (this._timer) { clearTimeout(this._timer); this._timer = null; }
        this._show(message);
        // Auto-hide so a stale failure doesn't cling to the UI.
        setTimeout(() => this._cancel(), 6000);
    }

    _show(message) {
        if (!this._el) return;
        const text = this._el.querySelector('.api-status-overlay__text');
        if (text) text.textContent = message;
        this._el.hidden = false;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    new ApiStatusIndicator().init();
});
