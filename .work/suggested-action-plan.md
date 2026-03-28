# Improvement Plan - CHAP2 Church Chorus Management System (Updated)

**Generated:** 2026-03-28 (Re-review)
**Based On:** Backend Findings Re-Review (BE-REVIEW-20260328-R2)
**Status:** In Progress -- Phase 1 Complete

---

## Overview

This updated plan reflects the re-review after the first round of fixes. Of the original 36 findings, 20 have been resolved (56%). All 4 critical issues are now closed. The remaining 16 issues (9 Major, 7 Minor) plus 3 newly discovered issues are organized into 4 phases.

### What Was Accomplished (Phase 1 - Complete)
- CORS restricted to configurable origins (SEC-001, SEC-002)
- Regex timeout added to prevent ReDoS (SEC-003)
- HTTPS enforcement added to API (SEC-005)
- Constructor null guards added across Application services (ARCH-005)
- Reflection removed from SlideToChorusService (ARCH-002)
- Domain event dispatch deduplication fixed (QUAL-002)
- DomainEvents list made read-only (QUAL-003)
- WebPortal CachedChorusRepository added (QUAL-005)
- Dead code removed (IServices, IController, Services) (MIN-001, MIN-002, MIN-005)
- AiSearchService async fixes (MIN-003)
- TryParse replacements (MIN-007, MIN-008)
- SearchApiRequest extracted to own file (NAME-001)
- New test files: DiskChorusRepositoryTests, CachedChorusRepositoryTests, ChorusSearchServiceTests, ChorusesControllerTests (TEST-001, TEST-002, TEST-004)
- All 62 tests passing

### Backward Compatibility Strategy

When introducing breaking changes or replacing legacy APIs, use the `[Obsolete]` attribute:

```csharp
[Obsolete("Use NewMethod() instead. This method will be removed in version X.0.")]
public void OldMethod() { ... }
```

---

## Phase 2: Error Handling and Security (High Priority)

### 2.1 Remove Exception Swallowing in ChorusApplicationService (ARCH-004)

**Finding:** All methods catch `Exception` and return `false`/`null`, masking real errors from callers.

**Tasks:**
- [ ] Remove blanket `catch (Exception)` blocks from `CreateChorusAsync`, `UpdateChorusAsync`, `DeleteChorusAsync`, `GetChorusByIdAsync`
- [ ] Let domain exceptions (ChorusNotFoundException, ChorusAlreadyExistsException, DomainException) propagate to callers
- [ ] Consider adopting a `Result<T>` pattern if the WebPortal needs structured error information without exceptions
- [ ] Update WebPortal controllers to handle domain exceptions if they don't already

**Files to modify:**
- `CHAP2.Application/Services/ChorusApplicationService.cs`

**Estimated effort:** Low

---

### 2.2 Evaluate Authentication Requirements (SEC-004)

**Finding:** No authentication or authorization on any API endpoint.

**Tasks:**
- [ ] Determine if the API is intended for internal-only use
- [ ] If internal-only: Add XML comment at Program.cs and README documenting the intentional lack of auth
- [ ] If externally exposed: Add API key authentication for write endpoints (POST, PUT, DELETE)
- [ ] If externally exposed: Add `[Authorize]` attributes on ChorusesController write actions
- [ ] If externally exposed: Add `[AllowAnonymous]` on read-only endpoints if desired

**Files to modify:**
- `CHAP2.Chorus.Api/Program.cs`
- `CHAP2.Chorus.Api/Controllers/ChorusesController.cs`
- `CHAP2.Chorus.Api/Controllers/SlideController.cs`

**Estimated effort:** Medium (if adding auth) / Low (if documenting intentional no-auth)

---

### 2.3 Add Regex Pattern Validation (SEC-006)

**Finding:** Regex search mode accepts arbitrary user patterns; 1-second timeout per match mitigates ReDoS but multiplied across choruses still allows load amplification.

**Tasks:**
- [ ] Add regex pattern length limit (e.g., max 100 characters)
- [ ] Reject patterns with known dangerous constructs (nested quantifiers like `(a+)+`)
- [ ] OR: Remove `SearchMode.Regex` from the public API entirely and replace with wildcard mode
- [ ] Add tests for pattern validation/rejection

**Files to modify:**
- `CHAP2.Application/Services/ChorusSearchService.cs` (IsRegexMatch method)
- `CHAP2.Application/Helpers/InputSanitizer.cs` (add regex validation method)

**Estimated effort:** Low

---

### 2.4 Fix ChorusCommandService Exception Types (NEW-003)

**Finding:** `ChorusCommandService.UpdateChorusAsync` and `DeleteChorusAsync` throw `InvalidOperationException` when chorus not found, but `ChorusesController` catches `ChorusNotFoundException`.

**Tasks:**
- [ ] Replace `throw new InvalidOperationException(...)` with `throw new ChorusNotFoundException(id)` in UpdateChorusAsync (line 58) and DeleteChorusAsync (line 88)

**Files to modify:**
- `CHAP2.Application/Services/ChorusCommandService.cs`

**Estimated effort:** Low (5 minutes)

---

## Phase 3: Test Coverage Expansion (Medium Priority)

### 3.1 Create Tests for SlideToChorusService (TEST-003)

**Finding:** 365-line slide conversion service with PowerPoint parsing, key extraction, and text processing has zero tests.

**Tasks:**
- [ ] Create `CHAP2.Tests/Application/SlideToChorusServiceTests.cs`
- [ ] Test key extraction from filename patterns
- [ ] Test text parsing and title case conversion
- [ ] Test error handling for invalid/corrupt input
- [ ] Test boundary cases (empty slides, very large files)

**Files to create:**
- `CHAP2.Tests/Application/SlideToChorusServiceTests.cs`

**Estimated effort:** Medium

---

### 3.2 Expand Error Path Tests (TEST-005)

**Finding:** ChorusCommandServiceTests only cover happy paths and basic not-found scenarios.

**Tasks:**
- [ ] Add test verifying domain events contain correct data (ChorusId, Name) after Create
- [ ] Add test verifying event dispatcher is called with `DispatchAndClearAsync`
- [ ] Add test verifying repository exceptions propagate correctly (not swallowed)
- [ ] Add test verifying `ClearDomainEvents()` is called after dispatch

**Files to modify:**
- `CHAP2.Tests/Application/ChorusCommandServiceTests.cs`

**Estimated effort:** Low

---

### 3.3 Add Missing Null Guard Tests

**Finding:** NEW-001 and NEW-002 identified constructors without null guards.

**Tasks:**
- [ ] Add null guards to `IntelligentSearchService` constructor for all 5 parameters
- [ ] Add null guards to `SlideController` constructor for `chorusResource` and `slideToChorusService`
- [ ] Add corresponding tests verifying ArgumentNullException is thrown

**Files to modify:**
- `CHAP2.UI/CHAP2.WebPortal/Services/IntelligentSearchService.cs`
- `CHAP2.Chorus.Api/Controllers/SlideController.cs`

**Estimated effort:** Low (15 minutes)

---

## Phase 4: Architecture Improvements (Medium Priority)

### 4.1 Make ChorusMetadata Immutable (ARCH-001)

**Finding:** ChorusMetadata in ValueObjects folder has mutable public setters and mutable collections, violating DDD value object semantics.

**Tasks:**
- [ ] Convert public setters to `init`-only
- [ ] Replace `List<string> Tags` with `ImmutableList<string>`
- [ ] Replace `Dictionary<string, string> CustomProperties` with `ImmutableDictionary<string, string>`
- [ ] Change `AddTag`/`RemoveTag`/`SetCustomProperty` to return new `ChorusMetadata` instances
- [ ] Update `ChorusMetadataJsonConverter` for immutable deserialization
- [ ] Update all callers (DTO mapping, entity methods)
- [ ] Add unit tests for ChorusMetadata value semantics

**Files to modify:**
- `CHAP2.Domain/ValueObjects/ChorusMetadata.cs`
- `CHAP2.Domain/ValueObjects/ChorusMetadataJsonConverter.cs`
- `CHAP2.Infrastructure/DTOs/ChorusDto.cs`

**Estimated effort:** Medium

---

### 4.2 Replace Reflection in DomainEventDispatcher (ARCH-003)

**Finding:** `GetMethod("HandleAsync")` with `Invoke()` is fragile; renaming the interface method silently breaks dispatching.

**Tasks:**
- [ ] Create a generic dispatch helper method that casts to `IDomainEventHandler<TEvent>`
- [ ] Replace `handlerType.GetMethod("HandleAsync")` and `handleMethod.Invoke(...)` with the typed helper
- [ ] OR: Evaluate using MediatR for event dispatching
- [ ] Add unit tests for DomainEventDispatcher

**Files to modify:**
- `CHAP2.Application/Services/DomainEventDispatcher.cs`

**Estimated effort:** Low-Medium

---

### 4.3 Align DI Lifetimes (QUAL-004)

**Finding:** Singleton repository injected into scoped services (captive dependency).

**Tasks:**
- [ ] Option A: Document the current approach as intentional (repository is stateless aside from file path)
- [ ] Option B: Register command/query services as Singleton to match repository
- [ ] Option C: Register repository as Scoped and rely on CachedChorusRepository's MemoryCache for cross-request caching

**Files to modify:**
- `CHAP2.Chorus.Api/Program.cs`

**Estimated effort:** Low

---

### 4.4 Extract IChorusSearchService Interface (QUAL-001)

**Finding:** ChorusSearchService exposes public methods (SearchByNameAsync, SearchByTextAsync, etc.) not on any interface.

**Tasks:**
- [ ] Create `CHAP2.Application/Interfaces/IChorusSearchService.cs` with the additional method signatures
- [ ] Have `ChorusSearchService` implement both `ISearchService` and `IChorusSearchService`
- [ ] Register `IChorusSearchService` in DI container
- [ ] Update consumers to depend on interface instead of concrete type

**Files to create:**
- `CHAP2.Application/Interfaces/IChorusSearchService.cs`

**Files to modify:**
- `CHAP2.Application/Services/ChorusSearchService.cs`
- `CHAP2.Chorus.Api/Program.cs`

**Estimated effort:** Low

---

### 4.5 Fix SlideController Layer Violation (MIN-009)

**Finding:** SlideController injects IChorusRepository directly, bypassing the service layer.

**Tasks:**
- [ ] Replace `IChorusRepository` injection with `IChorusCommandService` and `IChorusQueryService`
- [ ] Update `AddAsync`/`UpdateAsync`/`GetByNameAsync` calls to use service methods
- [ ] Ensure domain events are dispatched properly through service layer

**Files to modify:**
- `CHAP2.Chorus.Api/Controllers/SlideController.cs`

**Estimated effort:** Low

---

## Phase 5: Polish (Lower Priority)

### 5.1 Minor Cleanup Items

**Tasks:**
- [ ] Move `SearchRequest.cs` and `SearchResult.cs` from `Interfaces` folder to `Models` folder (MIN-012)
- [ ] Scope `AllowSynchronousIO` to streaming endpoints only (MIN-010)
- [ ] Add startup configuration validation for service URLs (MIN-014)
- [ ] Rename `MatchesSearch` to `IsTextMatchForSearchMode` (MIN-006)
- [ ] Refactor `ValidateFileContent` to avoid null-forgiving operator (MIN-011)
- [ ] Make `GetCountAsync` in DiskChorusRepository truly async or remove async wrapper (MIN-004)

**Estimated effort:** Low

---

## Execution Summary

| Phase | Items | Estimated Effort | Priority | Status |
|-------|-------|------------------|----------|--------|
| Phase 1: Critical Security & Foundation | 20 | High | Critical | COMPLETE |
| Phase 2: Error Handling & Security | 4 | Low-Medium | High | Ready |
| Phase 3: Test Coverage | 3 | Medium | Medium | Ready |
| Phase 4: Architecture | 5 | Medium | Medium | Ready |
| Phase 5: Polish | 6 | Low | Lower | Ready |

### Quick Wins (< 30 minutes each)

- Fix ChorusCommandService exception types to use ChorusNotFoundException (NEW-003) -- 5 minutes
- Add null guards to IntelligentSearchService and SlideController constructors (NEW-001, NEW-002) -- 15 minutes
- Document intentional no-auth decision or add API key auth (SEC-004) -- 15-30 minutes
- Extract IChorusSearchService interface (QUAL-001) -- 20 minutes
- Align DI lifetimes by documenting or changing to Singleton (QUAL-004) -- 15 minutes

---

## Progress Tracking

Phase 1: [x] [x] [x] [x] [x] [x] [x] [x] [x] [x] [x] [x] [x] [x] [x] [x] [x] [x] [x] [x] (20/20 COMPLETE)
Phase 2: [ ] [ ] [ ] [ ]
Phase 3: [ ] [ ] [ ]
Phase 4: [ ] [ ] [ ] [ ] [ ]
Phase 5: [ ] [ ] [ ] [ ] [ ] [ ]

---

## Re-Validation Checklist

After implementing Phase 2+ fixes:
- [ ] Run `dotnet build` to verify no regressions
- [ ] Run `dotnet test` to verify all 62+ tests pass
- [ ] Manual spot-check: Create/Update/Delete chorus via API and verify error responses
- [ ] Manual spot-check: Search with regex mode and verify pattern validation
- [ ] Re-run security reviewer on fixed SEC issues
- [ ] Re-run architecture reviewer on fixed ARCH issues

---
---

# Frontend Improvement Plan - CHAP2 WebPortal (Updated Re-Review)

**Generated:** 2026-03-28 (Updated after re-review)
**Based On:** Frontend Findings Re-Review (FE-REVIEW-2026-03-28-R2)
**Framework:** ASP.NET Razor + Vanilla JavaScript
**Status:** In Progress - 16 of 53 original findings resolved

---

## Overview

This updated plan reflects the re-review after initial fixes were applied. Of 53 original findings, 16 are now RESOLVED, 5 are PARTIALLY RESOLVED, 29 remain OPEN, and 3 NEW issues were introduced. The plan is restructured around the 37 remaining items, organized into 5 phases.

**Completed in previous fix round:**
- [x] XSS fixes across 7 JS files (innerHTML -> textContent, regex escaping)
- [x] Debug scripts conditionally loaded via `<environment>` tag
- [x] Ctrl+C/Escape overrides removed from detail.js
- [x] Skip-to-content link and ARIA landmarks added
- [x] ARIA labels/live regions added to main search
- [x] prefers-reduced-motion CSS added
- [x] ~350 console.log replaced with gated debug()
- [x] search-working.js deleted

---

## FE Phase 1: Remaining Security Issues (Critical Priority)

### FE-1.1 Fix Setlist XSS in refreshDisplay (SEC-004)

**Finding:** Setlist rendering still injects `chorus.name`, `chorus.key`, `chorus.type` directly into innerHTML template literals without escaping.

**Tasks:**
- [ ] Apply `utils.escapeHtml()` to `chorus.name` at `setlist.js:152`
- [ ] Apply `utils.escapeHtml()` to `chorus.key` and `chorus.type` at `setlist.js:154-156`
- [ ] Audit all template literals in `setlist.js` for unsanitized data

**Files to modify:**
- `wwwroot/js/setlist.js`

**Estimated effort:** Low (30 minutes)

### FE-1.2 Add CSRF Tokens to Edit Page API Calls (SEC-005)

**Finding:** `saveCurrentChorus()` and `deleteCurrentChorus()` in Edit.cshtml still make POST requests without `RequestVerificationToken` header.

**Tasks:**
- [ ] Add `getAntiForgeryToken()` function to Edit.cshtml inline script
- [ ] Add `RequestVerificationToken` header to save fetch call at `Edit.cshtml:400`
- [ ] Add `RequestVerificationToken` header to delete fetch call at `Edit.cshtml:438`

**Files to modify:**
- `Views/Home/Edit.cshtml` (inline script)

**Estimated effort:** Low (30 minutes)

### FE-1.3 Fix innerHTML with API Data in ai-search.js (SEC-007) -- NEW

**Finding:** `ai-search.js` injects chorus names and explanation text from API responses directly into innerHTML without escaping.

**Tasks:**
- [ ] Escape `chorusName` before innerHTML insertion at `ai-search.js:490`
- [ ] Escape `result.explanation` at lines 498, 646, 839, 860
- [ ] Escape `analysis` text at line 1288
- [ ] Use `textContent` where possible for text-only insertions

**Files to modify:**
- `wwwroot/js/ai-search.js`

**Estimated effort:** Medium (1-2 hours)

### FE-1.4 Fix Remaining innerHTML XSS in showNotification Duplicates

**Finding:** `settings.js:624` and `chorus-display.js:1807` still use innerHTML with `${message}` in their `showNotification`.

**Tasks:**
- [ ] Fix `settings.js:624` to use textContent for message
- [ ] Fix `chorus-display.js:1807` to use textContent for message

**Files to modify:**
- `wwwroot/js/settings.js`
- `wwwroot/js/chorus-display.js`

**Estimated effort:** Low (15 minutes)

---

## FE Phase 2: Accessibility Compliance (High Priority)

### FE-2.1 Keyboard Accessibility for Interactive Elements (A11Y-002) -- CRITICAL

**Tasks:**
- [ ] Add `tabindex="0"` and `role="row"` to `.result-row` elements in `search-v2.js:299-301`
- [ ] Add `onkeydown` handler for Enter/Space to trigger row click
- [ ] Add `tabindex="0"` to `.result-row` in `search-ui.js:227`
- [ ] Provide keyboard alternative for setlist drag-and-drop (arrow keys)

**Files to modify:**
- `wwwroot/js/search-v2.js`
- `wwwroot/js/search-ui.js`
- `wwwroot/js/setlist.js`

**Estimated effort:** Medium (1-2 hours)

### FE-2.2 Complete Form Label Coverage (A11Y-001)

**Tasks:**
- [ ] Add `aria-label` to AI search input at `Index.cshtml:100`
- [ ] Add `aria-label` to dynamically created search input in `search-ui.js:37-40`
- [ ] Add `aria-label` to dynamically created inputs in `search-integration.js`

**Files to modify:**
- `Views/Home/Index.cshtml`
- `wwwroot/js/search-ui.js`
- `wwwroot/js/search-integration.js`

**Estimated effort:** Low (30 minutes)

### FE-2.3 Modal Focus Trap and ARIA (A11Y-004, A11Y-007)

**Tasks:**
- [ ] Add `role="dialog"`, `aria-modal="true"`, `aria-labelledby` to all modals
- [ ] Implement focus trap (Tab cycles through modal elements only)
- [ ] Return focus to trigger element on modal close

**Files to modify:**
- `wwwroot/js/site.js`
- `wwwroot/js/delete-modal.js`
- `wwwroot/js/form.js`
- `Views/Home/Index.cshtml`

**Estimated effort:** Medium (2-3 hours)

### FE-2.4 Complete ARIA Live Regions and Status Indicators (A11Y-005, A11Y-006, A11Y-008)

**Tasks:**
- [ ] Add `aria-live="assertive"` to error/notification containers
- [ ] Add text label to connection status indicator
- [ ] Add `aria-label` to icon-only action buttons

**Files to modify:**
- `Views/Home/Index.cshtml`
- `wwwroot/js/search-v2.js`
- `wwwroot/js/search-ui.js`

**Estimated effort:** Low (1 hour)

---

## FE Phase 3: Performance and Code Quality (Medium Priority)

### FE-3.1 Fix Bundling and Cache-Busting (PERF-003, PERF-008)

**Tasks:**
- [ ] Remove `?v=@DateTime.Now.Ticks` and `&cb=@Guid.NewGuid()` from `_Layout.cshtml` script tags
- [ ] Rely solely on `asp-append-version="true"` for content-hash cache busting
- [ ] Configure ASP.NET Core bundling (WebOptimizer or BundlerMinifier)

**Files to modify:**
- `Views/Shared/_Layout.cshtml`

**Estimated effort:** Medium (2-3 hours)

### FE-3.2 Consolidate Search Implementations (PERF-002)

**Tasks:**
- [ ] Remove or consolidate `search-service.js` + `search-ui.js` overlap with `search-v2.js`
- [ ] Delete `test-enum-conversion.js` file (already dev-only but still on disk)

**Estimated effort:** Medium (2-3 hours)

### FE-3.3 Fix Race Condition and Error Paths (CODE-003, CODE-010)

**Tasks:**
- [ ] Replace boolean `isSearching` flag with `AbortController` in `search-service.js`
- [ ] Fix error fallback in `search-v2.js:309` to escape HTML
- [ ] Fix error fallback in `search-v2.js:377` to escape HTML

**Files to modify:**
- `wwwroot/js/search-service.js`
- `wwwroot/js/search-v2.js`

**Estimated effort:** Low (1 hour)

---

## FE Phase 4: Architecture Improvements (Medium Priority)

### FE-4.1 Extract Edit.cshtml Inline JavaScript (ARCH-003)

**Tasks:**
- [ ] Create `wwwroot/js/chorus-edit-nav.js` from Edit.cshtml inline JS (~290 lines)
- [ ] Remove console.log statements from extracted code
- [ ] Pass server data via `data-` attributes

**Estimated effort:** Medium (2-3 hours)

### FE-4.2 Consolidate Duplicate Code (ARCH-002, ARCH-005)

**Tasks:**
- [ ] Reduce 6 showNotification implementations to 1 in `utils.js`
- [ ] Move enum maps to `utils.js`, remove 4+ duplicates

**Estimated effort:** Medium (2-3 hours)

### FE-4.3 Remaining Architecture (ARCH-001, ARCH-004)

**Tasks:**
- [ ] Wrap JS files in IIFEs or module pattern (ARCH-001)
- [ ] Create shared layout for standalone pages (ARCH-004)

**Estimated effort:** High (4-6 hours)

---

## FE Phase 5: Test Coverage and UX (Lower Priority)

### FE-5.1 Set Up Test Framework and Critical Tests (TEST-001, TEST-002) -- CRITICAL

**Tasks:**
- [ ] Install Jest/Vitest with jsdom
- [ ] Test `utils.escapeHtml()` with XSS payloads
- [ ] Test `debug()` gating function
- [ ] Test `highlightSearchTerm()` with HTML/regex special chars
- [ ] Test form validation and enum mappings

**Estimated effort:** High (5-8 hours)

### FE-5.2 UX Improvements (UX-001 through UX-005)

**Tasks:**
- [ ] Progressive status for AI search (UX-001)
- [ ] Custom delete modal in mass edit (UX-002)
- [ ] Empty state for initial page load (UX-003)
- [ ] Reduce auto-save notification frequency (UX-004)
- [ ] Replace native `confirm()` with custom modal (UX-005)

**Estimated effort:** Medium (3-4 hours)

---

## Frontend Execution Summary (Updated)

| Phase | Items | Estimated Effort | Priority |
|-------|-------|------------------|----------|
| FE Phase 1: Security | 4 tasks | 2-4 hours | Critical |
| FE Phase 2: Accessibility | 4 tasks | 4-6 hours | High |
| FE Phase 3: Perf/Code | 3 tasks | 5-7 hours | Medium |
| FE Phase 4: Architecture | 3 tasks | 8-12 hours | Medium |
| FE Phase 5: Tests/UX | 2 tasks | 8-12 hours | Lower |

### Remaining Quick Wins (< 1 hour each)

- Fix remaining innerHTML XSS in `settings.js` and `chorus-display.js` (FE-1.4) - 15 min
- Add CSRF tokens to Edit page API calls (FE-1.2) - 30 min
- Escape setlist template literals (FE-1.1) - 30 min
- Complete aria-label on AI search input (FE-2.2) - 30 min
- Fix error-path unescaped text in search-v2.js (FE-3.3 partial) - 10 min
- Remove triple cache-busting from _Layout.cshtml (FE-3.1 partial) - 10 min

---

## Frontend Progress Tracking (Updated)

FE Phase 1: [ ] 1.1 [ ] 1.2 [ ] 1.3 [ ] 1.4
FE Phase 2: [ ] 2.1 [ ] 2.2 [ ] 2.3 [ ] 2.4
FE Phase 3: [ ] 3.1 [ ] 3.2 [ ] 3.3
FE Phase 4: [ ] 4.1 [ ] 4.2 [ ] 4.3
FE Phase 5: [ ] 5.1 [ ] 5.2

---

## Frontend Re-Validation Guidance (Updated)

After next fix round, verify:

1. **Security (FE Phase 1):**
   - Add chorus with name `<img src=x onerror=alert(1)>` to setlist - verify escaped in display
   - Verify CSRF tokens present in Edit page save/delete requests via DevTools Network tab
   - Check ai-search.js displays API data safely (mock HTML payload)
   - Verify settings.js and chorus-display.js notifications use textContent

2. **Accessibility (FE Phase 2):**
   - Tab through search results table - rows must be focusable
   - Press Enter on focused row - must open chorus display
   - Open any modal, verify Tab stays trapped inside
   - Test with VoiceOver - all inputs announced with labels

3. **Performance (FE Phase 3):**
   - Check _Layout.cshtml script tags have only `asp-append-version="true"`
   - Verify rapid typing in search produces no race condition errors

4. **Tests (FE Phase 5):**
   - `npm test` passes with >80% coverage on utility functions
   - XSS escape tests cover all OWASP vectors
