# Tasks: Module Management

**Input**: Design documents from `/specs/010-module-management/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Required per Constitution Principle VIII — unit tests for all service methods, integration tests for all endpoints.

**Organization**: Tasks grouped by user story. Each story delivers an independently testable increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Schema + Repository)

**Purpose**: Add ZIndex to the data model and extend the repository for Title uniqueness checks.

- [x] T001 [P] Add `public int ZIndex { get; set; }` property to DomainModels/Models/Module.cs
- [x] T002 [P] Add `public int ZIndex { get; set; }` property to EntityModels/Entities/ModuleEntity.cs
- [x] T003 Add `.Property(m => m.ZIndex).IsRequired()` to Persistence/Configurations/ModuleConfiguration.cs and generate EF migration via `dotnet ef migrations add AddModuleZIndex --project Persistence/Persistence.csproj --startup-project Application/Application.csproj`
- [x] T004 Add `Task<bool> HasTitleModuleInLessonAsync(Guid lessonId, Guid? excludeModuleId, CancellationToken ct = default)` to Domain/Interfaces/Repositories/IModuleRepository.cs
- [x] T005 Implement HasTitleModuleInLessonAsync in Repository/Repositories/ModuleRepository.cs — join Modules through LessonPages where LessonId matches, filter by ModuleType == Title, exclude optional moduleId

**Checkpoint**: `dotnet build Staccato.sln` passes. AutoMapper picks up ZIndex automatically via existing bidirectional mapping.

---

## Phase 2: Foundational (API Models + Service + Wiring)

**Purpose**: Create all DTOs, validators, service interface, service skeleton with shared validation methods, controller scaffold, and DI wiring. MUST complete before any user story.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T006 [P] Create ApiModels/Modules/CreateModuleRequest.cs — record with moduleType (string, required, valid enum), gridX (int, >= 0), gridY (int, >= 0), gridWidth (int, >= 1), gridHeight (int, >= 1), zIndex (int, >= 0), content (JsonElement, required, must be empty array). Create CreateModuleRequestValidator.cs alongside.
- [x] T007 [P] Create ApiModels/Modules/UpdateModuleRequest.cs — record with moduleType (string, required, valid enum), gridX, gridY, gridWidth, gridHeight, zIndex (same rules as create), content (JsonElement, required, array allowed non-empty). Create UpdateModuleRequestValidator.cs alongside.
- [x] T008 [P] Create ApiModels/Modules/PatchModuleLayoutRequest.cs — record with gridX (int, >= 0), gridY (int, >= 0), gridWidth (int, >= 1), gridHeight (int, >= 1), zIndex (int, >= 0). Create PatchModuleLayoutRequestValidator.cs alongside.
- [x] T009 [P] Create ApiModels/Modules/ModuleResponse.cs — record with Id (Guid), LessonPageId (Guid), ModuleType (string), GridX (int), GridY (int), GridWidth (int), GridHeight (int), ZIndex (int), Content (JsonElement — deserialized building block array).
- [x] T010 Create Domain/Services/IModuleService.cs with 5 method signatures: GetModulesByPageIdAsync, CreateModuleAsync, UpdateModuleAsync, UpdateModuleLayoutAsync, DeleteModuleAsync. Follow ILessonPageService pattern for parameter signatures (include Guid userId, CancellationToken ct).
- [x] T011 Create Domain/Services/ModuleService.cs with constructor injecting IModuleRepository, ILessonPageRepository, ILessonRepository, INotebookRepository, IUnitOfWork. Implement private helper methods: VerifyPageOwnershipAsync (page → lesson → notebook → userId check), VerifyModuleOwnershipAsync (module → page → lesson → notebook → userId check), ValidateGridPlacementAsync (minimum size from ModuleTypeConstraints, boundary from PageSizeDimensions, overlap via CheckOverlapAsync), ValidateContentAsync (building block type check from ModuleTypeConstraints.AllowedBlocks, breadcrumb empty check, malformed JSON check). Leave public methods as `throw new NotImplementedException()` stubs.
- [x] T012 [P] Add Module → ModuleResponse mapping in Api/Mapping/DomainToResponseProfile.cs. Map ModuleType enum to string. Deserialize ContentJson string to JsonElement for the Content property.
- [x] T013 [P] Create Api/Controllers/ModulesController.cs with `[ApiController]`, `[Authorize]` attributes, constructor injecting IModuleService and IMapper, and private `GetUserId()` helper. Add route stubs for all 5 endpoints returning NotImplemented.
- [x] T014 Register `IModuleService` → `ModuleService` as scoped in Application/Extensions/ServiceCollectionExtensions.cs AddDomainServices method.
- [x] T015 [P] Add localized error messages for MODULE_OVERLAP, MODULE_OUT_OF_BOUNDS, MODULE_TOO_SMALL, INVALID_BUILDING_BLOCK, BREADCRUMB_CONTENT_NOT_EMPTY, DUPLICATE_TITLE_MODULE, MODULE_TYPE_IMMUTABLE, MALFORMED_CONTENT_JSON to Application/Resources/BusinessErrors.en.resx and Application/Resources/BusinessErrors.hu.resx

**Checkpoint**: `dotnet build Staccato.sln` passes. All wiring in place. Service stubs throw NotImplementedException.

---

## Phase 3: User Story 1 — Place a New Module (Priority: P1) 🎯 MVP

**Goal**: Users can create a module on a lesson page with full server-side validation of all 6 grid placement rules.

**Independent Test**: POST /pages/{pageId}/modules with a valid Theory module returns 201. Invalid placements return the correct error code (422/409).

### Implementation for User Story 1

- [x] T016 [US1] Implement CreateModuleAsync in Domain/Services/ModuleService.cs — verify page ownership, validate empty content (FR-015), check Breadcrumb empty (FR-010), check Title uniqueness via HasTitleModuleInLessonAsync (FR-011), run ValidateGridPlacementAsync (FR-006/007/008), generate Guid.NewGuid() for Id, serialize content, call repository AddAsync + UnitOfWork CommitAsync, return created Module.
- [x] T017 [P] [US1] Implement POST /pages/{pageId}/modules endpoint in Api/Controllers/ModulesController.cs — route `[HttpPost("/pages/{pageId:guid}/modules")]`, accept CreateModuleRequest body, call CreateModuleAsync, map to ModuleResponse, return StatusCode(201, response).
- [x] T018 [P] [US1] Write unit tests for CreateModuleAsync in Tests/Unit/ModuleServiceTests.cs — happy path (returns module with generated Id), MODULE_TOO_SMALL (Theory with 6x3), MODULE_OUT_OF_BOUNDS (position exceeds A4 grid), MODULE_OVERLAP (overlapping rectangles), BREADCRUMB_CONTENT_NOT_EMPTY (non-empty Breadcrumb), DUPLICATE_TITLE_MODULE (second Title in lesson), valid boundary placement (gridX + gridWidth == pageGridWidth), adjacent non-overlapping modules (valid).
- [x] T019 [US1] Write integration tests for POST /pages/{pageId}/modules in Tests/Integration/Controllers/ModulesControllerTests.cs — setup factory with InMemory DB, seed instrument + notebook + lesson + page, test 201 happy path, 422 for each validation rule, 409 for duplicate Title, 403 for other user's page, 401 without auth token.

**Checkpoint**: POST endpoint fully functional. All 6 validation rules enforced. Unit + integration tests pass.

---

## Phase 4: User Story 2 — Edit Module Content (Priority: P2)

**Goal**: Users can update a module's content and position via PUT with building block type validation and moduleType immutability enforcement.

**Independent Test**: PUT /modules/{moduleId} with valid Theory content (SectionHeading + Text) returns 200. Invalid block type returns 422. moduleType mismatch returns 400.

### Implementation for User Story 2

- [x] T020 [US2] Implement UpdateModuleAsync in Domain/Services/ModuleService.cs — verify module ownership, validate moduleType matches stored value (throw BadRequestException "MODULE_TYPE_IMMUTABLE" on mismatch), run ValidateContentAsync (FR-009 allowed block types, FR-010 Breadcrumb empty, FR-016 malformed JSON), run ValidateGridPlacementAsync with excludeModuleId (FR-018 always run), update all fields, call repository Update + UnitOfWork CommitAsync, return updated Module.
- [x] T021 [P] [US2] Implement PUT /modules/{moduleId} endpoint in Api/Controllers/ModulesController.cs — route `[HttpPut("/modules/{moduleId:guid}")]`, accept UpdateModuleRequest body, call UpdateModuleAsync, map to ModuleResponse, return Ok(response).
- [x] T022 [P] [US2] Write unit tests for UpdateModuleAsync in Tests/Unit/ModuleServiceTests.cs — happy path (content updated + position changed), INVALID_BUILDING_BLOCK (ChordProgression in Theory), MODULE_TYPE_IMMUTABLE (type mismatch), BREADCRUMB_CONTENT_NOT_EMPTY (content in Breadcrumb on PUT), MODULE_OVERLAP (new position overlaps, excludes self), malformed JSON returns 400, content-only update still runs grid validation.
- [x] T023 [US2] Write integration tests for PUT /modules/{moduleId} in Tests/Integration/Controllers/ModulesControllerTests.cs — 200 happy path with content, 422 for invalid block type, 400 for moduleType mismatch, 422 for Breadcrumb with content, 403 for other user's module, 404 for non-existent module.

**Checkpoint**: PUT endpoint fully functional. Content validation + type immutability enforced. Unit + integration tests pass.

---

## Phase 5: User Story 3 — Drag and Resize Modules (Priority: P3)

**Goal**: Users can update module layout (position + size + zIndex) via lightweight PATCH without sending content.

**Independent Test**: PATCH /modules/{moduleId}/layout with new position returns 200 with updated position and unchanged content.

### Implementation for User Story 3

- [ ] T024 [US3] Implement UpdateModuleLayoutAsync in Domain/Services/ModuleService.cs — verify module ownership, run ValidateGridPlacementAsync with excludeModuleId (FR-006/007/008), update only gridX, gridY, gridWidth, gridHeight, zIndex (leave ContentJson and ModuleType unchanged), call repository Update + UnitOfWork CommitAsync, return updated Module.
- [ ] T025 [P] [US3] Implement PATCH /modules/{moduleId}/layout endpoint in Api/Controllers/ModulesController.cs — route `[HttpPatch("/modules/{moduleId:guid}/layout")]`, accept PatchModuleLayoutRequest body, call UpdateModuleLayoutAsync, map to ModuleResponse, return Ok(response).
- [ ] T026 [P] [US3] Write unit tests for UpdateModuleLayoutAsync in Tests/Unit/ModuleServiceTests.cs — happy path (position updated, content unchanged), MODULE_TOO_SMALL (resize below minimum), MODULE_OUT_OF_BOUNDS (drag past boundary), MODULE_OVERLAP (move into occupied space).
- [ ] T027 [US3] Write integration tests for PATCH /modules/{moduleId}/layout in Tests/Integration/Controllers/ModulesControllerTests.cs — 200 happy path (verify content unchanged), 422 for boundary/overlap/size violations, 403 for other user's module, 404 for non-existent module.

**Checkpoint**: PATCH endpoint fully functional. Grid validation enforced on layout-only updates. Unit + integration tests pass.

---

## Phase 6: User Story 4 — View All Modules on a Page (Priority: P4)

**Goal**: Users can retrieve all modules on a lesson page, ordered by GridY then GridX.

**Independent Test**: GET /pages/{pageId}/modules returns 200 with array of modules in correct order. Empty page returns [].

### Implementation for User Story 4

- [ ] T028 [US4] Implement GetModulesByPageIdAsync in Domain/Services/ModuleService.cs — verify page ownership, call repository GetByPageIdAsync, return module list.
- [ ] T029 [P] [US4] Implement GET /pages/{pageId}/modules endpoint in Api/Controllers/ModulesController.cs — route `[HttpGet("/pages/{pageId:guid}/modules")]`, call GetModulesByPageIdAsync, map to List<ModuleResponse>, return Ok(response).
- [ ] T030 [P] [US4] Write unit tests for GetModulesByPageIdAsync in Tests/Unit/ModuleServiceTests.cs — happy path (returns modules), empty page (returns empty list), NotFoundException for non-existent page, ForbiddenException for other user's page.
- [ ] T031 [US4] Write integration tests for GET /pages/{pageId}/modules in Tests/Integration/Controllers/ModulesControllerTests.cs — 200 with modules (verify ordering, content deserialized), 200 with empty array, 403 for other user's page, 401 without auth.

**Checkpoint**: GET endpoint fully functional. Module listing with correct ordering. Unit + integration tests pass.

---

## Phase 7: User Story 5 — Delete a Module (Priority: P5)

**Goal**: Users can permanently delete a module from a lesson page.

**Independent Test**: DELETE /modules/{moduleId} returns 204. Module no longer appears in GET.

### Implementation for User Story 5

- [ ] T032 [US5] Implement DeleteModuleAsync in Domain/Services/ModuleService.cs — verify module ownership, call repository Remove + UnitOfWork CommitAsync.
- [ ] T033 [P] [US5] Implement DELETE /modules/{moduleId} endpoint in Api/Controllers/ModulesController.cs — route `[HttpDelete("/modules/{moduleId:guid}")]`, call DeleteModuleAsync, return NoContent().
- [ ] T034 [P] [US5] Write unit tests for DeleteModuleAsync in Tests/Unit/ModuleServiceTests.cs — happy path (module removed), NotFoundException for non-existent module, ForbiddenException for other user's module.
- [ ] T035 [US5] Write integration tests for DELETE /modules/{moduleId} in Tests/Integration/Controllers/ModulesControllerTests.cs — 204 happy path (verify module gone via GET), 403 for other user's module, 404 for non-existent module, 401 without auth.

**Checkpoint**: DELETE endpoint fully functional. Hard delete with ownership check. Unit + integration tests pass.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Cross-story validation, cleanup, and final verification.

- [ ] T036 Write cross-story integration test: delete Title module then create new Title on different page (verifies slot freed) in Tests/Integration/Controllers/ModulesControllerTests.cs
- [ ] T037 Write cross-story integration test: all 5 endpoints return 401 without auth token (single parameterized test) in Tests/Integration/Controllers/ModulesControllerTests.cs
- [ ] T038 Run full test suite via `dotnet test Staccato.sln` and fix any failures
- [ ] T039 Run quickstart.md validation steps: build, test, smoke test POST endpoint

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories
- **User Stories (Phases 3–7)**: All depend on Phase 2 completion
  - US1 (P1): No dependencies on other stories
  - US2 (P2): No dependencies on other stories (can test via PUT directly)
  - US3 (P3): No dependencies on other stories
  - US4 (P4): No dependencies on other stories
  - US5 (P5): No dependencies on other stories
- **Polish (Phase 8)**: Depends on all story phases complete

### Within Each User Story

1. Service method first (implements business logic)
2. Controller endpoint + Unit tests in parallel (different files, both depend only on service method)
3. Integration tests last (depend on controller endpoint)

### Parallel Opportunities

- **Phase 1**: T001 and T002 in parallel (different projects)
- **Phase 2**: T006, T007, T008, T009 in parallel (separate files in ApiModels/Modules/). T012, T013, T015 in parallel (different files/projects).
- **Each US phase**: Controller endpoint [P] + Unit tests [P] after service method completes
- **Across stories**: All 5 user stories can run in parallel after Phase 2 (if team capacity allows)

---

## Parallel Example: User Story 1

```
# After T016 (service method) completes, launch in parallel:
T017 [P] — POST endpoint in ModulesController.cs
T018 [P] — Unit tests in ModuleServiceTests.cs

# After T017 completes:
T019 — Integration tests in ModulesControllerTests.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T005)
2. Complete Phase 2: Foundational (T006–T015)
3. Complete Phase 3: User Story 1 (T016–T019)
4. **STOP and VALIDATE**: POST endpoint works, all 6 validation rules enforced, tests pass
5. This is a deployable MVP — users can place modules on pages

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. + US1 (POST) → MVP: place modules with validation ✓
3. + US2 (PUT) → Content editing with block type validation ✓
4. + US3 (PATCH) → Drag/resize with grid validation ✓
5. + US4 (GET) → Page rendering with module list ✓
6. + US5 (DELETE) → Module removal ✓
7. + Polish → Cross-story tests, final verification ✓

### Parallel Team Strategy

With multiple developers after Phase 2 completes:

- Developer A: US1 (POST — most complex, all validation rules)
- Developer B: US4 (GET) + US5 (DELETE — simpler, can do both)
- Developer C: US2 (PUT) + US3 (PATCH — related update operations)

---

## Notes

- [P] tasks target different files with no dependencies — safe to parallelize
- [Story] labels map every task to its user story for traceability
- Tests are required per Constitution Principle VIII
- All service methods share the private validation helpers created in T011
- ModulesController.cs is a single file modified across multiple stories — endpoint tasks within a story are sequential with service method, but different stories' controller additions don't conflict (different methods)
- ModuleServiceTests.cs and ModulesControllerTests.cs grow across stories — each story adds new test methods to the same file
