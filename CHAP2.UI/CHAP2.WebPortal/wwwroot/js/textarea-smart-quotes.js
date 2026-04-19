/**
 * TextareaSmartQuotes - one responsibility: strip macOS/iOS smart-quote
 * substitution from a textarea so what the user types as ' or " is what
 * actually gets stored. Also converts smart hyphens/ellipses to ASCII.
 *
 * Usage:
 *   TextareaSmartQuotes.attach(document.getElementById('chorusTextArea'));
 */
(function () {
    const REPLACEMENTS = [
        [/[\u2018\u2019\u201A\u201B\u2032]/g, "'"],  // ‘ ’ ‚ ‛ ′ → '
        [/[\u201C\u201D\u201E\u201F\u2033]/g, '"'],  // “ ” „ ‟ ″ → "
        [/[\u2013\u2014]/g, '-'],                    // – — → -
        [/\u2026/g, '...']                           // … → ...
    ];

    function normalise(text) {
        if (!text) return text;
        let out = text;
        for (const [re, r] of REPLACEMENTS) out = out.replace(re, r);
        return out;
    }

    function attach(textarea) {
        if (!textarea || textarea.dataset.smartQuotesBound === '1') return;
        textarea.dataset.smartQuotesBound = '1';

        // Clean any existing smart quotes rendered into the textarea on load.
        const cleaned = normalise(textarea.value);
        if (cleaned !== textarea.value) textarea.value = cleaned;

        textarea.addEventListener('input', function () {
            const current = this.value;
            const next = normalise(current);
            if (next === current) return;

            // Preserve caret position across the replacement. Assumes
            // each substitution is length-preserving or shorter; the
            // multi-char ellipsis grows by 2 per occurrence so adjust.
            const pos = this.selectionStart;
            const before = current.slice(0, pos);
            const beforeNext = normalise(before);
            const delta = beforeNext.length - before.length;

            this.value = next;
            this.setSelectionRange(pos + delta, pos + delta);
        });
    }

    window.TextareaSmartQuotes = { attach, normalise };
})();
