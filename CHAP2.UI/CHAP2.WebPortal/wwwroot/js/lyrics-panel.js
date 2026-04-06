/**
 * LyricsPanel - Expandable/collapsible accordion for lyrics display.
 *
 * Single Responsibility: Only manages expand/collapse UI behavior.
 * Sets ARIA attributes for accessibility.
 * Respects prefers-reduced-motion.
 *
 * @interface ILyricsPanel
 * @method expand(): void
 * @method collapse(): void
 * @method toggle(): void
 * @method isExpanded(): boolean
 * @method setContent(html: string): void
 * @method destroy(): void
 */
class LyricsPanel {
    /**
     * @param {HTMLElement} panelElement - The .lyrics-panel container.
     * @param {HTMLElement} toggleElement - The button that toggles expand/collapse.
     * @param {Object} [options]
     * @param {function} [options.onExpand] - Called after expanding.
     * @param {function} [options.onCollapse] - Called after collapsing.
     */
    constructor(panelElement, toggleElement, options = {}) {
        this._panel = panelElement;
        this._toggle = toggleElement;
        this._content = panelElement.querySelector('.lyrics-panel__content');
        this._expanded = false;
        this._onExpand = options.onExpand || null;
        this._onCollapse = options.onCollapse || null;

        // Bind toggle click
        this._handleToggle = this.toggle.bind(this);
        this._toggle.addEventListener('click', this._handleToggle);
    }

    /** Expand the lyrics panel with animation. */
    expand() {
        if (this._expanded) return;
        this._expanded = true;

        this._panel.classList.add('lyrics-panel--expanded');
        this._toggle.classList.add('lyrics-panel__toggle--expanded');
        this._toggle.setAttribute('aria-expanded', 'true');
        this._panel.setAttribute('aria-hidden', 'false');

        if (this._onExpand) this._onExpand();
    }

    /** Collapse the lyrics panel with animation. */
    collapse() {
        if (!this._expanded) return;
        this._expanded = false;

        this._panel.classList.remove('lyrics-panel--expanded');
        this._toggle.classList.remove('lyrics-panel__toggle--expanded');
        this._toggle.setAttribute('aria-expanded', 'false');
        this._panel.setAttribute('aria-hidden', 'true');

        // Scroll back to top when collapsed so it's ready for next expand
        this._panel.scrollTop = 0;

        if (this._onCollapse) this._onCollapse();
    }

    /** Toggle between expanded and collapsed states. */
    toggle() {
        if (this._expanded) {
            this.collapse();
        } else {
            this.expand();
        }
    }

    /** @returns {boolean} Whether the panel is currently expanded. */
    isExpanded() {
        return this._expanded;
    }

    /**
     * Replace the lyrics content.
     * @param {string} html - The HTML content to display.
     */
    setContent(html) {
        if (this._content) {
            this._content.innerHTML = html;
        }
    }

    /** Clean up event listeners. */
    destroy() {
        if (this._toggle) {
            this._toggle.removeEventListener('click', this._handleToggle);
        }
        this._panel = null;
        this._toggle = null;
        this._content = null;
    }
}

// Register on namespace
window.CHAP2 = window.CHAP2 || {};
window.CHAP2.LyricsPanel = LyricsPanel;
