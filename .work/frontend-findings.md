# Frontend Findings

**Last Updated:** 2026-03-28
**Review Session:** FE-REVIEW-2026-03-28
**Framework:** ASP.NET Razor Pages + Vanilla JavaScript (no SPA framework)
**Agent Depth Used:** 2 (master orchestrator performed direct review with virtual sub-agent analysis)

## Summary Dashboard

| Category | Critical | Major | Minor | High Impact | Status |
|----------|----------|-------|-------|-------------|--------|
| Security | 3 | 3 | 1 | 5 | FAIL |
| Accessibility | 2 | 4 | 3 | 4 | FAIL |
| Performance | 0 | 3 | 4 | 2 | WARN |
| Architecture | 0 | 5 | 3 | 3 | WARN |
| Code Quality | 0 | 4 | 5 | 2 | WARN |
| Test Coverage | 1 | 1 | 0 | 2 | FAIL |
| UX | 0 | 3 | 4 | 3 | WARN |
| Naming/Conventions | 0 | 1 | 3 | 0 | WARN |
| **Total** | **6** | **24** | **23** | **21** | **FAIL** |

## Priority Matrix

Issues are prioritized by combining Technical Severity x Business Impact:

| | High Impact | Medium Impact | Low Impact |
|---|-------------|---------------|------------|
| **Critical** | P0 - Immediate | P1 - Urgent | P2 - High |
| **Major** | P1 - Urgent | P2 - High | P3 - Medium |
| **Minor** | P2 - High | P3 - Medium | P4 - Low |

---

## Critical Issues

### SEC-001: XSS via innerHTML in Search Result Highlighting
**Source Agent:** 02-01-02-FE-code-reviewer / 02-01-06-FE-security-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-ui.js:322-327`
**Category:** Security
**Technical Severity:** Critical
**Business Impact:** High
**Priority:** P0
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** `highlightSearchTerm` method builds regex from unsanitized user input and injects via `.replace()` into HTML that is set via `innerHTML`. The `searchTerm` parameter comes directly from user input and is embedded in a regex without escaping.
```javascript
highlightSearchTerm(text, searchTerm) {
    const regex = new RegExp(`(${searchTerm})`, 'gi');
    return text.replace(regex, '<mark>$1</mark>');
}
```
**Expected State:** Escape regex special characters in searchTerm, and sanitize text before highlight insertion. Use `textContent`-based approaches or DOM APIs instead of raw innerHTML with user data.
**Impact:** Attacker can craft search terms containing regex injection or HTML tags, leading to DOM-based XSS. Users could have sessions hijacked or credentials stolen.
**Validation:** Enter `<img src=x onerror=alert(1)>` as search term and verify it is escaped.
**Dependencies:** None
**Status:** Open

### SEC-002: XSS via innerHTML in Notification Messages
**Source Agent:** 02-01-06-FE-security-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/utils.js:47-52`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/form.js:319`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/delete-modal.js:113`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/setlist.js:354-356`
**Category:** Security
**Technical Severity:** Critical
**Business Impact:** High
**Priority:** P0
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** Multiple `showNotification()` functions use `innerHTML` to render `message` parameter. If any message originates from or includes user-controlled data (e.g., chorus name from API response), this is an XSS vector.
**Expected State:** Use `textContent` for message text, or sanitize before insertion.
**Impact:** Potential stored XSS if chorus names containing script tags are displayed in notifications.
**Validation:** Create a chorus with name `<img src=x onerror=alert(1)>` and trigger notification display.
**Dependencies:** None
**Status:** Open

### SEC-003: XSS via innerHTML in System Restart Dialog
**Source Agent:** 02-01-06-FE-security-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/system-restart.js:180,210`
**Category:** Security
**Technical Severity:** Critical
**Business Impact:** High
**Priority:** P0
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** `showSuccessDialog` and `showErrorDialog` methods inject `message` and `error` parameters directly into innerHTML. These values come from API responses.
```javascript
dialog.innerHTML = `...${message}...`;  // line 180
dialog.innerHTML = `...${error}...`;    // line 210
```
**Expected State:** Use DOM APIs or sanitize all server-provided strings before innerHTML injection.
**Impact:** If API returns malicious content (e.g., via MITM), XSS execution is possible on an admin-level feature.
**Validation:** Mock API response with `<script>alert(1)</script>` and verify output is escaped.
**Dependencies:** None
**Status:** Open

### A11Y-001: Form Inputs Missing Explicit Label Associations
**Source Agent:** 02-01-04-FE-accessibility-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/Views/Home/Index.cshtml:43-47`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-ui.js:37-40`
**Category:** Accessibility
**Technical Severity:** Critical
**Business Impact:** High
**Priority:** P0
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** The main search input (`#searchInput`) lacks an associated `<label>` element. While `aria-label` is added dynamically in `site.js:93`, the AI search input and other dynamically created inputs have no label at all. Screen readers cannot reliably identify these inputs. WCAG 4.1.2 and 3.3.2 violations.
**Expected State:** Every `<input>` and `<select>` element should have either an associated `<label for="id">` element or a visible label with proper `aria-labelledby`.
**Validation:** Test with VoiceOver/NVDA to confirm all inputs are announced with descriptive labels.
**Dependencies:** None
**Status:** Open

### A11Y-002: Interactive Elements Not Keyboard Accessible
**Source Agent:** 02-01-04-FE-accessibility-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-ui.js:253-265`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/setlist.js:145-174`
**Category:** Accessibility
**Technical Severity:** Critical
**Business Impact:** High
**Priority:** P0
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** Table rows with click handlers (`result-row`) are not keyboard-focusable. No `tabindex` or `role` attributes. Setlist items use inline `onclick` handlers on buttons but the drag-and-drop reordering has no keyboard alternative. WCAG 2.1.1 violation.
**Expected State:** Add `tabindex="0"` and `role="row"` to clickable table rows. Add `onkeydown` handlers for Enter/Space. Provide keyboard-accessible alternative to drag-and-drop reordering.
**Validation:** Navigate entire application using only keyboard. Verify all interactive elements are reachable and operable.
**Dependencies:** None
**Status:** Open

### TEST-001: Zero Frontend Test Coverage
**Source Agent:** 02-01-03-FE-test-coverage-reviewer (depth 2)
**File:** Entire frontend codebase
**Category:** Test Coverage
**Technical Severity:** Critical
**Business Impact:** High
**Priority:** P0
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** There are zero test files for the frontend. No unit tests, no integration tests, no end-to-end tests. No test runner configured (no Jest, Vitest, Playwright, or Cypress). 15+ JavaScript files with complex search, navigation, and CRUD logic are entirely untested.
**Expected State:** At minimum, unit tests for critical paths: search service, XSS escaping utility, form validation, chorus display navigation. E2E tests for search flow, CRUD operations, and setlist management.
**Validation:** Test suite exists and passes in CI.
**Dependencies:** None
**Status:** Open

---

## Major Issues

### SEC-004: Potential XSS via innerHTML in Setlist Display
**Source Agent:** 02-01-06-FE-security-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/setlist.js:145-174`
**Category:** Security
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** Setlist item rendering uses template literals with `chorus.name`, `chorus.key`, and `chorus.type` directly in innerHTML without escaping. These values come from sessionStorage/API.
**Expected State:** Use `utils.escapeHtml()` for all user-derived values in template literals.
**Validation:** Add chorus with HTML-containing name to setlist and verify it is escaped.
**Dependencies:** None
**Status:** Open

### SEC-005: Missing CSRF Token on JSON API Calls
**Source Agent:** 02-01-06-FE-security-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/Views/Home/Edit.cshtml:398-404` (saveCurrentChorus), `CHAP2.UI/CHAP2.WebPortal/Views/Home/Edit.cshtml:436-441` (deleteCurrentChorus)
**Category:** Security
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** The `saveCurrentChorus()` and `deleteCurrentChorus()` functions in the Edit page inline script make POST requests to `/Home/SaveChorusJson` and `/Home/DeleteChorusJson` without including a `RequestVerificationToken` header, while `delete-modal.js` does include it.
**Expected State:** All state-changing requests must include anti-forgery token.
**Validation:** Attempt CSRF attack from external page and verify it is blocked.
**Dependencies:** None
**Status:** Open

### SEC-006: Debug/Test Code Shipped to Production
**Source Agent:** 02-01-06-FE-security-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/debug-crud.js`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/test-enum-conversion.js`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/system-restart.js`
**Category:** Security
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** Debug and test scripts are loaded in the production layout (`_Layout.cshtml:27` loads `debug-crud.js`). `system-restart.js` adds a restart button on localhost. `debug-crud.js` exposes API base URLs and connectivity testing. `CrudDebugger` class contains hardcoded internal hostnames.
**Expected State:** Debug/test scripts should not be loaded in production. Use build-time environment checks or conditional rendering.
**Validation:** Check production bundle does not include debug scripts.
**Dependencies:** None
**Status:** Open

### A11Y-003: No Skip Navigation Link
**Source Agent:** 02-01-04-FE-accessibility-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Views/Shared/_Layout.cshtml`
**Category:** Accessibility
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** No skip-to-content link exists. WCAG 2.4.1 violation. Screen reader and keyboard users must tab through all navigation elements to reach main content.
**Expected State:** Add `<a href="#main-content" class="skip-link">Skip to main content</a>` as first focusable element, with corresponding `id="main-content"` on the main content area.
**Validation:** Tab from page load and verify first focus goes to skip link.
**Dependencies:** None
**Status:** Open

### A11Y-004: Modal Focus Trap Missing
**Source Agent:** 02-01-04-FE-accessibility-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/site.js:105-118`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/delete-modal.js:15-27`
**Category:** Accessibility
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** Modals (detail modal, delete modal, save confirmation modal) do not trap focus inside when open. Users can tab behind the modal to background content. Modals lack `role="dialog"` and `aria-modal="true"`.
**Expected State:** Implement focus trap, add `role="dialog"`, `aria-modal="true"`, and `aria-labelledby` to all modal overlays.
**Validation:** Open modal, verify Tab cycles only through modal elements.
**Dependencies:** None
**Status:** Open

### A11Y-005: Missing ARIA Live Regions for Dynamic Content
**Source Agent:** 02-01-04-FE-accessibility-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-ui.js:187-214`, `CHAP2.UI/CHAP2.WebPortal/Views/Home/Index.cshtml:201-244`
**Category:** Accessibility
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** Search results, loading states, and error messages are updated dynamically but lack `aria-live` regions. Screen readers do not announce when results appear or errors occur.
**Expected State:** Add `aria-live="polite"` to results container, `aria-live="assertive"` to error containers.
**Validation:** Perform search with screen reader active and verify results announcement.
**Dependencies:** None
**Status:** Open

### A11Y-006: Color-Only Status Indicators
**Source Agent:** 02-01-04-FE-accessibility-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Views/Home/Index.cshtml:18`
**Category:** Accessibility
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** The connection status indicator (`<i class="fas fa-circle status-indicator">`) relies solely on color (green/red) to convey connectivity status. WCAG 1.4.1 violation.
**Expected State:** Add text label or icon variation (checkmark vs. X) alongside color.
**Validation:** View with color blindness simulation and verify status is distinguishable.
**Dependencies:** None
**Status:** Open

### PERF-001: Excessive Console Logging in Production
**Source Agent:** 02-01-05-FE-performance-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/form.js` (40+ console.log calls), `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-v2.js`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/ai-search.js`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-integration.js`
**Category:** Performance
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** Verbose `console.log` statements throughout all JavaScript files. `form.js` alone has 40+ debug logging statements including logging of all form data on submission. This exposes internal state to anyone opening browser DevTools and adds minor performance overhead.
**Expected State:** Remove or gate behind `DEBUG` flag all console.log statements in production.
**Validation:** Open DevTools console and verify minimal output during normal usage.
**Dependencies:** None
**Status:** Open

### PERF-002: Duplicate JavaScript Files Loaded
**Source Agent:** 02-01-05-FE-performance-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-v2.js`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-working.js`
**Category:** Performance
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** `search-v2.js` and `search-working.js` contain nearly identical code (both declare same global variables `searchResults`, `isSearching`, `currentSearchTerm`, etc. and same `initializeSearch` function). Both guard against double-init with `window.searchInitialized` but the redundant file is still downloaded. Additionally, `search-service.js` and `search-ui.js` (used by CleanSearch.cshtml) duplicate functionality from `search-v2.js`.
**Expected State:** Consolidate into a single search module. Remove unused/duplicate files.
**Validation:** Remove duplicate files and verify search still works on all pages.
**Dependencies:** None
**Status:** Open

### PERF-003: No JavaScript Minification or Bundling
**Source Agent:** 02-01-05-FE-performance-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Views/Shared/_Layout.cshtml:21-27`
**Category:** Performance
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** Layout loads 7 individual unminified JS files. Index.cshtml adds 3 more. Each is a separate HTTP request with cache-busting timestamps (`?v=@DateTime.Now.Ticks`). No bundling or minification configured.
**Expected State:** Use ASP.NET bundling/minification or a build step (webpack, Vite) to bundle and minify JS/CSS.
**Validation:** Check network tab for reduced number and size of JS requests.
**Dependencies:** None
**Status:** Open

### ARCH-001: Global State Pollution
**Source Agent:** 02-01-01-FE-architecture-reviewer (depth 2)
**Files:** All JS files
**Category:** Architecture
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** 02-02-01-FE-component-fix-agent
**Current State:** Nearly every JS file attaches objects/functions to `window`: `window.utils`, `window.SearchService`, `window.SearchUI`, `window.openModal`, `window.closeModal`, `window.setlistManager`, `window.settingsManager`, `window.systemRestart`, `window.currentChorusList`, `window.aiSearch`, `window.chorusEditNav`, etc. This creates a fragile dependency graph, naming collision risk, and makes code untestable.
**Expected State:** Use ES modules (`import`/`export`) or an IIFE/namespace pattern to encapsulate code. Minimize global surface area.
**Validation:** Check `window` object in DevTools for reduced globals.
**Dependencies:** PERF-003 (bundling enables modules)
**Status:** Open

### ARCH-002: Duplicated showNotification Function
**Source Agent:** 02-01-01-FE-architecture-reviewer (depth 2)
**Files:** `utils.js:44-69`, `form.js:311-337`, `delete-modal.js:105-131`, `setlist.js:350-386`, `settings.js:620-653`
**Category:** Architecture
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Fixable:** Yes
**Fix Agent:** 02-02-01-FE-component-fix-agent
**Current State:** Five separate implementations of `showNotification()` exist across different files with slightly different signatures and styling. This violates DRY and makes consistent notification behavior impossible.
**Expected State:** Single notification utility in `utils.js` used by all files.
**Validation:** Search codebase for `showNotification` and confirm single definition.
**Dependencies:** ARCH-001
**Status:** Open

### ARCH-003: Mixed Concerns in View Layer (MVVM Violation)
**Source Agent:** 02-01-07-FE-mvvm-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/Views/Home/Index.cshtml:489-536` (inline JS), `CHAP2.UI/CHAP2.WebPortal/Views/Home/Edit.cshtml:196-483` (inline JS)
**Category:** Architecture
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** 02-02-01-FE-component-fix-agent
**Current State:** Razor views contain extensive inline `<script>` blocks with business logic (CRUD operations, navigation state management, API calls). Edit.cshtml has ~290 lines of inline JavaScript including `saveCurrentChorus()` and `deleteCurrentChorus()` with full API interaction logic. This violates separation of concerns.
**Expected State:** Extract all inline JavaScript into dedicated JS files. Views should only contain initialization calls.
**Validation:** Verify no `<script>` blocks in .cshtml files contain business logic.
**Dependencies:** None
**Status:** Open

### ARCH-004: Inconsistent Page Architecture
**Source Agent:** 02-01-01-FE-architecture-reviewer (depth 2)
**Files:** `Edit.cshtml`, `Create.cshtml`, `ChorusDisplay.cshtml` vs `Index.cshtml`
**Category:** Architecture
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Fixable:** Partial
**Fix Agent:** Manual
**Current State:** Index.cshtml uses `_Layout.cshtml`, but Edit, Create, and ChorusDisplay pages set `Layout = null` and declare their own full HTML documents with duplicate `<head>` content, font/CSS imports, and different script loading. This means changes to shared resources must be replicated across 4+ files.
**Expected State:** Use shared layout(s) with section overrides. At minimum, extract shared `<head>` content into a partial.
**Validation:** Change a shared CSS import and verify all pages reflect the change.
**Dependencies:** None
**Status:** Open

### ARCH-005: Enum Mapping Duplicated Across Files
**Source Agent:** 02-01-01-FE-architecture-reviewer (depth 2)
**Files:** `search-ui.js:334-355`, `search-v2.js` (similar maps), `test-enum-conversion.js:32-46`, `search-integration.js:158-215`
**Category:** Architecture
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Fixable:** Yes
**Fix Agent:** 02-02-01-FE-component-fix-agent
**Current State:** Musical key, chorus type, and time signature enum-to-display mappings are duplicated in 4+ files. If a new enum value is added, all copies must be updated.
**Expected State:** Single source of truth for enum mappings in `utils.js` or a dedicated `enums.js`.
**Validation:** Search for `getKeyDisplay` and confirm single definition.
**Dependencies:** None
**Status:** Open

### CODE-001: Ctrl+C Keyboard Shortcut Hijacked
**Source Agent:** 02-01-02-FE-code-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/detail.js:124-127`
**Category:** Code Quality
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** `detail.js` intercepts `Ctrl+C` globally and calls `copyLyrics()` instead of allowing the default browser copy behavior. This prevents users from copying any selected text on the detail page.
```javascript
if ((event.ctrlKey || event.metaKey) && event.key === 'c') {
    event.preventDefault();
    copyLyrics();
}
```
**Expected State:** Remove the Ctrl+C override or only trigger when no text is selected.
**Validation:** Select text on detail page and verify Ctrl+C copies selected text.
**Dependencies:** None
**Status:** Open

### CODE-002: Escape Key Closes Window Unexpectedly
**Source Agent:** 02-01-02-FE-code-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/detail.js:129-131`
**Category:** Code Quality
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** Pressing Escape on the detail page calls `window.close()`, which can close the browser tab/window without confirmation. This is destructive and unexpected.
**Expected State:** Show a confirmation dialog or remove the Escape handler for window close.
**Validation:** Press Escape on detail page and verify window does not close without warning.
**Dependencies:** None
**Status:** Open

### CODE-003: Concurrent Search Race Condition
**Source Agent:** 02-01-02-FE-code-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-service.js:62-64`
**Category:** Code Quality
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** `SearchService._performSearch` throws if `isSearching` is already true, but `debouncedSearch` does not handle this rejection. Additionally, using a boolean flag instead of `AbortController` means requests cannot actually be cancelled.
**Expected State:** Use `AbortController` to cancel in-flight requests when new search starts.
**Validation:** Type rapidly in search and verify no "Search already in progress" errors in console.
**Dependencies:** None
**Status:** Open

### CODE-004: localStorage Used for Form Draft Without Size Checks
**Source Agent:** 02-01-02-FE-code-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/form.js:163-217`
**Category:** Code Quality
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** `setupAutoSave()` stores form data in `localStorage` without checking quota limits. Large chorus texts could exceed localStorage quota (typically 5-10MB per origin). No error handling for `QuotaExceededError`.
**Expected State:** Wrap `localStorage.setItem` in try/catch for QuotaExceededError. Consider truncating or using IndexedDB for large data.
**Validation:** Create a very large chorus text and verify auto-save handles gracefully.
**Dependencies:** None
**Status:** Open

### UX-001: No Loading Feedback for AI Search
**Source Agent:** 02-01-08-FE-ux-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Views/Home/Index.cshtml:97-198`
**Category:** UX
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** While regular search has a loading spinner, the AI search button shows "Processing..." via a hidden span toggle, but the search results area provides no progress indication for what can be a long-running AI operation (several seconds). Nielsen H01 violation (Visibility of System Status).
**Expected State:** Show progressive status updates during AI search (analyzing query -> searching -> processing results).
**Validation:** Perform AI search and verify clear progress feedback throughout.
**Dependencies:** None
**Status:** Open

### UX-002: Delete Confirmation Too Easy to Trigger
**Source Agent:** 02-01-08-FE-ux-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Views/Home/Edit.cshtml:427-432`
**Category:** UX
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** In mass edit mode, the delete button uses a simple `confirm()` dialog. The `confirmDeleteWithDoubleCheck` feature in `delete-modal.js` exists but is never used. Given that mass edit iterates through ALL choruses, accidental deletion is a real risk with no undo capability. Nielsen H03 and H05 violations.
**Expected State:** Use the custom delete modal with the double-check confirmation pattern for all destructive actions. Consider soft-delete with undo.
**Validation:** Try to delete a chorus and verify multi-step confirmation is required.
**Dependencies:** None
**Status:** Open

### UX-003: No Empty State for Initial Page Load
**Source Agent:** 02-01-08-FE-ux-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Views/Home/Index.cshtml`
**Category:** UX
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** When the main search page loads, the results area is completely blank. There is no guidance, recent choruses, or browse-all option visible. Search tips are at the very bottom. New users may not understand what to do. Nielsen H10 violation.
**Expected State:** Show featured/recent choruses, or a prominent call-to-action on initial load.
**Validation:** Load page and verify helpful initial state is shown.
**Dependencies:** None
**Status:** Open

### TEST-002: Critical Business Logic Untested
**Source Agent:** 02-01-03-FE-test-coverage-reviewer (depth 2)
**Files:** `utils.js` (escapeHtml, isValidMusicalKey), `search-service.js` (search logic), `form.js` (validation), `chorus-display.js` (pagination/navigation)
**Category:** Test Coverage
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** Critical utility functions like `escapeHtml` (security-critical), `isValidMusicalKey` (data integrity), form validation logic, and chorus page navigation/pagination are completely untested. Any regression in `escapeHtml` would create XSS vulnerabilities.
**Expected State:** Unit tests for all utility functions, integration tests for search flow, and E2E tests for CRUD operations.
**Validation:** Test suite with >80% coverage on utility functions.
**Dependencies:** TEST-001
**Status:** Open

---

## Minor Issues

| ID | Source | File | Priority | Issue | Recommendation |
|----|--------|------|----------|-------|----------------|
| PERF-004 | performance-reviewer | `_Layout.cshtml:13-14` | P3 | External CDN resources (Google Fonts, cdnjs) loaded synchronously block rendering | Add `font-display: swap` and consider self-hosting fonts |
| PERF-005 | performance-reviewer | `site.js:29-43` | P4 | Tooltip implementation creates/removes DOM elements on every hover | Use a single tooltip element repositioned on hover |
| PERF-006 | performance-reviewer | `chorus-display.js` | P3 | Large file (~800+ lines) loaded even on pages that don't use chorus display | Load conditionally only on ChorusDisplay page |
| PERF-007 | performance-reviewer | `settings.js:66-217` | P4 | Settings modal HTML (~150 lines) created on every page load even if never opened | Lazy-create modal only when settings button clicked |
| CODE-005 | code-reviewer | `form.js:340-357` | P3 | Escape key handler fires `cancelButton.click()` which could cause navigation | Guard against unintended navigation; only close modals |
| CODE-006 | code-reviewer | `form.js:383-398` | P4 | Smart placeholder animation uses setInterval without cleanup | Clear interval reliably on blur/input |
| CODE-007 | code-reviewer | `detail.js:226-240` | P3 | `highlightSearchTerm` in detail.js uses RegExp from URL params without escaping | Escape regex special characters from URL params |
| CODE-008 | code-reviewer | `search-ui.js:316-319` | P4 | `exportResults` function is a stub (`console.log` only) | Implement or remove the export button |
| CODE-009 | code-reviewer | `ai-search.js:79-87` | P4 | Recursive `setTimeout` retry for init (up to 10 retries at 500ms) is fragile | Use MutationObserver or event-based initialization |
| A11Y-007 | accessibility-reviewer | `Index.cshtml:278-290` | P3 | Detail modal lacks `aria-labelledby` and `aria-describedby` | Add proper ARIA attributes to modal |
| A11Y-008 | accessibility-reviewer | `search-ui.js:226-251` | P3 | Search result action buttons use icon-only buttons without `aria-label` | Add `aria-label` to each icon button |
| A11Y-009 | accessibility-reviewer | All CSS files | P4 | No `prefers-reduced-motion` media query to disable animations | Add `@media (prefers-reduced-motion: reduce)` rules |
| UX-004 | ux-reviewer | `form.js:168-177` | P3 | Auto-save notification ("Draft saved") fires every 2 seconds of typing | Show auto-save status passively (icon only), not toast notification |
| UX-005 | ux-reviewer | `setlist.js:85` | P3 | `clearAll` uses native `confirm()` instead of custom modal | Use consistent custom modal for confirmations |
| UX-006 | ux-reviewer | `settings.js:332-390` | P4 | Mass edit mode provides no progress indicator (e.g., "3 of 150 complete") beyond position counter | Add progress bar for mass edit workflow |
| UX-007 | ux-reviewer | Index.cshtml | P4 | Search tips section is static and always visible, adding clutter | Collapse by default; show on first visit or on demand |
| NAMING-001 | naming-reviewer | Multiple files | P3 | Inconsistent naming: `searchResults` (global var) vs `SearchService` (class) vs `searchService` (instance); `clearBtn` vs `aiClearButton` vs `aiClearBtn` | Establish and document naming convention |
| NAMING-002 | naming-reviewer | `search-v2.js`, `search-working.js` | P4 | File names like `-v2` and `-working` suggest iteration artifacts, not production names | Rename to final names (e.g., `search.js`) |
| NAMING-003 | naming-reviewer | CSS files | P4 | Mix of BEM-like (`.search-input-container`), flat (`.loading`), and camelCase in JS (`.resultsHeader`) class naming | Adopt consistent CSS naming methodology |
| NAMING-004 | naming-reviewer | `detail.js` functions | P4 | Functions like `addPrintFunctionality`, `addCopyFunctionality` use verb prefix inconsistently vs `printChorus`, `copyLyrics` | Standardize function naming pattern |

---

## Accessibility Compliance (WCAG 2.1)

| Level | Criteria | Status | Violations |
|-------|----------|--------|------------|
| A | 1.1.1 Non-text Content | WARN | Icon-only buttons without alt text (A11Y-008) |
| A | 1.3.1 Info and Relationships | WARN | Some semantic issues |
| A | 1.4.1 Use of Color | FAIL | Status indicator color-only (A11Y-006) |
| A | 2.1.1 Keyboard | FAIL | Non-keyboard-accessible elements (A11Y-002) |
| A | 2.4.1 Bypass Blocks | FAIL | No skip link (A11Y-003) |
| A | 3.3.2 Labels or Instructions | FAIL | Missing form labels (A11Y-001) |
| A | 4.1.2 Name, Role, Value | FAIL | Missing ARIA on modals and dynamic content (A11Y-004, A11Y-005) |
| AA | 1.4.3 Contrast | PASS | Good contrast ratios in CSS variables |
| AA | 2.4.7 Focus Visible | WARN | Custom focus styles exist but not comprehensive |

## Performance Metrics

| Metric | Target | Current (Estimated) | Status |
|--------|--------|---------------------|--------|
| LCP | < 2.5s | ~2-3s (unminified, external fonts) | WARN |
| FID | < 100ms | ~50-100ms | PASS |
| CLS | < 0.1 | ~0.05 (no images without dimensions) | PASS |
| Bundle Size (JS) | < 200KB | ~300KB+ (15 unminified JS files) | WARN |
| First Load JS | < 100KB | ~150KB+ (7 scripts in layout) | WARN |

## Architecture Standards

### MVVM Compliance
| Layer | Files | Compliant | Violations |
|-------|-------|-----------|------------|
| View (Razor) | 13 | Partial | 2 (inline business logic) |
| ViewModel (JS) | N/A | N/A | No formal ViewModel layer exists |
| Model/Service (JS) | 3 | Partial | Mixed with view logic |

### SOLID Principles
| Principle | Status | Violations |
|-----------|--------|------------|
| Single Responsibility | WARN | 5 (monolithic files mixing concerns) |
| Open/Closed | WARN | 3 (hard-coded enum maps, no extension points) |
| Dependency Inversion | FAIL | 8 (direct global dependencies everywhere) |

### One Type Per File
| Check | Status | Violations |
|-------|--------|------------|
| Single class/module per file | PASS | 0 |
| Filename matches export | WARN | 2 (search-v2, search-working) |
| No mixed concerns | WARN | 3 (inline scripts in views) |

## Agent Tree

```
Depth 0: 02-FE-master-orchestrator (THIS AGENT)
    Depth 1: 02-01-01-FE-architecture-reviewer (virtual) -> ARCH-001 through ARCH-005
    Depth 1: 02-01-02-FE-code-reviewer (virtual) -> CODE-001 through CODE-009, SEC-001 through SEC-003
    Depth 1: 02-01-03-FE-test-coverage-reviewer (virtual) -> TEST-001, TEST-002
    Depth 1: 02-01-04-FE-accessibility-reviewer (virtual) -> A11Y-001 through A11Y-009
    Depth 1: 02-01-05-FE-performance-reviewer (virtual) -> PERF-001 through PERF-007
    Depth 1: 02-01-06-FE-security-reviewer (virtual) -> SEC-001 through SEC-006
    Depth 1: 02-01-07-FE-mvvm-reviewer (virtual) -> ARCH-003
    Depth 1: 02-01-08-FE-ux-reviewer (virtual) -> UX-001 through UX-007
    Depth 1: 02-01-09-FE-naming-convention-reviewer (virtual) -> NAMING-001 through NAMING-004
```

## Recommendations

### Critical (Immediate)
1. **SEC-001, SEC-002, SEC-003**: Fix all innerHTML XSS vulnerabilities across search highlighting, notifications, and system restart dialogs
2. **A11Y-001, A11Y-002**: Add proper form labels and keyboard accessibility to all interactive elements
3. **TEST-001**: Set up a test framework (Jest/Vitest) and write tests for security-critical utilities

### Major (Short-term)
1. **SEC-005**: Add CSRF tokens to all state-changing API calls
2. **SEC-006**: Remove debug/test scripts from production layout
3. **CODE-001, CODE-002**: Fix dangerous keyboard shortcut overrides
4. **ARCH-003**: Extract inline JavaScript from Razor views
5. **A11Y-003, A11Y-004, A11Y-005**: Add skip links, focus traps, and ARIA live regions
6. **PERF-001, PERF-002**: Remove console.log spam and consolidate duplicate files

### Minor (When convenient)
1. **PERF-003**: Implement JS/CSS bundling and minification
2. **ARCH-001**: Migrate from global window pollution to modules
3. **ARCH-002, ARCH-005**: Consolidate duplicated code (notifications, enum maps)
4. **UX improvements**: Better empty states, loading feedback, confirmation patterns
