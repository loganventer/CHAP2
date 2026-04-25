/* Bible lookup modal.
 *
 * Triggered by the book icon in the search bar (#bibleTrigger).
 * On selecting a verse, the modal closes itself and asks
 * window.bibleOverlay to render the chapter.
 */
(function () {
    'use strict';

    const SEARCH_DEBOUNCE_MS = 300;
    const SEARCH_MIN_CHARS = 2;
    const SEARCH_MAX_RESULTS = 50;

    const els = {};
    function cacheDom() {
        els.modal      = document.getElementById('bibleModal');
        els.container  = els.modal && els.modal.querySelector('.modal-container');
        els.closeBtn   = document.getElementById('bibleModalClose');
        els.cancelBtn  = document.getElementById('bibleModalCancel');
        els.openBtn    = document.getElementById('bibleModalOpen');
        els.searchInp  = document.getElementById('bibleModalSearch');
        els.bookSel    = document.getElementById('bibleModalBook');
        els.chSel      = document.getElementById('bibleModalChapter');
        els.vSel       = document.getElementById('bibleModalVerse');
        els.results    = document.getElementById('bibleModalResults');
    }

    let books = [];
    let booksById = Object.create(null);
    let lastFocus = null;
    let searchTimer = 0;
    let searchSeq = 0;
    let lastChapterVerseCount = 1;

    function escapeHtml(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }
    function escapeAttr(s) { return escapeHtml(s); }

    async function ensureBooks() {
        if (books.length) return;
        try {
            books = await window.BibleData.getBooks();
        } catch (_) {
            books = [];
            renderError('Kon nie boeke laai nie.');
            return;
        }
        booksById = Object.create(null);
        books.forEach(function (b) { booksById[b.id] = b; });
        populateBookSelect();
    }

    function populateBookSelect() {
        els.bookSel.innerHTML = books.map(function (b) {
            return '<option value="' + escapeAttr(b.id) + '">' + escapeHtml(b.name) + '</option>';
        }).join('');
        const first = books[0];
        if (first) {
            els.bookSel.value = first.id;
            populateChapterSelect(first);
        }
    }
    function populateChapterSelect(book, selected) {
        const opts = [];
        for (let i = 1; i <= book.chapterCount; i++) {
            opts.push('<option value="' + i + '"' + (i === (selected || 1) ? ' selected' : '') + '>' + i + '</option>');
        }
        els.chSel.innerHTML = opts.join('');
        // Verse list defaults to 1 until a chapter is loaded; we lazily load it
        // when the user changes the chapter dropdown.
        els.vSel.innerHTML = '<option value="1" selected>1</option>';
        lastChapterVerseCount = 1;
    }
    async function loadVerseCountForCurrentChapter() {
        const bookId = els.bookSel.value;
        const chapter = parseInt(els.chSel.value, 10) || 1;
        try {
            const r = await fetch('/Home/BibleChapter?bookId=' + encodeURIComponent(bookId) + '&chapter=' + chapter, { credentials: 'same-origin' });
            if (!r.ok) return;
            const dto = await r.json();
            lastChapterVerseCount = dto.verses.length;
            const opts = [];
            for (let i = 1; i <= lastChapterVerseCount; i++) {
                opts.push('<option value="' + i + '">' + i + '</option>');
            }
            els.vSel.innerHTML = opts.join('');
        } catch (_) { /* keep the default 1 */ }
    }

    function renderError(message) {
        els.results.innerHTML = '<div class="bible-modal-error">' + escapeHtml(message) + '</div>';
    }
    function renderEmpty(message) {
        els.results.innerHTML = '<div class="bible-modal-empty">' + escapeHtml(message) + '</div>';
    }
    function renderResults(items) {
        if (!items || !items.length) {
            els.results.innerHTML = '';
            return;
        }
        els.results.innerHTML = items.map(function (v) {
            return '<button class="bible-modal-result" type="button"'
                + ' data-book="' + escapeAttr(v.bookId) + '"'
                + ' data-chapter="' + v.chapter + '"'
                + ' data-verse="' + v.verse + '">'
                + '<span class="bible-modal-result__ref">' + escapeHtml(v.bookName) + ' ' + v.chapter + ':' + v.verse + '</span>'
                + '<span class="bible-modal-result__text">' + escapeHtml(v.text) + '</span>'
                + '</button>';
        }).join('');
    }

    function debouncedSearch() {
        clearTimeout(searchTimer);
        const q = (els.searchInp.value || '').trim();
        if (q.length < SEARCH_MIN_CHARS) {
            els.results.innerHTML = '';
            return;
        }
        searchTimer = setTimeout(function () { runSearch(q); }, SEARCH_DEBOUNCE_MS);
    }
    async function runSearch(q) {
        const seq = ++searchSeq;
        // Only try reference resolve if the input has a digit -- partial
        // words don't parse as references and would noisily 404.
        if (/\d/.test(q)) {
            try {
                const rr = await fetch('/Home/BibleResolve?ref=' + encodeURIComponent(q), { credentials: 'same-origin' });
                if (seq !== searchSeq) return;
                if (rr.ok) {
                    const ref = await rr.json();
                    renderResults([{
                        bookId: ref.bookId,
                        bookName: ref.bookName,
                        chapter: ref.chapter,
                        verse: ref.verse || 1,
                        text: 'Spring na ' + ref.bookName + ' ' + ref.chapter + (ref.verse ? ':' + ref.verse : ''),
                    }]);
                    return;
                }
            } catch (_) { /* fall through */ }
        }

        try {
            const r = await fetch('/Home/BibleSearch?q=' + encodeURIComponent(q) + '&max=' + SEARCH_MAX_RESULTS, { credentials: 'same-origin' });
            if (seq !== searchSeq) return;
            if (!r.ok) {
                renderError('Soek het misluk.');
                return;
            }
            const data = await r.json();
            if (!data.results || !data.results.length) {
                renderEmpty('Geen verse gevind nie.');
                return;
            }
            renderResults(data.results);
        } catch (_) {
            renderError('Soek het misluk.');
        }
    }

    function trapTab(e) {
        if (e.key !== 'Tab') return;
        const focusables = els.container.querySelectorAll('button, input, select, [tabindex]:not([tabindex="-1"])');
        const visible = Array.prototype.filter.call(focusables, function (el) { return !el.disabled && el.offsetParent !== null; });
        if (!visible.length) return;
        const first = visible[0];
        const last = visible[visible.length - 1];
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    }
    function onKeyDown(e) {
        if (els.modal.hidden) return;
        if (e.key === 'Escape') { e.preventDefault(); close(); return; }
        if (e.key === 'Enter' && document.activeElement === els.searchInp) {
            e.preventDefault();
            // Open the first visible result if there is one, otherwise treat
            // the input as a reference and try to open it directly.
            const first = els.results.querySelector('.bible-modal-result');
            if (first) { first.click(); return; }
            tryOpenFromForm();
            return;
        }
        trapTab(e);
    }

    function tryOpenFromForm() {
        const bookId = els.bookSel.value;
        const chapter = parseInt(els.chSel.value, 10) || 1;
        const verse = parseInt(els.vSel.value, 10) || 1;
        if (!bookId) return;
        openOverlayWith({ bookId: bookId, chapter: chapter, verse: verse });
    }

    function openOverlayWith(ref) {
        close();
        if (window.bibleOverlay && typeof window.bibleOverlay.open === 'function') {
            window.bibleOverlay.open(ref);
        }
    }

    async function open() {
        await ensureBooks();
        lastFocus = document.activeElement;
        els.modal.hidden = false;
        // Focus the search input by default — it's the most flexible entry point.
        setTimeout(function () { els.searchInp.focus({ preventScroll: true }); }, 0);
    }
    function close() {
        els.modal.hidden = true;
        els.searchInp.value = '';
        els.results.innerHTML = '';
        if (lastFocus && typeof lastFocus.focus === 'function') {
            lastFocus.focus({ preventScroll: true });
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        cacheDom();
        if (!els.modal) return;

        els.closeBtn.addEventListener('click', close);
        els.cancelBtn.addEventListener('click', close);
        els.modal.addEventListener('click', function (e) {
            if (e.target === els.modal) close();
        });

        els.openBtn.addEventListener('click', tryOpenFromForm);

        els.bookSel.addEventListener('change', function () {
            const book = booksById[els.bookSel.value];
            if (book) populateChapterSelect(book);
        });
        els.chSel.addEventListener('change', loadVerseCountForCurrentChapter);
        els.searchInp.addEventListener('input', debouncedSearch);
        els.results.addEventListener('click', function (e) {
            const btn = e.target.closest('.bible-modal-result');
            if (!btn) return;
            openOverlayWith({
                bookId: btn.getAttribute('data-book'),
                chapter: parseInt(btn.getAttribute('data-chapter'), 10),
                verse: parseInt(btn.getAttribute('data-verse'), 10),
            });
        });

        document.addEventListener('keydown', onKeyDown);
    });

    window.bibleModal = { open: open, close: close };
})();
