# Tasks: Lesson & Lesson Page Management

**Input**: Design documents from `/specs/009-lesson-page-management/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/endpoints.md, quickstart.md

**Tests**: Included — Constitution Principle VIII mandates unit tests for every service and integration tests for every controller.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No new project setup needed. All 9 projects exist. Entity models, EF configurations, repositories, and AutoMapper entity↔domain profiles already scaffolded in previous features.

*(No tasks — skip to Phase 2)*

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared domain models, repository extensions, API models, and AutoMapper mappings that ALL user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Domain Models

- [x] T001 [P] Create `LessonSummary` domain model with Id, NotebookId, Title, CreatedAt, PageCount in `DomainModels/Models/LessonSummary.cs`
- [x] T002 [P] Create `NotebookIndexEntry` domain model with LessonId, Title, CreatedAt, StartPageNumber in `DomainModels/Models/NotebookIndexEntry.cs`
- [x] T003 [P] Add `ModuleCount` property (int) to existing `LessonPage` domain model in `DomainModels/Models/LessonPage.cs`

### Repository Extensions

- [x] T004 [P] Add `GetSummariesByNotebookIdAsync(Guid notebookId, CancellationToken ct)` to `Domain/Interfaces/Repositories/ILessonRepository.cs`. Returns `Task<IReadOnlyList<LessonSummary>>` ordered by CreatedAt ascending with page counts.
- [x] T005 [P] Add `GetPageCountByLessonIdAsync(Guid lessonId, CancellationToken ct)` and `GetMaxPageNumberByLessonIdAsync(Guid lessonId, CancellationToken ct)` to `Domain/Interfaces/Repositories/ILessonPageRepository.cs`. Both return `Task<int>`.
- [x] T006 Implement `GetSummariesByNotebookIdAsync` in `Repository/Repositories/LessonRepository.cs` using EF `.Select()` projection with `LessonPages.Count()` for PageCount.
- [x] T007 Implement `GetPageCountByLessonIdAsync` and `GetMaxPageNumberByLessonIdAsync` in `Repository/Repositories/LessonPageRepository.cs`. Use `CountAsync` and `MaxAsync` respectively.
- [x] T008 Update `GetByLessonIdOrderedAsync` in `Repository/Repositories/LessonPageRepository.cs` to populate `ModuleCount` (via `Include(p => p.Modules)` or `.Select()` projection with `Modules.Count()`).
- [x] T009 Update `GetWithPagesAsync` in `Repository/Repositories/LessonRepository.cs` to populate `ModuleCount` on returned pages.

### API Models (Request DTOs + Validators)

- [x] T010 [P] Create `CreateLessonRequest` class (Title property) in `ApiModels/Lessons/CreateLessonRequest.cs`
- [x] T011 [P] Create `CreateLessonRequestValidator` with NotEmpty + MaximumLength(200) rules for Title in `ApiModels/Lessons/CreateLessonRequestValidator.cs`
- [x] T012 [P] Create `UpdateLessonRequest` class (Title property) in `ApiModels/Lessons/UpdateLessonRequest.cs`
- [x] T013 [P] Create `UpdateLessonRequestValidator` with NotEmpty + MaximumLength(200) rules for Title in `ApiModels/Lessons/UpdateLessonRequestValidator.cs`

### API Models (Response DTOs)

- [x] T014 [P] Create `LessonSummaryResponse` record (Id, Title, CreatedAt, PageCount) in `ApiModels/Lessons/LessonSummaryResponse.cs`
- [x] T015 [P] Create `LessonDetailResponse` record (Id, NotebookId, Title, CreatedAt, Pages list) in `ApiModels/Lessons/LessonDetailResponse.cs`
- [x] T016 [P] Create `LessonPageResponse` record (Id, LessonId, PageNumber, ModuleCount) in `ApiModels/Lessons/LessonPageResponse.cs`
- [x] T017 [P] Create `LessonPageWithWarningResponse` record (Data as LessonPageResponse, Warning as string?) in `ApiModels/Lessons/LessonPageWithWarningResponse.cs`
- [x] T018 [P] Create `NotebookIndexEntryResponse` record (LessonId, Title, CreatedAt, StartPageNumber) in `ApiModels/Lessons/NotebookIndexEntryResponse.cs`
- [x] T019 [P] Create `NotebookIndexResponse` record (Entries as list of NotebookIndexEntryResponse) in `ApiModels/Lessons/NotebookIndexResponse.cs`

### AutoMapper Mappings

- [x] T020 Add domain-to-response mappings for LessonSummary→LessonSummaryResponse, Lesson→LessonDetailResponse (with pages), LessonPage→LessonPageResponse, NotebookIndexEntry→NotebookIndexEntryResponse in `Api/Mapping/DomainToResponseProfile.cs`. Use `ConstructUsing` with ISO 8601 date formatting (`ToString("o")`).

**Checkpoint**: All shared models, DTOs, repository methods, and mappings ready. User story implementation can begin.

---

## Phase 3: User Story 1 — Create and Manage Lessons (Priority: P1) 🎯 MVP

**Goal**: Full CRUD for lessons within a notebook. Creating a lesson auto-creates the first page. Listing returns summaries with page counts. Ownership enforced via notebook.UserId.

**Independent Test**: Create a notebook → create/list/get/update/delete lessons within it. Verify first page auto-creation, ordering by CreatedAt, and cascade deletion.

### Implementation for User Story 1

- [x] T021 [US1] Create `ILessonService` interface in `Domain/Services/ILessonService.cs` with methods: `GetByNotebookIdAsync`, `CreateAsync`, `GetByIdAsync`, `UpdateAsync`, `DeleteAsync`. All accept `Guid userId` and `CancellationToken ct`. CreateAsync returns `(Lesson, IReadOnlyList<LessonPage>)`. GetByIdAsync returns `(Lesson, IReadOnlyList<LessonPage>)`.
- [x] T022 [US1] Implement `LessonService` in `Domain/Services/LessonService.cs`. Inject `ILessonRepository`, `ILessonPageRepository`, `INotebookRepository`, `IUnitOfWork`. For each method: load notebook via `INotebookRepository.GetByIdAsync` to verify ownership (throw `ForbiddenException` on mismatch, `NotFoundException` if not found). `CreateAsync`: generate Guid for lesson and first page, set `CreatedAt = UpdatedAt = DateTime.UtcNow`, add both via repositories, commit. `UpdateAsync`: load lesson with pages, verify ownership via notebook, set new title + `UpdatedAt`, update, commit. `DeleteAsync`: load lesson, verify ownership, remove, commit.
- [x] T023 [US1] Create `LessonsController` in `Api/Controllers/LessonsController.cs`. Use `[Authorize]`, inject `ILessonService` + `IMapper`. Add `GetUserId()` helper. Endpoints: `GET /notebooks/{id}/lessons` → 200 with `List<LessonSummaryResponse>`. `POST /notebooks/{id}/lessons` → 201 with `LessonDetailResponse`. `GET /lessons/{id}` → 200 with `LessonDetailResponse`. `PUT /lessons/{id}` → 200 with `LessonDetailResponse`. `DELETE /lessons/{id}` → 204 NoContent.
- [x] T024 [US1] Register `ILessonService` → `LessonService` as scoped in `Application/Extensions/ServiceCollectionExtensions.cs` `AddDomainServices` method.

### Tests for User Story 1

- [x] T025 [P] [US1] Create `LessonServiceTests` in `Tests/Unit/LessonServiceTests.cs`. Mock `ILessonRepository`, `ILessonPageRepository`, `INotebookRepository`, `IUnitOfWork`. Test: CreateAsync happy path (lesson + first page created, CommitAsync called), CreateAsync with non-existent notebook (NotFoundException), CreateAsync with wrong user (ForbiddenException), GetByNotebookIdAsync returns ordered summaries, GetByIdAsync happy path, GetByIdAsync not found, GetByIdAsync forbidden, UpdateAsync happy path (title changed, UpdatedAt set), UpdateAsync not found, UpdateAsync forbidden, DeleteAsync happy path (Remove called, CommitAsync called), DeleteAsync not found, DeleteAsync forbidden.
- [x] T026 [P] [US1] Create `LessonsControllerTests` in `Tests/Integration/LessonsControllerTests.cs`. Use `WebApplicationFactory<Program>` with InMemory EF. Inject test JWT via `AuthHelper`. Test: POST /notebooks/{id}/lessons returns 201 with lesson detail including page 1, GET /notebooks/{id}/lessons returns ordered list with pageCount, GET /lessons/{id} returns detail with pages, PUT /lessons/{id} returns updated detail, DELETE /lessons/{id} returns 204 and cascades, POST with empty title returns 400, POST with >200 char title returns 400, GET with non-existent id returns 404, operations on another user's notebook return 403.

**Checkpoint**: Lesson CRUD fully functional. Can create notebooks, create/list/get/update/delete lessons with auto-first-page and ownership enforcement.

---

## Phase 4: User Story 2 — Manage Lesson Pages (Priority: P2)

**Goal**: Add, list, and delete pages within lessons. Page numbers auto-assigned. Soft limit warning at 10+ pages. Cannot delete last page.

**Independent Test**: Create a lesson → add pages, list them, delete non-last pages. Verify page numbering, soft limit warning, and last-page guard.

### Implementation for User Story 2

- [x] T027 [US2] Create `ILessonPageService` interface in `Domain/Services/ILessonPageService.cs` with methods: `GetByLessonIdAsync(Guid lessonId, Guid userId, CancellationToken ct)` → `IReadOnlyList<LessonPage>`, `AddPageAsync(Guid lessonId, Guid userId, CancellationToken ct)` → `(LessonPage page, bool isOverSoftLimit)`, `DeletePageAsync(Guid lessonId, Guid pageId, Guid userId, CancellationToken ct)`.
- [x] T028 [US2] Implement `LessonPageService` in `Domain/Services/LessonPageService.cs`. Inject `ILessonRepository`, `ILessonPageRepository`, `INotebookRepository`, `IUnitOfWork`. Ownership check: load lesson → load notebook via lesson.NotebookId → verify UserId. `AddPageAsync`: get max page number + 1, get page count, create page with `Guid.NewGuid()`, add via repo, commit, return `(page, pageCount >= 10)`. `DeletePageAsync`: load page, verify `page.LessonId == lessonId` (else NotFoundException), verify ownership, check page count >= 2 (else `BadRequestException("LAST_PAGE_DELETION", ...)`), remove, commit.
- [x] T029 [US2] Create `LessonPagesController` in `Api/Controllers/LessonPagesController.cs`. Use `[Authorize]`, inject `ILessonPageService` + `IMapper`. Endpoints: `GET /lessons/{id}/pages` → 200 with `List<LessonPageResponse>`. `POST /lessons/{id}/pages` → if `isOverSoftLimit` return 200 else 201, both with `LessonPageWithWarningResponse` envelope (warning null or localized string). `DELETE /lessons/{lessonId}/pages/{pageId}` → 204 NoContent.
- [x] T030 [US2] Register `ILessonPageService` → `LessonPageService` as scoped in `Application/Extensions/ServiceCollectionExtensions.cs` `AddDomainServices` method.

### Tests for User Story 2

- [x] T031 [P] [US2] Create `LessonPageServiceTests` in `Tests/Unit/LessonPageServiceTests.cs`. Mock all dependencies. Test: AddPageAsync happy path (page created with correct number, CommitAsync called), AddPageAsync returns isOverSoftLimit=false when <10 pages, AddPageAsync returns isOverSoftLimit=true when >=10 pages, AddPageAsync on non-existent lesson (NotFoundException), AddPageAsync with wrong user (ForbiddenException), GetByLessonIdAsync returns ordered pages, DeletePageAsync happy path (Remove called), DeletePageAsync last page throws BadRequestException with code LAST_PAGE_DELETION, DeletePageAsync page belongs to different lesson throws NotFoundException, DeletePageAsync with wrong user (ForbiddenException), DeletePageAsync non-existent page (NotFoundException).
- [x] T032 [P] [US2] Create `LessonPagesControllerTests` in `Tests/Integration/LessonPagesControllerTests.cs`. Use `WebApplicationFactory<Program>` with InMemory EF. Test: POST /lessons/{id}/pages returns 201 with envelope {data, warning: null}, POST when 10+ pages returns 200 with warning string, GET /lessons/{id}/pages returns ordered list with moduleCount, DELETE returns 204, DELETE last page returns 400 with LAST_PAGE_DELETION code, DELETE page belonging to different lesson returns 404, operations on another user's lesson return 403.

**Checkpoint**: Page management fully functional. Can add, list, delete pages with auto-numbering, soft limit warning, and last-page protection.

---

## Phase 5: User Story 3 — View Notebook Index (Priority: P3)

**Goal**: Auto-generated table of contents for a notebook. Each entry shows lesson title and calculated global start page number (`2 + sum of previous page counts`).

**Independent Test**: Create a notebook with multiple lessons of varying page counts → request index → verify startPageNumber values match the formula.

**Dependencies**: Requires US1 (LessonService) to be complete since index is added to the same service.

### Implementation for User Story 3

- [x] T033 [US3] Add `GetNotebookIndexAsync(Guid notebookId, Guid userId, CancellationToken ct)` method signature to `Domain/Services/ILessonService.cs`. Returns `Task<IReadOnlyList<NotebookIndexEntry>>`.
- [x] T034 [US3] Implement `GetNotebookIndexAsync` in `Domain/Services/LessonService.cs`. Load notebook to verify ownership. Call `GetSummariesByNotebookIdAsync` to get lessons with page counts ordered by CreatedAt. Compute `startPageNumber` with running sum: for each lesson, `startPageNumber = 2 + cumulativePageCount`, then `cumulativePageCount += lesson.PageCount`. Return list of `NotebookIndexEntry`.
- [x] T035 [US3] Add `GET /notebooks/{id}/index` endpoint to `Api/Controllers/LessonsController.cs`. Return 200 with `NotebookIndexResponse` containing mapped entries.

### Tests for User Story 3

- [x] T036 [P] [US3] Add index calculation unit tests to `Tests/Unit/LessonServiceTests.cs`. Test: GetNotebookIndexAsync with 3 lessons (3, 2, 1 pages) returns startPageNumbers [2, 5, 7], empty notebook returns empty list, single lesson returns startPageNumber 2, notebook not found throws NotFoundException, wrong user throws ForbiddenException.
- [x] T037 [P] [US3] Add index integration tests to `Tests/Integration/LessonsControllerTests.cs`. Test: GET /notebooks/{id}/index returns correct entries with calculated startPageNumbers, empty notebook returns empty entries array, index updates after adding/deleting pages, another user's notebook returns 403.

**Checkpoint**: Notebook index fully functional. All three user stories work independently.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation across all stories.

- [ ] T038 Build the entire solution with `dotnet build Staccato.sln` and fix any compilation errors.
- [ ] T039 Run full test suite with `dotnet test Staccato.sln` and verify all tests pass.
- [ ] T040 Verify no remaining TODO markers or placeholder code in new files.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Skipped — nothing to do.
- **Foundational (Phase 2)**: No phase dependency — can start immediately. BLOCKS all user stories.
- **User Story 1 (Phase 3)**: Depends on Phase 2 completion.
- **User Story 2 (Phase 4)**: Depends on Phase 2 completion. Independent of US1 (different service/controller files).
- **User Story 3 (Phase 5)**: Depends on Phase 2 AND Phase 3 (US1) — adds methods to ILessonService/LessonService.
- **Polish (Phase 6)**: Depends on all user stories being complete.

### User Story Dependencies

```
Phase 2 (Foundational)
├── US1 (Phase 3) ─── can start immediately after Phase 2
├── US2 (Phase 4) ─── can start immediately after Phase 2 (parallel with US1)
└── US3 (Phase 5) ─── depends on US1 completion (modifies LessonService)
```

### Within Each User Story

- Service interface before implementation
- Service implementation before controller
- DI registration after service implementation
- Unit tests and integration tests can run in parallel (different files)

### Parallel Opportunities

**Phase 2 (max parallelism)**:
- T001, T002, T003 — domain models (3 different files)
- T004, T005 — repository interfaces (2 different files)
- T010–T019 — API models (10 different files)

**Phase 3 + Phase 4 (story parallelism)**:
- US1 and US2 can proceed in parallel (different services, controllers, test files)
- Within each: unit tests [P] and integration tests [P] are parallel

---

## Parallel Example: Phase 2 (Foundational)

```
# Batch 1: All domain models + interfaces + API models in parallel
T001 (LessonSummary.cs)
T002 (NotebookIndexEntry.cs)
T003 (LessonPage.cs modification)
T004 (ILessonRepository.cs modification)
T005 (ILessonPageRepository.cs modification)
T010–T019 (all ApiModels/Lessons/*.cs files)

# Batch 2: Repository implementations + AutoMapper (depend on interfaces + models)
T006 (LessonRepository.cs)
T007 (LessonPageRepository.cs)
T008 (LessonPageRepository.cs — same file as T007, must be sequential)
T009 (LessonRepository.cs — same file as T006, must be sequential)
T020 (DomainToResponseProfile.cs)
```

## Parallel Example: US1 + US2 in parallel

```
# Developer A: User Story 1
T021 → T022 → T023 → T024 → (T025 ∥ T026)

# Developer B: User Story 2
T027 → T028 → T029 → T030 → (T031 ∥ T032)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Foundational
2. Complete Phase 3: User Story 1 (Lesson CRUD)
3. **STOP and VALIDATE**: Create notebook, create lesson (verify auto first page), list/get/update/delete
4. Run unit + integration tests for US1

### Incremental Delivery

1. Phase 2 → Foundation ready
2. Add US1 → Lesson CRUD works → Test independently (MVP!)
3. Add US2 → Page management works → Test independently
4. Add US3 → Notebook index works → Test independently
5. Phase 6 → Full build + test pass

### Parallel Team Strategy

With two developers after Phase 2 completion:
- Developer A: US1 (Phase 3) → US3 (Phase 5, modifies US1 files)
- Developer B: US2 (Phase 4) → Polish (Phase 6)

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- US4 (Ownership Enforcement) is cross-cutting — verified within US1/US2/US3 integration tests via 403 test cases:
  - US4 Scenario 1 (lesson access by non-owner → 403): Covered in T026 (LessonsControllerTests — GET/PUT/DELETE 403 cases)
  - US4 Scenario 2 (page access by non-owner → 403): Covered in T032 (LessonPagesControllerTests — GET/POST/DELETE 403 cases)
  - US4 Scenario 3 (index access by non-owner → 403): Covered in T037 (LessonsControllerTests — GET /notebooks/{id}/index 403 case)
- All repository extensions modify existing files — execute T006-T009 sequentially per file
- AutoMapper mappings (T020) must be added after domain models and response DTOs exist
- DI registrations (T024, T030) modify the same file but different lines — can be sequential
