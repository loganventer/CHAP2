# Backend Findings

**Last Updated:** 2026-03-28
**Review Session:** BE-REVIEW-20260328
**Target Framework:** .NET 9.0
**Agent Depth Used:** 2 (master -> quality orchestrator -> 5 parallel reviewers)

## Summary Dashboard

| Category | Critical | Major | Minor | High Impact | Status |
|----------|----------|-------|-------|-------------|--------|
| Architecture | 1 | 5 | 3 | 3 | Warning |
| Security | 2 | 3 | 2 | 4 | CRITICAL |
| Code Quality | 0 | 5 | 4 | 2 | Warning |
| Test Coverage | 1 | 4 | 2 | 3 | Warning |
| Naming | 0 | 1 | 3 | 0 | Warning |
| **Total** | **4** | **18** | **14** | **12** | **CRITICAL** |

## Priority Matrix

Issues are prioritized by combining Technical Severity x Business Impact:

| | High Impact | Medium Impact | Low Impact |
|---|-------------|---------------|------------|
| **Critical** | P0 - Immediate | P1 - Urgent | P2 - High |
| **Major** | P1 - Urgent | P2 - High | P3 - Medium |
| **Minor** | P2 - High | P3 - Medium | P4 - Low |

---

## Critical Issues

### SEC-001: Overly Permissive CORS - AllowAnyOrigin on API
**Source Agent:** 01-01-04-BE-security-reviewer (depth 2)
**File:** `CHAP2.Chorus.Api/Program.cs:18-27`
**Category:** Security (OWASP A05 - Security Misconfiguration)
**Technical Severity:** Critical
**Business Impact:** High
**Priority:** P0
**Fixable:** Yes
**Fix Agent:** Manual
**Description:** The API configures CORS with `AllowAnyOrigin()`, `AllowAnyMethod()`, and `AllowAnyHeader()`. This allows any website to make requests to the API, enabling cross-site request forgery and data exfiltration attacks.
**Current State:** `policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()`
**Expected State:** Restrict to known origins: `policy.WithOrigins("https://your-domain.com").AllowAnyMethod().AllowAnyHeader()`
**Impact:** Any malicious website can call API endpoints and manipulate chorus data if a user's browser has access.
**Recommendation:** Replace `AllowAnyOrigin()` with `WithOrigins()` specifying known frontend domains. Same issue in WebPortal Program.cs lines 76-84.
**Validation:** Verify CORS headers only return configured origins. Test cross-origin requests from unlisted domains fail.
**Dependencies:** None
**Status:** Open

### SEC-002: Overly Permissive CORS - AllowAnyOrigin on WebPortal
**Source Agent:** 01-01-04-BE-security-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Program.cs:76-84`
**Category:** Security (OWASP A05 - Security Misconfiguration)
**Technical Severity:** Critical
**Business Impact:** High
**Priority:** P0
**Fixable:** Yes
**Fix Agent:** Manual
**Description:** Same permissive CORS configuration as SEC-001, duplicated in the WebPortal.
**Current State:** `policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()`
**Expected State:** Restrict to known origins.
**Impact:** Same as SEC-001 - any website can make requests.
**Recommendation:** Configure specific allowed origins.
**Validation:** Same as SEC-001.
**Dependencies:** None
**Related:** SEC-001
**Status:** Open

### SEC-003: Regex Denial of Service (ReDoS) via User-Supplied Regex Patterns
**Source Agent:** 01-01-02-BE-code-reviewer (depth 2)
**File:** `CHAP2.Application/Services/ChorusSearchService.cs:441-452`
**Category:** Security (OWASP A03 - Injection)
**Technical Severity:** Critical
**Business Impact:** High
**Priority:** P0
**Fixable:** Yes
**Fix Agent:** 01-02-01-BE-code-quality-fix-agent
**Description:** The `IsRegexMatch` method passes user-supplied regex patterns directly to `Regex.IsMatch()` without a timeout. A crafted regex pattern (e.g., `(a+)+$` with a long input) can cause catastrophic backtracking, consuming CPU indefinitely and causing a denial of service.
**Current State:** `Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase)` - no timeout
**Expected State:** `Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1))` with try/catch for RegexMatchTimeoutException
**Impact:** An attacker can crash or freeze the application by submitting a malicious regex via the search API endpoint.
**Recommendation:** Add a `TimeSpan` timeout parameter to the Regex call and catch `RegexMatchTimeoutException`.
**Validation:** Test with known ReDoS patterns and verify they timeout gracefully.
**Dependencies:** None
**Status:** Open

### TEST-001: No Tests for Infrastructure Layer (DiskChorusRepository, CachedChorusRepository)
**Source Agent:** 01-01-03-BE-test-coverage-reviewer (depth 2)
**File:** `CHAP2.Infrastructure/Repositories/DiskChorusRepository.cs` (entire file)
**Category:** Test Coverage
**Technical Severity:** Critical
**Business Impact:** High
**Priority:** P0
**Fixable:** Yes
**Fix Agent:** 01-02-02-BE-test-fix-agent
**Description:** The `DiskChorusRepository` is the sole persistence mechanism (file-based JSON storage). It has zero test coverage despite being the critical data access layer. `CachedChorusRepository` also has no tests. Any regression in serialization, file I/O, or caching would silently corrupt or lose data.
**Current State:** No test files exist for Infrastructure project.
**Expected State:** DiskChorusRepositoryTests.cs and CachedChorusRepositoryTests.cs with tests for all CRUD operations, error handling, and concurrency.
**Impact:** Data corruption or loss could go undetected; file locking bugs could cause race conditions in production.
**Recommendation:** Create integration tests for DiskChorusRepository using a temporary directory, and unit tests for CachedChorusRepository using a mocked inner repository.
**Validation:** `dotnet test` passes with new tests; coverage for Infrastructure > 80%.
**Dependencies:** None
**Status:** Open

---

## Major Issues

### ARCH-001: ChorusMetadata Is a Mutable Class Posing as a Value Object
**Source Agent:** 01-01-01-BE-architecture-reviewer (depth 2)
**File:** `CHAP2.Domain/ValueObjects/ChorusMetadata.cs:1-52`
**Category:** Architecture (SOLID - DDD Value Object violation)
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Description:** `ChorusMetadata` is in the ValueObjects folder but has mutable public setters on all properties, mutable collections (`List<string> Tags`, `Dictionary<string, string> CustomProperties`), and mutation methods (`AddTag`, `RemoveTag`, `SetCustomProperty`). DDD value objects must be immutable. This breaks value semantics and can cause subtle bugs when shared references are mutated unexpectedly.
**Current State:** Mutable class with public setters.
**Expected State:** Immutable record with `with` expressions or builder pattern for creating modified copies.
**Recommendation:** Convert to a record or make all setters `init`-only; use `ImmutableList` and `ImmutableDictionary` for collections; replace mutation methods with methods that return new instances.
**Validation:** Build succeeds; all tests pass; no external code mutates properties directly.
**Dependencies:** Will require updates in ChorusMetadataJsonConverter, Infrastructure DTO mapping.
**Status:** Open

### ARCH-002: SlideToChorusService Uses Reflection to Set Private Property
**Source Agent:** 01-01-01-BE-architecture-reviewer (depth 2)
**File:** `CHAP2.Application/Services/SlideToChorusService.cs:92-99`
**Category:** Architecture (Encapsulation violation)
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Description:** The service uses `typeof(Chorus).GetProperty("Key")` and `keyProperty.SetValue(chorus, key)` to bypass the private setter on `Chorus.Key`. This violates domain encapsulation, will silently fail if the property is renamed, and is fragile.
**Current State:** Reflection-based property setting.
**Expected State:** Add a method like `Chorus.SetKey(MusicalKey key)` or extend `CreateFromSlide` to accept a key parameter.
**Recommendation:** Add a public `SetKey` method on the Chorus entity or modify `CreateFromSlide` to accept optional key/type/timeSignature parameters.
**Validation:** `dotnet build` succeeds; slide import correctly sets key without reflection.
**Dependencies:** ARCH-001 (metadata changes may interact)
**Status:** Open

### ARCH-003: DomainEventDispatcher Uses IServiceProvider (Service Locator Anti-Pattern)
**Source Agent:** 01-01-01-BE-architecture-reviewer (depth 2)
**File:** `CHAP2.Application/Services/DomainEventDispatcher.cs:13-14`
**Category:** Architecture (DIP violation / Service Locator anti-pattern)
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Fixable:** Partial
**Fix Agent:** Manual
**Description:** `DomainEventDispatcher` takes `IServiceProvider` and uses reflection to resolve handlers via `scope.ServiceProvider.GetServices(handlerType)` and `handlerType.GetMethod("HandleAsync")`. This is a known anti-pattern that hides dependencies, makes testing harder, and is fragile.
**Current State:** IServiceProvider injection with reflection-based handler invocation.
**Expected State:** Use MediatR or a typed handler registry pattern. Alternatively, inject `IEnumerable<IDomainEventHandler<T>>` directly for known event types.
**Recommendation:** Consider using MediatR for event dispatching, or register a typed dispatcher factory. The reflection approach is the pragmatic minimum for generic event dispatching, but the reflection on method names is the real concern.
**Validation:** Events still dispatch correctly; unit tests can mock handlers without IServiceProvider.
**Dependencies:** None
**Status:** Open

### ARCH-004: ChorusApplicationService Swallows Exceptions and Returns Boolean
**Source Agent:** 01-01-02-BE-code-reviewer (depth 2)
**File:** `CHAP2.Application/Services/ChorusApplicationService.cs:25-47`
**Category:** Architecture / Error Handling
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Fixable:** Yes
**Fix Agent:** 01-02-01-BE-code-quality-fix-agent
**Description:** `CreateChorusAsync`, `UpdateChorusAsync`, `DeleteChorusAsync`, and `GetChorusByIdAsync` catch all exceptions and return `false`/`null` instead of propagating domain exceptions. The caller (WebPortal) has no way to distinguish between "chorus not found", "validation failed", and "database error". This masks real errors and makes debugging extremely difficult.
**Current State:** `catch (Exception ex) { _logger.LogError(ex, ...); return false; }`
**Expected State:** Let domain exceptions propagate to the caller; only catch infrastructure exceptions if there's a meaningful recovery strategy.
**Impact:** Users see generic failures; administrators cannot diagnose issues from logs alone because the exception is caught and discarded at the service layer.
**Recommendation:** Remove the blanket try-catch blocks; let domain exceptions propagate. If error translation is needed, use a Result<T> pattern instead of boolean returns.
**Validation:** Verify that domain exceptions (ChorusNotFoundException, etc.) reach the controller layer and produce appropriate HTTP responses.
**Dependencies:** None
**Status:** Open

### ARCH-005: Missing Constructor Null Validation in Multiple Services
**Source Agent:** 01-01-02-BE-code-reviewer (depth 2)
**Files:** `CHAP2.Application/Services/ChorusApplicationService.cs:20-23`, `CHAP2.Application/Services/ChorusSearchService.cs:28-33`, `CHAP2.UI/CHAP2.WebPortal/Services/IntelligentSearchService.cs:15-27`
**Category:** Code Quality (DI best practice)
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Fixable:** Yes
**Fix Agent:** 01-02-01-BE-code-quality-fix-agent
**Description:** Several services accept constructor parameters without null validation (`?? throw new ArgumentNullException`). While DI containers typically prevent null injection, this violates defensive programming standards and makes the code fragile to manual construction in tests.
**Current State:** Direct assignment without null checks: `_chorusCommandService = chorusCommandService;`
**Expected State:** `_chorusCommandService = chorusCommandService ?? throw new ArgumentNullException(nameof(chorusCommandService));`
**Recommendation:** Add null validation to all constructor parameters across ChorusApplicationService, ChorusSearchService, CachedChorusRepository, IntelligentSearchService.
**Validation:** `dotnet build` succeeds; constructing with null throws ArgumentNullException.
**Dependencies:** None
**Status:** Open

### SEC-004: No Authentication or Authorization on Any API Endpoint
**Source Agent:** 01-01-04-BE-security-reviewer (depth 2)
**File:** `CHAP2.Chorus.Api/Controllers/ChorusesController.cs` (entire file)
**Category:** Security (OWASP A01 - Broken Access Control)
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Fixable:** Yes
**Fix Agent:** Manual
**Description:** No `[Authorize]` attributes exist on any controller or action. The API has no authentication middleware configured. Anyone with network access can create, update, and delete choruses. While this may be intentional for an internal church application, it is a significant security gap if the API is exposed to the internet.
**Current State:** No authentication or authorization configured.
**Expected State:** At minimum, API key authentication for write operations; ideally OAuth2/OpenID Connect for full authentication.
**Impact:** Unauthorized users can modify or delete all chorus data.
**Recommendation:** If the API is internal-only, document this decision. If exposed externally, add authentication middleware and `[Authorize]` attributes on write endpoints (POST, PUT, DELETE).
**Validation:** Unauthenticated requests to write endpoints return 401.
**Dependencies:** None
**Status:** Open

### SEC-005: Missing HTTPS Enforcement on API
**Source Agent:** 01-01-04-BE-security-reviewer (depth 2)
**File:** `CHAP2.Chorus.Api/Program.cs:76-86`
**Category:** Security (OWASP A02 - Cryptographic Failures)
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Description:** The API's Program.cs does not call `app.UseHttpsRedirection()`. The WebPortal does enforce HTTPS in non-development, but the API does not. Data in transit (including chorus content) is unencrypted.
**Current State:** No `UseHttpsRedirection()` in API Program.cs.
**Expected State:** `app.UseHttpsRedirection()` added before `app.MapControllers()`.
**Recommendation:** Add HTTPS redirection to the API Program.cs.
**Validation:** HTTP requests to the API redirect to HTTPS.
**Dependencies:** None
**Status:** Open

### SEC-006: Regex Search Mode Accepts Arbitrary User Patterns Without Sanitization
**Source Agent:** 01-01-04-BE-security-reviewer (depth 2)
**File:** `CHAP2.Chorus.Api/Controllers/ChorusesController.cs:92-96`
**Category:** Security (OWASP A03 - Injection)
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** 01-02-01-BE-code-quality-fix-agent
**Description:** The search endpoint accepts a `SearchMode.Regex` parameter allowing users to submit arbitrary regex patterns. Combined with SEC-003 (no timeout), this is an injection vector for ReDoS attacks. Even with the timeout fix from SEC-003, exposing regex to untrusted users is risky.
**Current State:** User can pass `searchMode=Regex` with any pattern.
**Expected State:** Either disable Regex mode for external API, or validate/sandbox the regex pattern, or limit it to simple wildcard patterns only.
**Recommendation:** Consider removing Regex search mode from the public API, or add pattern complexity limits.
**Validation:** Regex mode is either removed or validated; complex patterns are rejected.
**Dependencies:** SEC-003
**Status:** Open

### QUAL-001: ChorusSearchService Implements ISearchService But Also Has Additional Public Methods
**Source Agent:** 01-01-02-BE-code-reviewer (depth 2)
**File:** `CHAP2.Application/Services/ChorusSearchService.cs`
**Category:** Code Quality (ISP violation)
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Fixable:** Yes
**Fix Agent:** Manual
**Description:** `ChorusSearchService` implements `ISearchService` (which has `SearchAsync` and `SearchWithAiAsync`) but also exposes `SearchByNameAsync`, `SearchByTextAsync`, `SearchByKeyAsync`, `SearchAllAsync`, and `InvalidateCache` as public methods that are not on any interface. This means consumers depend on the concrete type rather than the abstraction.
**Current State:** Extra public methods not on any interface.
**Expected State:** Either add these to a separate interface or make them private/internal.
**Recommendation:** Create an `IChorusSearchService` interface for the additional methods, or make them internal.
**Validation:** `dotnet build` succeeds; no external consumers reference concrete methods.
**Dependencies:** None
**Status:** Open

### QUAL-002: Duplicate Domain Event Dispatch in ChorusCommandService.CreateChorusAsync
**Source Agent:** 01-01-02-BE-code-reviewer (depth 2)
**File:** `CHAP2.Application/Services/ChorusCommandService.cs:31-36`
**Category:** Bug Risk
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** 01-02-01-BE-code-quality-fix-agent
**Description:** `Chorus.Create()` already adds a `ChorusCreatedEvent` to `DomainEvents` (line 83 of Chorus.cs). Then `ChorusCommandService.CreateChorusAsync` manually dispatches a *new* `ChorusCreatedEvent` (line 36). This means the created event is raised twice - once in the entity and once explicitly. The entity's event list is never cleared or dispatched.
**Current State:** Duplicate event creation - entity adds one, service creates another.
**Expected State:** Either dispatch the entity's domain events using `DispatchAndClearAsync(chorus.DomainEvents)`, or don't add events in the entity's factory method.
**Impact:** Event handlers fire with different event instances; if handlers are idempotent this is wasteful, if not it could cause duplicate side effects.
**Recommendation:** Remove the explicit `new ChorusCreatedEvent()` from the service and instead call `_eventDispatcher.DispatchAndClearAsync(chorus.DomainEvents)`.
**Validation:** Verify event handlers fire exactly once per create operation.
**Dependencies:** None
**Status:** Open

### QUAL-003: DomainEvents List on Chorus Entity Is Publicly Mutable
**Source Agent:** 01-01-01-BE-architecture-reviewer (depth 2)
**File:** `CHAP2.Domain/Entities/Chorus.cs:19`
**Category:** Architecture (Encapsulation)
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** 01-02-01-BE-code-quality-fix-agent
**Description:** `public List<IDomainEvent> DomainEvents { get; } = new();` - the list itself is exposed as a mutable `List<IDomainEvent>`. Any consumer can `Clear()`, `Add()`, or `Remove()` events. This breaks encapsulation of the aggregate root.
**Current State:** `public List<IDomainEvent> DomainEvents { get; } = new();`
**Expected State:** Expose as `IReadOnlyCollection<IDomainEvent>` with a private backing list and explicit `AddDomainEvent`/`ClearDomainEvents` methods.
**Recommendation:** Change to private `_domainEvents` list with public `IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly()` and internal `AddDomainEvent`/`ClearDomainEvents` methods.
**Validation:** `dotnet build` succeeds; test that external code cannot mutate the events list.
**Dependencies:** QUAL-002 (fixing event dispatch should be done together)
**Status:** Open

### QUAL-004: DiskChorusRepository Registered as Singleton with File I/O
**Source Agent:** 01-01-02-BE-code-reviewer (depth 2)
**File:** `CHAP2.Chorus.Api/Program.cs:39-44`
**Category:** Code Quality (Lifetime mismatch)
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Description:** `DiskChorusRepository` is registered as `Singleton` but uses `SemaphoreSlim` for file locking. While the semaphore works correctly as a singleton, the repository holds `JsonSerializerOptions` as instance state. More critically, the `CachedChorusRepository` decorator wrapping it is also singleton, but the scoped services (`ChorusCommandService`, `ChorusQueryService`) depend on `IChorusRepository`. This is a captive dependency - scoped services hold references to a singleton, which works but violates DI best practices and can mask memory leaks.
**Current State:** Singleton repository injected into scoped services.
**Expected State:** Either register repository as scoped (with appropriate caching), or register command/query services as singleton too.
**Recommendation:** The current setup works functionally but should be documented. Consider making the repository scoped with cache at a higher level.
**Validation:** No runtime errors; verify no memory leaks under load.
**Dependencies:** None
**Status:** Open

### QUAL-005: WebPortal Registers DiskChorusRepository Directly (Scoped) Without Cache Decorator
**Source Agent:** 01-01-01-BE-architecture-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Program.cs:46-55`
**Category:** Architecture (Inconsistency)
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** Manual
**Description:** The API uses `CachedChorusRepository` wrapping `DiskChorusRepository`, but the WebPortal registers a bare `DiskChorusRepository` as `IChorusRepository`. This means the WebPortal reads all JSON files from disk on every request without caching, causing significant I/O overhead. Additionally, the WebPortal creates its own DiskChorusRepository pointing to a relative path, which could differ from the API's data path.
**Current State:** WebPortal uses uncached `DiskChorusRepository` with relative path `../../CHAP2.Chorus.Api/data/chorus`.
**Expected State:** WebPortal should either use the API via HTTP (it already has `IChorusApiService`), or use `CachedChorusRepository` like the API does.
**Impact:** Every search request in the WebPortal reads every JSON file from disk, causing slow responses as the dataset grows.
**Recommendation:** Remove direct repository registration from WebPortal and use the API service exclusively, or add the same caching decorator.
**Validation:** WebPortal search performance is acceptable; no duplicate data path configurations.
**Dependencies:** None
**Status:** Open

### TEST-002: No Tests for ChorusSearchService
**Source Agent:** 01-01-03-BE-test-coverage-reviewer (depth 2)
**File:** `CHAP2.Application/Services/ChorusSearchService.cs` (entire file)
**Category:** Test Coverage
**Technical Severity:** Major
**Business Impact:** High
**Priority:** P1
**Fixable:** Yes
**Fix Agent:** 01-02-02-BE-test-fix-agent
**Description:** The search service (453 lines, the largest service in the codebase) has zero test coverage. It contains complex caching logic, parallel processing, regex matching, and result ranking. The search is a core feature of the application.
**Current State:** No tests exist.
**Expected State:** ChorusSearchServiceTests.cs with tests for each search mode, scope, caching behavior, parallel processing, and error handling.
**Recommendation:** Create comprehensive unit tests covering: SearchByName, SearchByText, SearchByKey, SearchAll, regex mode, cache hit/miss, empty results, null handling.
**Validation:** `dotnet test` passes; search service coverage > 80%.
**Dependencies:** None
**Status:** Open

### TEST-003: No Tests for SlideToChorusService
**Source Agent:** 01-01-03-BE-test-coverage-reviewer (depth 2)
**File:** `CHAP2.Application/Services/SlideToChorusService.cs` (entire file)
**Category:** Test Coverage
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** 01-02-02-BE-test-fix-agent
**Description:** The slide conversion service (365 lines) has no tests. It handles PowerPoint parsing, musical key extraction, and text processing. Bugs here would cause corrupt chorus data during bulk imports.
**Current State:** No tests exist.
**Expected State:** SlideToChorusServiceTests.cs with tests for key extraction, text parsing, error handling.
**Recommendation:** Create unit tests with sample PowerPoint files or mocked data for key extraction patterns, title case conversion, and error scenarios.
**Validation:** `dotnet test` passes with new tests.
**Dependencies:** None
**Status:** Open

### TEST-004: No Tests for API Controllers
**Source Agent:** 01-01-03-BE-test-coverage-reviewer (depth 2)
**File:** `CHAP2.Chorus.Api/Controllers/ChorusesController.cs` (entire file)
**Category:** Test Coverage
**Technical Severity:** Major
**Business Impact:** Medium
**Priority:** P2
**Fixable:** Yes
**Fix Agent:** 01-02-02-BE-test-fix-agent
**Description:** No controller tests exist. The ChorusesController has input validation, sanitization, error mapping, and search parameter handling that are untested.
**Current State:** No tests exist.
**Expected State:** ChorusesControllerTests.cs with tests for each endpoint, validation, error responses.
**Recommendation:** Create controller unit tests using mocked services.
**Validation:** `dotnet test` passes with new tests.
**Dependencies:** None
**Status:** Open

### TEST-005: Existing Tests Do Not Cover Error Paths Completely
**Source Agent:** 01-01-03-BE-test-coverage-reviewer (depth 2)
**File:** `CHAP2.Tests/Application/ChorusCommandServiceTests.cs`
**Category:** Test Coverage
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Fixable:** Yes
**Fix Agent:** 01-02-02-BE-test-fix-agent
**Description:** The existing command service tests don't verify: (1) that domain events contain correct data after create, (2) concurrent create scenarios, (3) that the event dispatcher is called with correct event type, (4) that exceptions from the repository propagate correctly.
**Current State:** Basic happy path and not-found tests only.
**Expected State:** Additional tests for event data verification, exception propagation, and edge cases.
**Recommendation:** Expand test suite with additional scenarios.
**Validation:** `dotnet test` passes with expanded coverage.
**Dependencies:** None
**Status:** Open

### NAME-001: Multiple Public Types in Single File (SearchController.cs)
**Source Agent:** 01-01-05-BE-naming-convention-reviewer (depth 2)
**File:** `CHAP2.UI/CHAP2.WebPortal/Controllers/SearchController.cs:144-149`
**Category:** Naming / One-Type-Per-File
**Technical Severity:** Major
**Business Impact:** Low
**Priority:** P3
**Fixable:** Yes
**Fix Agent:** 01-02-01-BE-code-quality-fix-agent
**Description:** `SearchApiRequest` record is defined at the bottom of `SearchController.cs` alongside the `SearchController` class. While small DTOs/records co-located with controllers are sometimes acceptable, this violates the one-type-per-file rule.
**Current State:** Two public types in one file.
**Expected State:** Move `SearchApiRequest` to its own file, e.g., `Requests/SearchApiRequest.cs` or `DTOs/SearchApiRequest.cs`.
**Recommendation:** Extract to a separate file.
**Validation:** `dotnet build` succeeds.
**Dependencies:** None
**Status:** Open

---

## Minor Issues

| ID | Source | File | Priority | Issue | Recommendation |
|----|--------|------|----------|-------|----------------|
| MIN-001 | 01-01-01-BE-architecture-reviewer | `CHAP2.Chorus.Api/Interfaces/IServices.cs` | P4 | `IServices` and `Services` are placeholder/dead code with no real consumers. Returns `object` from `GetSampleData`. | Remove if unused, or rename to something meaningful. |
| MIN-002 | 01-01-01-BE-architecture-reviewer | `CHAP2.Chorus.Api/Interfaces/IController.cs` | P4 | `IController` is an empty marker interface with no methods. Provides no value. | Remove and have controllers extend only `ControllerBase`. |
| MIN-003 | 01-01-02-BE-code-reviewer | `CHAP2.Application/Services/AiSearchService.cs:15-116` | P3 | `AiSearchService` methods are `async` but don't use `await`. They return synchronously computed results. Compiler will warn about missing await. | Either make methods synchronous and return `Task.FromResult`, or mark them correctly. |
| MIN-004 | 01-01-02-BE-code-reviewer | `CHAP2.Infrastructure/Repositories/DiskChorusRepository.cs:133-136` | P4 | `GetCountAsync` uses `Task.FromResult(files.Length)` wrapping synchronous `Directory.GetFiles`. Not truly async. | Use `Task.Run` or document as synchronous. |
| MIN-005 | 01-01-05-BE-naming-convention-reviewer | `CHAP2.Chorus.Api/Services/Services.cs` | P4 | Class named `Services` implementing `IServices` - extremely generic, unclear purpose. | Rename to `ApplicationInfoService` / `IApplicationInfoService`. |
| MIN-006 | 01-01-05-BE-naming-convention-reviewer | `CHAP2.Application/Services/ChorusSearchService.cs:427` | P4 | Private static method `MatchesSearch` could be more descriptively named. | Rename to `IsTextMatchForSearchMode`. |
| MIN-007 | 01-01-02-BE-code-reviewer | `CHAP2.Application/Services/ChorusApplicationService.cs:79` | P3 | `Guid.Parse(id)` without TryParse - will throw FormatException on invalid GUID string. | Use `Guid.TryParse` and return appropriate error. |
| MIN-008 | 01-01-02-BE-code-reviewer | `CHAP2.Application/Services/ChorusApplicationService.cs:106` | P3 | `Enum.Parse<SearchMode>(searchMode)` without TryParse - will throw on invalid enum values. | Use `Enum.TryParse` and return default or error. |
| MIN-009 | 01-01-01-BE-architecture-reviewer | `CHAP2.Chorus.Api/Controllers/SlideController.cs:12` | P3 | `SlideController` injects `IChorusRepository` directly (bypassing service layer), violating the layered architecture pattern used everywhere else. | Inject `IChorusCommandService`/`IChorusQueryService` instead. |
| MIN-010 | 01-01-02-BE-code-reviewer | `CHAP2.UI/CHAP2.WebPortal/Program.cs:19-21` | P3 | `AllowSynchronousIO = true` on Kestrel is a performance anti-pattern. Used for streaming but should be scoped. | Scope synchronous IO allowance to specific endpoints if possible. |
| MIN-011 | 01-01-04-BE-security-reviewer | `CHAP2.Chorus.Api/Controllers/SlideController.cs:42` | P3 | Null-forgiving operator `fileContent!` after a null check that returns false. Safe here but indicates a design smell. | Refactor ValidateFileContent to use out parameter or separate null check. |
| MIN-012 | 01-01-05-BE-naming-convention-reviewer | `CHAP2.Application/Interfaces/SearchRequest.cs`, `SearchResult.cs` | P4 | These are records in the `Interfaces` folder but are DTOs/value types, not interfaces. | Move to a `Models` or `DTOs` subfolder for clarity. |
| MIN-013 | 01-01-02-BE-code-reviewer | `CHAP2.Domain/Entities/Chorus.cs:19` | P3 | `DomainEvents` list is initialized with `new()` but never cleared after dispatch. Events accumulate. | Add `ClearDomainEvents()` method called after dispatch. Related to QUAL-002/QUAL-003. |
| MIN-014 | 01-01-04-BE-security-reviewer | `CHAP2.UI/CHAP2.WebPortal/appsettings.json:9` | P3 | Internal service URLs hardcoded in appsettings (`http://chap2-api:5001`, `http://langchain-service:8000`). Not a secret, but should be validated at startup. | Add configuration validation to fail fast on missing/invalid URLs. |

---

## Standards Compliance

### iDesign Architecture
| Check | Status | Violations |
|-------|--------|------------|
| Service Classification | Warning | 2 - `Services` class is misnamed; `SlideController` bypasses service layer |
| Layer Dependencies | Warning | 2 - SlideController accesses repository directly; WebPortal duplicates repository setup |
| Naming Conventions | Warning | 3 - `Services`/`IServices`, `IController` marker interface, `SearchApiRequest` co-located |

### SOLID Principles
| Principle | Status | Violations |
|-----------|--------|------------|
| Single Responsibility | Pass | 0 |
| Open/Closed | Pass | 0 |
| Liskov Substitution | Pass | 0 |
| Interface Segregation | Warning | 1 - ChorusSearchService has public methods not on interface |
| Dependency Inversion | Warning | 2 - DomainEventDispatcher uses IServiceProvider; SlideToChorusService uses reflection |

### One Type Per File
| Status | Violations |
|--------|------------|
| Warning | 1 - SearchController.cs contains SearchApiRequest record |

---

## Agent Tree

```
Depth 0: 01-BE-master-orchestrator
  Depth 1: 01-01-BE-quality-orchestrator
    Depth 2: 01-01-01-BE-architecture-reviewer (6 findings)
    Depth 2: 01-01-02-BE-code-reviewer (9 findings)
    Depth 2: 01-01-03-BE-test-coverage-reviewer (5 findings)
    Depth 2: 01-01-04-BE-security-reviewer (6 findings)
    Depth 2: 01-01-05-BE-naming-convention-reviewer (4 findings)
```

---

## Recommendations

### Critical (Immediate)
1. [SEC-001/SEC-002]: Restrict CORS to known origins on both API and WebPortal
2. [SEC-003]: Add regex timeout to prevent ReDoS
3. [TEST-001]: Create tests for DiskChorusRepository - core data layer is untested

### Major (Short-term)
1. [ARCH-004]: Remove exception-swallowing in ChorusApplicationService
2. [SEC-004]: Evaluate authentication requirements; document if intentionally open
3. [QUAL-002]: Fix duplicate domain event dispatch
4. [TEST-002]: Create tests for ChorusSearchService
5. [QUAL-005]: Fix WebPortal to use cached repository or API-only access

### Minor (When convenient)
1. Clean up dead code (IServices, IController, Services)
2. Move ChorusMetadata toward immutability
3. Extract SearchApiRequest to its own file
4. Add Guid.TryParse/Enum.TryParse for input validation
