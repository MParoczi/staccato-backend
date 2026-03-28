# Tasks: Notebook CRUD and Style Management

**Input**: Design documents from `/specs/008-notebook-crud-styles/`
**Branch**: `008-notebook-crud-styles`
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ contracts/ ✅

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[Story]**: Which user story this task belongs to (US1–US5)
- Exact file paths are included in every task description

---

## Phase 1: Setup — Entity & Migration

**Purpose**: Add `CoverColor` to the database schema and fix seed data. These changes must exist before any other code can compile and run against the database.

- [x] T001 Add `public string CoverColor { get; set; } = string.Empty;` to `EntityModels/Entities/NotebookEntity.cs`
- [x] T002 Add `: IEntity` to `EntityModels/Entities/SystemStylePresetEntity.cs` class declaration
- [x] T003 Add `.Property(n => n.CoverColor).IsRequired().HasMaxLength(7);` to `Persistence/Configurations/NotebookConfiguration.cs`
- [x] T004 Generate migration `AddNotebookCoverColor` via `dotnet ef migrations add AddNotebookCoverColor --project Persistence/Persistence.csproj --startup-project Application/Application.csproj`; verify the generated migration adds `CoverColor nvarchar(7) NOT NULL DEFAULT '#000000'`
- [x] T005 Fix `IsDefault` values in `Persistence/Seed/SystemStylePresetSeeder.cs`: change Classic to `false`, Colorful to `true`

**Checkpoint**: Solution compiles. Migration applies cleanly. `GET /presets` would return Colorful with `isDefault: true` once wired up.

---

## Phase 2: Foundational — Domain, Repository, AutoMapper, DTOs, DI

**Purpose**: Core infrastructure that MUST be complete before any user story can be implemented.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T006 Add `public string CoverColor { get; set; } = string.Empty;` to `DomainModels/Models/Notebook.cs`
- [x] T007 [P] Create `DomainModels/Models/NotebookSummary.cs` with properties: `Id`, `UserId`, `Title`, `InstrumentName : string`, `PageSize : PageSize`, `CoverColor : string`, `LessonCount : int`, `CreatedAt : DateTime`, `UpdatedAt : DateTime`
- [x] T008 [P] Create `Domain/Interfaces/Repositories/ISystemStylePresetRepository.cs` extending `IRepository<SystemStylePreset>` with two methods: `GetAllAsync` (for GET /presets) and `GetDefaultAsync` (returns the preset where `IsDefault = true`, used by `CreateAsync` to avoid loading all 5 presets when only the default is needed)
- [x] T009 Update `Domain/Interfaces/Repositories/INotebookRepository.cs`: change `GetByUserIdAsync` return type from `IReadOnlyList<Notebook>` to `IReadOnlyList<NotebookSummary>`
- [x] T010 [P] Add `HasActiveExportForNotebookAsync(Guid notebookId, CancellationToken ct = default) : Task<bool>` to `Domain/Interfaces/Repositories/IPdfExportRepository.cs`; implement the method in `Repository/Repositories/PdfExportRepository.cs` querying for any export with the given `NotebookId` and a status of `InProgress`
- [x] T011 [P] Verify and create exception classes in `Domain/Exceptions/`: (1) `ConflictException.cs` mapping to HTTP 409 — create if absent; (2) `InstrumentNotFoundException.cs` mapping to HTTP **422** for `INSTRUMENT_NOT_FOUND` — this is a **required new class**: the existing `NotFoundException` maps to 404, not 422, so it cannot be reused for this error code
- [x] T012 ~~Add `NotebookEntity → NotebookSummary` to `EntityToDomainProfile`~~ — No AutoMapper mapping needed for this path. The `GetByUserIdAsync` query uses a direct `.Select()` projection that computes `InstrumentName = n.Instrument.DisplayName` and `LessonCount = n.Lessons.Count` inside the SQL query (EF translates to `COUNT(*)` subquery). Verify `EntityToDomainProfile` does NOT attempt a `NotebookEntity → NotebookSummary` mapping that would conflict.
- [x] T013 Update `Repository/Repositories/NotebookRepository.cs` — rewrite `GetByUserIdAsync` to `.Include(n => n.Instrument).Include(n => n.Lessons).Where(n => n.UserId == userId).OrderBy(n => n.CreatedAt)` then project/map to `IReadOnlyList<NotebookSummary>`
- [x] T014 [P] Create `Repository/Repositories/SystemStylePresetRepository.cs` extending `RepositoryBase<SystemStylePresetEntity, SystemStylePreset>`; implement `GetAllAsync` with `.OrderBy(p => p.DisplayOrder).ToListAsync(ct)`
- [x] T015 [P] Create `ApiModels/Notebooks/ModuleStyleRequest.cs` (record: `ModuleType`, `BackgroundColor`, `BorderColor`, `BorderStyle`, `BorderWidth`, `BorderRadius`, `HeaderBgColor`, `HeaderTextColor`, `BodyTextColor`, `FontFamily`) and `ApiModels/Notebooks/ModuleStyleRequestValidator.cs` (all hex colours via `^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$`, `BorderStyle`/`FontFamily` enum parse, `BorderWidth` ∈ [0, 20], `BorderRadius` ∈ [0, 50])
- [x] T016 [P] Create `ApiModels/Notebooks/ModuleStyleResponse.cs` (record: `Id : Guid`, `NotebookId : Guid`, `ModuleType : string`, and all nine style property fields)
- [x] T017 [P] Create `ApiModels/Notebooks/NotebookSummaryResponse.cs` (record: `Id`, `Title`, `InstrumentName`, `PageSize`, `CoverColor`, `LessonCount`, `CreatedAt : string`, `UpdatedAt : string`)
- [x] T018 [P] Create `ApiModels/Notebooks/NotebookDetailResponse.cs` (record: `Id`, `Title`, `InstrumentId`, `InstrumentName`, `PageSize`, `CoverColor`, `LessonCount`, `CreatedAt : string`, `UpdatedAt : string`, `Styles : List<ModuleStyleResponse>`)
- [x] T019 [P] Create `ApiModels/Notebooks/SystemStylePresetResponse.cs` (record: `Id`, `Name`, `DisplayOrder`, `IsDefault`, `Styles : List<ModuleStyleResponse>`)
- [x] T020 Add `NotebookModuleStyle → ModuleStyleResponse` `ITypeConverter` to `Api/Mapping/DomainToResponseProfile.cs`; include a private `StyleProperties` record for `System.Text.Json` deserialization of `StylesJson`; deserialize each field into the typed response properties
- [x] T021 Create `Domain/Services/INotebookService.cs` declaring all 8 method signatures: `GetAllByUserAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetStylesAsync`, `BulkUpdateStylesAsync`, `ApplyPresetAsync` (parameter types and returns per plan.md §Step 4)
- [x] T022 Register `ISystemStylePresetRepository → SystemStylePresetRepository` in `AddRepositories()` and `INotebookService → NotebookService` in `AddDomainServices()` in `Application/Extensions/ServiceCollectionExtensions.cs`

**Checkpoint**: Solution compiles end-to-end. DI graph resolves. Repository and service stubs ready for user story implementation.

---

## Phase 3: User Story 1 — Create and Browse Notebooks (Priority: P1) 🎯 MVP

**Goal**: `GET /notebooks`, `POST /notebooks`, and `GET /notebooks/{id}` are fully functional. Creating a notebook without styles auto-applies the Colorful preset and returns 12 style records.

**Independent Test**: Register → create a notebook (no styles) → list notebooks (1 result with correct summary fields) → fetch detail (12 styles matching Colorful). Then create with explicit styles → verify custom styles returned.

### Implementation for User Story 1

- [x] T023 [P] [US1] Create `ApiModels/Notebooks/CreateNotebookRequest.cs` (properties: `Title`, `InstrumentId : Guid`, `PageSize : string`, `CoverColor : string`, `Styles : List<ModuleStyleRequest>?`) and `ApiModels/Notebooks/CreateNotebookRequestValidator.cs` (title max 200, `PageSize` enum parse, `CoverColor` hex regex, if `Styles` not null: count == 12 and all 12 `ModuleType` values present with no duplicates)
- [x] T024 [US1] Create `Domain/Services/NotebookService.cs`; implement `GetAllByUserAsync` (delegates to `INotebookRepository.GetByUserIdAsync`), `GetByIdAsync` (existence check → 404, ownership check → 403, return tuple), and `CreateAsync` (instrument existence check → 422, resolve default Colorful preset if styles null, create Notebook + 12 NotebookModuleStyle records with `Guid.NewGuid()` IDs, commit via `IUnitOfWork`, return via `GetWithStylesAsync`)
- [x] T025 [US1] Create `Api/Controllers/NotebooksController.cs` with `[Authorize]`, `private Guid GetUserId()` parsing `NameIdentifier` claim, and private static `ToStyleDomain(ModuleStyleRequest r)` serializer; implement `GET /notebooks` → 200, `POST /notebooks` → 201, `GET /notebooks/{id}` → 200
- [x] T026 [US1] Add `NotebookSummary → NotebookSummaryResponse` and `(Notebook, IReadOnlyList<NotebookModuleStyle>) → NotebookDetailResponse` mappings to `Api/Mapping/DomainToResponseProfile.cs`
- [x] T027 [P] [US1] Write `Tests/Unit/Services/NotebookServiceTests.cs` with mocked dependencies (`INotebookRepository`, `INotebookModuleStyleRepository`, `ISystemStylePresetRepository`, `IUserSavedPresetRepository`, `IInstrumentRepository`, `IPdfExportRepository`, `IUnitOfWork`); implement tests: `CreateAsync_WithNullStyles_AppliesColorfulPreset`, `CreateAsync_WithExplicitStyles_UsesProvidedStyles`, `CreateAsync_WithUnknownInstrumentId_ThrowsInstrumentNotFoundException`, `GetByIdAsync_NotebookBelongsToOtherUser_ThrowsForbiddenException`, `GetByIdAsync_NotebookDoesNotExist_ThrowsNotFoundException`
- [x] T028 [P] [US1] Write `Tests/Integration/Controllers/NotebooksControllerTests.cs` using `WebApplicationFactory<Program>` + InMemory EF + `AuthHelper`; implement tests: `GetNotebooks_Returns200WithEmptyArray_WhenNoNotebooks`, `GetNotebooks_Returns200OrderedByCreatedAtAsc`, `GetNotebooks_Returns401_WhenUnauthenticated`, `CreateNotebook_Returns201WithTwelveStyles_WhenNoStylesProvided`, `CreateNotebook_Returns201WithProvidedStyles`, `CreateNotebook_Returns400_WhenTitleMissing`, `CreateNotebook_Returns400_WhenCoverColorInvalid`, `CreateNotebook_Returns422_WhenInstrumentIdNotFound`, `GetNotebook_Returns200WithStyles`, `GetNotebook_Returns403_WhenNotOwnedByUser`, `GetNotebook_Returns404_WhenNotFound`

**Checkpoint**: User Story 1 is independently functional and fully tested. All 11 US1 integration tests pass.

---

## Phase 4: User Story 2 — Edit and Delete Notebooks (Priority: P2)

**Goal**: `PUT /notebooks/{id}` and `DELETE /notebooks/{id}` are fully functional. Immutable-field enforcement returns 400 with correct error codes. Deletion blocked by active export.

**Independent Test**: Create → update title and coverColor → verify updated values in 200 response → send PUT with `instrumentId` → verify 400 + `NOTEBOOK_INSTRUMENT_IMMUTABLE` → delete → verify 404.

### Implementation for User Story 2

- [x] T029 [P] [US2] Create `ApiModels/Notebooks/UpdateNotebookRequest.cs` (properties: `Title : string`, `CoverColor : string`, `InstrumentId : Guid?`, `PageSize : string?`) and `ApiModels/Notebooks/UpdateNotebookRequestValidator.cs` (title max 200, `CoverColor` hex, `InstrumentId` `Must(v => v == null).WithErrorCode("NOTEBOOK_INSTRUMENT_IMMUTABLE")`, `PageSize` `Must(v => v == null).WithErrorCode("NOTEBOOK_PAGE_SIZE_IMMUTABLE")`)
- [x] T030 [US2] Implement `UpdateAsync` (ownership check, update `Title`/`CoverColor`/`UpdatedAt`, commit, return full detail) and `DeleteAsync` (ownership check, check `IPdfExportRepository.HasActiveExportForNotebookAsync` → throw `ConflictException("ACTIVE_EXPORT_EXISTS")` if true, remove, commit) in `Domain/Services/NotebookService.cs`
- [x] T031 [US2] Add `PUT /notebooks/{id}` (→ 200) and `DELETE /notebooks/{id}` (→ 204) actions to `Api/Controllers/NotebooksController.cs`
- [x] T032 [P] [US2] Extend `Tests/Unit/Services/NotebookServiceTests.cs` with US2 tests: `UpdateAsync_ChangesOnlyTitleAndCoverColor`, `UpdateAsync_NotebookNotFound_ThrowsNotFoundException`, `UpdateAsync_NotebookBelongsToOtherUser_ThrowsForbiddenException`, `DeleteAsync_RemovesNotebook`, `DeleteAsync_NotebookNotFound_ThrowsNotFoundException`, `DeleteAsync_NotebookBelongsToOtherUser_ThrowsForbiddenException`, `DeleteAsync_ActiveExportExists_ThrowsConflictException`
- [x] T033 [P] [US2] Extend `Tests/Integration/Controllers/NotebooksControllerTests.cs` with US2 tests: `UpdateNotebook_Returns200WithUpdatedValues`, `UpdateNotebook_Returns400_WhenPageSizeIncluded`, `UpdateNotebook_Returns400_WhenInstrumentIdIncluded`, `UpdateNotebook_Returns403_WhenNotOwnedByUser`, `UpdateNotebook_Returns404_WhenNotFound`, `DeleteNotebook_Returns204`, `DeleteNotebook_Returns409_WhenActiveExportExists`

**Checkpoint**: User Stories 1 and 2 both work independently. All 7 US2 integration tests pass.

---

## Phase 5: User Story 3 — View and Bulk-Update Module Styles (Priority: P3)

**Goal**: `GET /notebooks/{id}/styles` and `PUT /notebooks/{id}/styles` are fully functional. Bulk replace updates all 12 records in-place (IDs preserved) and refreshes `Notebook.UpdatedAt`.

**Independent Test**: Create notebook → GET styles (12 items ordered by ModuleType enum value) → PUT replacement set of 12 → confirm all 12 updated values returned and `Notebook.UpdatedAt` advanced.

### Implementation for User Story 3

- [ ] T034 [US3] Implement `GetStylesAsync` (ownership check, return `INotebookModuleStyleRepository.GetByNotebookIdAsync`) and `BulkUpdateStylesAsync` (ownership check, validate 12 unique ModuleTypes → `BadRequestException` if invalid, fetch existing 12 records, match by `ModuleType` and overwrite `StylesJson` in-place, `_notebookRepo.Update` with refreshed `UpdatedAt`, commit, return updated records) in `Domain/Services/NotebookService.cs`
- [ ] T035 [US3] Add `GET /notebooks/{id}/styles` (→ 200) and `PUT /notebooks/{id}/styles` (→ 200) actions to `Api/Controllers/NotebooksController.cs`; the PUT action maps each `ModuleStyleRequest` via `ToStyleDomain` before calling the service
- [ ] T036 [P] [US3] Extend `Tests/Unit/Services/NotebookServiceTests.cs` with US3 tests: `GetStylesAsync_ReturnsAllTwelveStyles`, `GetStylesAsync_NotebookNotFound_ThrowsNotFoundException`, `GetStylesAsync_NotebookBelongsToOtherUser_ThrowsForbiddenException`, `BulkUpdateStylesAsync_ReplacesAllTwelveStyles`, `BulkUpdateStylesAsync_InvalidStyleCount_ThrowsBadRequestException`, `BulkUpdateStylesAsync_NotebookBelongsToOtherUser_ThrowsForbiddenException`, `BulkUpdateStylesAsync_NotebookNotFound_ThrowsNotFoundException`
- [ ] T037 [P] [US3] Extend `Tests/Integration/Controllers/NotebooksControllerTests.cs` with US3 tests: `GetStyles_Returns200WithTwelveItems`, `BulkUpdateStyles_Returns200WithUpdatedStyles`, `BulkUpdateStyles_Returns400_WhenNotTwelveItems`

**Checkpoint**: User Stories 1, 2, and 3 all work independently. All 3 US3 integration tests pass.

---

## Phase 6: User Story 4 — Apply a Preset to a Notebook (Priority: P4)

**Goal**: `POST /notebooks/{id}/styles/apply-preset/{presetId}` is fully functional. System presets checked first, user-saved presets second; ownership enforced; all 12 styles updated in-place; `Notebook.UpdatedAt` refreshed.

**Independent Test**: Apply Classic preset to a notebook → confirm all 12 returned styles match Classic's palette. Apply non-existent preset ID → 404.

### Implementation for User Story 4

- [ ] T038 [US4] Implement `ApplyPresetAsync` in `Domain/Services/NotebookService.cs`: (1) ownership check on notebook, (2) try `ISystemStylePresetRepository.GetByIdAsync` → if null try `IUserSavedPresetRepository.GetByIdAsync`, (3) user-saved preset ownership check → `ForbiddenException`, (4) both null → `NotFoundException`, (5) deserialize preset `StylesJson` array into per-`ModuleType` style map, (6) fetch existing 12 notebook style records, (7) for each record overwrite `StylesJson` from map entry, (8) refresh `notebook.UpdatedAt`, (9) commit, (10) return updated records
- [ ] T039 [US4] Add `POST /notebooks/{id}/styles/apply-preset/{presetId}` (→ 200) action to `Api/Controllers/NotebooksController.cs`
- [ ] T040 [P] [US4] Extend `Tests/Unit/Services/NotebookServiceTests.cs` with US4 tests: `ApplyPresetAsync_SystemPreset_UpdatesAllTwelveStyles`, `ApplyPresetAsync_UserPreset_OwnershipMismatch_ThrowsForbidden`, `ApplyPresetAsync_PresetNotFound_ThrowsNotFoundException`
- [ ] T041 [P] [US4] Extend `Tests/Integration/Controllers/NotebooksControllerTests.cs` with US4 tests: `ApplyPreset_Returns200_WithSystemPreset`, `ApplyPreset_Returns200_WithUserSavedPreset`, `ApplyPreset_Returns403_WhenNotebookNotOwnedByUser`, `ApplyPreset_Returns404_WhenPresetNotFound`

**Checkpoint**: User Stories 1–4 all work independently. All 4 US4 integration tests pass.

---

## Phase 7: User Story 5 — Browse System Style Presets (Priority: P5)

**Goal**: `GET /presets` returns all 5 system presets with full style definitions, ordered by `displayOrder`. No auth required.

**Independent Test**: Call `GET /presets` with no `Authorization` header → 200, 5 presets, Colorful has `"isDefault": true`, Classic has `"isDefault": false`, ordered by `displayOrder` 1–5.

### Implementation for User Story 5

- [ ] T042 [US5] Add `SystemStylePreset → SystemStylePresetResponse` mapping to `Api/Mapping/DomainToResponseProfile.cs`; deserialize `StylesJson` array from `SystemStylePreset` into `List<ModuleStyleResponse>`, setting `Id = Guid.Empty` and `NotebookId = Guid.Empty` on each entry
- [ ] T043 [US5] Create `Api/Controllers/PresetsController.cs` with `[ApiController][Route("presets")]` (no `[Authorize]`); implement `GET /presets` → call `ISystemStylePresetRepository.GetAllAsync`, map to `List<SystemStylePresetResponse>`, return 200
- [ ] T044 [P] [US5] Write `Tests/Integration/Controllers/PresetsControllerTests.cs`; implement tests: `GetPresets_Returns200WithFivePresets_WhenUnauthenticated`, `GetPresets_Returns200OrderedByDisplayOrder`, `GetPresets_HasColorfulAsDefault`

**Checkpoint**: All 5 user stories work independently. All 3 US5 integration tests pass. End-to-end flow from quickstart.md is executable.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [ ] T045 Update `specs/008-notebook-crud-styles/contracts/api-contracts.md` — add `Error 409: ACTIVE_EXPORT_EXISTS` to the `DELETE /notebooks/{id}` section and add `ACTIVE_EXPORT_EXISTS | 409 | Active PDF export exists for the notebook` row to the Error Code Reference table
- [ ] T046 Run `dotnet build Staccato.sln` and resolve any remaining compiler errors or warnings
- [ ] T047 Run `dotnet test Staccato.sln` and confirm all tests pass (unit + integration)
- [ ] T048 Apply migration (`dotnet ef database update --project Persistence/Persistence.csproj --startup-project Application/Application.csproj`) and run the quickstart.md manual test sequence end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user stories
- **Phase 3–7 (User Stories)**: All depend on Phase 2 completion; can proceed in priority order (P1 → P5) or in parallel if staffed
- **Phase 8 (Polish)**: Depends on all desired user stories complete

### User Story Dependencies

| Story | Depends on | Notes |
|-------|------------|-------|
| US1 (P1) | Phase 2 only | First story — no story dependencies |
| US2 (P2) | Phase 2 only | Shares `NotebooksController.cs` with US1 — implement sequentially |
| US3 (P3) | Phase 2 only | Adds actions to same controller as US1/US2 |
| US4 (P4) | Phase 2 only | Adds one action to `NotebooksController.cs` |
| US5 (P5) | Phase 2 only | Separate `PresetsController.cs` — can run in parallel with US1–US4 |

### Within Each User Story

- API model / validator tasks marked `[P]` → implement before service
- Service implementation → before controller actions
- Unit tests `[P]` and integration tests `[P]` → can be written alongside implementation

### Parallel Opportunities

- All Phase 2 tasks marked `[P]` can run simultaneously (T007–T011, T014–T019)
- US5 (T042–T044) can run fully in parallel with US2–US4 (different controller file)
- Unit test files and integration test files for a given story can be written in parallel with each other

---

## Parallel Example: Phase 2 Foundational

```
Parallel batch A (domain/interface layer — different files):
  T007  Create DomainModels/Models/NotebookSummary.cs
  T008  Create Domain/Interfaces/Repositories/ISystemStylePresetRepository.cs
  T010  Add HasActiveExportForNotebookAsync to Domain/Interfaces/Repositories/IPdfExportRepository.cs
  T011  Verify/create Domain/Exceptions/ConflictException.cs

Parallel batch B (response DTOs — all different files):
  T015  ApiModels/Notebooks/ModuleStyleRequest.cs + Validator
  T016  ApiModels/Notebooks/ModuleStyleResponse.cs
  T017  ApiModels/Notebooks/NotebookSummaryResponse.cs
  T018  ApiModels/Notebooks/NotebookDetailResponse.cs
  T019  ApiModels/Notebooks/SystemStylePresetResponse.cs

Sequential (depend on batch A):
  T009  Update INotebookRepository.cs (depends on T007)
  T012  Update EntityToDomainProfile.cs (depends on T007)
  T013  Update NotebookRepository.cs (depends on T009, T012)
  T014  Create SystemStylePresetRepository.cs (depends on T008)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (Setup)
2. Complete Phase 2 (Foundational — CRITICAL)
3. Complete Phase 3 (User Story 1)
4. **STOP and VALIDATE**: Run `dotnet test --filter "FullyQualifiedName~NotebooksController"` — all 11 US1 tests pass
5. Run quickstart.md steps 1–3 manually

### Incremental Delivery

1. Setup + Foundational → foundation ready (T001–T022)
2. US1 → notebooks are listable, creatable, fetchable (T023–T028)
3. US2 → notebooks are editable and deletable (T029–T033)
4. US3 → styles are viewable and bulk-updatable (T034–T037)
5. US4 → presets can be applied to notebooks (T038–T041)
6. US5 → preset browsing available publicly (T042–T044)
7. Polish → everything verified end-to-end (T045–T048)

---

## Notes

- `[P]` tasks operate on different files with no incomplete dependencies — safe to parallelize
- `[Story]` label maps each task to a specific user story for traceability
- `NotebookService.cs` is implemented incrementally across US1–US4 — a single class file gains methods phase by phase
- `NotebooksController.cs` similarly gains actions across US1–US4
- `DomainToResponseProfile.cs` gains mappings in Foundational (T020), US1 (T026), and US5 (T042) — coordinate to avoid merge conflicts
- Commit after each phase checkpoint to preserve a working state at each increment
