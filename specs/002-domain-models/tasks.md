# Tasks: Domain Model Implementation

**Input**: Design documents from `/specs/002-domain-models/`
**Prerequisites**: plan.md ✓ | spec.md ✓ | research.md ✓ | data-model.md ✓ | contracts/type-hierarchy.md ✓ | quickstart.md ✓

**Tests**: Included — success criteria SC-002, SC-003, SC-004, SC-006 mandate automated assertions.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US5 from spec.md)
- Exact file paths are included in every task description

---

## Phase 1: Setup

**Purpose**: Verify the existing project compiles clean before any files are added.

- [x] T001 Verify `DomainModels/DomainModels.csproj` baseline compiles with zero warnings via `dotnet build DomainModels/DomainModels.csproj`

**Checkpoint**: Clean baseline confirmed — file authoring can begin.

---

## Phase 2: Foundational — Enums

**Purpose**: All 9 enums must exist before any domain model or building block can reference them. Every subsequent phase depends on this phase being complete.

**⚠️ CRITICAL**: No user story work can begin until all enums are in place.

- [x] T002 [P] Create `DomainModels/Enums/ModuleType.cs` — file-scoped namespace `DomainModels.Enums`; 12 values: `Title, Breadcrumb, Subtitle, Theory, Practice, Example, Important, Tip, Homework, Question, ChordTablature, FreeText`
- [x] T003 [P] Create `DomainModels/Enums/BuildingBlockType.cs` — file-scoped namespace `DomainModels.Enums`; 10 values: `SectionHeading, Date, Text, BulletList, NumberedList, CheckboxList, Table, MusicalNotes, ChordProgression, ChordTablatureGroup`
- [x] T004 [P] Create `DomainModels/Enums/BorderStyle.cs` — file-scoped namespace `DomainModels.Enums`; 4 values: `None, Solid, Dashed, Dotted`
- [x] T005 [P] Create `DomainModels/Enums/FontFamily.cs` — file-scoped namespace `DomainModels.Enums`; 3 values: `Default, Monospace, Serif`
- [x] T006 [P] Create `DomainModels/Enums/PageSize.cs` — file-scoped namespace `DomainModels.Enums`; 5 values: `A4, A5, A6, B5, B6`
- [x] T007 [P] Create `DomainModels/Enums/ExportStatus.cs` — file-scoped namespace `DomainModels.Enums`; 4 values: `Pending, Processing, Ready, Failed`
- [x] T008 [P] Create `DomainModels/Enums/InstrumentKey.cs` — file-scoped namespace `DomainModels.Enums`; 7 values: `Guitar6String, Guitar7String, Bass4String, Bass5String, Ukulele4String, Banjo4String, Banjo5String`
- [x] T009 [P] Create `DomainModels/Enums/ChordStringState.cs` — file-scoped namespace `DomainModels.Enums`; 3 values: `Open, Fretted, Muted`
- [x] T010 [P] Create `DomainModels/Enums/Language.cs` — file-scoped namespace `DomainModels.Enums`; 2 values: `English, Hungarian`

**Checkpoint**: All 9 enums in place — user story implementation can now begin.

---

## Phase 3: User Story 1 — Notebook Creation and Configuration (Priority: P1) 🎯 MVP

**Goal**: Domain models for the core notebook lifecycle (User through NotebookModuleStyle) and the `PageSizeDimensions` static lookup are complete and unit-tested.

**Independent Test**: Run `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Unit.DomainModels.PageSizeDimensionsTests"` — all assertions pass for all 5 PageSize values.

### Implementation for User Story 1

- [x] T011 [P] [US1] Create `DomainModels/Models/User.cs` — namespace `DomainModels.Models`; properties: `Guid Id`, `string Email`, `string? PasswordHash`, `string? GoogleId`, `string FirstName`, `string LastName`, `string? AvatarUrl`, `DateTime CreatedAt`, `DateTime? ScheduledDeletionAt`, `Language Language`; no EF/validation attributes
- [x] T012 [P] [US1] Create `DomainModels/Models/RefreshToken.cs` — namespace `DomainModels.Models`; properties: `Guid Id`, `string Token`, `Guid UserId`, `DateTime ExpiresAt`, `DateTime CreatedAt`, `bool IsRevoked`; no EF/validation attributes
- [x] T013 [P] [US1] Create `DomainModels/Models/UserSavedPreset.cs` — namespace `DomainModels.Models`; properties: `Guid Id`, `Guid UserId`, `string Name`, `string StylesJson`; no EF/validation attributes
- [x] T014 [P] [US1] Create `DomainModels/Models/SystemStylePreset.cs` — namespace `DomainModels.Models`; properties: `Guid Id`, `string Name`, `int DisplayOrder`, `bool IsDefault`, `string StylesJson`; no EF/validation attributes
- [x] T015 [P] [US1] Create `DomainModels/Models/Instrument.cs` — namespace `DomainModels.Models`; properties: `Guid Id`, `InstrumentKey Key`, `string DisplayName`, `int StringCount`; no EF/validation attributes
- [x] T016 [P] [US1] Create `DomainModels/Models/Notebook.cs` — namespace `DomainModels.Models`; properties: `Guid Id`, `Guid UserId`, `string Title`, `Guid InstrumentId { get; init; }`, `PageSize PageSize { get; init; }`, `DateTime CreatedAt`, `DateTime UpdatedAt`; `InstrumentId` and `PageSize` MUST use init-only setters; no EF/validation attributes
- [x] T017 [P] [US1] Create `DomainModels/Models/NotebookModuleStyle.cs` — namespace `DomainModels.Models`; properties: `Guid Id`, `Guid NotebookId`, `ModuleType ModuleType`, `string StylesJson`; no EF/validation attributes
- [x] T018 [P] [US1] Create `DomainModels/Constants/PageSizeDimensions.cs` — namespace `DomainModels.Constants`; `public static class PageSizeDimensions`; one `static readonly` field: `IReadOnlyDictionary<PageSize, (int WidthMm, int HeightMm, int GridWidth, int GridHeight)> Dimensions` with entries: A4(210,297,42,59), A5(148,210,29,42), A6(105,148,21,29), B5(176,250,35,50), B6(125,176,25,35); grid = floor(mm÷5)
- [x] T019 [US1] Create `Tests/Unit/DomainModels/PageSizeDimensionsTests.cs` — namespace `Tests.Unit.DomainModels`; xUnit; assert all 5 PageSize values have an entry; assert A4→(210,297,42,59), A5→(148,210,29,42), A6→(105,148,21,29), B5→(176,250,35,50), B6→(125,176,25,35); covers SC-003

**Checkpoint**: User Story 1 complete — notebook core models and PageSizeDimensions are fully functional and tested.

---

## Phase 4: User Story 2 — Module Placement and Type Validation (Priority: P1)

**Goal**: Domain models for the lesson/page/module hierarchy and the `ModuleTypeConstraints` static lookup are complete and unit-tested.

**Independent Test**: Run `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Unit.DomainModels.ModuleTypeConstraintsTests"` — all assertions pass for all 12 ModuleType values.

### Implementation for User Story 2

- [x] T020 [P] [US2] Create `DomainModels/Models/Lesson.cs` — namespace `DomainModels.Models`; properties: `Guid Id`, `Guid NotebookId`, `string Title`, `DateTime CreatedAt`, `DateTime UpdatedAt`; no EF/validation attributes
- [x] T021 [P] [US2] Create `DomainModels/Models/LessonPage.cs` — namespace `DomainModels.Models`; properties: `Guid Id`, `Guid LessonId`, `int PageNumber`; no EF/validation attributes
- [x] T022 [P] [US2] Create `DomainModels/Models/Module.cs` — namespace `DomainModels.Models`; properties: `Guid Id`, `Guid LessonPageId`, `ModuleType ModuleType`, `int GridX`, `int GridY`, `int GridWidth`, `int GridHeight`, `string ContentJson`; no timestamps; no EF/validation attributes
- [x] T023 [P] [US2] Create `DomainModels/Constants/ModuleTypeConstraints.cs` — namespace `DomainModels.Constants`; `public static class ModuleTypeConstraints`; two `static readonly` fields — `IReadOnlyDictionary<ModuleType, IReadOnlySet<BuildingBlockType>> AllowedBlocks` and `IReadOnlyDictionary<ModuleType, (int MinWidth, int MinHeight)> MinimumSizes`; populate AllowedBlocks per `STACCATO_FRONTEND_DOCUMENTATION.md §5.4` (see data-model.md); MinimumSizes: Title(20,4), Breadcrumb(20,3), Subtitle(10,3), Theory(8,5), Practice(8,5), Example(8,5), Important(8,4), Tip(8,4), Homework(8,5), Question(8,4), ChordTablature(8,10), FreeText(4,4)
- [x] T024 [US2] Create `Tests/Unit/DomainModels/ModuleTypeConstraintsTests.cs` — namespace `Tests.Unit.DomainModels`; xUnit; assert all 12 ModuleType values have entries in both `AllowedBlocks` and `MinimumSizes`; assert `Breadcrumb` maps to empty allowed-block set; assert `ChordTablature` allowed-blocks contains `ChordTablatureGroup`; assert `FreeText` allowed-blocks count == 10; assert min-size values for all 12 types; covers SC-002 and SC-006

**Checkpoint**: User Story 2 complete — lesson/page/module models and ModuleTypeConstraints are fully functional and tested.

---

## Phase 5: User Story 3 — Module Content Authoring (Priority: P2)

**Goal**: The full building block hierarchy — abstract base, TextSpan leaf, all 6 support types, all 10 concrete block types — is complete and each type's discriminator is verified.

**Independent Test**: Run `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Unit.DomainModels.BuildingBlockTypeTests"` — all 10 concrete block types instantiate with the correct `Type` discriminator value.

### Group A — Base Types (both [P], no dependency on each other)

- [ ] T025 [P] [US3] Create `DomainModels/BuildingBlocks/TextSpan.cs` — namespace `DomainModels.BuildingBlocks`; `public record TextSpan`; properties: `string Text`, `bool Bold`; no other properties; no attributes
- [ ] T026 [P] [US3] Create `DomainModels/BuildingBlocks/BuildingBlock.cs` — namespace `DomainModels.BuildingBlocks`; `public abstract class BuildingBlock`; single property: `BuildingBlockType Type { get; }`; no setter; no serialization attributes

### Group B — Simple Block Types (all [P], depend on T025 + T026)

- [ ] T027 [P] [US3] Create `DomainModels/BuildingBlocks/SectionHeadingBlock.cs` — inherits `BuildingBlock`; constructor sets `Type = BuildingBlockType.SectionHeading`; property: `List<TextSpan> Spans`
- [ ] T028 [P] [US3] Create `DomainModels/BuildingBlocks/DateBlock.cs` — inherits `BuildingBlock`; constructor sets `Type = BuildingBlockType.Date`; property: `List<TextSpan> Spans`
- [ ] T029 [P] [US3] Create `DomainModels/BuildingBlocks/TextBlock.cs` — inherits `BuildingBlock`; constructor sets `Type = BuildingBlockType.Text`; property: `List<TextSpan> Spans`
- [ ] T030 [P] [US3] Create `DomainModels/BuildingBlocks/BulletListBlock.cs` — inherits `BuildingBlock`; constructor sets `Type = BuildingBlockType.BulletList`; property: `List<List<TextSpan>> Items`
- [ ] T031 [P] [US3] Create `DomainModels/BuildingBlocks/NumberedListBlock.cs` — inherits `BuildingBlock`; constructor sets `Type = BuildingBlockType.NumberedList`; property: `List<List<TextSpan>> Items`
- [ ] T032 [P] [US3] Create `DomainModels/BuildingBlocks/MusicalNotesBlock.cs` — inherits `BuildingBlock`; constructor sets `Type = BuildingBlockType.MusicalNotes`; property: `List<string> Notes`

### Group C — Support Types (all [P], depend on T025)

- [ ] T033 [P] [US3] Create `DomainModels/BuildingBlocks/CheckboxListItem.cs` — namespace `DomainModels.BuildingBlocks`; plain class (not BuildingBlock); properties: `List<TextSpan> Spans`, `bool IsChecked`; no attributes
- [ ] T034 [P] [US3] Create `DomainModels/BuildingBlocks/TableColumn.cs` — namespace `DomainModels.BuildingBlocks`; plain class; property: `List<TextSpan> Header`; no attributes
- [ ] T035 [P] [US3] Create `DomainModels/BuildingBlocks/ChordBeat.cs` — namespace `DomainModels.BuildingBlocks`; plain class; properties: `Guid ChordId`, `string DisplayName`, `int Beats`; no attributes
- [ ] T036 [P] [US3] Create `DomainModels/BuildingBlocks/ChordTablatureItem.cs` — namespace `DomainModels.BuildingBlocks`; plain class; properties: `Guid ChordId`, `string Label`; no attributes

### Group D — Composite Block Types (all [P], each depends on its own support type from Group C + T026)

- [ ] T037 [P] [US3] Create `DomainModels/BuildingBlocks/CheckboxListBlock.cs` — inherits `BuildingBlock`; constructor sets `Type = BuildingBlockType.CheckboxList`; property: `List<CheckboxListItem> Items`
- [ ] T038 [P] [US3] Create `DomainModels/BuildingBlocks/TableBlock.cs` — inherits `BuildingBlock`; constructor sets `Type = BuildingBlockType.Table`; properties: `List<TableColumn> Columns`, `List<List<List<TextSpan>>> Rows`
- [ ] T039 [P] [US3] Create `DomainModels/BuildingBlocks/ChordMeasure.cs` — namespace `DomainModels.BuildingBlocks`; plain class; property: `List<ChordBeat> Chords`; no attributes
- [ ] T040 [P] [US3] Create `DomainModels/BuildingBlocks/ChordTablatureGroupBlock.cs` — inherits `BuildingBlock`; constructor sets `Type = BuildingBlockType.ChordTablatureGroup`; property: `List<ChordTablatureItem> Items`

### Group E — Deep Chord Hierarchy (sequential — each depends on the previous)

- [ ] T041 [US3] Create `DomainModels/BuildingBlocks/ChordProgressionSection.cs` — namespace `DomainModels.BuildingBlocks`; plain class; properties: `string Label`, `int Repeat`, `List<ChordMeasure> Measures`; no attributes; depends on T039 (ChordMeasure)
- [ ] T042 [US3] Create `DomainModels/BuildingBlocks/ChordProgressionBlock.cs` — inherits `BuildingBlock`; constructor sets `Type = BuildingBlockType.ChordProgression`; properties: `string TimeSignature`, `List<ChordProgressionSection> Sections`; depends on T041 (ChordProgressionSection)

### Tests for User Story 3

- [ ] T043 [US3] Create `Tests/Unit/DomainModels/BuildingBlockTypeTests.cs` — namespace `Tests.Unit.DomainModels`; xUnit; for each of the 10 concrete block types assert that a newly constructed instance has `Type` equal to the expected `BuildingBlockType` enum value; covers SC-004

**Checkpoint**: User Story 3 complete — entire building block hierarchy is in place and all 10 type discriminators verified.

---

## Phase 6: User Story 4 — Chord and Instrument Reference Data (Priority: P3)

**Goal**: The `Chord` domain model is complete, representing an immutable seeded chord record with a raw fingering-positions payload.

**Independent Test**: Construct a `Chord` instance with all required properties and verify `PositionsJson` is a non-nullable string; verify `Suffix = ""` is accepted as valid per spec FR-015.

### Implementation for User Story 4

- [ ] T044 [US4] Create `DomainModels/Models/Chord.cs` — namespace `DomainModels.Models`; properties: `Guid Id`, `Guid InstrumentId`, `string Name`, `string Suffix`, `string PositionsJson`; all strings non-nullable; no EF/validation attributes

**Checkpoint**: User Story 4 complete — Chord model ready for seeder and repository layers.

---

## Phase 7: User Story 5 — PDF Export Lifecycle (Priority: P3)

**Goal**: The `PdfExport` domain model is complete, representing the four-state async export lifecycle.

**Independent Test**: Construct `PdfExport` instances in all four `ExportStatus` states and verify `BlobReference` is `string?` (nullable) and `CompletedAt` is `DateTime?`.

### Implementation for User Story 5

- [ ] T045 [US5] Create `DomainModels/Models/PdfExport.cs` — namespace `DomainModels.Models`; properties: `Guid Id`, `Guid NotebookId`, `Guid UserId`, `ExportStatus Status`, `DateTime CreatedAt`, `DateTime? CompletedAt`, `string? BlobReference`; no EF/validation attributes

**Checkpoint**: User Story 5 complete — full domain model surface is now implemented.

---

## Phase 8: Polish & Cross-Cutting Verification

**Purpose**: Confirm the full deliverable meets SC-001 (DomainModels compiles clean) and that the solution-wide build is unaffected.

- [ ] T046 Run `dotnet build DomainModels/DomainModels.csproj` — must produce zero errors and zero warnings with nullable reference types enabled; covers SC-001
- [ ] T047 Run `dotnet build Staccato.sln` — all 9 projects in the solution must compile successfully with the new DomainModels content in place
- [ ] T048 Run `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Unit.DomainModels"` — all 3 test classes (PageSizeDimensionsTests, ModuleTypeConstraintsTests, BuildingBlockTypeTests) must pass; covers SC-002, SC-003, SC-004, SC-006

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Enums)**: Depends on Phase 1 — BLOCKS all user story phases
- **Phase 3 (US1)**: Depends on Phase 2 completion
- **Phase 4 (US2)**: Depends on Phase 2 completion — independent of Phase 3
- **Phase 5 (US3)**: Depends on Phase 2 completion — independent of Phases 3 and 4
- **Phase 6 (US4)**: Depends on Phase 2 completion — independent of Phases 3, 4, 5
- **Phase 7 (US5)**: Depends on Phase 2 completion — independent of Phases 3, 4, 5, 6
- **Phase 8 (Polish)**: Depends on all user story phases being complete

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2 — no dependencies on other stories
- **US2 (P1)**: Starts after Phase 2 — no dependencies on other stories (parallel with US1)
- **US3 (P2)**: Starts after Phase 2 — no dependencies on other stories (parallel with US1, US2)
- **US4 (P3)**: Starts after Phase 2 — no dependencies on other stories
- **US5 (P3)**: Starts after Phase 2 — no dependencies on other stories

### Within Each User Story

- Domain model classes → constants class → unit tests (sequential)
- Within each group: [P] tasks can run simultaneously
- Building blocks: Group A → (Group B ∥ Group C) → Group D → Group E (sequential at Group E)

### Parallel Opportunities

- All 9 enum tasks (T002–T010) can run simultaneously
- All 7 domain model classes in US1 (T011–T017) can run simultaneously with PageSizeDimensions (T018)
- All 3 domain model classes in US2 (T020–T022) can run simultaneously with ModuleTypeConstraints (T023)
- All of Phases 3–7 can run simultaneously once Phase 2 is complete (5-way parallel if staffed)
- Within Phase 5: Group B (T027–T032), Group C (T033–T036), and Group D (T037–T040) tasks are all internally parallel

---

## Parallel Example: Phase 2 (Enums)

```
Simultaneously launch:
  T002 ModuleType.cs
  T003 BuildingBlockType.cs
  T004 BorderStyle.cs
  T005 FontFamily.cs
  T006 PageSize.cs
  T007 ExportStatus.cs
  T008 InstrumentKey.cs
  T009 ChordStringState.cs
  T010 Language.cs
```

## Parallel Example: Phase 5 Group B + Group C

```
After T025 (TextSpan) and T026 (BuildingBlock) complete, simultaneously launch:

Group B:                              Group C:
  T027 SectionHeadingBlock.cs           T033 CheckboxListItem.cs
  T028 DateBlock.cs                     T034 TableColumn.cs
  T029 TextBlock.cs                     T035 ChordBeat.cs
  T030 BulletListBlock.cs               T036 ChordTablatureItem.cs
  T031 NumberedListBlock.cs
  T032 MusicalNotesBlock.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational enums (T002–T010)
3. Complete Phase 3: User Story 1 (T011–T019)
4. **STOP and VALIDATE**: Run `dotnet test --filter PageSizeDimensions` — must pass
5. MVP deliverable: Notebook/Instrument/User models + PageSizeDimensions constant

### Incremental Delivery

1. Setup + Enums → Foundation ready (T001–T010)
2. US1 → Notebook models + PageSizeDimensions + tests ✓
3. US2 → Lesson/Page/Module models + ModuleTypeConstraints + tests ✓
4. US3 → Full building block hierarchy + discriminator tests ✓
5. US4 → Chord model ✓
6. US5 → PdfExport model ✓
7. Polish → Full build + solution build + all tests pass ✓

### Parallel Team Strategy

After Phase 2 (enums) completes:
- Developer A: US1 (T011–T019) — notebook models and PageSizeDimensions
- Developer B: US2 (T020–T024) — module hierarchy and ModuleTypeConstraints
- Developer C: US3 (T025–T043) — building block hierarchy
- Developers A or B (after their story): US4 (T044) + US5 (T045)

---

## Notes

- [P] tasks = different files, no shared-file conflicts, safe to run simultaneously
- All files use file-scoped namespaces per constitution Principle IX
- No EF Core, FluentValidation, or JSON serialization attributes on any type (FR-034)
- All `string` properties are non-nullable unless explicitly `string?` (FR-035)
- `TextSpan` is a `record` — not a class or struct (FR-022)
- `BuildingBlock.Type` has no setter — set in each concrete constructor (FR-023 / research.md Decision 6)
- `Notebook.InstrumentId` and `Notebook.PageSize` use `{ get; init; }` (FR-016)
- `ModuleTypeConstraints.AllowedBlocks` values sourced from `STACCATO_FRONTEND_DOCUMENTATION.md §5.4`
- Commit after each phase or logical group
- Stop at any checkpoint to validate story independently
- Run quickstart.md steps as a final integration check
