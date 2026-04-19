/**
 * StarlightField - one responsibility: populate #starlightField with
 * N individual star spans, each randomized in position, size, phase
 * and duration so they blink independently like real stars.
 *
 * The field is CSS-hidden unless body carries the .theme-starlight
 * class, so it costs nothing on other themes. No teardown needed on
 * theme switch -- just toggles visibility.
 */
class StarlightField {
    constructor(containerId = 'starlightField', count = 50) {
        this._containerId = containerId;
        this._count = count;
    }

    init() {
        const field = document.getElementById(this._containerId);
        if (!field || field.dataset.init === '1') return;
        field.dataset.init = '1';

        const frag = document.createDocumentFragment();
        for (let i = 0; i < this._count; i++) {
            frag.appendChild(this._makeStar());
        }
        field.appendChild(frag);
    }

    _makeStar() {
        const s = document.createElement('span');
        s.className = 'star';
        s.style.top = (Math.random() * 100).toFixed(2) + '%';
        s.style.left = (Math.random() * 100).toFixed(2) + '%';
        // Negative delays pin each star's starting phase randomly in the cycle.
        s.style.animationDelay = (-Math.random() * 6).toFixed(2) + 's';
        // Vary durations so they aren't locked in step.
        s.style.animationDuration = (1.6 + Math.random() * 3.2).toFixed(2) + 's';
        const size = (0.8 + Math.random() * 2.2).toFixed(2);
        s.style.width = size + 'px';
        s.style.height = size + 'px';
        // A few brighter "showy" stars get a stronger glow.
        if (Math.random() < 0.25) {
            s.classList.add('star--bright');
        }
        return s;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    new StarlightField().init();
});
