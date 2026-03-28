# Frontend Findings (Re-Review)

**Last Updated:** 2026-03-28 (Re-Review after fixes)
**Review Session:** FE-REVIEW-2026-03-28-R2
**Framework:** ASP.NET Razor Pages + Vanilla JavaScript (no SPA framework)
**Agent Depth Used:** 2 (master orchestrator with virtual sub-agent analysis)
**Previous Review:** 53 findings (6 Critical, 24 Major, 23 Minor)

## Re-Review Summary

| Status | Count | Details |
|--------|-------|---------|
| RESOLVED | 16 | Fixes verified and working |
| OPEN (unchanged) | 29 | Still present, not addressed |
| PARTIALLY RESOLVED | 5 | Fix applied but incomplete |
| NEW (introduced by fixes) | 3 | Regressions or new patterns found |
| **Total Remaining** | **37** | 2 Critical, 18 Major, 17 Minor |

## Summary Dashboard

| Category | Critical | Major | Minor | High Impact | Status |
|----------|----------|-------|-------|-------------|--------|
| Security | 0 | 3 | 0 | 2 | WARN |
| Accessibility | 1 | 3 | 2 | 3 | FAIL |
| Performance | 0 | 2 | 4 | 1 | WARN |
| Architecture | 0 | 5 | 3 | 3 | WARN |
| Code Quality | 0 | 2 | 4 | 1 | WARN |
| Test Coverage | 1 | 1 | 0 | 2 | FAIL |
| UX | 0 | 2 | 4 | 2 | WARN |
| Naming/Conventions | 0 | 0 | 3 | 0 | WARN |
| **Total** | **2** | **18** | **20** | **14** | **FAIL** |

**Improvement from previous review:** 6 Critical -> 2 Critical, 24 Major -> 18 Major, 23 Minor -> 20 Minor (net -13)

## Priority Matrix

| | High Impact | Medium Impact | Low Impact |
|---|-------------|---------------|------------|
| **Critical** | P0 - Immediate | P1 - Urgent | P2 - High |
| **Major** | P1 - Urgent | P2 - High | P3 - Medium |
| **Minor** | P2 - High | P3 - Medium | P4 - Low |

---

## Resolution Status of Previous Critical Issues

| ID | Title | Previous | Current | Notes |
|----|-------|----------|---------|-------|
| SEC-001 | XSS via innerHTML in search highlighting | Critical | **RESOLVED** | `search-ui.js` and `search-v2.js` both escape HTML and regex chars |
| SEC-002 | XSS via innerHTML in notifications | Critical | **RESOLVED** | `utils.js`, `form.js`, `delete-modal.js`, `setlist.js` all use `textContent` |
| SEC-003 | XSS via innerHTML in system restart dialog | Critical | **RESOLVED** | `system-restart.js` uses `textContent` for API response data |
| A11Y-001 | Form inputs missing labels | Critical | **PARTIALLY RESOLVED** | Main search has `aria-label` in HTML; AI search input still lacks label |
| A11Y-002 | Interactive elements not keyboard accessible | Critical | **OPEN** | No `tabindex` or `role` on `.result-row` elements |
| TEST-001 | Zero frontend test coverage | Critical | **OPEN** | Still no test framework or test files |

---

## Critical Issues (2 remaining)

### A11Y-002: Interactive Table Rows Not Keyboard Accessible
**Source Agent:** 02-01-04-FE-accessibility-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-v2.js:298-348`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-ui.js:253-265`
**Category:** Accessibility
**Technical Severity:** Critical
**Business Impact:** High
**Priority:** P0
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** Table rows with click handlers (`result-row`) are still not keyboard-focusable. No `tabindex`, `role`, or `onkeydown` attributes. Setlist items also have no keyboard alternative to drag-and-drop. WCAG 2.1.1 violation.
**Expected State:** Add `tabindex="0"` and `role="row"` to clickable table rows. Add `onkeydown` handlers for Enter/Space. Provide keyboard-accessible alternative to drag-and-drop.
**Validation:** Navigate entire application using only keyboard. Verify all interactive elements are reachable and operable.
**Dependencies:** None
**Status:** Open (unchanged from previous review)

### TEST-001: Zero Frontend Test Coverage
**Source Agent:** 02-01-03-FE-test-coverage-reviewer (depth 2)
**File:** Entire frontend codebase
**Category:** Test Coverage
**Technical Severity:** Critical
**Business Impact:** High
**Priority:** P0
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** There are still zero test files for the frontend. No unit tests, no integration tests, no end-to-end tests. No test runner configured. 14 JavaScript files with complex search, navigation, and CRUD logic are entirely untested. The newly added `debug()` function and `escapeHtml()` utility are security-critical but untested.
**Expected State:** At minimum, unit tests for critical paths: search service, XSS escaping utility, form validation, chorus display navigation.
**Validation:** Test suite exists and passes in CI.
**Dependencies:** None
**Status:** Open (unchanged from previous review)

---

## Major Issues (18 remaining)

### SEC-004: innerHTML with Unsanitized Data in Setlist Display
**Source Agent:** 02-01-06-FE-security-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/setlist.js:145-174`
**Category:** Security
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** Setlist `refreshDisplay()` builds HTML with `chorus.name`, `chorus.key`, and `chorus.type` injected directly into template literals via innerHTML (line 152-157, 177). These values originate from API data and sessionStorage. The `showNotification` in this file was fixed to use `textContent`, but the main rendering path is still vulnerable.
**Expected State:** Use `utils.escapeHtml()` for all user-derived values in template literals, or build DOM nodes programmatically.
**Validation:** Add chorus with name `<img src=x onerror=alert(1)>` to setlist and verify it is escaped in the display.
**Dependencies:** None
**Status:** Open (unchanged)

### SEC-005: Missing CSRF Token on JSON API Calls in Edit Page
**Source Agent:** 02-01-06-FE-security-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Views/Home/Edit.cshtml:398-441`
**Category:** Security
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** The `saveCurrentChorus()` (line 398) and `deleteCurrentChorus()` (line 436) functions make POST requests without including a `RequestVerificationToken` header. The `delete-modal.js` correctly uses `getAntiForgeryToken()` but the Edit page inline script does not.
**Expected State:** All state-changing requests must include anti-forgery token.
**Validation:** Verify `RequestVerificationToken` header present in Network tab for save/delete requests.
**Dependencies:** None
**Status:** Open (unchanged)

### SEC-007: innerHTML with Unsanitized API Data in ai-search.js (NEW)
**Source Agent:** 02-01-06-FE-security-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/ai-search.js:486-498, 646, 839, 860, 1288`
**Category:** Security
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** `ai-search.js` injects `chorusName` (line 490), `result.explanation` (lines 498, 646, 839, 860), and `analysis` text (line 1288) directly into innerHTML without escaping. These values come from API responses. While inner-network, a compromised API or MITM could inject XSS. This file was not addressed in the previous fix round.
**Expected State:** Escape all API-derived text before innerHTML insertion, or use `textContent` for text-only fields.
**Validation:** Mock API response with HTML payload and verify it is escaped.
**Dependencies:** None
**Status:** NEW (not in previous review scope)

### A11Y-001: AI Search Input Missing Label (PARTIALLY RESOLVED)
**Source Agent:** 02-01-04-FE-accessibility-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Views/Home/Index.cshtml:99-100`
**Category:** Accessibility
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** The main search input now has `aria-label` directly in HTML (line 47), which resolves the primary issue. However, the AI search input (line 99-100) and dynamically created inputs in `search-integration.js` still lack labels. The `search-ui.js` also creates a search input at line 37-40 without a label element.
**Expected State:** Every `<input>` element should have an associated `<label>` or `aria-label`.
**Validation:** Test with screen reader to confirm all inputs announced correctly.
**Dependencies:** None
**Status:** Partially Resolved

### A11Y-004: Modal Focus Trap Missing
**Source Agent:** 02-01-04-FE-accessibility-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/site.js:105-118`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/delete-modal.js:15-27`
**Category:** Accessibility
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** Modals still do not trap focus. No `role="dialog"`, `aria-modal="true"`, or `aria-labelledby` attributes found on any modal element in the codebase (confirmed via grep). Users can still tab behind the modal to background content.
**Expected State:** Implement focus trap, add `role="dialog"`, `aria-modal="true"`, `aria-labelledby`.
**Validation:** Open modal, verify Tab cycles only through modal elements.
**Dependencies:** None
**Status:** Open (unchanged)

### A11Y-005: Missing ARIA Live Regions for Dynamic Content (PARTIALLY RESOLVED)
**Source Agent:** 02-01-04-FE-accessibility-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/Views/Home/Index.cshtml:203`
**Category:** Accessibility
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** A single `aria-live="polite"` was added to the results container at line 203. However, error containers, loading states, and notification toasts still lack `aria-live` regions. Screen readers will not announce errors or status changes.
**Expected State:** Add `aria-live="assertive"` to error/notification containers.
**Validation:** Trigger an error with screen reader active and verify announcement.
**Dependencies:** None
**Status:** Partially Resolved

### A11Y-006: Color-Only Status Indicators
**Source Agent:** 02-01-04-FE-accessibility-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Views/Home/Index.cshtml:18`
**Category:** Accessibility
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** The connection status indicator still relies solely on color (green/red circle icon). No text label added. WCAG 1.4.1 violation.
**Expected State:** Add text label or icon variation alongside color.
**Validation:** View with color blindness simulation.
**Dependencies:** None
**Status:** Open (unchanged)

### PERF-002: Duplicate Search Implementations Still Loaded (PARTIALLY RESOLVED)
**Source Agent:** 02-01-05-FE-performance-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-v2.js`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-service.js`, `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-ui.js`
**Category:** Performance
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** `search-working.js` was deleted (confirmed - file no longer exists). However, `search-v2.js` (used by Index), `search-service.js` + `search-ui.js` (used by CleanSearch), and `search-integration.js` still contain overlapping search logic and duplicate enum maps. Three parallel search implementations remain.
**Expected State:** Single canonical search module.
**Validation:** Remove duplicate files and verify search works on all pages.
**Dependencies:** None
**Status:** Partially Resolved

### PERF-003: No JavaScript Minification or Bundling
**Source Agent:** 02-01-05-FE-performance-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Views/Shared/_Layout.cshtml:24-32`
**Category:** Performance
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Fixable:** Yes
**Fix Agent:** Manual
**Current State:** Layout still loads 7 individual unminified JS files. Cache-busting uses `DateTime.Now.Ticks` (non-deterministic) and even `Guid.NewGuid()` for search-v2.js (line 26), which prevents any client-side caching.
**Expected State:** Use ASP.NET bundling/minification. Replace timestamp cache-busting with `asp-append-version="true"` content hashes alone.
**Validation:** Check network tab for reduced JS requests.
**Dependencies:** None
**Status:** Open (unchanged)

### ARCH-001: Global State Pollution
**Source Agent:** 02-01-01-FE-architecture-reviewer (depth 2)
**Files:** All JS files
**Category:** Architecture
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** 02-02-01-FE-component-fix-agent
**Current State:** Still attaches many objects/functions to `window`. No change from previous review.
**Expected State:** Use ES modules or IIFE/namespace pattern.
**Dependencies:** PERF-003
**Status:** Open (unchanged)

### ARCH-002: Duplicated showNotification Function (PARTIALLY RESOLVED)
**Source Agent:** 02-01-01-FE-architecture-reviewer (depth 2)
**Files:** `utils.js:44`, `form.js:263`, `delete-modal.js:105`, `setlist.js:350`, `settings.js:620`, `chorus-display.js:1803`
**Category:** Architecture
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Fixable:** Yes
**Fix Agent:** 02-02-01-FE-component-fix-agent
**Current State:** The XSS fix correctly changed individual `showNotification` implementations to use `textContent` instead of `innerHTML`. However, 6 separate implementations still exist. The `settings.js:624` version still uses `innerHTML` with `${message}` directly. The `chorus-display.js:1807` version also still uses innerHTML with `${message}`.
**Expected State:** Single notification utility in `utils.js` used by all files.
**Validation:** Search codebase for `showNotification` and confirm single definition.
**Dependencies:** ARCH-001
**Status:** Partially Resolved (XSS fixed in most copies, but duplication remains; 2 copies still use innerHTML)

### ARCH-003: Mixed Concerns in View Layer (MVVM Violation)
**Source Agent:** 02-01-07-FE-mvvm-reviewer (depth 2)
**Files:** `CHAP2.UI/CHAP2.WebPortal/Views/Home/Edit.cshtml:196-483`
**Category:** Architecture
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** 02-02-01-FE-component-fix-agent
**Current State:** Edit.cshtml still contains ~290 lines of inline JavaScript including `saveCurrentChorus()` and `deleteCurrentChorus()` with full API interaction logic, console.log statements, and no CSRF tokens. No change from previous review.
**Expected State:** Extract inline JavaScript into dedicated JS files.
**Validation:** Verify no `<script>` blocks contain business logic in .cshtml files.
**Dependencies:** None
**Status:** Open (unchanged)

### ARCH-004: Inconsistent Page Architecture
**Source Agent:** 02-01-01-FE-architecture-reviewer (depth 2)
**Files:** `Edit.cshtml`, `Create.cshtml`, `ChorusDisplay.cshtml` vs `Index.cshtml`
**Category:** Architecture
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Current State:** No change from previous review.
**Status:** Open (unchanged)

### ARCH-005: Enum Mapping Duplicated Across Files
**Source Agent:** 02-01-01-FE-architecture-reviewer (depth 2)
**Files:** `search-ui.js:338-358`, `search-v2.js:262-295`, `search-integration.js`, `ai-search.js`
**Category:** Architecture
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Current State:** No change from previous review. 4+ copies of enum-to-display maps.
**Status:** Open (unchanged)

### CODE-003: Concurrent Search Race Condition
**Source Agent:** 02-01-02-FE-code-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-service.js:62-64`
**Category:** Code Quality
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Current State:** No change. Still uses boolean flag instead of `AbortController`.
**Status:** Open (unchanged)

### CODE-004: localStorage Used for Form Draft Without Size Checks
**Source Agent:** 02-01-02-FE-code-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/form.js:127`
**Category:** Code Quality
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Current State:** No change. No `QuotaExceededError` handling.
**Status:** Open (unchanged)

### UX-001: No Loading Feedback for AI Search
**Source Agent:** 02-01-08-FE-ux-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Views/Home/Index.cshtml:97-198`
**Category:** UX
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Current State:** No change from previous review.
**Status:** Open (unchanged)

### UX-002: Delete Confirmation Too Easy to Trigger
**Source Agent:** 02-01-08-FE-ux-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Views/Home/Edit.cshtml:427-432`
**Category:** UX
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Current State:** No change. Mass edit delete still uses `confirm()` dialog.
**Status:** Open (unchanged)

### TEST-002: Critical Business Logic Untested
**Source Agent:** 02-01-03-FE-test-coverage-reviewer (depth 2)
**Files:** `utils.js`, `search-service.js`, `form.js`, `chorus-display.js`
**Category:** Test Coverage
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Current State:** No change. Still no tests.
**Status:** Open (unchanged)

---

## Minor Issues (20 remaining)

| ID | Source | File | Priority | Issue | Status |
|----|--------|------|----------|-------|--------|
| PERF-004 | performance-reviewer | `_Layout.cshtml:13-14` | P3 | External CDN resources (Google Fonts, cdnjs) loaded synchronously | Open |
| PERF-005 | performance-reviewer | `site.js:29-43` | P4 | Tooltip creates/removes DOM elements on every hover | Open |
| PERF-006 | performance-reviewer | `chorus-display.js` | P3 | Large ~800-line file loaded on all pages | Open |
| PERF-007 | performance-reviewer | `settings.js:66-217` | P4 | Settings modal HTML created on every page load | Open |
| CODE-005 | code-reviewer | `form.js:312-318` | P3 | Escape key fires `cancelButton.click()` potentially causing navigation | Open |
| CODE-006 | code-reviewer | `form.js:345-358` | P4 | Smart placeholder setInterval - interval now cleared on blur (improved) | Improved but fragile |
| CODE-007 | code-reviewer | `detail.js:226-240` | P3 | `highlightSearchTerm` uses URL params for regex - **RESOLVED**: now escapes regex chars | **RESOLVED** |
| CODE-008 | code-reviewer | `search-ui.js:316-319` | P4 | `exportResults` is still a stub (console.log -> debug) | Open |
| CODE-009 | code-reviewer | `ai-search.js:79-87` | P4 | Recursive `setTimeout` retry for init remains | Open |
| CODE-010 | code-reviewer | `search-v2.js:308,332` | P3 | Error path in highlightSearchTerm returns unescaped `text` (NEW) | **NEW** |
| A11Y-007 | accessibility-reviewer | `Index.cshtml:278-290` | P3 | Detail modal lacks `aria-labelledby` and `aria-describedby` | Open |
| A11Y-008 | accessibility-reviewer | `search-ui.js:238-248` | P3 | Action buttons lack `aria-label` | Open |
| A11Y-009 | accessibility-reviewer | CSS files | P4 | `prefers-reduced-motion` - **RESOLVED**: added to `site.css:314` | **RESOLVED** |
| UX-003 | ux-reviewer | `Index.cshtml` | P2 | No empty state for initial page load | Open |
| UX-004 | ux-reviewer | `form.js:128` | P3 | Auto-save fires every 2 seconds of typing | Open |
| UX-005 | ux-reviewer | `setlist.js:85` | P3 | `clearAll` uses native `confirm()` | Open |
| UX-006 | ux-reviewer | `settings.js:332-390` | P4 | Mass edit lacks progress indicator | Open |
| UX-007 | ux-reviewer | `Index.cshtml` | P4 | Search tips section always visible | Open |
| NAMING-001 | naming-reviewer | Multiple files | P3 | Inconsistent naming conventions | Open |
| NAMING-002 | naming-reviewer | `search-v2.js` | P4 | File name `-v2` is iteration artifact | Open |
| NAMING-003 | naming-reviewer | CSS files | P4 | Mixed CSS naming methodologies | Open |

### New Issues Found (3)

#### SEC-007: innerHTML with Unsanitized API Data in ai-search.js
(Described in Major Issues above)

#### CODE-010: Error Path Returns Unescaped Text in search-v2.js
**File:** `CHAP2.UI/CHAP2.WebPortal/wwwroot/js/search-v2.js:307-309, 377`
**Category:** Code Quality
**Technical Severity:** Minor
**Business Impact:** Medium
**Priority:** P3
**Current State:** In `createResultRow`, when `highlightSearchTerm` throws an error, the catch block falls back to `result.name || 'Unknown'` which is unescaped. In `highlightSearchTerm` error catch at line 377, the fallback also returns raw `text`. This means an error in regex processing would bypass the HTML escaping.
**Expected State:** Error fallback should also escape HTML: `utils.escapeHtml(text) || ''`.
**Dependencies:** None
**Status:** NEW

#### PERF-008: Double Cache-Busting on search-v2.js Script Tag
**File:** `CHAP2.UI/CHAP2.WebPortal/Views/Shared/_Layout.cshtml:26`
**Category:** Performance
**Technical Severity:** Minor
**Business Impact:** Low
**Priority:** P4
**Current State:** Line 26 uses both `?v=@DateTime.Now.Ticks&cb=@Guid.NewGuid()` AND `asp-append-version="true"`, resulting in triple cache-busting parameters. This prevents any browser caching whatsoever on this file.
**Expected State:** Use only `asp-append-version="true"` which appends a content hash.
**Dependencies:** None
**Status:** NEW

---

## Resolved Issues Summary (16 items)

| ID | Title | Resolution |
|----|-------|------------|
| SEC-001 | XSS via innerHTML in search highlighting | Fixed: `search-ui.js` and `search-v2.js` escape HTML entities and regex special chars before highlighting |
| SEC-002 | XSS via innerHTML in notifications | Fixed: `utils.js`, `form.js`, `delete-modal.js`, `setlist.js` all use `textContent` for message text |
| SEC-003 | XSS via innerHTML in system restart dialog | Fixed: `system-restart.js` uses `textContent` via separate DOM element for API response text |
| SEC-006 | Debug scripts shipped to production | Fixed: `debug-crud.js` wrapped in `<environment include="Development">` tag in `_Layout.cshtml:30-32` |
| CODE-001 | Ctrl+C keyboard shortcut hijacked | Fixed: Removed from `detail.js:123` with comment noting intentional preservation of native copy |
| CODE-002 | Escape closes window unexpectedly | Fixed: Removed from `detail.js:125` with comment noting intentional preservation of standard behavior |
| A11Y-003 | No skip navigation link | Fixed: Skip link added as `<a href="#main-content" class="skip-link">` in `_Layout.cshtml:17`; `<main id="main-content">` landmark added |
| A11Y-009 | No prefers-reduced-motion CSS | Fixed: `@media (prefers-reduced-motion: reduce)` added to `site.css:314-320` |
| PERF-001 | Excessive console.log in production | Fixed: ~350 console.log replaced with gated `debug()` function. Remaining 38 occurrences are only in `utils.js` (debug function definition), `debug-crud.js` (dev-only), and `test-enum-conversion.js` (dev-only) |
| CODE-007 | highlightSearchTerm regex from URL params | Fixed: `detail.js:225` now escapes regex special characters |
| NAMING-004 | Inconsistent function naming in detail.js | Moot: functions simplified during fix |
| -- | search-working.js deleted | Fixed: File removed as duplicate |
| -- | `role="search"` on container | Fixed: Added to `Index.cshtml:5` |
| -- | `aria-label` on main search input | Fixed: Added to `Index.cshtml:47` |
| -- | `aria-live="polite"` on results | Fixed: Added to `Index.cshtml:203` |
| -- | debug() gating function | Fixed: `utils.js:166-170` - gated behind `window.CHAP2_DEBUG` flag |

---

## Accessibility Compliance (WCAG 2.1)

| Level | Criteria | Status | Violations |
|-------|----------|--------|------------|
| A | 1.1.1 Non-text Content | WARN | Icon-only buttons without aria-label (A11Y-008) |
| A | 1.3.1 Info and Relationships | PASS | Improved: role="search", <main> landmark added |
| A | 1.4.1 Use of Color | FAIL | Status indicator color-only (A11Y-006) |
| A | 2.1.1 Keyboard | FAIL | Non-keyboard-accessible row elements (A11Y-002) |
| A | 2.4.1 Bypass Blocks | PASS | Skip link added (RESOLVED) |
| A | 3.3.2 Labels or Instructions | WARN | Main input labeled; AI search input still unlabeled |
| A | 4.1.2 Name, Role, Value | FAIL | Missing ARIA on modals (A11Y-004) |
| AA | 1.4.3 Contrast | PASS | Good contrast ratios |
| AA | 2.4.7 Focus Visible | WARN | Custom focus styles exist but not comprehensive |

## Performance Metrics

| Metric | Target | Current (Estimated) | Status |
|--------|--------|---------------------|--------|
| LCP | < 2.5s | ~2-3s (unminified, external fonts) | WARN |
| FID | < 100ms | ~50-80ms (console.log removed) | PASS |
| CLS | < 0.1 | ~0.05 | PASS |
| Bundle Size (JS) | < 200KB | ~280KB (14 unminified JS files) | WARN |
| First Load JS | < 100KB | ~140KB (7 scripts in layout) | WARN |

## Architecture Standards

### MVVM Compliance
| Layer | Files | Compliant | Violations |
|-------|-------|-----------|------------|
| View (Razor) | 13 | Partial | 1 (Edit.cshtml inline JS) |
| ViewModel (JS) | N/A | N/A | No formal ViewModel layer |
| Model/Service (JS) | 3 | Partial | Mixed with view logic |

### SOLID Principles
| Principle | Status | Violations |
|-----------|--------|------------|
| Single Responsibility | WARN | 5 (monolithic files) |
| Open/Closed | WARN | 3 (hard-coded enum maps) |
| Dependency Inversion | FAIL | 8 (direct global dependencies) |

### One Type Per File
| Check | Status | Violations |
|-------|--------|------------|
| Single class/module per file | PASS | 0 |
| Filename matches export | WARN | 1 (search-v2) |
| No mixed concerns | WARN | 1 (Edit.cshtml inline scripts) |

## Agent Tree

```
Depth 0: 02-FE-master-orchestrator (THIS AGENT)
    Depth 1: 02-01-01-FE-architecture-reviewer (virtual) -> ARCH-001 through ARCH-005
    Depth 1: 02-01-02-FE-code-reviewer (virtual) -> CODE-003 through CODE-010
    Depth 1: 02-01-03-FE-test-coverage-reviewer (virtual) -> TEST-001, TEST-002
    Depth 1: 02-01-04-FE-accessibility-reviewer (virtual) -> A11Y-001 through A11Y-009
    Depth 1: 02-01-05-FE-performance-reviewer (virtual) -> PERF-002 through PERF-008
    Depth 1: 02-01-06-FE-security-reviewer (virtual) -> SEC-004, SEC-005, SEC-007
    Depth 1: 02-01-07-FE-mvvm-reviewer (virtual) -> ARCH-003
    Depth 1: 02-01-08-FE-ux-reviewer (virtual) -> UX-001 through UX-007
    Depth 1: 02-01-09-FE-naming-convention-reviewer (virtual) -> NAMING-001 through NAMING-003
```

## Recommendations

### Critical (Immediate)
1. **A11Y-002**: Add keyboard accessibility to all interactive table rows and setlist items
2. **TEST-001**: Set up test framework and write tests for `escapeHtml`, `debug()`, and search functions

### Major (Short-term)
1. **SEC-004**: Escape HTML in setlist rendering template literals
2. **SEC-005**: Add CSRF tokens to Edit page inline API calls
3. **SEC-007**: Escape API-derived content in ai-search.js innerHTML usage
4. **A11Y-004**: Add focus traps and ARIA dialog attributes to all modals
5. **ARCH-002**: Consolidate 6 showNotification implementations into the utils.js version; fix remaining innerHTML in settings.js and chorus-display.js
6. **ARCH-003**: Extract Edit.cshtml inline JavaScript to dedicated file

### Minor (When convenient)
1. Consolidate duplicate enum maps (ARCH-005)
2. Fix error-path fallback escaping (CODE-010)
3. Remove triple cache-busting on search-v2.js (PERF-008)
4. Add aria-label to icon-only action buttons (A11Y-008)
