/* Bible chapter overlay.
 *
 * Public API:
 *   window.BibleData.getBooks() -> Promise<BibleBookDto[]>  (shared cache)
 *   window.bibleOverlay.open({ bookId, chapter, verse })    (verse optional)
 *   window.bibleOverlay.close()
 *
 * The overlay loads the *whole* chapter, scrolls to the requested verse,
 * and lets the user navigate to any other passage without closing
 * (built-in book/chapter/verse dropdowns + reference/search input).
 */
(function () {
    'use strict';

    // Default landing passage when the user clicks the Bible trigger
    // with no prior context: Efesi\u00ebrs 4:5 — "een Here, een geloof, een doop".
    const DEFAULT_REFERENCE = { bookId: 'efesiers', chapter: 4, verse: 5 };

    const FONT_SCALE_KEY = 'chap2.bible.fontScale';
    const COMPACT_KEY = 'chap2.bible.compact';
    // 480p projection target -- defaults pushed high. Min is clamped so
    // text stays legible; max is unbounded so users can crank it as
    // large as their projector / screen needs.
    const FONT_MIN = 0.8;
    const FONT_MAX = Infinity;
    const FONT_STEP = 0.15;
    const SEARCH_DEBOUNCE_MS = 300;
    const SEARCH_MIN_CHARS = 2;
    const SEARCH_MAX_RESULTS = 50;

    // ---------- shared book-list cache (used by modal + search too) ----------
    let booksPromise = null;
    function getBooks() {
        if (!booksPromise) {
            booksPromise = fetch('/Home/BibleBooks', { credentials: 'same-origin' })
                .then(function (r) {
                    if (!r.ok) throw new Error('books-fetch-failed');
                    return r.json();
                })
                .catch(function (err) {
                    booksPromise = null;
                    throw err;
                });
        }
        return booksPromise;
    }
    window.BibleData = window.BibleData || { getBooks: getBooks };

    // ---------- DOM refs ----------
    const els = {};
    function cacheDom() {
        els.overlay     = document.getElementById('bibleOverlay');
        els.sheet       = els.overlay && els.overlay.querySelector('.bible-overlay__sheet');
        els.title       = document.getElementById('bibleOverlayTitle');
        els.body        = document.getElementById('bibleOverlayBody');
        els.chapterEl   = document.getElementById('bibleOverlayChapterContent');
        els.live        = document.getElementById('bibleOverlayLive');
        els.closeBtn    = document.getElementById('bibleOverlayClose');
        els.prevBtn     = document.getElementById('bibleOverlayPrev');
        els.nextBtn     = document.getElementById('bibleOverlayNext');
        els.fontMinus   = document.getElementById('bibleOverlayFontMinus');
        els.fontPlus    = document.getElementById('bibleOverlayFontPlus');
        els.compactBtn  = document.getElementById('bibleOverlayCompact');
        els.resetBtn    = document.getElementById('bibleOverlayReset');
        els.searchInput = document.getElementById('bibleOverlaySearch');
        els.bookSelect  = document.getElementById('bibleOverlayBook');
        els.chSelect    = document.getElementById('bibleOverlayChapter');
        els.vSelect     = document.getElementById('bibleOverlayVerse');
        els.results     = document.getElementById('bibleOverlayResults');
    }

    // ---------- state ----------
    let books = [];
    let booksById = Object.create(null);
    let current = null; // { book, chapter, verse }
    let lastFocus = null;
    let searchTimer = 0;
    let searchSeq = 0;
    // When the overlay was opened from a setlist runner, the prev/next
    // chevrons advance the setlist instead of navigating chapters.
    let setlistContext = null;

    // ---------- font scale ----------
    // Browsers with strict tracking prevention (Edge in default mode for
    // some sites) throw on any localStorage access. Both read and write
    // need try/catch -- without it, an unhandled throw inside open()
    // skips the rest of the function and the overlay never loads its
    // chapter / dropdowns.
    // Default scale matches --bible-font-scale in bible.css. Returned only
    // when the user has no saved preference; existing saved values win.
    const DEFAULT_FONT_SCALE = 5;
    function readFontScale() {
        try {
            const v = parseFloat(localStorage.getItem(FONT_SCALE_KEY) || '');
            return Number.isFinite(v) ? Math.min(FONT_MAX, Math.max(FONT_MIN, v)) : DEFAULT_FONT_SCALE;
        } catch (_) { return DEFAULT_FONT_SCALE; }
    }
    function applyFontScale(scale) {
        const clamped = Math.min(FONT_MAX, Math.max(FONT_MIN, scale));
        els.overlay.style.setProperty('--bible-font-scale', String(clamped));
        try { localStorage.setItem(FONT_SCALE_KEY, String(clamped)); } catch (_) { /* ignore */ }
    }
    function bumpFontScale(delta) {
        const cur = parseFloat(getComputedStyle(els.overlay).getPropertyValue('--bible-font-scale')) || 1;
        applyFontScale(cur + delta);
    }

    // ---------- compact mode ----------
    function readCompact() {
        try { return localStorage.getItem(COMPACT_KEY) === '1'; }
        catch (_) { return false; }
    }
    function applyCompact(on) {
        els.overlay.classList.toggle('bible-overlay--compact', !!on);
        if (els.compactBtn) els.compactBtn.setAttribute('aria-pressed', on ? 'true' : 'false');
        try { localStorage.setItem(COMPACT_KEY, on ? '1' : '0'); } catch (_) { /* ignore */ }
    }
    function toggleCompact() { applyCompact(!els.overlay.classList.contains('bible-overlay--compact')); }

    // ---------- reset all text settings ----------
    function resetTextSettings() {
        applyFontScale(DEFAULT_FONT_SCALE);
        applyCompact(false);
    }

    // ---------- rendering ----------
    function renderSkeleton() {
        const lines = Array.from({ length: 8 }, function () { return '<div class="bible-overlay__skeleton-line"></div>'; }).join('');
        els.chapterEl.innerHTML = '<div class="bible-overlay__skeleton">' + lines + '</div>';
    }
    function renderError(message) {
        els.chapterEl.innerHTML = '<div class="bible-overlay__notice bible-overlay__notice--error" role="alert">'
            + escapeHtml(message) + '</div>';
    }
    function renderNotice(message) {
        els.chapterEl.innerHTML = '<div class="bible-overlay__notice">' + escapeHtml(message) + '</div>';
    }
    function renderChapter(dto, targetVerse) {
        // Each verse is its own block (CSS `display: block` + first-line
        // text-indent gives the e-Sword-style indented verse number with
        // wrapped lines flowing back to the margin). The target verse is
        // both aria-current (visual highlight) and pre-selected (bold).
        const parts = dto.verses.map(function (v) {
            const isTarget = targetVerse && v.verse === targetVerse;
            const classes = 'bible-overlay__verse' + (isTarget ? ' bible-overlay__verse--selected' : '');
            const ariaCurrent = isTarget ? ' aria-current="true"' : '';
            return '<span class="' + classes + '" id="v' + v.verse + '"' + ariaCurrent + '>'
                +    '<span class="bible-overlay__verse-num">' + v.verse + '</span>'
                +    escapeHtml(v.text)
                + '</span>';
        });
        els.chapterEl.innerHTML = parts.join('');

        if (targetVerse) {
            const target = els.chapterEl.querySelector('#v' + targetVerse);
            if (target) {
                const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
                target.scrollIntoView({ block: 'center', behavior: reduced ? 'auto' : 'smooth' });
            }
        } else {
            els.body.scrollTop = 0;
        }
    }
    function renderInlineResults(results) {
        if (!results || !results.length) {
            els.results.innerHTML = '';
            return;
        }
        els.results.innerHTML = results.map(function (v) {
            return '<button class="bible-modal-result" type="button"'
                + ' data-book="' + escapeAttr(v.bookId) + '"'
                + ' data-chapter="' + v.chapter + '"'
                + ' data-verse="' + v.verse + '">'
                + '<span class="bible-modal-result__ref">' + escapeHtml(v.bookName) + ' ' + v.chapter + ':' + v.verse + '</span>'
                + '<span class="bible-modal-result__text">' + escapeHtml(v.text) + '</span>'
                + '</button>';
        }).join('');
    }

    function escapeHtml(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }
    function escapeAttr(s) { return escapeHtml(s); }

    // ---------- header dropdowns ----------
    function populateBookSelect() {
        els.bookSelect.innerHTML = books.map(function (b) {
            return '<option value="' + escapeAttr(b.id) + '">' + escapeHtml(b.name) + '</option>';
        }).join('');
    }
    function populateChapterSelect(book, selected) {
        const count = book && Number(book.chapterCount);
        if (!Number.isFinite(count) || count < 1) return;
        const opts = [];
        for (let i = 1; i <= count; i++) {
            opts.push('<option value="' + i + '">' + i + '</option>');
        }
        els.chSelect.innerHTML = opts.join('');
        if (selected) els.chSelect.value = String(selected);
    }
    function populateVerseSelect(verseCount, selected) {
        const opts = [];
        for (let i = 1; i <= verseCount; i++) {
            opts.push('<option value="' + i + '">' + i + '</option>');
        }
        els.vSelect.innerHTML = opts.join('');
        if (selected) els.vSelect.value = String(selected);
    }

    function syncHeaderToCurrent() {
        if (!current) return;
        els.title.textContent = current.book.name + ' ' + current.chapter;
        if (els.bookSelect.value !== current.book.id) els.bookSelect.value = current.book.id;
        populateChapterSelect(current.book, current.chapter);
        populateVerseSelect(current.verseCount || 1, current.verse || 1);
        els.prevBtn.disabled = !canGoPrev();
        els.nextBtn.disabled = !canGoNext();
    }

    function canGoPrev() {
        if (inSetlistMode()) return true;   // wraps, always enabled
        if (!current) return false;
        if (current.chapter > 1) return true;
        return current.book.ordinal > 1;
    }
    function canGoNext() {
        if (inSetlistMode()) return true;   // wraps, always enabled
        if (!current) return false;
        if (current.chapter < current.book.chapterCount) return true;
        return current.book.ordinal < 66;
    }
    function inSetlistMode() {
        return setlistContext && window.setlistManager && window.setlistManager.isRunnerActive();
    }
    function gotoPrev() {
        if (inSetlistMode()) { window.setlistManager.advance(-1); return; }
        if (!current) return;
        if (current.chapter > 1) {
            navigate({ bookId: current.book.id, chapter: current.chapter - 1 });
            return;
        }
        const prev = books.find(function (b) { return b.ordinal === current.book.ordinal - 1; });
        if (prev) navigate({ bookId: prev.id, chapter: prev.chapterCount });
    }
    function gotoNext() {
        if (inSetlistMode()) { window.setlistManager.advance(+1); return; }
        if (!current) return;
        if (current.chapter < current.book.chapterCount) {
            navigate({ bookId: current.book.id, chapter: current.chapter + 1 });
            return;
        }
        const next = books.find(function (b) { return b.ordinal === current.book.ordinal + 1; });
        if (next) navigate({ bookId: next.id, chapter: 1 });
    }

    // ---------- navigation ----------
    let lastNavRef = null;   // remember the last open ref so we can retry
                             // after the API wakes up (chap2:api-recovered)

    async function navigate(ref) {
        lastNavRef = ref;
        const book = booksById[ref.bookId];
        if (!book) {
            renderError('Boek nie gevind nie.');
            return;
        }
        renderSkeleton();
        let dto;
        try {
            const resp = await fetch('/Home/BibleChapter?bookId=' + encodeURIComponent(book.id) + '&chapter=' + ref.chapter, { credentials: 'same-origin' });
            if (resp.status === 503) {
                // API asleep / waking. Co-opt the existing probe loop so
                // the "API starting up" overlay surfaces and chap2:api-recovered
                // brings us back here.
                renderNotice('Server still waking up — checking connection...');
                document.dispatchEvent(new CustomEvent('chap2:api-probe', {
                    detail: { message: 'Server still waking up — checking connection...' },
                }));
                return;
            }
            if (!resp.ok) throw new Error('chapter-fetch-failed');
            dto = await resp.json();
        } catch (err) {
            renderError('Kon nie hoofstuk laai nie.');
            document.dispatchEvent(new CustomEvent('chap2:api-probe', {
                detail: { message: 'Server still waking up — checking connection...' },
            }));
            return;
        }

        const resolvedBook = dto.book || book;
        if (dto.book) booksById[dto.book.id] = dto.book;
        current = {
            book: resolvedBook,
            chapter: dto.chapter,
            verse: ref.verse || null,
            verseCount: dto.verses.length,
        };
        syncHeaderToCurrent();
        renderChapter(dto, ref.verse);

        // If the user asked for a specific verse and the loaded chapter
        // doesn't have it, surface that clearly above the chapter text
        // (the chapter still renders so they can see the available range).
        if (ref.verse && !dto.verses.some(function (v) { return v.verse === ref.verse; })) {
            const lastVerse = dto.verses.length ? dto.verses[dto.verses.length - 1].verse : 0;
            const notice = document.createElement('div');
            notice.className = 'bible-overlay__notice bible-overlay__notice--warning';
            notice.setAttribute('role', 'alert');
            notice.textContent = 'Vers ' + ref.verse + ' bestaan nie in '
                + resolvedBook.name + ' ' + dto.chapter
                + ' nie. Laaste vers: ' + lastVerse + '.';
            els.chapterEl.insertBefore(notice, els.chapterEl.firstChild);
            announce(notice.textContent);
        } else {
            announce(resolvedBook.name + ' hoofstuk ' + dto.chapter + ' oopgemaak');
        }
    }

    // ---------- search inside the overlay ----------
    function debouncedSearch() {
        clearTimeout(searchTimer);
        const q = (els.searchInput.value || '').trim();
        if (q.length < SEARCH_MIN_CHARS) {
            renderInlineResults([]);
            return;
        }
        searchTimer = setTimeout(function () { runSearch(q); }, SEARCH_DEBOUNCE_MS);
    }
    async function runSearch(q) {
        const seq = ++searchSeq;
        // Try reference resolution first only when the input has a digit
        // (references look like "Joh 3:16" / "Psalms 23"). Skipping the
        // resolve call for partial words ("liefd...") avoids per-keystroke
        // 404 spam in the console.
        if (/\d/.test(q)) {
            try {
                const resolveResp = await fetch('/Home/BibleResolve?ref=' + encodeURIComponent(q), { credentials: 'same-origin' });
                if (seq !== searchSeq) return;
                if (resolveResp.ok) {
                    const ref = await resolveResp.json();
                    renderInlineResults([{
                        bookId: ref.bookId,
                        bookName: ref.bookName,
                        chapter: ref.chapter,
                        verse: ref.verse || 1,
                        text: 'Spring na ' + ref.bookName + ' ' + ref.chapter + (ref.verse ? ':' + ref.verse : ''),
                    }]);
                    return;
                }
            } catch (_) { /* fall through to text search */ }
        }

        try {
            const resp = await fetch('/Home/BibleSearch?q=' + encodeURIComponent(q) + '&max=' + SEARCH_MAX_RESULTS, { credentials: 'same-origin' });
            if (seq !== searchSeq) return;
            if (!resp.ok) {
                renderInlineResults([]);
                return;
            }
            const data = await resp.json();
            renderInlineResults(data.results || []);
        } catch (_) {
            renderInlineResults([]);
        }
    }

    // ---------- focus trap + open/close ----------
    function trapTab(e) {
        if (e.key !== 'Tab') return;
        const focusables = els.sheet.querySelectorAll('button, input, select, [tabindex]:not([tabindex="-1"])');
        const visible = Array.prototype.filter.call(focusables, function (el) { return !el.disabled && el.offsetParent !== null; });
        if (!visible.length) return;
        const first = visible[0];
        const last = visible[visible.length - 1];
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    }

    function isTypingInForm(target) {
        if (!target) return false;
        const tag = target.tagName;
        return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || target.isContentEditable;
    }
    function onKeyDown(e) {
        if (els.overlay.hidden) return;
        if (e.key === 'Escape') { e.preventDefault(); close(); return; }

        // Don't hijack shortcuts while the user is typing in the search
        // input or interacting with a dropdown.
        const inForm = isTypingInForm(e.target);
        if (!inForm) {
            // ←/→ and ↑/↓ both navigate -- in setlist mode they walk the
            // mixed setlist, otherwise they paginate chapters. ↑/↓ pairs
            // with chorus-display's chorus-nav keys so the muscle memory
            // is the same on either surface.
            if (e.key === 'ArrowLeft' || e.key === 'ArrowUp')   { e.preventDefault(); gotoPrev(); return; }
            if (e.key === 'ArrowRight' || e.key === 'ArrowDown') { e.preventDefault(); gotoNext(); return; }
            if (e.key === '+' || e.key === '=') { e.preventDefault(); bumpFontScale(FONT_STEP); return; }
            if (e.key === '-' || e.key === '_') { e.preventDefault(); bumpFontScale(-FONT_STEP); return; }
        }
        // Ctrl/Cmd variants still work even when focus is on a control,
        // matching browser-native zoom UX expectations.
        if ((e.ctrlKey || e.metaKey) && (e.key === '+' || e.key === '=')) { e.preventDefault(); bumpFontScale(FONT_STEP); return; }
        if ((e.ctrlKey || e.metaKey) && (e.key === '-' || e.key === '_')) { e.preventDefault(); bumpFontScale(-FONT_STEP); return; }
        trapTab(e);
    }

    function announce(msg) {
        if (!els.live) return;
        els.live.textContent = '';
        // Force re-announce by toggling content on next tick.
        setTimeout(function () { els.live.textContent = msg; }, 30);
    }


    async function open(ref) {
        if (!ref || !ref.bookId || !ref.chapter) return;
        // Caller may pass setlistContext to switch the prev/next chevrons
        // into "advance the setlist" mode (vs. the default chapter nav).
        setlistContext = ref.setlistContext || null;
        try { books = books.length ? books : await getBooks(); }
        catch (_) {
            // Books unavailable — open the overlay anyway with an error.
            books = [];
        }
        booksById = Object.create(null);
        books.forEach(function (b) { booksById[b.id] = b; });

        if (els.bookSelect && !els.bookSelect.options.length && books.length) populateBookSelect();

        lastFocus = document.activeElement;
        els.overlay.hidden = false;
        document.body.classList.add('bible-overlay-open');
        applyFontScale(readFontScale());
        applyCompact(readCompact());

        await navigate(ref);
        // Move focus to the close button so Esc/Tab cycle from a known anchor.
        if (els.closeBtn) els.closeBtn.focus({ preventScroll: true });
    }

    function close() {
        els.overlay.hidden = true;
        document.body.classList.remove('bible-overlay-open');
        if (lastFocus && typeof lastFocus.focus === 'function') {
            lastFocus.focus({ preventScroll: true });
        }
    }

    // ---------- wire up ----------
    document.addEventListener('DOMContentLoaded', function () {
        cacheDom();
        if (!els.overlay) return;

        const trigger = document.getElementById('bibleTrigger');
        if (trigger) {
            trigger.addEventListener('click', function () { open(DEFAULT_REFERENCE); });
        }

        els.closeBtn.addEventListener('click', close);
        els.overlay.addEventListener('click', function (e) {
            // Click on the dim backdrop (outside the sheet) closes.
            if (e.target === els.overlay) close();
        });
        els.prevBtn.addEventListener('click', gotoPrev);
        els.nextBtn.addEventListener('click', gotoNext);
        els.fontMinus.addEventListener('click', function () { bumpFontScale(-FONT_STEP); });
        els.fontPlus.addEventListener('click', function () { bumpFontScale(FONT_STEP); });
        if (els.compactBtn) els.compactBtn.addEventListener('click', toggleCompact);
        if (els.resetBtn)   els.resetBtn.addEventListener('click', resetTextSettings);

        els.bookSelect.addEventListener('change', function () {
            const book = booksById[els.bookSelect.value];
            if (book) navigate({ bookId: book.id, chapter: 1 });
        });
        els.chSelect.addEventListener('change', function () {
            if (!current) return;
            navigate({ bookId: current.book.id, chapter: parseInt(els.chSelect.value, 10) || 1 });
        });
        els.vSelect.addEventListener('change', function () {
            if (!current) return;
            const v = parseInt(els.vSelect.value, 10) || 1;
            current.verse = v;
            const target = els.chapterEl.querySelector('#v' + v);
            if (target) {
                const prev = els.chapterEl.querySelector('[aria-current="true"]');
                if (prev) prev.removeAttribute('aria-current');
                target.setAttribute('aria-current', 'true');
                const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
                target.scrollIntoView({ block: 'center', behavior: reduced ? 'auto' : 'smooth' });
            }
        });

        // Click a verse to toggle bold selection. Single-select: tapping
        // another verse moves the bold from the previous one.
        els.chapterEl.addEventListener('click', function (e) {
            const verseEl = e.target.closest('.bible-overlay__verse');
            if (!verseEl) return;
            const prev = els.chapterEl.querySelector('.bible-overlay__verse--selected');
            if (prev && prev !== verseEl) prev.classList.remove('bible-overlay__verse--selected');
            verseEl.classList.toggle('bible-overlay__verse--selected');
        });

        els.searchInput.addEventListener('input', debouncedSearch);
        els.results.addEventListener('click', function (e) {
            const btn = e.target.closest('.bible-modal-result');
            if (!btn) return;
            const bookId = btn.getAttribute('data-book');
            const chapter = parseInt(btn.getAttribute('data-chapter'), 10);
            const verse = parseInt(btn.getAttribute('data-verse'), 10);
            els.searchInput.value = '';
            renderInlineResults([]);
            navigate({ bookId: bookId, chapter: chapter, verse: verse });
        });

        document.addEventListener('keydown', onKeyDown);

        // After a Render cold-start the chorus side's probe loop fires
        // chap2:api-recovered; if the overlay is open and was showing an
        // error / wake-up notice, retry the last chapter load.
        document.addEventListener('chap2:api-recovered', function () {
            if (!els.overlay.hidden && lastNavRef) navigate(lastNavRef);
        });
    });

    window.bibleOverlay = { open: open, close: close };
})();
