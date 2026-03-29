# Tasks: PDF Export Pipeline

**Input**: Design documents from `/specs/011-pdf-export-pipeline/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Included — required by Constitution Principle VIII (Test Discipline).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the foundational abstractions and infrastructure contracts needed by all stories. No business logic.

- [ ] T001 [P] Create `IPdfExportQueue` interface with `EnqueueAsync(Guid exportId, CancellationToken ct)` in `Domain/Interfaces/IPdfExportQueue.cs`
- [ ] T002 [P] Create `PdfExportChannel` implementing `IPdfExportQueue`, wrapping `Channel<Guid>` with `BoundedChannelOptions(capacity: 50, FullMode: Wait)`, exposing `ChannelReader<Guid> Reader` in `Application/Channels/PdfExportChannel.cs`
- [ ] T003 [P] Add `Task PdfFailed(string exportId, string errorCode)` to `INotificationClient` interface in `Application/Hubs/NotificationHub.cs`
- [ ] T004 [P] Add `GetByStatusAsync(ExportStatus status, CancellationToken ct)` to `IPdfExportRepository` in `Domain/Interfaces/Repositories/IPdfExportRepository.cs`
- [ ] T005 Implement `GetByStatusAsync` in `PdfExportRepository` and update `GetExpiredExportsAsync` to return both Ready exports (CompletedAt + 24h <= cutoff) and Failed exports (CreatedAt + 24h <= cutoff) in `Repository/Repositories/PdfExportRepository.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Service interface, API models, controller shell, and DI wiring. MUST complete before any user story implementation.

**CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T006 [P] Create `CreatePdfExportRequest` record with `NotebookId` (Guid) and `LessonIds` (List<Guid>?) in `ApiModels/Requests/CreatePdfExportRequest.cs`
- [ ] T007 [P] Create `PdfExportResponse` record with Id, NotebookId, NotebookTitle, Status, CreatedAt, CompletedAt, LessonIds in `ApiModels/Responses/PdfExportResponse.cs`
- [ ] T008 [P] Create `CreatePdfExportResponse` record with ExportId and Status in `ApiModels/Responses/CreatePdfExportResponse.cs`
- [ ] T009 [P] Create `CreatePdfExportRequestValidator` — NotebookId not empty; LessonIds if present must not be empty list; each ID must not be Guid.Empty in `ApiModels/Validators/CreatePdfExportRequestValidator.cs`
- [ ] T010 Create `IPdfExportService` interface with methods: `QueueExportAsync`, `GetExportByIdAsync`, `DownloadExportAsync`, `GetExportsByUserAsync`, `DeleteExportAsync`, `MarkAsProcessingAsync`, `MarkAsReadyAsync`, `MarkAsFailedAsync`, `ResetStaleProcessingExportsAsync` in `Domain/Services/IPdfExportService.cs`
- [ ] T011 Add `PdfExport → PdfExportResponse` and `PdfExport → CreatePdfExportResponse` mappings to `Api/Mapping/DomainToResponseProfile.cs`
- [ ] T012 Register `PdfExportChannel` as singleton, forward `IPdfExportQueue` to same instance, register `IPdfExportService` as scoped in `Application/Extensions/ServiceCollectionExtensions.cs`

**Checkpoint**: Foundation ready — interfaces defined, DTOs created, DI configured. User story implementation can begin.

---

## Phase 3: User Story 1 + 5 — Export Full Notebook with Accurate Rendering (Priority: P1) MVP

**Goal**: A user queues a full notebook export, the background service renders a complete PDF (cover + index + lesson pages with all building blocks), uploads to Azure Blob, notifies via SignalR, and the user downloads it.

**Independent Test**: Create a notebook with lessons/pages/modules containing mixed building block types → POST /exports → poll or await SignalR → GET /exports/{id}/download → verify PDF contains cover, index, and all lesson pages with correct layout.

### Implementation for User Story 1 + 5

#### Service Layer

- [ ] T013 [US1] Implement `PdfExportService` with `QueueExportAsync` (validate notebook ownership, check active export conflict via repo, create PdfExport with Guid.NewGuid(), enqueue via IPdfExportQueue, commit via IUnitOfWork), `GetExportByIdAsync` (ownership check, throw NotFoundException/ForbiddenException), `DownloadExportAsync` (verify Ready status, check 24h expiry, get stream from IAzureBlobService, load notebook title for filename, sanitize filename), `MarkAsProcessingAsync`, `MarkAsReadyAsync`, `MarkAsFailedAsync`, `ResetStaleProcessingExportsAsync` in `Domain/Services/PdfExportService.cs`

#### Controller (Core Endpoints)

- [ ] T014 [US1] Create `ExportsController` with `[Authorize]`, implement `POST /exports` (call QueueExportAsync, return 202 with CreatePdfExportResponse), `GET /exports/{id}` (call GetExportByIdAsync, return 200), `GET /exports/{id}/download` (call DownloadExportAsync, return FileStreamResult with Content-Disposition) in `Api/Controllers/ExportsController.cs`

#### PDF Rendering Infrastructure

- [ ] T015 [P] [US1] Create `PdfRenderModels.cs` with `PdfExportData` (including `Language` for locale-dependent rendering per FR-005/FR-006), `LessonRenderData`, `PageRenderData`, `ModuleRenderData`, `ModuleStyleData` record types matching the data-model.md schema in `Application/Pdf/PdfRenderModels.cs`
- [ ] T016 [P] [US1] Create `DottedPaperBackground` as a reusable QuestPDF component using Canvas API — draw light gray (#CCCCCC) circles at 0.5mm diameter, 5mm spacing across the page in `Application/Pdf/DottedPaperBackground.cs`
- [ ] T017 [P] [US1] Create `CoverPageRenderer` — solid color background filling page (notebook CoverColor), centered title, instrument name, owner name (FirstName LastName), locale-formatted creation date, no page number in `Application/Pdf/CoverPageRenderer.cs`
- [ ] T018 [P] [US1] Create `IndexPageRenderer` — dotted paper background, localized "Table of Contents" heading (en/hu), lesson titles with global page numbers, page numbered starting at 1, multi-page support in `Application/Pdf/IndexPageRenderer.cs`

#### Module and Building Block Rendering

- [ ] T019 [P] [US1] Create `ModuleRenderer` — render module box at grid position (gridUnit * 5mm), apply NotebookModuleStyle (background, border color/width/radius, header bg/text color, body text color), clip content to bounds, render in ZIndex order in `Application/Pdf/ModuleRenderer.cs`
- [ ] T020 [P] [US1] Create text-based building block renderers (SectionHeading, Date, Text) with TextSpan bold support, skip empty blocks in `Application/Pdf/BuildingBlockRenderers.cs`
- [ ] T021 [P] [US1] Add list building block renderers (BulletList with bullet markers, NumberedList with sequential numbers, CheckboxList with checked/unchecked indicators) in `Application/Pdf/BuildingBlockRenderers.cs`
- [ ] T022 [P] [US1] Add Table building block renderer (column headers, rows, cells with TextSpan support) in `Application/Pdf/BuildingBlockRenderers.cs`
- [ ] T023 [P] [US1] Add MusicalNotes renderer (circular badges per note) and ChordProgression renderer (horizontal pill badges with beat counts, section labels, repeat markers) in `Application/Pdf/BuildingBlockRenderers.cs`
- [ ] T024 [US1] Add ChordTablatureGroup renderer using QuestPDF Canvas API — draw fretboard diagrams as vector graphics (string lines, fret lines, finger position dots, barre indicators, muted/open markers, chord name label), render placeholder with DisplayName when chord not found in `Application/Pdf/BuildingBlockRenderers.cs`

#### Document Assembly and Page Layout

- [ ] T025 [US1] Create `LessonPageRenderer` — dotted paper background, iterate modules sorted by ZIndex, delegate to ModuleRenderer, render empty module box for empty ContentJson in `Application/Pdf/LessonPageRenderer.cs`
- [ ] T026 [US1] Create `StaccatoPdfDocument` implementing `QuestPDF.Infrastructure.IDocument` — compose CoverPage, IndexPage, LessonPages using page size from PageSizeDimensions, global sequential page numbering in bottom-right (starting at 1 for index, cover excluded) in `Application/Pdf/StaccatoPdfDocument.cs`

#### Data Loading

- [ ] T027 [US1] Create `PdfDataLoader` — inject repository interfaces, load PdfExport → Notebook (with User for owner name and Language) → Instrument → NotebookModuleStyles → Lessons (ordered by CreatedAt) → LessonPages (ordered by PageNumber) → Modules → deserialize ContentJson into BuildingBlock objects → load referenced Chords. Return `PdfExportData` in `Application/Pdf/PdfDataLoader.cs`

#### Background Service

- [ ] T028 [US1] Implement `PdfExportBackgroundService` as `BackgroundService` — on startup call `ResetStaleProcessingExportsAsync` and re-enqueue recovered exports; in `ExecuteAsync` loop: `await foreach` on `PdfExportChannel.Reader`, create `IServiceScope` per job, call MarkAsProcessing, load data via PdfDataLoader, render via StaccatoPdfDocument, upload via IAzureBlobService at path `exports/{userId}/{exportId}.pdf`, call MarkAsReady, notify via `IHubContext<NotificationHub, INotificationClient>.Clients.User(userId).PdfReady(exportId, fileName)`. On failure: call MarkAsFailed, notify PdfFailed with errorCode. Handle missing record gracefully (FR-027). Respect CancellationToken for graceful shutdown (FR-029) in `Application/BackgroundServices/PdfExportBackgroundService.cs`

### Tests for User Story 1 + 5

- [ ] T029 [US1] Unit tests for `PdfExportService` — happy path for QueueExportAsync, GetExportByIdAsync, DownloadExportAsync, MarkAs* methods; exception paths: NotFoundException (export not found), ForbiddenException (wrong user), ConflictException (active export exists, ACTIVE_EXPORT_EXISTS), NotFoundException (download not Ready, EXPORT_NOT_READY), NotFoundException (download expired, EXPORT_EXPIRED) in `Tests/Unit/PdfExportServiceTests.cs`
- [ ] T030 [US1] Integration tests for `ExportsController` — POST /exports returns 202 with exportId; POST /exports with active export returns 409; GET /exports/{id} returns export details; GET /exports/{id} for other user returns 403; GET /exports/{id}/download for non-Ready returns 404 in `Tests/Integration/ExportsControllerTests.cs`

**Checkpoint**: Full notebook export pipeline functional end-to-end. User can queue, process, notify, and download. All 10 building block types render. MVP complete.

---

## Phase 4: User Story 2 — Export Selected Lessons (Priority: P2)

**Goal**: A user can export a subset of lessons by providing specific lesson IDs. Only those lessons appear in the PDF.

**Independent Test**: Create a notebook with 5 lessons → POST /exports with 2 lessonIds → download PDF → verify only 2 lessons present. Also test invalid lessonIds returns 400.

### Implementation for User Story 2

- [ ] T031 [US2] Update `PdfExportService.QueueExportAsync` — add lesson ID validation (all IDs belong to notebook, throw BadRequestException with code INVALID_LESSON_IDS if not), deduplicate lesson IDs before saving in `Domain/Services/PdfExportService.cs`
- [ ] T032 [US2] Update `PdfDataLoader` — when PdfExport.LessonIds is non-null, filter loaded lessons to only those IDs (maintain CreatedAt order); index page reflects only selected lessons in `Application/Pdf/PdfDataLoader.cs`

### Tests for User Story 2

- [ ] T033 [US2] Unit tests for lesson filtering — QueueExportAsync with valid lessonIds succeeds; invalid lessonIds throws BadRequestException(INVALID_LESSON_IDS); duplicate IDs are deduplicated in `Tests/Unit/PdfExportServiceTests.cs`
- [ ] T034 [US2] Integration tests — POST /exports with lessonIds returns 202; POST with invalid lessonIds returns 400 with INVALID_LESSON_IDS error code in `Tests/Integration/ExportsControllerTests.cs`

**Checkpoint**: Partial export works. Users can export specific lessons or full notebook.

---

## Phase 5: User Story 3 — View and Manage Export History (Priority: P3)

**Goal**: Users can list all their exports, view individual export status, and delete exports in any status.

**Independent Test**: Create multiple exports → GET /exports returns ordered list → DELETE export removes record and blob → DELETE Pending export is allowed.

### Implementation for User Story 3

- [ ] T035 [US3] Implement `GetExportsByUserAsync` (call repo GetByUserIdAsync, return list) and `DeleteExportAsync` (ownership check, delete blob via IAzureBlobService if BlobReference exists, remove record, commit) in `Domain/Services/PdfExportService.cs`
- [ ] T036 [US3] Add `GET /exports` (call GetExportsByUserAsync, return 200 with mapped list) and `DELETE /exports/{id}` (call DeleteExportAsync, return 204) endpoints to `Api/Controllers/ExportsController.cs`

### Tests for User Story 3

- [ ] T038 [US3] Unit tests for DeleteExportAsync — happy path deletes record + blob; delete without blob (Pending/Failed) succeeds; ForbiddenException for wrong user; NotFoundException for missing export. Unit tests for GetExportsByUserAsync in `Tests/Unit/PdfExportServiceTests.cs`
- [ ] T039 [US3] Integration tests — GET /exports returns list ordered by createdAt desc; DELETE /exports/{id} returns 204; DELETE for other user's export returns 403 in `Tests/Integration/ExportsControllerTests.cs`

**Checkpoint**: Full export management. Users can list, inspect, and delete their exports.

---

## Phase 6: User Story 4 — Automatic Cleanup of Expired Exports (Priority: P4)

**Goal**: A daily background service removes expired Ready exports (24h from completion) and Failed exports (24h from creation), deleting both DB records and Azure Blobs.

**Independent Test**: Seed export records with past timestamps → trigger cleanup → verify records and blobs deleted; verify non-expired exports are untouched.

### Implementation for User Story 4

- [ ] T040 [US4] Implement `ExportCleanupService` as `BackgroundService` with a 24-hour `PeriodicTimer` — on each tick: call `GetExpiredExportsAsync(DateTime.UtcNow)`, for each expired export delete blob via `IAzureBlobService.DeleteAsync` (if BlobReference exists), remove record via repo, commit. Log cleanup count. Use `IServiceScope` per cycle in `Application/BackgroundServices/ExportCleanupService.cs`

### Tests for User Story 4

- [ ] T041 [US4] Unit tests for `ExportCleanupService` — verify expired Ready exports deleted with blob; expired Failed exports deleted (no blob); non-expired exports untouched; blob delete failure does not crash service in `Tests/Unit/ExportCleanupServiceTests.cs`

**Checkpoint**: Automated lifecycle management. No manual intervention needed for expired exports.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Observability, final verification, and quality assurance across all stories.

- [ ] T042 [P] Add structured logging (ILogger) for export lifecycle events per FR-030: log queued (with exportId, userId), processing started, completed (with duration), failed (with errorCode) across `Domain/Services/PdfExportService.cs` and `Application/BackgroundServices/PdfExportBackgroundService.cs`
- [ ] T043 [P] Add queue-full timeout handling to `PdfExportService.QueueExportAsync` — wrap `EnqueueAsync` with 5-second `CancellationTokenSource` timeout, throw `ServiceUnavailableException` on timeout (FR-026) in `Domain/Services/PdfExportService.cs`
- [ ] T044 Build verification — `dotnet build Staccato.sln` passes with zero warnings
- [ ] T045 Full test suite — `dotnet test Staccato.sln` passes all existing + new tests
- [ ] T046 Run quickstart.md validation — manual smoke test of end-to-end export flow

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **US1+US5 (Phase 3)**: Depends on Foundational — this is the MVP
- **US2 (Phase 4)**: Depends on US1 (extends QueueExportAsync and PdfDataLoader)
- **US3 (Phase 5)**: Depends on US1 (extends service and controller, adds abort logic to background service)
- **US4 (Phase 6)**: Depends on Phase 1 (repo methods) — can run in parallel with US2/US3
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **US1+US5 (P1)**: Can start after Foundational (Phase 2) — no other story dependencies
- **US2 (P2)**: Depends on US1 — extends existing service and data loader
- **US3 (P3)**: Depends on US1 — extends service, controller, and background service
- **US4 (P4)**: Depends only on Phase 1 repo changes — can run in parallel with US2/US3

### Within Each User Story

- Service layer before controller (controller calls service)
- Render models before renderers (renderers use models)
- Individual renderers before document assembly (document composes renderers)
- Data loader before background service (background service uses loader)
- Background service before end-to-end tests
- Implementation before tests

### Parallel Opportunities

- Phase 1: T001–T004 can all run in parallel (different files)
- Phase 2: T006–T009 can all run in parallel (different files in ApiModels)
- Phase 3: T015–T018 can run in parallel (render infrastructure — different files)
- Phase 3: T019–T023 can run in parallel (building block renderers — same file but independent sections)
- Phase 6: US4 can run in parallel with US2 and US3

---

## Parallel Example: User Story 1 Phase 3

```bash
# Launch render infrastructure in parallel (different files):
T015: "Create PdfRenderModels in Application/Pdf/PdfRenderModels.cs"
T016: "Create DottedPaperBackground in Application/Pdf/DottedPaperBackground.cs"
T017: "Create CoverPageRenderer in Application/Pdf/CoverPageRenderer.cs"
T018: "Create IndexPageRenderer in Application/Pdf/IndexPageRenderer.cs"

# Then launch module/block renderers in parallel (same file, independent sections):
T019: "Create ModuleRenderer in Application/Pdf/ModuleRenderer.cs"
T020: "Text block renderers in Application/Pdf/BuildingBlockRenderers.cs"
T021: "List block renderers in Application/Pdf/BuildingBlockRenderers.cs"
T022: "Table block renderer in Application/Pdf/BuildingBlockRenderers.cs"
T023: "Music block renderers in Application/Pdf/BuildingBlockRenderers.cs"
```

---

## Implementation Strategy

### MVP First (US1 + US5 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: US1+US5 (core export + rendering)
4. **STOP and VALIDATE**: Full export pipeline works end-to-end
5. Deploy/demo if ready — users can export full notebooks

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add US1+US5 → Test independently → Deploy/Demo (**MVP!**)
3. Add US2 → Partial export works → Deploy/Demo
4. Add US3 → Export management works → Deploy/Demo
5. Add US4 → Automated cleanup works → Deploy/Demo
6. Polish → Logging, final verification

### Parallel Team Strategy

With multiple developers after Foundational completes:

1. **Developer A**: US1+US5 (core pipeline + all rendering)
2. **Developer B**: US4 (cleanup service — independent of US1)
3. After US1 completes:
   - **Developer A**: US2 (extends US1)
   - **Developer B**: US3 (extends US1)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US1 and US5 are merged because rendering quality (US5) is inseparable from the core export flow (US1)
- No database migration needed — PdfExport entity and configuration already exist
- All new code fits within existing 9-project architecture — no new projects
- Building block renderers are in a single file (`BuildingBlockRenderers.cs`) but each renderer is independent and can be implemented in parallel sections
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
