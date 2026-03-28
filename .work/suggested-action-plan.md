# Improvement Plan - CHAP2 Church Chorus Management System

**Generated:** 2026-03-28
**Based On:** Backend Findings Review (BE-REVIEW-20260328)
**Status:** Planning Phase

---

## Overview

This plan addresses 36 findings (4 Critical, 18 Major, 14 Minor) across security, architecture, test coverage, code quality, and naming. The plan is structured in 5 phases prioritized by: Security -> Test Foundation -> Architecture -> Code Quality -> Polish. Each phase can be executed independently, though Phase 1 should be completed first.

### Backward Compatibility Strategy

When introducing breaking changes or replacing legacy APIs, use the `[Obsolete]` attribute to provide a deprecation path:

```csharp
[Obsolete("Use NewMethod() instead. This method will be removed in version X.0.")]
public void OldMethod() { ... }
```

---

## Phase 1: Security Hardening (Critical Priority)

### 1.1 Restrict CORS Configuration (SEC-001, SEC-002)

**Finding:** Both the API and WebPortal use `AllowAnyOrigin()` CORS policy, allowing any website to make cross-origin requests.

**Tasks:**
- [ ] Create a `CorsSettings` class in `CHAP2.Shared/Configuration/` with `AllowedOrigins` array
- [ ] Update `CHAP2.Chorus.Api/Program.cs` to read allowed origins from configuration
- [ ] Update `CHAP2.UI/CHAP2.WebPortal/Program.cs` to read allowed origins from configuration
- [ ] Add CORS configuration section to both `appsettings.json` files
- [ ] Add `appsettings.Development.json` with permissive CORS for local dev

**Files to modify:**
- `CHAP2.Chorus.Api/Program.cs`
- `CHAP2.UI/CHAP2.WebPortal/Program.cs`
- `CHAP2.Shared/Configuration/CorsSettings.cs` (new)
- `CHAP2.Chorus.Api/appsettings.json`
- `CHAP2.UI/CHAP2.WebPortal/appsettings.json`

**Estimated effort:** Low

### 1.2 Fix Regex Denial of Service (SEC-003)

**Finding:** `ChorusSearchService.IsRegexMatch` passes user-supplied regex to `Regex.IsMatch` without a timeout, enabling ReDoS attacks.

**Tasks:**
- [ ] Add `TimeSpan.FromSeconds(1)` timeout parameter to `Regex.IsMatch` call
- [ ] Add catch for `RegexMatchTimeoutException` returning false
- [ ] Add unit test for ReDoS-style pattern to verify timeout works

**Files to modify:**
- `CHAP2.Application/Services/ChorusSearchService.cs` (line 445)

**Estimated effort:** Low

### 1.3 Add HTTPS Enforcement to API (SEC-005)

**Finding:** The API does not call `UseHttpsRedirection()`.

**Tasks:**
- [ ] Add `app.UseHttpsRedirection()` before `app.MapControllers()` in API Program.cs
- [ ] Verify HTTPS certificate is configured for the API

**Files to modify:**
- `CHAP2.Chorus.Api/Program.cs`

**Estimated effort:** Low

### 1.4 Evaluate and Document Authentication Requirements (SEC-004)

**Finding:** No authentication or authorization on any API endpoint.

**Tasks:**
- [ ] Document whether the API is intended for internal-only access
- [ ] If externally exposed: Add API key authentication middleware for write operations
- [ ] If internal only: Add a comment in Program.cs documenting the security boundary
- [ ] Consider adding `[Authorize]` attributes on POST/PUT/DELETE endpoints

**Files to modify:**
- `CHAP2.Chorus.Api/Program.cs`
- `CHAP2.Chorus.Api/Controllers/ChorusesController.cs`

**Estimated effort:** Medium (if adding auth), Low (if documenting)

### 1.5 Restrict or Sandbox Regex Search Mode (SEC-006)

**Finding:** Users can submit arbitrary regex patterns via the search API.

**Tasks:**
- [ ] Option A: Remove `SearchMode.Regex` from public API (keep for internal use)
- [ ] Option B: Add regex pattern complexity validation (max length, disallow backreferences)
- [ ] Add tests for pattern validation

**Files to modify:**
- `CHAP2.Application/Services/ChorusSearchService.cs`
- `CHAP2.Chorus.Api/Controllers/ChorusesController.cs`

**Estimated effort:** Low

---

## Phase 2: Test Coverage Foundation (High Priority)

### 2.1 Infrastructure Layer Tests (TEST-001)

**Finding:** DiskChorusRepository and CachedChorusRepository have zero test coverage despite being the sole persistence mechanism.

**Tasks:**
- [ ] Create `CHAP2.Tests/Infrastructure/DiskChorusRepositoryTests.cs`
  - [ ] Test `GetByIdAsync` with existing and non-existent files
  - [ ] Test `GetAllAsync` with empty directory and multiple files
  - [ ] Test `AddAsync` writes correct JSON
  - [ ] Test `UpdateAsync` overwrites correctly
  - [ ] Test `DeleteAsync` removes file
  - [ ] Test `SearchAsync` with various terms
  - [ ] Test `ExistsAsync` by name and ID
  - [ ] Test concurrent access (multiple readers/writers)
- [ ] Create `CHAP2.Tests/Infrastructure/CachedChorusRepositoryTests.cs`
  - [ ] Test cache hit returns cached data
  - [ ] Test cache miss delegates to inner repository
  - [ ] Test write operations invalidate cache
  - [ ] Test cache expiration

**Files to create:**
- `CHAP2.Tests/Infrastructure/DiskChorusRepositoryTests.cs`
- `CHAP2.Tests/Infrastructure/CachedChorusRepositoryTests.cs`

**Estimated effort:** High

### 2.2 Search Service Tests (TEST-002)

**Finding:** ChorusSearchService (453 lines, largest service) has zero test coverage.

**Tasks:**
- [ ] Create `CHAP2.Tests/Application/ChorusSearchServiceTests.cs`
  - [ ] Test `SearchByNameAsync` with Contains, Exact, Regex modes
  - [ ] Test `SearchByTextAsync` with various scopes
  - [ ] Test `SearchAllAsync` with deduplication
  - [ ] Test `SearchAsync` (ISearchService) with all scope/mode combinations
  - [ ] Test caching behavior (hit/miss/invalidation)
  - [ ] Test parallel processing path (>100 results)
  - [ ] Test empty/null query handling
  - [ ] Test regex timeout (after SEC-003 fix)

**Files to create:**
- `CHAP2.Tests/Application/ChorusSearchServiceTests.cs`

**Estimated effort:** High

### 2.3 Slide Conversion Tests (TEST-003)

**Finding:** SlideToChorusService has no tests despite complex parsing logic.

**Tasks:**
- [ ] Create `CHAP2.Tests/Application/SlideToChorusServiceTests.cs`
  - [ ] Test key extraction from filename (all key patterns)
  - [ ] Test key extraction from slide title
  - [ ] Test title case conversion
  - [ ] Test text extraction (mock PowerPoint data)
  - [ ] Test error handling for invalid files
- [ ] Create test fixture data in `CHAP2.Tests/TestData/` folder

**Files to create:**
- `CHAP2.Tests/Application/SlideToChorusServiceTests.cs`
- `CHAP2.Tests/TestData/` (test PowerPoint files)

**Estimated effort:** Medium

### 2.4 Controller Tests (TEST-004)

**Finding:** No API controller tests exist.

**Tasks:**
- [ ] Create `CHAP2.Tests/Api/ChorusesControllerTests.cs`
  - [ ] Test `AddChorus` with valid/invalid requests
  - [ ] Test `GetAllChoruses` returns results
  - [ ] Test `GetChorusById` with valid/invalid IDs
  - [ ] Test `SearchChoruses` with various parameters
  - [ ] Test `UpdateChorus` with valid/invalid data
  - [ ] Test `DeleteChorus` with existing/missing chorus
  - [ ] Test input sanitization is applied
- [ ] Create `CHAP2.Tests/Api/SlideControllerTests.cs`

**Files to create:**
- `CHAP2.Tests/Api/ChorusesControllerTests.cs`
- `CHAP2.Tests/Api/SlideControllerTests.cs`

**Estimated effort:** Medium

### 2.5 Expand Existing Test Coverage (TEST-005)

**Finding:** Existing tests cover happy paths but miss edge cases.

**Tasks:**
- [ ] Add event data verification tests to ChorusCommandServiceTests
- [ ] Add exception propagation tests
- [ ] Add concurrent operation tests
- [ ] Add null input validation tests for ChorusQueryService

**Files to modify:**
- `CHAP2.Tests/Application/ChorusCommandServiceTests.cs`
- `CHAP2.Tests/Application/ChorusQueryServiceTests.cs`

**Estimated effort:** Low

---

## Phase 3: Architecture Improvements (Medium Priority)

### 3.1 Fix Duplicate Domain Event Dispatch (QUAL-002, QUAL-003, MIN-013)

**Finding:** ChorusCreatedEvent is dispatched twice (once from entity, once explicitly). DomainEvents list is publicly mutable and never cleared.

**Tasks:**
- [ ] Change `Chorus.DomainEvents` to private backing field with `IReadOnlyCollection<IDomainEvent>` public accessor
- [ ] Add `internal void ClearDomainEvents()` method to Chorus
- [ ] Remove explicit `new ChorusCreatedEvent()` from `ChorusCommandService.CreateChorusAsync`
- [ ] Use `_eventDispatcher.DispatchAndClearAsync(chorus.DomainEvents)` instead
- [ ] Apply same fix to Update path (ChorusUpdatedEvent)
- [ ] Update tests to verify single event dispatch

**Files to modify:**
- `CHAP2.Domain/Entities/Chorus.cs`
- `CHAP2.Application/Services/ChorusCommandService.cs`

**Estimated effort:** Medium

### 3.2 Fix Exception Swallowing in ChorusApplicationService (ARCH-004)

**Finding:** ChorusApplicationService catches all exceptions and returns false/null, hiding real errors.

**Tasks:**
- [ ] Remove blanket try-catch from `CreateChorusAsync`, `UpdateChorusAsync`, `DeleteChorusAsync`, `GetChorusByIdAsync`
- [ ] Let domain exceptions propagate
- [ ] Change return types from `bool` to `Chorus` or use `Result<T>` pattern
- [ ] Update `IChorusApplicationService` interface accordingly
- [ ] Mark old methods with `[Obsolete]` if consumers exist
- [ ] Update WebPortal consumers to handle exceptions

**Files to modify:**
- `CHAP2.Application/Services/ChorusApplicationService.cs`
- `CHAP2.Application/Interfaces/IChorusApplicationService.cs`
- `CHAP2.UI/CHAP2.WebPortal/Controllers/HomeController.cs` (consumer)

**Estimated effort:** Medium

### 3.3 Fix SlideToChorusService Reflection Usage (ARCH-002)

**Finding:** Uses reflection to bypass private setter on Chorus.Key.

**Tasks:**
- [ ] Add optional key parameter to `Chorus.CreateFromSlide(string name, string chorusText, MusicalKey key = MusicalKey.NotSet)`
- [ ] Remove reflection code from `SlideToChorusService.ConvertToChorus`
- [ ] Update tests

**Files to modify:**
- `CHAP2.Domain/Entities/Chorus.cs`
- `CHAP2.Application/Services/SlideToChorusService.cs`

**Estimated effort:** Low

### 3.4 Fix WebPortal Repository Configuration (QUAL-005)

**Finding:** WebPortal registers bare DiskChorusRepository without caching, inconsistent with API.

**Tasks:**
- [ ] Option A (Recommended): Remove direct repository registration from WebPortal; use API via HttpClient exclusively
- [ ] Option B: Add CachedChorusRepository decorator matching API configuration
- [ ] Remove duplicate repository path configuration
- [ ] Verify all WebPortal features work through API

**Files to modify:**
- `CHAP2.UI/CHAP2.WebPortal/Program.cs`

**Estimated effort:** Medium

### 3.5 Fix SlideController Layer Violation (MIN-009)

**Finding:** SlideController injects IChorusRepository directly, bypassing the service layer.

**Tasks:**
- [ ] Create `ISlideConversionCommandService` interface with appropriate methods
- [ ] Inject `IChorusCommandService` and `IChorusQueryService` instead of `IChorusRepository`
- [ ] Move business logic from controller to service

**Files to modify:**
- `CHAP2.Chorus.Api/Controllers/SlideController.cs`

**Estimated effort:** Low

---

## Phase 4: Code Quality Improvements (Lower Priority)

### 4.1 Add Constructor Null Validation (ARCH-005)

**Finding:** Multiple services lack constructor parameter null validation.

**Tasks:**
- [ ] Add `?? throw new ArgumentNullException(nameof(...))` to all constructor parameters in:
  - [ ] `ChorusApplicationService`
  - [ ] `ChorusSearchService`
  - [ ] `CachedChorusRepository`
  - [ ] `IntelligentSearchService`

**Files to modify:**
- `CHAP2.Application/Services/ChorusApplicationService.cs`
- `CHAP2.Application/Services/ChorusSearchService.cs`
- `CHAP2.Infrastructure/Repositories/CachedChorusRepository.cs`
- `CHAP2.UI/CHAP2.WebPortal/Services/IntelligentSearchService.cs`

**Estimated effort:** Low

### 4.2 Fix Async Methods Without Await (MIN-003)

**Finding:** AiSearchService methods are marked async but don't await anything.

**Tasks:**
- [ ] Remove `async` keyword from `GenerateSearchTermsAsync` and `AnalyzeSearchContextAsync`
- [ ] Return `Task.FromResult(...)` instead
- [ ] Or add `#pragma warning disable CS1998` with comment explaining why

**Files to modify:**
- `CHAP2.Application/Services/AiSearchService.cs`

**Estimated effort:** Low

### 4.3 Add Input Validation for String-to-Type Parsing (MIN-007, MIN-008)

**Finding:** `Guid.Parse` and `Enum.Parse` used without TryParse, causing unhandled FormatExceptions.

**Tasks:**
- [ ] Replace `Guid.Parse(id)` with `Guid.TryParse` in `ChorusApplicationService`
- [ ] Replace `Enum.Parse<SearchMode>` with `Enum.TryParse` in `ChorusApplicationService`
- [ ] Return appropriate error messages for invalid inputs

**Files to modify:**
- `CHAP2.Application/Services/ChorusApplicationService.cs`

**Estimated effort:** Low

### 4.4 Extract SearchApiRequest to Own File (NAME-001)

**Finding:** SearchApiRequest record defined inside SearchController.cs file.

**Tasks:**
- [ ] Move `SearchApiRequest` record to `CHAP2.UI/CHAP2.WebPortal/DTOs/SearchApiRequest.cs`
- [ ] Update using statements

**Files to modify:**
- `CHAP2.UI/CHAP2.WebPortal/Controllers/SearchController.cs`
- `CHAP2.UI/CHAP2.WebPortal/DTOs/SearchApiRequest.cs` (new)

**Estimated effort:** Low

### 4.5 Extract ChorusSearchService Public Methods to Interface (QUAL-001)

**Finding:** ChorusSearchService has public methods not on any interface.

**Tasks:**
- [ ] Create `IChorusSearchService` interface with `SearchByNameAsync`, `SearchByTextAsync`, etc.
- [ ] Have `ChorusSearchService` implement both `ISearchService` and `IChorusSearchService`
- [ ] Or make the additional methods `internal`

**Files to modify:**
- `CHAP2.Application/Interfaces/IChorusSearchService.cs` (new or repurpose existing)
- `CHAP2.Application/Services/ChorusSearchService.cs`

**Estimated effort:** Low

---

## Phase 5: Cleanup and Polish (Lowest Priority)

### 5.1 Remove Dead Code

**Tasks:**
- [ ] Remove `IServices` interface and `Services` class (placeholder code)
- [ ] Remove `IController` empty marker interface
- [ ] Update DI registrations to remove `IServices`/`Services` binding
- [ ] Move `SearchRequest` and `SearchResult` records from `Interfaces` to `Models` folder

**Files to modify/delete:**
- `CHAP2.Chorus.Api/Interfaces/IServices.cs` (delete)
- `CHAP2.Chorus.Api/Services/Services.cs` (delete)
- `CHAP2.Chorus.Api/Interfaces/IController.cs` (delete)
- `CHAP2.Chorus.Api/Controllers/ChapControllerAbstractBase.cs` (remove IController)
- `CHAP2.Chorus.Api/Program.cs` (remove IServices registration)

**Estimated effort:** Low

### 5.2 Improve ChorusMetadata Immutability (ARCH-001)

**Tasks:**
- [ ] Convert `ChorusMetadata` to use `init` setters
- [ ] Replace `List<string> Tags` with `IReadOnlyList<string>`
- [ ] Replace `Dictionary<string, string> CustomProperties` with `IReadOnlyDictionary<string, string>`
- [ ] Add factory/builder methods for creating modified copies
- [ ] Update `ChorusMetadataJsonConverter` for new design
- [ ] Update all consumers

**Files to modify:**
- `CHAP2.Domain/ValueObjects/ChorusMetadata.cs`
- `CHAP2.Domain/ValueObjects/ChorusMetadataJsonConverter.cs`
- `CHAP2.Infrastructure/DTOs/ChorusDto.cs`
- Various consumer files

**Estimated effort:** High

### 5.3 Add Configuration Validation (MIN-014)

**Tasks:**
- [ ] Add startup validation for required configuration sections
- [ ] Use `IOptions<T>.Validate()` or `IStartupFilter` to fail fast on missing config

**Files to modify:**
- `CHAP2.Chorus.Api/Program.cs`
- `CHAP2.UI/CHAP2.WebPortal/Program.cs`

**Estimated effort:** Low

---

## Execution Summary

| Phase | Items | Estimated Effort | Priority |
|-------|-------|------------------|----------|
| Phase 1: Security Hardening | 5 | Low-Medium | Critical |
| Phase 2: Test Coverage | 5 | High | High |
| Phase 3: Architecture | 5 | Medium | Medium |
| Phase 4: Code Quality | 5 | Low | Medium |
| Phase 5: Cleanup | 3 | Low-High | Lower |

### Quick Wins (< 1 hour each)

- Add regex timeout to ChorusSearchService (SEC-003) - 15 minutes
- Add `UseHttpsRedirection()` to API Program.cs (SEC-005) - 5 minutes
- Add constructor null validation to 4 services (ARCH-005) - 30 minutes
- Fix async-without-await in AiSearchService (MIN-003) - 10 minutes
- Extract SearchApiRequest to its own file (NAME-001) - 10 minutes
- Add Guid.TryParse / Enum.TryParse (MIN-007/MIN-008) - 15 minutes
- Remove dead code: IServices, IController, Services (Phase 5.1) - 20 minutes

---

## Progress Tracking

Phase 1: [ ] [ ] [ ] [ ] [ ]
Phase 2: [ ] [ ] [ ] [ ] [ ]
Phase 3: [ ] [ ] [ ] [ ] [ ]
Phase 4: [ ] [ ] [ ] [ ] [ ]
Phase 5: [ ] [ ] [ ]

---
---

# Frontend Improvement Plan - CHAP2 WebPortal

**Generated:** 2026-03-28
**Based On:** Frontend Findings Review (FE-REVIEW-2026-03-28)
**Framework:** ASP.NET Razor + Vanilla JavaScript
**Status:** Planning Phase

---

## Overview

This plan addresses 53 findings across 8 categories discovered during a comprehensive frontend code review of the CHAP2 WebPortal. Issues are organized into 6 phases prioritized by: Security > Accessibility > Performance > Architecture > Tests > Code Quality. Each phase contains specific tasks with file locations and effort estimates.

---

## FE Phase 1: Security Hardening (Critical Priority)

XSS vulnerabilities and CSRF gaps that could expose user data or enable attacks.

### FE-1.1 Fix innerHTML XSS Vulnerabilities (SEC-001, SEC-002, SEC-003)

**Finding:** Multiple locations use `innerHTML` with unsanitized user input or API data, enabling DOM-based XSS.

**Tasks:**
- [ ] Fix `highlightSearchTerm()` in `search-ui.js:322-327` - escape regex special chars and sanitize output
- [ ] Fix `highlightSearchTerm()` in `detail.js:226-240` - same pattern, user input from URL params
- [ ] Fix `showNotification()` in `utils.js:47-52` - use `textContent` for message
- [ ] Fix `showNotification()` in `form.js:319` - use `textContent` for message
- [ ] Fix `showNotification()` in `delete-modal.js:113` - use `textContent` for message
- [ ] Fix `showNotification()` in `setlist.js:354-356` - use `textContent` for message
- [ ] Fix `showSuccessDialog()` in `system-restart.js:180` - sanitize API response
- [ ] Fix `showErrorDialog()` in `system-restart.js:210` - sanitize API response
- [ ] Add `escapeRegex()` utility to `utils.js` for safe regex construction

**Files to modify:**
- `wwwroot/js/utils.js`
- `wwwroot/js/search-ui.js`
- `wwwroot/js/detail.js`
- `wwwroot/js/form.js`
- `wwwroot/js/delete-modal.js`
- `wwwroot/js/setlist.js`
- `wwwroot/js/system-restart.js`

**Estimated effort:** Medium (2-4 hours)

### FE-1.2 Fix Setlist XSS Vulnerability (SEC-004)

**Finding:** Setlist rendering injects chorus names directly into innerHTML without escaping.

**Tasks:**
- [ ] Apply `utils.escapeHtml()` to all user-derived values in `refreshDisplay()` template literals
- [ ] Audit all other template literal usages for unsanitized data

**Files to modify:**
- `wwwroot/js/setlist.js`

**Estimated effort:** Low (30 minutes)

### FE-1.3 Add CSRF Tokens to JSON API Calls (SEC-005)

**Finding:** Save and delete operations in Edit page inline scripts omit anti-forgery tokens.

**Tasks:**
- [ ] Add `RequestVerificationToken` header to `saveCurrentChorus()` fetch call
- [ ] Add `RequestVerificationToken` header to `deleteCurrentChorus()` fetch call
- [ ] Verify all POST/PUT/DELETE endpoints validate the token server-side

**Files to modify:**
- `Views/Home/Edit.cshtml` (inline script)

**Estimated effort:** Low (30 minutes)

### FE-1.4 Remove Debug/Test Scripts from Production (SEC-006)

**Finding:** Debug and test scripts are loaded in the production layout.

**Tasks:**
- [ ] Remove `debug-crud.js` from `_Layout.cshtml`
- [ ] Remove `test-enum-conversion.js` from any page references
- [ ] Conditionally load `system-restart.js` only in Development environment
- [ ] Add `@if (Environment.IsDevelopment())` guards in Razor for debug scripts

**Files to modify:**
- `Views/Shared/_Layout.cshtml`

**Estimated effort:** Low (15 minutes)

---

## FE Phase 2: Accessibility Compliance (High Priority)

WCAG violations affecting users with disabilities and potential legal compliance issues.

### FE-2.1 Add Form Labels and ARIA (A11Y-001)

**Finding:** Search inputs lack associated label elements.

**Tasks:**
- [ ] Add visually-hidden `<label>` elements for `#searchInput`, `#aiSearchInput`
- [ ] Add `aria-label` attributes to all filter `<select>` elements
- [ ] Ensure all dynamically-created inputs in `search-integration.js` have labels
- [ ] Add `aria-label` to icon-only buttons throughout (A11Y-008)

**Files to modify:**
- `Views/Home/Index.cshtml`
- `wwwroot/js/search-integration.js`
- `wwwroot/js/search-ui.js`

**Estimated effort:** Medium (1-2 hours)

### FE-2.2 Keyboard Accessibility (A11Y-002)

**Finding:** Clickable table rows and setlist items are not keyboard-accessible.

**Tasks:**
- [ ] Add `tabindex="0"`, `role="row"` to `.result-row` elements
- [ ] Add `keydown` handler for Enter/Space to trigger row click
- [ ] Provide keyboard alternative for setlist drag-and-drop
- [ ] Ensure all custom buttons created via JS are keyboard-accessible

**Files to modify:**
- `wwwroot/js/search-ui.js`
- `wwwroot/js/search-v2.js`
- `wwwroot/js/setlist.js`

**Estimated effort:** Medium (1-2 hours)

### FE-2.3 Skip Navigation and ARIA Landmarks (A11Y-003)

**Finding:** No skip-to-content link exists.

**Tasks:**
- [ ] Add skip link as first element in `_Layout.cshtml`
- [ ] Add `<main id="main-content">` landmark to content area
- [ ] Add `role="search"` to search container
- [ ] Add `<nav>` landmark to tab navigation

**Files to modify:**
- `Views/Shared/_Layout.cshtml`
- `Views/Home/Index.cshtml`

**Estimated effort:** Low (30 minutes)

### FE-2.4 Modal Accessibility (A11Y-004, A11Y-007)

**Finding:** Modals lack focus trapping, ARIA roles, and proper keyboard handling.

**Tasks:**
- [ ] Add `role="dialog"`, `aria-modal="true"`, `aria-labelledby` to all modal overlays
- [ ] Implement focus trap (cycle Tab through modal elements only)
- [ ] Return focus to trigger element when modal closes
- [ ] Ensure Escape key closes all modals consistently

**Files to modify:**
- `wwwroot/js/site.js`
- `wwwroot/js/delete-modal.js`
- `wwwroot/js/form.js`
- `Views/Home/Index.cshtml`

**Estimated effort:** Medium (2-3 hours)

### FE-2.5 ARIA Live Regions and Reduced Motion (A11Y-005, A11Y-006, A11Y-009)

**Finding:** Dynamic content updates not announced; status uses color only; no reduced motion support.

**Tasks:**
- [ ] Add `aria-live="polite"` to search results container
- [ ] Add `aria-live="assertive"` to error containers
- [ ] Add text label to connection status indicator
- [ ] Add `@media (prefers-reduced-motion: reduce)` to disable animations

**Files to modify:**
- `Views/Home/Index.cshtml`
- `wwwroot/css/site.css`
- `wwwroot/css/chorus-display.css`

**Estimated effort:** Low (1 hour)

---

## FE Phase 3: Performance Optimization (High Priority)

Bundle size, loading performance, and runtime efficiency.

### FE-3.1 Remove Console Logging (PERF-001)

**Finding:** 100+ console.log statements across all JS files, including sensitive form data.

**Tasks:**
- [ ] Remove or gate all `console.log` statements behind a DEBUG constant
- [ ] Remove form data logging from `form.js` (security concern)
- [ ] Create a `utils.debug()` wrapper that only logs in development

**Files to modify:** All JS files (primary: `form.js`, `search-v2.js`, `ai-search.js`, `search-integration.js`)

**Estimated effort:** Medium (1-2 hours)

### FE-3.2 Consolidate Duplicate Files (PERF-002)

**Finding:** `search-v2.js` and `search-working.js` are near-identical duplicates.

**Tasks:**
- [ ] Determine which search implementation is canonical
- [ ] Remove `search-working.js`
- [ ] Remove `test-enum-conversion.js`
- [ ] Update all page references

**Files to delete:** `wwwroot/js/search-working.js`, `wwwroot/js/test-enum-conversion.js`

**Estimated effort:** Medium (1-2 hours)

### FE-3.3 Implement Bundling and Minification (PERF-003)

**Finding:** 15+ individual unminified JS/CSS files loaded with cache-busting timestamps.

**Tasks:**
- [ ] Configure ASP.NET Core bundling (BundlerMinifier or WebOptimizer)
- [ ] Create bundles for layout, search, form, and display scripts
- [ ] Enable CSS minification
- [ ] Replace `DateTime.Now.Ticks` cache busting with content hashes
- [ ] Consider self-hosting Google Fonts and Font Awesome (PERF-004)

**Estimated effort:** High (3-5 hours)

---

## FE Phase 4: Architecture Improvements (Medium Priority)

### FE-4.1 Extract Inline JavaScript from Views (ARCH-003)

**Tasks:**
- [ ] Create `wwwroot/js/chorus-edit-nav.js` from Edit.cshtml inline JS
- [ ] Create `wwwroot/js/index-tabs.js` from Index.cshtml inline JS
- [ ] Pass server data via `data-` attributes or minimal `window.pageConfig`

**Estimated effort:** Medium (2-3 hours)

### FE-4.2 Consolidate Duplicate Code (ARCH-002, ARCH-005)

**Tasks:**
- [ ] Make `utils.showNotification()` the single implementation (remove 4 duplicates)
- [ ] Create centralized enum maps in `utils.js` (remove 4 duplicates)

**Estimated effort:** Medium (2-3 hours)

### FE-4.3 Unify Page Layout Architecture (ARCH-004)

**Tasks:**
- [ ] Create `_StandalonePage.cshtml` shared layout for Edit, Create, ChorusDisplay pages
- [ ] Extract common `<head>` content into a partial

**Estimated effort:** Medium (2-3 hours)

### FE-4.4 Reduce Global State Pollution (ARCH-001)

**Tasks:**
- [ ] Wrap JS files in IIFEs or module pattern
- [ ] Minimize `window` attachments to a single namespace

**Estimated effort:** High (4-6 hours)

---

## FE Phase 5: Test Coverage (Medium Priority)

### FE-5.1 Set Up Test Framework (TEST-001)

**Tasks:**
- [ ] Install Jest/Vitest with jsdom
- [ ] Configure test runner
- [ ] Create test directory structure

**Estimated effort:** Medium (1-2 hours)

### FE-5.2 Write Critical Path Tests (TEST-002)

**Tasks:**
- [ ] Test `utils.escapeHtml()` with XSS payloads
- [ ] Test `utils.isValidMusicalKey()` with all inputs
- [ ] Test `SearchService` search/cancel logic
- [ ] Test form validation functions
- [ ] Test enum display mapping completeness

**Estimated effort:** High (4-6 hours)

---

## FE Phase 6: Code Quality and UX (Lower Priority)

### FE-6.1 Fix Keyboard Shortcut Issues (CODE-001, CODE-002)

**Tasks:**
- [ ] Remove Ctrl+C override in `detail.js`
- [ ] Remove Escape-closes-window in `detail.js`

**Estimated effort:** Low (30 minutes)

### FE-6.2 UX Improvements

**Tasks:**
- [ ] Add progressive status updates for AI search (UX-001)
- [ ] Use custom delete modal in mass edit mode (UX-002)
- [ ] Add initial page state guidance (UX-003)
- [ ] Reduce auto-save notification frequency (UX-004)
- [ ] Replace native `confirm()` with custom modal (UX-005)

**Estimated effort:** Medium (3-4 hours)

### FE-6.3 Naming Cleanup

**Tasks:**
- [ ] Rename `search-v2.js` to `search.js`
- [ ] Standardize element ID naming conventions

**Estimated effort:** Low (1 hour)

---

## Frontend Execution Summary

| Phase | Items | Estimated Effort | Priority |
|-------|-------|------------------|----------|
| FE Phase 1: Security | 4 tasks | 3-5 hours | Critical |
| FE Phase 2: Accessibility | 5 tasks | 5-8 hours | High |
| FE Phase 3: Performance | 3 tasks | 5-9 hours | High |
| FE Phase 4: Architecture | 4 tasks | 10-15 hours | Medium |
| FE Phase 5: Test Coverage | 2 tasks | 5-8 hours | Medium |
| FE Phase 6: Code Quality/UX | 3 tasks | 4-6 hours | Lower |

### Frontend Quick Wins (< 1 hour each)

- Remove debug scripts from `_Layout.cshtml` (SEC-006) - 15 min
- Add CSRF tokens to Edit page API calls (SEC-005) - 30 min
- Add skip-to-content link (A11Y-003) - 30 min
- Fix Ctrl+C and Escape key overrides (CODE-001, CODE-002) - 30 min
- Fix setlist innerHTML XSS (SEC-004) - 30 min
- Add `aria-live` regions to search results (A11Y-005) - 30 min

---

## Frontend Progress Tracking

FE Phase 1: [ ] 1.1 [ ] 1.2 [ ] 1.3 [ ] 1.4
FE Phase 2: [ ] 2.1 [ ] 2.2 [ ] 2.3 [ ] 2.4 [ ] 2.5
FE Phase 3: [ ] 3.1 [ ] 3.2 [ ] 3.3
FE Phase 4: [ ] 4.1 [ ] 4.2 [ ] 4.3 [ ] 4.4
FE Phase 5: [ ] 5.1 [ ] 5.2
FE Phase 6: [ ] 6.1 [ ] 6.2 [ ] 6.3

---

## Frontend Re-Validation Guidance

After fixes are applied, verify with these steps:

1. **Security (FE Phase 1):**
   - Enter `<img src=x onerror=alert(1)>` as search term - must be escaped
   - Create chorus with HTML in name - notifications must not render HTML
   - Verify CSRF tokens present in all POST requests via DevTools Network tab

2. **Accessibility (FE Phase 2):**
   - Full keyboard-only navigation test (Tab through entire application)
   - Screen reader test with VoiceOver (macOS) or NVDA (Windows)
   - Run axe DevTools audit on all pages

3. **Performance (FE Phase 3):**
   - Open DevTools Console - should see minimal logging
   - Check Network tab for reduced number of JS/CSS requests
   - Run Lighthouse performance audit

4. **Architecture (FE Phase 4):**
   - Verify no inline `<script>` blocks with business logic in .cshtml files
   - Search for `showNotification` - should find single definition

5. **Tests (FE Phase 5):**
   - Run `npm test` and verify all tests pass
   - Check coverage report for >80% on utility functions
