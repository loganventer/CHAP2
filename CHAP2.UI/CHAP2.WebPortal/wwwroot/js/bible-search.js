/* Bible reference shortcut for the main search bar.
 *
 * If the user types something that parses as a Bible reference (e.g.
 * "joh 3:16", "psalms 23"), an inline "📖 Open …" suggestion is
 * inserted above the chorus search results. Clicking it opens the
 * Bible chapter overlay and bypasses the lookup modal.
 *
 * Mirrors the chorus search rules: 300 ms debounce, 2-char minimum.
 * Sanitization is server-side (InputSanitizer.SanitizeSearchQuery).
 */
(function () {
    'use strict';

    const DEBOUNCE_MS = 300;
    const MIN_CHARS = 2;
    const SUGGESTION_ID = 'bibleInlineSuggestion';

    let timer = 0;
    let seq = 0;

    function getInput() { return document.getElementById('searchInput'); }
    function getResultsContainer() {
        // Try a few well-known anchors used by search-v2.js. Falling back
        // to inserting after the search box keeps the suggestion visible
        // even if the results container hasn't been created yet.
        return document.getElementById('searchResults')
            || document.querySelector('.search-results')
            || document.querySelector('.search-input-container');
    }

    function clearSuggestion() {
        const existing = document.getElementById(SUGGESTION_ID);
        if (existing) existing.remove();
    }

    function showSuggestion(ref) {
        clearSuggestion();
        const target = getResultsContainer();
        if (!target) return;
        const div = document.createElement('button');
        div.id = SUGGESTION_ID;
        div.type = 'button';
        div.className = 'bible-inline-suggestion';
        div.setAttribute('aria-label', 'Maak ' + ref.bookName + ' ' + ref.chapter + (ref.verse ? ':' + ref.verse : '') + ' oop');
        div.innerHTML = '<i class="fas fa-book"></i>'
            + '<span>Maak <strong>' + escapeHtml(ref.bookName) + ' ' + ref.chapter + (ref.verse ? ':' + ref.verse : '') + '</strong> oop</span>';
        div.addEventListener('click', function () {
            if (window.bibleOverlay && typeof window.bibleOverlay.open === 'function') {
                window.bibleOverlay.open({
                    bookId: ref.bookId,
                    chapter: ref.chapter,
                    verse: ref.verse || null,
                });
            }
        });
        // Insert at the top of the results container.
        if (target.firstChild) {
            target.insertBefore(div, target.firstChild);
        } else {
            target.appendChild(div);
        }
    }

    function escapeHtml(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    async function tryResolve(query) {
        const my = ++seq;
        try {
            const r = await fetch('/Home/BibleResolve?ref=' + encodeURIComponent(query), { credentials: 'same-origin' });
            if (my !== seq) return;
            if (!r.ok) { clearSuggestion(); return; }
            const ref = await r.json();
            showSuggestion(ref);
        } catch (_) {
            clearSuggestion();
        }
    }

    function onInput() {
        clearTimeout(timer);
        const input = getInput();
        if (!input) return;
        const q = (input.value || '').trim();
        // Only attempt reference resolution if the typed text has a digit
        // (a chapter/verse number) -- otherwise it's not a reference and
        // calling resolve just spams 404s into the console.
        if (q.length < MIN_CHARS || !/\d/.test(q)) {
            clearSuggestion();
            return;
        }
        timer = setTimeout(function () { tryResolve(q); }, DEBOUNCE_MS);
    }

    document.addEventListener('DOMContentLoaded', function () {
        const input = getInput();
        if (!input) return;
        input.addEventListener('input', onInput);
    });
})();
