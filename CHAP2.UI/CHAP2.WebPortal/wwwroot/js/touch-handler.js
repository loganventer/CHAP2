/**
 * TouchHandler - Touch gesture recognition service.
 *
 * Single Responsibility: Only recognizes touch gestures (swipe directions).
 * Reusable across any page/component that needs swipe detection.
 *
 * @interface ITouchHandler
 * @method enable(): void
 * @method disable(): void
 * @method destroy(): void
 */
class TouchHandler {
    /**
     * @param {HTMLElement} element - The element to listen for touch events on.
     * @param {Object} options
     * @param {number} [options.swipeThreshold=50] - Minimum distance in px to register a swipe.
     * @param {function} [options.onSwipeLeft] - Called on left swipe.
     * @param {function} [options.onSwipeRight] - Called on right swipe.
     * @param {function} [options.onSwipeUp] - Called on upward swipe.
     * @param {function} [options.onSwipeDown] - Called on downward swipe.
     */
    constructor(element, options = {}) {
        this._element = element;
        this._swipeThreshold = options.swipeThreshold || 50;
        this._callbacks = {
            onSwipeLeft: options.onSwipeLeft || null,
            onSwipeRight: options.onSwipeRight || null,
            onSwipeUp: options.onSwipeUp || null,
            onSwipeDown: options.onSwipeDown || null
        };

        this._startX = 0;
        this._startY = 0;
        this._enabled = false;

        // Bind handlers for proper removal
        this._onTouchStart = this._handleTouchStart.bind(this);
        this._onTouchEnd = this._handleTouchEnd.bind(this);
    }

    /** Start listening for touch events. */
    enable() {
        if (this._enabled) return;
        this._element.addEventListener('touchstart', this._onTouchStart, { passive: true });
        this._element.addEventListener('touchend', this._onTouchEnd, { passive: true });
        this._enabled = true;
    }

    /** Stop listening for touch events. */
    disable() {
        if (!this._enabled) return;
        this._element.removeEventListener('touchstart', this._onTouchStart);
        this._element.removeEventListener('touchend', this._onTouchEnd);
        this._enabled = false;
    }

    /** Clean up all listeners. */
    destroy() {
        this.disable();
        this._element = null;
        this._callbacks = {};
    }

    /** @private */
    _handleTouchStart(e) {
        const touch = e.changedTouches[0];
        this._startX = touch.screenX;
        this._startY = touch.screenY;
    }

    /** @private */
    _handleTouchEnd(e) {
        const touch = e.changedTouches[0];
        const deltaX = touch.screenX - this._startX;
        const deltaY = touch.screenY - this._startY;

        const absX = Math.abs(deltaX);
        const absY = Math.abs(deltaY);

        // Only register if above threshold and the dominant axis is clear
        if (Math.max(absX, absY) < this._swipeThreshold) return;

        if (absX > absY) {
            // Horizontal swipe
            if (deltaX < 0 && this._callbacks.onSwipeLeft) {
                this._callbacks.onSwipeLeft();
            } else if (deltaX > 0 && this._callbacks.onSwipeRight) {
                this._callbacks.onSwipeRight();
            }
        } else {
            // Vertical swipe
            if (deltaY < 0 && this._callbacks.onSwipeUp) {
                this._callbacks.onSwipeUp();
            } else if (deltaY > 0 && this._callbacks.onSwipeDown) {
                this._callbacks.onSwipeDown();
            }
        }
    }
}

// Register on namespace
window.CHAP2 = window.CHAP2 || {};
window.CHAP2.TouchHandler = TouchHandler;
