/**
 * BrowserExitGuard - one responsibility: arm the browser's native
 * beforeunload confirmation so a user who tries to close the tab/window
 * while the chorus overlay is open is prompted before they leave.
 *
 * Note: modern browsers ignore the custom message text and show their
 * own generic prompt. We still set it in e.returnValue as a hint to any
 * environment that honours it.
 */
class BrowserExitGuard {
    constructor() {
        this._onBeforeUnload = this._onBeforeUnload.bind(this);
    }

    init() {
        window.addEventListener('beforeunload', this._onBeforeUnload);
    }

    _onBeforeUnload(e) {
        // Only nag when a chorus is being displayed; closing from plain
        // search should not be interrupted.
        if (!document.body.classList.contains('chorus-overlay-open')) return;

        const msg = 'Are you sure you want to exit? To go back to search, press Esc.';
        e.preventDefault();
        e.returnValue = msg;
        return msg;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    new BrowserExitGuard().init();
});
