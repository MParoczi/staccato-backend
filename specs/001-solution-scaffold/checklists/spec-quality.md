# Architecture Review Checklist: Solution Scaffold

**Purpose**: Formal gate review validating requirement quality, clarity, completeness, and cross-artifact consistency across spec.md, plan.md, data-model.md, and contracts/. Intended as a pre-implementation peer-review gate.
**Created**: 2026-03-01
**Resolved**: 2026-03-01
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md) | [data-model.md](../data-model.md)
**Scope**: Spec + Plan + Contracts (cross-artifact)
**Depth**: Formal architecture review gate

---

## Requirement Completeness

- [x] CHK001 — Is the `RateLimitOptions` class formally captured as a requirement in the spec? **Resolved**: Added to FR-019 and Key Entities.

- [x] CHK002 — Does the spec require `FluentValidation` in `ApiModels.csproj`? **Resolved**: FR-022 added.

- [x] CHK003 — Does the spec require `FluentValidation.AspNetCore` in `Application.csproj`? **Resolved**: FR-004 extended.

- [x] CHK004 — Is the removal of `Microsoft.AspNetCore.OpenApi` stated as an explicit requirement? **Resolved**: Captured in Assumptions (pre-existing violation corrected by this feature).

- [x] CHK005 — Is `[Authorize]` on `NotificationHub` captured in the spec? **Resolved**: FR-015 now specifies the hub requires authentication; unauthenticated attempts return 401.

- [x] CHK006 — Does the spec formally require `Tests/Unit/` and `Tests/Integration/` subdirectories? **Resolved**: FR-025 added.

- [x] CHK007 — Is the deletion of `UnitTest1.cs` specified? **Resolved**: FR-003 updated to cover both `Class1.cs` and `UnitTest1.cs`.

- [x] CHK008 — Are stub extension methods formally required? **Resolved**: Captured in Assumptions (stub methods, empty bodies, exist so future features only need to add bodies).

- [x] CHK009 — Is the `QuestPDF.Settings.License` startup call formally required? **Resolved**: FR-023 added.

- [x] CHK010 — Does the spec require `Nullable` enabled in all 9 `.csproj` files? **Resolved**: FR-026 added.

- [x] CHK011 — Does the spec require file-scoped namespaces? **Resolved**: FR-027 added.

---

## Requirement Clarity

- [x] CHK012 — Is "appropriate HTTP status code" in FR-014 quantified? **Resolved**: FR-014 now specifies the mapping — 403 (ownership), 409 (conflict), 400 (input rule), 422 (all other business rule violations, default).

- [x] CHK013 — Is "standard development machine" defined with measurable specs? **Resolved** (Q2-A): Qualifier removed from SC-003 and SC-006 — restated as "under typical development conditions."

- [x] CHK014 — Does FR-015 specify the hub route? **Resolved**: FR-015 now names route `/hubs/notifications`.

- [x] CHK015 — Does FR-016 specify which assemblies AutoMapper scans? **Resolved**: FR-016 now specifies `Application` and `Api` project assemblies.

- [x] CHK016 — Does FR-017 distinguish stub from functional registration? **Resolved** (Q3-A): FR-017 now explicitly says "stub `IHostedService` classes with no implementation logic."

- [x] CHK017 — Is FR-003 grammatically correct? **Resolved**: FR-003 rewritten as "Every `Class1.cs` and `UnitTest1.cs` placeholder file MUST be deleted."

- [x] CHK018 — Does FR-010 specify HTTP methods and headers alongside `AllowCredentials()`? **Resolved**: FR-010 now includes `AllowAnyHeader()` and `AllowAnyMethod()`.

- [x] CHK019 — Does FR-011 specify the 429 response format? **Resolved**: FR-011 now specifies HTTP 429 with `Retry-After` header.

- [x] CHK020 — Does FR-012 specify the FluentValidation error format? **Resolved**: FR-012 now specifies `{ "errors": { "fieldName": ["message"] } }`.

- [x] CHK021 — Is the minimum `SecretKey` length defined? **Resolved**: Key Entities entry for `JwtOptions` now states "minimum 32 characters for HS256 validity."

---

## Requirement Consistency

- [x] CHK022 — `DomainException` vs `BusinessException` naming conflict. **Resolved** (Q1-A): `BusinessException` is canonical throughout — all occurrences of `DomainException` in spec replaced to align with constitution §III, §G4, §G7. Documented in Clarifications §Architecture Review.

- [x] CHK023 — FR-021 is out of document sequence (inserted between FR-015 and FR-016). **By design**: FR-021 was added via clarification; its ID is preserved for traceability. The ordering within the spec document follows narrative grouping, not strict ID order.

- [x] CHK024 — `RateLimitOptions` missing from FR-019 and Key Entities. **Resolved**: Covered by CHK001.

- [x] CHK025 — `BusinessException.StatusCode` property missing from spec Key Entity. **Resolved**: Key Entities entry for `BusinessException` now includes `int StatusCode` (default 422).

- [x] CHK026 — Are the three clarifications fully reflected in FRs? **Resolved**: FR-010 (CORS credentials), FR-021 (auth middleware), FR-014 (BusinessException catch type) — all consistent with Clarifications §Session 2026-03-01 after CHK022 fix.

- [x] CHK027 — FR-017 ("MUST register all IHostedService implementations") conflicts with Assumptions ("stub registrations"). **Resolved** (Q3-A): FR-017 rewritten as "MUST register stub classes." Assumptions updated to describe no-op `StartAsync`/`StopAsync` bodies.

---

## Acceptance Criteria Quality

- [x] CHK028 — SC-001 "zero warnings related to project structure" — qualifier too narrow. **Resolved**: SC-001 now says "zero warnings (of any category)."

- [x] CHK029 — SC-004 not verifiable at scaffold time (no auth controller). **Resolved**: SC-004 now explicitly states "10 non-429 responses (including HTTP 404 if no route is mapped yet)" — 404 counts as non-rate-limited.

- [x] CHK030 — SC-008 count must be updated after all new FRs. **Resolved**: SC-008 now says "27 functional requirements (FR-001 through FR-027)."

- [x] CHK031 — SC-002 verifiability method unclear. **Resolved**: SC-002 now says "verifiable by inspecting each `.csproj` file."

- [x] CHK032 — SC-007 "takes effect immediately" unclear on restart. **Resolved**: SC-007 now says "Changing any value in `appsettings.json` and **restarting** the application takes effect immediately."

---

## Cross-Artifact Consistency

- [x] CHK033 — Extension method naming (`AddCorsPolicy`, `AddRateLimiting`) vs FR naming. **By design**: Extension method names are implementation details captured in `plan.md §ServiceCollectionExtensions`; FRs specify intent, not method names.

- [x] CHK034 — `SomeApiModelsMarker` placeholder type not formally specified. **Resolved** (Q4-A): `ApiModelsAssemblyMarker` empty static class added to Key Entities and FR-022. Added to Assumptions.

- [x] CHK035 — `[Authorize]` on hub in contracts but not in spec. **Resolved**: Covered by CHK005.

- [x] CHK036 — 429 format in contracts not in spec acceptance scenarios. **Resolved**: Covered by CHK019; SC-004 and User Story 2 acceptance scenario 3 now reference the 429 + `Retry-After` response.

- [x] CHK037 — `RememberMeExpiryDays ≥ RefreshTokenExpiryDays` validation only in data-model. **Resolved**: Added to `JwtOptions` Key Entity in spec.

- [x] CHK038 — `RateLimitOptions` validation rules only in data-model. **Resolved**: Added to `RateLimitOptions` Key Entity in spec (both > 0, startup failure if violated).

- [x] CHK039 — `AzureBlobOptions` validation rules only in data-model. **Resolved**: Added to `AzureBlobOptions` Key Entity in spec.

- [x] CHK040 — FR-002 lacks enforcement mechanism. **Resolved**: FR-002 now states "Any violation causes a build error and MUST be corrected before merging."

- [x] CHK041 — Transitive `FluentValidation` exposure not documented. **Resolved**: Added to Assumptions as intentional.

- [x] CHK042 — Pre-existing `.csproj` violations not documented in spec. **Resolved**: Added to Assumptions with all three specific violations listed.

---

## Architecture & Dependency Coverage

*(Items in this category were merged into CHK040–CHK042 above.)*

---

## Middleware & Pipeline Coverage

- [x] CHK043 — Middleware ordering not a formal requirement. **Resolved**: FR-024 added with exact 9-step pipeline order and rationale for why deviations cause runtime failures.

- [x] CHK044 — Two-stage exception handling (outer `BusinessExceptionMiddleware` + inner `UseExceptionHandler`) not in spec. **Resolved**: FR-024 defines the pipeline order; FR-013 and FR-014 together specify the two-stage strategy. FR-014 now explicitly states "all non-`BusinessException` types propagate to the Problem Details handler."

- [x] CHK045 — `BusinessExceptionMiddleware` throwing not covered. **Resolved**: Added to Edge Cases — error propagates to outer Problem Details handler, returns HTTP 500.

- [x] CHK046 — `UseHttpsRedirection` before `UseCors` ordering not specified. **Resolved**: FR-024 mandates exact pipeline order with `UseHttpsRedirection` at position 3 (after exception handlers, before CORS), ensuring CORS pre-flight OPTIONS requests are not redirected.

---

## Configuration & Security Coverage

- [x] CHK047 — FR-020 didn't cover secrets in committed `appsettings.json`. **Resolved** (Q5-A): FR-020 extended to require placeholder values in committed `appsettings.json`; real secrets via user-secrets, environment variables, or secrets manager.

- [x] CHK048 — `AllowCredentials()` + wildcard incompatibility not documented. **Resolved**: FR-010 now explicitly states "the `AllowedOrigins` array MUST contain only specific origin strings — wildcards are prohibited."

- [x] CHK049 — FR-019 didn't specify `.ValidateOnStart()`. **Resolved**: FR-019 now explicitly requires `.ValidateDataAnnotations()` and `.ValidateOnStart()`.

---

## Edge Case Coverage

- [x] CHK050 — Null vs empty `AllowedOrigins` not distinguished. **Resolved**: Key Entities `CorsOptions` entry now states empty array → startup succeeds (all cross-origin requests rejected); null value → startup fails. Added as explicit edge case.

- [x] CHK051 — Zero/negative `RateLimitOptions` not covered. **Resolved**: Added to Edge Cases and `RateLimitOptions` Key Entity validation rules.

- [x] CHK052 — Null `BusinessException.Code` not covered. **Resolved**: Added to Edge Cases; `BusinessException` Key Entity now states "non-null `Code` string — subclasses MUST guarantee a non-null value."

- [x] CHK053 — Malformed origin URL not covered. **Resolved**: Added to Edge Cases — CORS middleware rejects requests from that origin without startup failure.

---

## Notes

- All 53 items resolved in a single session (2026-03-01).
- CHK022 (`DomainException` → `BusinessException`) was the highest-impact fix — it affects the public base class inherited by all future domain exception features.
- CHK023 (FR-021 out of sequence) is intentional by-design and does not require renumbering; FR-021 was added via clarification session and its ID is preserved for traceability.
- CHK033 (extension method names) is implementation-detail-level and intentionally not promoted to spec FRs.
- 6 new FRs added: FR-022 through FR-027. SC-008 updated to reflect 27 total requirements.
