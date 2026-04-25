/* Bible search integration for the main search bar.
 *
 * Two behaviours triggered by typing in #searchInput:
 *
 * 1. Reference shortcut: if the input parses as a Bible reference
 *    (e.g. "joh 3:16", "psalms 23"), an inline "Open Johannes 3:16"
 *    suggestion is pinned at the top of the results area. Only fires
 *    for inputs containing a digit.
 *
 * 2. Verse text search: every search runs in parallel against the
 *    Bible too, with results rendered in a dedicated section below
 *    the chorus results. Clicking a verse opens the chapter overlay
 *    scrolled to that verse.
 *
 * Mirrors the chorus search rules: 300ms debounce, 2-char minimum,
 * sanitization is server-side.
 */
(function () {
    'use strict';

    const DEBOUNCE_MS = 300;
    const MIN_CHARS = 2;
    const MAX_RESULTS = 50;
    const SUGGESTION_ID = 'bibleInlineSuggestion';

    let timer = 0;
    let seq = 0;

    function getInput() { return document.getElementById('searchInput'); }
    function getResultsContainer() {
        return document.querySelector('.results-container')
            || document.getElementById('searchResults')
            || document.querySelector('.search-input-container');
    }
    function getBibleResults() { return document.getElementById('bibleSearchResults'); }
    function getBibleResultsList() { return document.getElementById('bibleSearchResultsList'); }
    function getBibleResultsCount() { return document.getElementById('bibleSearchResultsCount'); }

    function escapeHtml(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }
    function escapeAttr(s) { return escapeHtml(s); }

    // ---------- inline reference suggestion ----------
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
        div.setAttribute('aria-label', 'Open ' + ref.bookName + ' ' + ref.chapter + (ref.verse ? ':' + ref.verse : ''));
        div.innerHTML = '<i class="fas fa-book"></i>'
            + '<span>Open <strong>' + escapeHtml(ref.bookName) + ' ' + ref.chapter + (ref.verse ? ':' + ref.verse : '') + '</strong></span>';
        div.addEventListener('click', function () {
            openOverlay(ref.bookId, ref.chapter, ref.verse || null);
        });
        if (target.firstChild) target.insertBefore(div, target.firstChild);
        else target.appendChild(div);
    }
    async function tryResolve(query) {
        const my = seq;
        try {
            const r = await fetch('/Home/BibleResolve?ref=' + encodeURIComponent(query), { credentials: 'same-origin' });
            if (my !== seq) return;
            if (!r.ok) { clearSuggestion(); return; }
            const ref = await r.json();
            showSuggestion(ref);
        } catch (_) { clearSuggestion(); }
    }

    // ---------- verse text-search results ----------
    function hideVerseResults() {
        const section = getBibleResults();
        if (section) section.hidden = true;
        const list = getBibleResultsList();
        if (list) list.innerHTML = '';
        const count = getBibleResultsCount();
        if (count) count.textContent = '';
    }
    function renderVerseResults(query, verses) {
        const section = getBibleResults();
        const list = getBibleResultsList();
        const countEl = getBibleResultsCount();
        if (!section || !list) return;
        if (!verses || !verses.length) {
            hideVerseResults();
            return;
        }
        const needle = (query || '').trim().toLowerCase();
        const html = verses.map(function (v) {
            const ref = escapeHtml(v.bookName) + ' ' + v.chapter + ':' + v.verse;
            const snippet = highlight(escapeHtml(v.text), needle);
            return '<button type="button" class="bible-search-result" role="listitem"'
                + ' data-book="' + escapeAttr(v.bookId) + '"'
                + ' data-chapter="' + v.chapter + '"'
                + ' data-verse="' + v.verse + '">'
                + '<span class="bible-search-result__ref">' + ref + '</span>'
                + '<span class="bible-search-result__text">' + snippet + '</span>'
                + '</button>';
        }).join('');
        list.innerHTML = html;
        if (countEl) countEl.textContent = verses.length === 1 ? '1 verse' : verses.length + ' verses';
        section.hidden = false;
    }
    function highlight(safeText, needle) {
        if (!needle) return safeText;
        // needle is plain user input; safeText is already HTML-escaped, so
        // we can do a simple case-insensitive search-and-wrap on the text.
        const lower = safeText.toLowerCase();
        const out = [];
        let i = 0;
        while (i < safeText.length) {
            const hit = lower.indexOf(needle, i);
            if (hit === -1) { out.push(safeText.slice(i)); break; }
            out.push(safeText.slice(i, hit));
            out.push('<mark>' + safeText.slice(hit, hit + needle.length) + '</mark>');
            i = hit + needle.length;
        }
        return out.join('');
    }
    async function runVerseSearch(query) {
        const my = seq;
        try {
            const r = await fetch('/Home/BibleSearch?q=' + encodeURIComponent(query) + '&max=' + MAX_RESULTS, { credentials: 'same-origin' });
            if (my !== seq) return;
            if (!r.ok) { hideVerseResults(); return; }
            const data = await r.json();
            renderVerseResults(query, data.results || []);
        } catch (_) { hideVerseResults(); }
    }

    function openOverlay(bookId, chapter, verse) {
        if (window.bibleOverlay && typeof window.bibleOverlay.open === 'function') {
            window.bibleOverlay.open({ bookId: bookId, chapter: chapter, verse: verse || null });
        }
    }

    // ---------- input handler ----------
    function onInput() {
        clearTimeout(timer);
        const input = getInput();
        if (!input) return;
        const q = (input.value || '').trim();
        if (q.length < MIN_CHARS) {
            clearSuggestion();
            hideVerseResults();
            return;
        }
        seq++;
        timer = setTimeout(function () {
            // Reference resolve only when the input has a digit (cheap
            // heuristic to skip per-keystroke 404s).
            if (/\d/.test(q)) tryResolve(q);
            else clearSuggestion();
            // Always run text search for verses.
            runVerseSearch(q);
        }, DEBOUNCE_MS);
    }

    document.addEventListener('DOMContentLoaded', function () {
        const input = getInput();
        if (!input) return;
        input.addEventListener('input', onInput);

        // The chorus-search clear button doesn't dispatch an 'input' event,
        // so listen for its click directly to clear our own state too.
        const clearBtn = document.getElementById('clearBtn');
        if (clearBtn) {
            clearBtn.addEventListener('click', function () {
                seq++;
                clearTimeout(timer);
                clearSuggestion();
                hideVerseResults();
            });
        }

        // Click-delegation on the results list -- one listener instead of
        // wiring per-row click handlers as the list re-renders.
        const list = getBibleResultsList();
        if (list) {
            list.addEventListener('click', function (e) {
                const btn = e.target.closest('.bible-search-result');
                if (!btn) return;
                openOverlay(
                    btn.getAttribute('data-book'),
                    parseInt(btn.getAttribute('data-chapter'), 10),
                    parseInt(btn.getAttribute('data-verse'), 10)
                );
            });
        }
    });
})();
