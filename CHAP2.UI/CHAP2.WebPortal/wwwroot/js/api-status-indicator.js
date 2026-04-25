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
 *   chap2:api-probe        detail = { message? }
 *       -- enter "probing" mode: keep overlay visible and poll the
 *          health endpoint until the API answers, then dispatch
 *          'chap2:api-recovered' so the caller can re-run its action.
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
        this._probeTimer = null;
        this._probeUrl = '/Home/TestConnectivity';
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
        // When a request fails because the API isn't reachable, switch
        // into probing mode: keep the overlay visible and poll the
        // health endpoint until we get a real answer back. Once we do,
        // the overlay closes and chap2:api-recovered fires for callers
        // (e.g. search-v2) to re-run their last query.
        document.addEventListener('chap2:api-probe', (e) => {
            const message = (e.detail && e.detail.message)
                || 'Server still waking up — checking connection...';
            this._beginProbing(message);
        });
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
        if (this._timer) { clearTimeout(this._timer); this._timer = null; }
        this._cancelProbing();
        // If the overlay is already up (e.g. we just handed off from
        // probe-success to an auto-retry), refresh the message in place
        // -- never let it flash off and back on. Only when starting cold
        // do we delay so quick responses don't flash the overlay.
        if (this._el && !this._el.hidden) {
            this._show(message);
            return;
        }
        this._timer = setTimeout(() => this._show(message), delayMs);
    }

    /**
     * Enter probing mode: overlay stays visible while we poll the
     * health endpoint. Once the API answers OK we hide the overlay
     * and dispatch chap2:api-recovered so callers can re-run their
     * last action (e.g. search). Lazy reconnect -- no traffic until
     * a user action triggers the probe.
     */
    _beginProbing(message) {
        // Kill any pending wait-start timer so its delayed message
        // doesn't overwrite the probing message a second after we set it.
        if (this._timer) { clearTimeout(this._timer); this._timer = null; }
        this._cancelProbing();
        this._show(message);
        // Probe immediately, then on a tight interval so the moment the
        // API answers we see it. The _probing flag suppresses
        // overlapping requests while a single probe is still in flight
        // (probe HttpClient timeout is 45s for cold starts), so a 2s
        // tick is fine -- it just controls the latency between the API
        // becoming reachable and our next attempt to detect that.
        this._probing = false;
        this._probeOnce();
        this._probeTimer = setInterval(() => this._probeOnce(), 2000);
    }

    async _probeOnce() {
        if (this._probing) return;
        this._probing = true;
        try {
            const resp = await fetch(this._probeUrl, {
                method: 'GET',
                cache: 'no-store',
                redirect: 'manual',                  // catch auth redirects
                headers: { 'Accept': 'application/json' }
            });
            // A 302 / login redirect / any non-2xx counts as "not up yet".
            if (!resp.ok) return;
            // Defensive: if the response isn't actually JSON (e.g. an
            // HTML login page handed back with 200), treat as failure.
            const ct = resp.headers.get('content-type') || '';
            if (!ct.includes('application/json')) return;
            const data = await resp.json().catch(() => null);
            if (data && data.connected === true) {
                // API is reachable -- stop probing but KEEP the overlay
                // up. The caller will re-run its action; the overlay
                // only closes when that action actually succeeds
                // (via chap2:api-wait-end).
                this._cancelProbing();
                this._show('Server is back — fetching your results...');
                document.dispatchEvent(new Event('chap2:api-recovered'));
            }
        } catch {
            // Probe failed -- stay in probing mode, will try again on tick.
        } finally {
            this._probing = false;
        }
    }

    _cancelProbing() {
        if (this._probeTimer) { clearInterval(this._probeTimer); this._probeTimer = null; }
    }

    _cancel() {
        if (this._timer) { clearTimeout(this._timer); this._timer = null; }
        this._cancelProbing();
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
