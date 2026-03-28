# Pre-Implementation Requirements Quality Checklist: Notebook CRUD and Style Management

**Purpose**: Validate that requirements across spec, plan, and contracts are complete, clear, and consistent enough to begin implementation without ambiguity.
**Created**: 2026-03-28
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md) | [contracts/api-contracts.md](../contracts/api-contracts.md)
**Audience**: Author — use before writing any code
**Scope**: Spec + Plan + Contracts (all three artifacts)

---

## Requirement Completeness

- [x] CHK001 — Is the behavior when `instrumentId` or `pageSize` appears in a `PUT /notebooks/{id}` request body explicitly defined as a _mechanism_ (not just an outcome)? The spec says 400 is returned, but ASP.NET Core's model binder silently drops unknown fields — the plan says "the controller's action signature only binds these two fields," which means the error would _never_ trigger without additional enforcement. Is the enforcement approach (e.g., required DTO properties, custom model binder, or explicit service-level check) specified anywhere? [Completeness, Conflict — Spec §FR-005, Plan Step 6]

- [x] CHK002 — Are the error codes `NOTEBOOK_INSTRUMENT_IMMUTABLE` and `NOTEBOOK_PAGE_SIZE_IMMUTABLE` referenced in the spec (FR-005) or only in the contracts? If these codes are expected in the response body, they should appear in FR-005's acceptance scenarios, not only in the contracts error table. [Completeness, Spec §US2-Scenario2, Contracts §Error Code Reference]

- [x] CHK003 — Does the spec define what happens when no system preset has `IsDefault = true` (e.g., if seeder didn't run) and `POST /notebooks` is called without `styles`? The spec assumption says "if no default is seeded, this is a data error" — but no error type, status code, or client-visible message is specified for this failure. [Completeness, Gap — Spec §Assumptions]

- [x] CHK004 — Are the requirements for `IInstrumentNotFoundException` (status 422) complete? The plan mentions "a new `InstrumentNotFoundException` (422)" but no new exception class is defined in data-model.md, and the existing `NotFoundException` uses status 404. Is the mapping from `INSTRUMENT_NOT_FOUND` → 422 specified in a way that implementation can follow without creating a new exception class? [Completeness, Gap — Plan §Step 4, Contracts §Error Code Reference]

- [x] CHK005 — Is `updatedAt` refresh behavior specified for write operations other than `PUT /notebooks/{id}`? The spec defines that PUT refreshes the notebook — but does applying a style preset or bulk-updating styles also update `Notebook.UpdatedAt`? Neither the spec, plan, nor contracts address this. [Completeness, Gap — Spec §FR-009, FR-010]

- [x] CHK006 — Are 401 (unauthenticated) responses documented for all protected endpoints in the contracts? The contracts only list 403 and 404 for most endpoints. `GET /notebooks` has no error section at all. Is 401 implied by the auth requirement, or should it be explicit? [Completeness, Gap — Contracts §Notebooks]

- [x] CHK007 — Is the `styles` array ordering in `POST /notebooks/{id}/styles/apply-preset/{presetId}` response defined? The response for `GET /notebooks/{id}/styles` specifies "ordered by `ModuleType` enum integer value ascending" — but no ordering is documented for the apply-preset response. [Completeness, Gap — Contracts §POST apply-preset]

---

## Requirement Clarity

- [x] CHK008 — Is "disallowed fields present" in the `PUT /notebooks/{id}` contract sufficiently defined to implement? The contracts say "Error 400: `title` missing, `coverColor` missing, or disallowed fields present." What constitutes a "disallowed field" — only `instrumentId` and `pageSize`, or any unrecognized field? [Clarity, Ambiguity — Contracts §PUT /notebooks/{id}]

- [x] CHK009 — Is the `NotebookSummary → NotebookSummaryResponse` AutoMapper mapping specified clearly enough to implement? The data-model.md says add `NotebookEntity → NotebookSummary` to `EntityToDomainProfile`, but does not define how `InstrumentName` (from `Instrument.DisplayName` navigation property) and `LessonCount` (count of `Lessons` collection) are projected. [Clarity, Gap — Plan §Step 3, data-model.md]

- [x] CHK010 — Is the `StylesJson` format for `NotebookModuleStyleEntity` defined precisely? The data-model.md shows the JSON schema — but is it specified whether property names are camelCase (matching the seeder's `JsonNamingPolicy.CamelCase`) or PascalCase? The deserialization in `DomainToResponseProfile` depends on this being consistent. [Clarity, Ambiguity — data-model.md §NotebookModuleStyleEntity, research.md §Decision 6]

- [x] CHK011 — Is the `ModuleStyleResponse` shape for preset styles (`GET /presets`) unambiguous? The `SystemStylePresetResponse` uses `List<ModuleStyleResponse>` which has `Id: Guid` and `NotebookId: Guid` fields — but preset style entries have no `Id` or `NotebookId`. Will these fields be zeroed-out Guids, omitted, or does a separate slimmer DTO need to be defined? [Clarity, Ambiguity — Contracts §GET /presets, data-model.md §SystemStylePresetResponse]

- [x] CHK012 — Is the hex color validation regex (`^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$`) consistent with the database column constraint (`nvarchar(7)`)? A 6-digit hex is 7 chars (`#RRGGBB`), which fits `nvarchar(7)`. A 3-digit hex is 4 chars (`#RGB`), which also fits. Is it specified whether the stored value is normalized to 6-digit, or stored as-is? [Clarity, Ambiguity — data-model.md §NotebookConfiguration, Spec §Assumptions]

- [x] CHK013 — Is the term "bulk-replace" in FR-009 clear about what happens to the existing 12 style records? Does "replace" mean the existing `NotebookModuleStyle` records are updated in place (preserving their `Id` values), or deleted and recreated (changing their `Id` values)? This affects how the frontend caches references. [Clarity, Ambiguity — Spec §FR-009, Plan §Step 4 BulkUpdateStylesAsync]

- [x] CHK014 — Is it specified which JSON property naming convention is used in `ModuleStyleRequest` and `ModuleStyleResponse`? The contracts show `backgroundColor`, `borderColor`, etc. (camelCase) — are these the actual JSON field names or just illustrative? Is `System.Text.Json` camelCase policy applied globally in the app? [Clarity, Ambiguity — Contracts §PUT /notebooks/{id}/styles]

---

## Requirement Consistency

- [x] CHK015 — Is the `PUT /notebooks/{id}` behavior consistent between the spec and the contracts? The spec (FR-005) says "Attempting to change instrument or page size MUST be rejected with status 400." The plan (Step 6) says "The controller's action signature only binds these two fields," implying extra fields are silently ignored. These two statements appear to contradict each other — only one can be true at runtime. [Consistency, Conflict — Spec §FR-005, Plan §Step 6]

- [x] CHK016 — Is the ownership check ordering consistent across all service methods? The plan specifies that for `GetByIdAsync`, the service first checks existence (404 if null) then checks ownership (403 if wrong user). Are `GetStylesAsync`, `BulkUpdateStylesAsync`, and `ApplyPresetAsync` specified to follow the same sequence? [Consistency — Plan §Step 4]

- [x] CHK017 — Is the `lessonCount` field present in both `NotebookSummaryResponse` and `NotebookDetailResponse`? The contracts show it in the POST 201 response body as `"lessonCount": 0`. The spec (FR-001) lists it for summary. Is it also explicitly required in the detail response, or only the summary? [Consistency — Spec §FR-004, Contracts §GET /notebooks/{id}]

- [x] CHK018 — Is the style ordering rule consistent between `GET /notebooks/{id}/styles`, `PUT /notebooks/{id}/styles`, and `POST apply-preset`? The contract for GET specifies "ordered by `ModuleType` enum integer value ascending." Are the other two style-returning endpoints required to use the same ordering? [Consistency — Contracts §GET /styles, PUT /styles, POST apply-preset]

- [x] CHK019 — Is the validation for the `styles` array in `POST /notebooks` consistent with the validation for `PUT /notebooks/{id}/styles`? The spec FR-003 (creation) and FR-009 (bulk update) both say "exactly 12, one per ModuleType." The edge cases section and data-model.md specify "no duplicate types" for both. Is the exact error message/code for this failure consistent between the two endpoints? [Consistency — Spec §FR-003, FR-009, data-model.md §Validation Rules]

---

## Acceptance Criteria Quality

- [x] CHK020 — Can SC-003 ("Applying a style preset replaces all 12 module type styles in a single round-trip; no partial updates occur under any condition") be objectively verified from the spec alone? It references atomicity behavior, but no explicit rollback requirement or observable failure mode is defined. What should a client observe if the DB write fails mid-way? [Measurability — Spec §SC-003]

- [x] CHK021 — Is US2-Scenario2's acceptance criterion testable as written? "Given a logged-in user, When they attempt to update a notebook's instrument or page size, Then the request is rejected with status 400 and error code `NOTEBOOK_INSTRUMENT_IMMUTABLE` or `NOTEBOOK_PAGE_SIZE_IMMUTABLE` respectively." Given CHK015's conflict, can this scenario actually pass without additional implementation guidance? [Acceptance Criteria — Spec §US2-Scenario2]

- [x] CHK022 — Is SC-006 ("Every new notebook has exactly 12 module type style records immediately after creation") testable without specifying which database transaction isolation level is assumed? Could a concurrent read theoretically observe fewer than 12 styles between notebook insert and style inserts? [Measurability — Spec §SC-006]

---

## API Contract Coverage

- [x] CHK023 — Does the contract for `POST /notebooks` specify what status code and error format are returned when the `styles` array is provided but fails per-ModuleType validation (invalid enum value, duplicate type, wrong count)? The contract only shows a generic 400 example for title/coverColor validation. [Coverage, Gap — Contracts §POST /notebooks]

- [x] CHK024 — Is there a contract entry for what `GET /notebooks` returns when the authenticated user has no notebooks? The contracts show a sample with one item but don't explicitly state the empty-array response. [Coverage, Gap — Contracts §GET /notebooks]

- [x] CHK025 — Is the `PUT /notebooks/{id}/styles` contract complete for all validation failure cases? It lists "Array count ≠ 12, duplicate module types, or missing module types" as 400 — but doesn't specify the response body format (FluentValidation `{ errors }` format vs business error `{ code, message }` format). [Coverage, Gap — Contracts §PUT /notebooks/{id}/styles]

- [x] CHK026 — Is a 401 response documented for `GET /presets`? It's a public endpoint, but should the contract explicitly state that no auth is required and that no 401 is ever returned — to prevent accidental auth middleware from being applied? [Coverage, Gap — Contracts §GET /presets]

---

## Data Model & Validation Coverage

- [x] CHK027 — Are `BorderWidth` and `BorderRadius` minimum and maximum values defined in data-model.md? The validator rule says `>= 0` for both, but no maximum is specified. Are arbitrarily large values acceptable (e.g., `borderWidth: 999`)? [Coverage, Gap — data-model.md §ModuleStyleRequest]

- [x] CHK028 — Is the `CoverColor` maximum length constraint (`nvarchar(7)`) consistent with the accepted hex regex (`#RGB` = 4 chars, `#RRGGBB` = 7 chars)? Should the column be `nvarchar(7)` to accommodate 6-digit format, or `nvarchar(4)` for the 3-digit short form? Is the minimum column length specified to reject values like `#` or `#1`? [Coverage — data-model.md §NotebookEntity, §CreateNotebookRequest]

- [x] CHK029 — Is the `NotebookSummary` domain model (new) defined with enough precision to implement the `GetByUserIdAsync` EF query? Specifically: is it specified whether `LessonCount` is computed via `.Count()` on the loaded collection or via a projected `.Select(n => n.Lessons.Count)` — the latter avoids loading all lessons into memory. [Coverage, Gap — data-model.md §NotebookSummary, Plan §Step 3]

---

## Plan Pre-Work Completeness

- [x] CHK030 — Is the `AddNotebookCoverColor` migration's `DEFAULT` value for existing rows defined? The plan says `DEFAULT '#FFFFFF'` — but is this business-acceptable? Existing notebooks (if any) would get a white cover. Is this the intended fallback or should it be a different default (e.g., the most commonly used color)? [Completeness — Plan §Step 2]

- [x] CHK031 — Is the `SystemStylePresetSeeder` `IsDefault` fix specified to be idempotent for databases already seeded with `Classic` as default? The plan notes the seeder has `if (await context.SystemStylePresets.AnyAsync(ct)) return;` — meaning a fix to the seeder won't affect already-seeded databases. Is a data correction script or migration required, or is the quickstart SQL snippet the official remediation path? [Completeness — Plan §Step 2, quickstart.md]

- [x] CHK032 — Is the `ISystemStylePresetRepository.GetAllAsync` method sufficient for both `GET /presets` and the `CreateAsync` default-preset lookup? `GetAllAsync` returns all presets; `CreateAsync` needs only the one where `IsDefault = true`. Should a more targeted `GetDefaultAsync()` method be defined instead to avoid fetching all 5 presets when only one is needed? [Completeness, Gap — Plan §Step 3, §Step 4 CreateAsync]

---

## Edge Case Coverage

- [x] CHK033 — Is it specified what happens when `POST /notebooks/{id}/styles/apply-preset/{presetId}` is called and the preset's `StylesJson` has fewer than 12 entries (malformed seeded data)? The service would fail to find a matching entry for some `ModuleType` values when iterating. Is silent skipping, partial update, or error the expected behavior? [Edge Case, Gap — Plan §Step 4 ApplyPresetAsync, research.md §Decision 7]

- [x] CHK034 — Is it specified what happens when a notebook ID in `GET /notebooks/{id}/styles` or `PUT /notebooks/{id}/styles` is valid but the notebook has fewer than 12 style records (data integrity violation)? Should the service detect and report this, or is it assumed impossible? [Edge Case, Gap — Spec §FR-008, FR-009]

- [x] CHK035 — Are concurrent request scenarios addressed? If the same user sends two simultaneous `PUT /notebooks/{id}/styles` requests, is the final state well-defined? Neither the spec nor plan mentions optimistic concurrency or last-write-wins behavior. [Edge Case, Gap — Spec §FR-009]

- [x] CHK036 — Is the behavior defined when a `DELETE /notebooks/{id}` is called for a notebook that is currently being exported (active PDF export)? The error code `ACTIVE_EXPORT_EXISTS` exists in the system — should this block notebook deletion? The spec (FR-006) says deletion cascades everything but doesn't mention export conflicts. [Edge Case, Gap — Spec §FR-006, CLAUDE.md §Key error codes]

## Notes

- Mark items as `[x]` when the requirement gap has been resolved (in spec, plan, or contracts).
- **CHK001 and CHK015** are the highest-priority items — they represent a direct conflict between the spec's stated behavior and the plan's implementation approach for `PUT /notebooks/{id}` immutable-field enforcement.
- **CHK011** (preset styles using `ModuleStyleResponse` with Id/NotebookId) may require a new, simpler DTO to avoid misleading null fields in the `GET /presets` response.
- **CHK032** (missing `GetDefaultAsync`) is a minor design gap but affects code readability in `NotebookService.CreateAsync`.
