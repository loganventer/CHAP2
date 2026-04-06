/**
 * DeviceDetector - Viewport and device detection service.
 *
 * Single Responsibility: Only detects device/viewport characteristics.
 * Dependency Inversion: Consumers depend on the IDeviceDetector contract (documented via JSDoc).
 *
 * @interface IDeviceDetector
 * @method isMobile(): boolean
 * @method isTouch(): boolean
 * @method onViewportChange(callback: (isMobile: boolean) => void): void
 * @method getViewportWidth(): number
 * @method destroy(): void
 */
class DeviceDetector {
    constructor(breakpoint = 768) {
        this._breakpoint = breakpoint;
        this._mediaQuery = window.matchMedia(`(max-width: ${breakpoint}px)`);
        this._listeners = [];
    }

    /** @returns {boolean} True if viewport is at or below the mobile breakpoint. */
    isMobile() {
        return this._mediaQuery.matches;
    }

    /** @returns {boolean} True if the device supports touch input. */
    isTouch() {
        return 'ontouchstart' in window || navigator.maxTouchPoints > 0;
    }

    /** @returns {number} Current viewport width in pixels. */
    getViewportWidth() {
        return window.innerWidth;
    }

    /**
     * Register a callback that fires when the viewport crosses the mobile breakpoint.
     * @param {function(boolean): void} callback - Receives true when entering mobile, false when leaving.
     */
    onViewportChange(callback) {
        const handler = (e) => callback(e.matches);
        this._mediaQuery.addEventListener('change', handler);
        this._listeners.push({ handler });
    }

    /** Clean up all event listeners. */
    destroy() {
        for (const { handler } of this._listeners) {
            this._mediaQuery.removeEventListener('change', handler);
        }
        this._listeners = [];
    }
}

// Register on namespace
window.CHAP2 = window.CHAP2 || {};
window.CHAP2.DeviceDetector = DeviceDetector;
