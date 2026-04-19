/**
 * StarlightField - one responsibility: populate #starlightField with
 * N individual star spans, each randomized in position, size, colour,
 * ray angle, phase and duration so they blink truly independently.
 *
 * Each star exposes its random values as CSS custom properties
 * (--star-color, --ray-angle) which the .star rules in themes.css
 * consume. The field is CSS-hidden unless body carries .theme-starlight.
 */
class StarlightField {
    constructor(containerId = 'starlightField', count = 38) {
        this._containerId = containerId;
        this._count = count;
        // A palette of subtle star temperatures so the field doesn't read
        // as a wall of identical white dots.
        this._palette = [
            '#ffffff',   // pure white
            '#f8f5ff',   // cool
            '#e8f0ff',   // pale blue
            '#fff4d6',   // warm ivory
            '#ffe7b0',   // amber
            '#e0d4ff',   // lilac
            '#d6eaff'    // soft blue
        ];
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

        // Each star blinks on its OWN clock: negative delay pins the
        // phase randomly; durations are slower (3-10s) so the field
        // feels calm rather than frantic.
        s.style.animationDelay = (-Math.random() * 10).toFixed(2) + 's';
        s.style.animationDuration = (3 + Math.random() * 7).toFixed(2) + 's';

        // Random size -- tiny dust to prominent beacons.
        const size = (0.8 + Math.random() * 2.4).toFixed(2);
        s.style.width = size + 'px';
        s.style.height = size + 'px';

        // Random colour temperature + random ray-cross angle.
        const colour = this._palette[Math.floor(Math.random() * this._palette.length)];
        s.style.setProperty('--star-color', colour);
        s.style.setProperty('--ray-angle', Math.floor(Math.random() * 180) + 'deg');

        // ~12% of stars get a stronger glow + visible ray cross at peak
        // (lower than before to avoid a busy sky).
        if (Math.random() < 0.12) {
            s.classList.add('star--bright');
        }
        return s;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    new StarlightField().init();
});
