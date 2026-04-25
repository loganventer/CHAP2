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
    let lastQuery = '';        // remembered so we can re-run after the API
                               // wakes up (chap2:api-recovered)
    let currentStream = null;  // EventSource for the in-flight verse search

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
    function renderVerseResults(query, verses, done) {
        const section = getBibleResults();
        const list = getBibleResultsList();
        const countEl = getBibleResultsCount();
        if (!section || !list) return;
        if (!verses || !verses.length) {
            // While streaming and still empty, keep the section visible so
            // the spinner / count message is visible. Only fully hide once
            // the stream has finished with no results.
            if (done) hideVerseResults();
            return;
        }
        const needle = (query || '').trim().toLowerCase();
        const html = verses.map(function (v) {
            const ref = escapeHtml(v.bookName) + ' ' + v.chapter + ':' + v.verse;
            const snippet = highlight(escapeHtml(v.text), needle);
            const addBtn = '<button type="button" class="bible-search-result__add"'
                + ' data-book="' + escapeAttr(v.bookId) + '"'
                + ' data-book-name="' + escapeAttr(v.bookName) + '"'
                + ' data-chapter="' + v.chapter + '"'
                + ' data-verse="' + v.verse + '"'
                + ' data-text="' + escapeAttr(v.text) + '"'
                + ' aria-label="Add ' + escapeAttr(v.bookName + ' ' + v.chapter + ':' + v.verse) + ' to setlist"'
                + ' title="Add to setlist">'
                + '<i class="fas fa-plus"></i></button>';
            return '<div class="bible-search-result" role="listitem"'
                + ' data-book="' + escapeAttr(v.bookId) + '"'
                + ' data-chapter="' + v.chapter + '"'
                + ' data-verse="' + v.verse + '">'
                + '<button type="button" class="bible-search-result__open"'
                + ' data-book="' + escapeAttr(v.bookId) + '"'
                + ' data-chapter="' + v.chapter + '"'
                + ' data-verse="' + v.verse + '"'
                + ' aria-label="Open ' + escapeAttr(v.bookName + ' ' + v.chapter + ':' + v.verse) + '">'
                + '<span class="bible-search-result__ref">' + ref + '</span>'
                + '<span class="bible-search-result__text">' + snippet + '</span>'
                + '</button>'
                + addBtn
                + '</div>';
        }).join('');
        list.innerHTML = html;
        if (countEl) {
            const base = verses.length === 1 ? '1 verse' : verses.length + ' verses';
            countEl.textContent = done ? base : base + ' (loading…)';
        }
        section.hidden = false;
    }
    function highlight(safeText, needle) {
        if (!needle) return safeText;
        // Highlight (a) the contiguous phrase if present and (b) each
        // individual query word. Without (b), in-order / out-of-order
        // matches surfaced no <mark> at all because the words weren't
        // contiguous in the verse text. Spans are merged so adjacent or
        // overlapping hits never get double-wrapped.
        const phrase = needle.trim();
        const words = phrase.split(/\s+/).filter(Boolean);
        if (!words.length) return safeText;
        const targets = words.length > 1 ? [phrase, ...words] : words;

        const lower = safeText.toLowerCase();
        const spans = [];
        for (const t of targets) {
            if (!t) continue;
            let from = 0;
            while (from < lower.length) {
                const hit = lower.indexOf(t, from);
                if (hit === -1) break;
                spans.push([hit, hit + t.length]);
                from = hit + 1; // overlapping single-word hits handled by the merge below
            }
        }
        if (!spans.length) return safeText;

        spans.sort(function (a, b) { return a[0] - b[0] || b[1] - a[1]; });
        const merged = [];
        for (const span of spans) {
            const top = merged.length ? merged[merged.length - 1] : null;
            if (top && span[0] <= top[1]) {
                if (span[1] > top[1]) top[1] = span[1];
            } else {
                merged.push([span[0], span[1]]);
            }
        }

        const out = [];
        let cursor = 0;
        for (const [s, e] of merged) {
            out.push(safeText.slice(cursor, s));
            out.push('<mark>');
            out.push(safeText.slice(s, e));
            out.push('</mark>');
            cursor = e;
        }
        out.push(safeText.slice(cursor));
        return out.join('');
    }
    function closeStream() {
        if (currentStream) {
            try { currentStream.close(); } catch (_) { /* ignore */ }
            currentStream = null;
        }
    }

    /**
     * Stream-based verse search. Opens an EventSource against the
     * SSE proxy; renders rows incrementally as they arrive, re-sorting
     * the in-memory list by relevance every time so higher-scored
     * matches naturally bubble to the top mid-stream. DOM updates are
     * coalesced via requestAnimationFrame so a fast burst of events
     * doesn't churn layout.
     */
    function runVerseSearch(query) {
        closeStream();
        const my = seq;
        const hits = [];           // sorted in-memory result list
        let firstReceived = false; // gates the api-wait-end event
        let doneReceived = false;
        let renderScheduled = false;

        function scheduleRender() {
            if (renderScheduled) return;
            renderScheduled = true;
            requestAnimationFrame(function () {
                renderScheduled = false;
                if (my !== seq) return;
                renderVerseResults(query, hits, doneReceived);
            });
        }

        document.dispatchEvent(new CustomEvent('chap2:api-wait-start', {
            detail: { delayMs: 1500 },
        }));

        const url = '/Home/BibleSearchStream?q=' + encodeURIComponent(query);
        let es;
        try {
            es = new EventSource(url, { withCredentials: true });
        } catch (e) {
            document.dispatchEvent(new CustomEvent('chap2:api-probe', {
                detail: { message: 'Server still waking up — checking connection...' },
            }));
            return;
        }
        currentStream = es;

        es.onmessage = function (e) {
            if (my !== seq) return;
            if (!firstReceived) {
                firstReceived = true;
                document.dispatchEvent(new Event('chap2:api-wait-end'));
            }
            let v;
            try { v = JSON.parse(e.data); } catch (_) { return; }

            hits.push(v);
            // Score desc, then verse-text length asc (denser hit). Array.sort
            // is stable, so within ties the canonical emit order from the
            // server (book ordinal -> chapter -> verse) is preserved.
            hits.sort(function (a, b) {
                return (b.score || 0) - (a.score || 0)
                    || (a.text ? a.text.length : 0) - (b.text ? b.text.length : 0);
            });
            if (hits.length > MAX_RESULTS) hits.length = MAX_RESULTS;
            scheduleRender();
        };

        es.addEventListener('done', function () {
            if (my !== seq) return;
            doneReceived = true;
            if (!firstReceived) {
                document.dispatchEvent(new Event('chap2:api-wait-end'));
            }
            scheduleRender();
            closeStream();
        });

        es.onerror = function () {
            if (my !== seq) return;
            // EventSource auto-reconnects; we don't want that during a
            // cold-start because the auth cookie / probe machinery handle
            // recovery. Close and co-opt the api-probe loop -- when the
            // API answers, chap2:api-recovered re-runs the search.
            closeStream();
            document.dispatchEvent(new CustomEvent('chap2:api-probe', {
                detail: { message: 'Server still waking up — checking connection...' },
            }));
        };
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
            // Bumping seq invalidates any in-flight stream from before the
            // clear, so a slow earlier response can't repaint the results
            // we just cleared.
            seq++;
            closeStream();
            clearSuggestion();
            hideVerseResults();
            return;
        }
        seq++;
        lastQuery = q;
        timer = setTimeout(function () {
            // Reference resolve only when the input has a digit (cheap
            // heuristic to skip per-keystroke 404s).
            if (/\d/.test(q)) tryResolve(q);
            else clearSuggestion();
            // Always run text search for verses.
            runVerseSearch(q);
        }, DEBOUNCE_MS);
    }

    // When the chorus side's probe detects the API is back up, re-run
    // our last verse search so the user sees fresh results without
    // re-typing.
    document.addEventListener('chap2:api-recovered', function () {
        if (lastQuery && lastQuery.length >= MIN_CHARS) {
            seq++;
            if (/\d/.test(lastQuery)) tryResolve(lastQuery);
            runVerseSearch(lastQuery);
        }
    });

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
                closeStream();
                clearSuggestion();
                hideVerseResults();
            });
        }

        // Click-delegation on the results list. Two buttons per row:
        //  - .bible-search-result__open  -> open the verse in the overlay
        //  - .bible-search-result__add   -> add the verse to the setlist
        const list = getBibleResultsList();
        if (list) {
            list.addEventListener('click', function (e) {
                const addBtn = e.target.closest('.bible-search-result__add');
                if (addBtn) {
                    e.stopPropagation();
                    window.dispatchEvent(new CustomEvent('chap2:add-verse-to-setlist', {
                        detail: {
                            bookId: addBtn.getAttribute('data-book'),
                            bookName: addBtn.getAttribute('data-book-name'),
                            chapter: parseInt(addBtn.getAttribute('data-chapter'), 10),
                            verse: parseInt(addBtn.getAttribute('data-verse'), 10),
                            text: addBtn.getAttribute('data-text') || '',
                        },
                    }));
                    const row = addBtn.closest('.bible-search-result');
                    if (row) {
                        row.classList.add('bible-search-result--just-added');
                        setTimeout(function () { row.classList.remove('bible-search-result--just-added'); }, 600);
                    }
                    return;
                }
                const openBtn = e.target.closest('.bible-search-result__open');
                if (openBtn) {
                    openOverlay(
                        openBtn.getAttribute('data-book'),
                        parseInt(openBtn.getAttribute('data-chapter'), 10),
                        parseInt(openBtn.getAttribute('data-verse'), 10)
                    );
                }
            });
        }
    });
})();
