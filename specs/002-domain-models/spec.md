# Feature Specification: Domain Model Implementation

**Feature Branch**: `002-domain-models`
**Created**: 2026-03-02
**Status**: Draft
**Input**: User description: "Implement all domain model classes and enums in the DomainModels project for the Staccato instrument learning notebook application."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Notebook Creation and Configuration (Priority: P1)

A musician can create a digital notebook by choosing a page size (A4, A5, A6, B5, B6) and an instrument type (6-string guitar, 7-string guitar, 4/5-string bass, 4-string ukulele, 4/5-string banjo). The system permanently stores these choices and cannot change them after creation. Given any page size, the system can immediately report the exact grid dimensions (width × height in 5mm dot units) to guide module placement.

**Why this priority**: Notebook creation is the entry point to the entire application. All downstream features — lessons, pages, modules, exports — depend on a correctly structured Notebook model. Without it, nothing can be built or tested.

**Independent Test**: Can be fully tested by constructing Notebook, Instrument, and NotebookModuleStyle domain model instances with every PageSize and InstrumentKey combination and verifying that PageSizeDimensions returns correct grid dimensions.

**Acceptance Scenarios**:

1. **Given** valid notebook creation data, **When** a Notebook domain model is constructed with PageSize A4 and InstrumentKey Guitar6String, **Then** the model holds the owning user reference, title, page size, instrument reference, and two UTC timestamps.
2. **Given** a PageSize enum value, **When** PageSizeDimensions is queried, **Then** it returns the correct grid width and height in 5mm-spaced dot units (A4=42×59, A5=29×42, A6=21×29, B5=35×50, B6=25×35).
3. **Given** a newly created Notebook, **When** the associated module styles are checked, **Then** exactly 12 NotebookModuleStyle records exist — one for each of the 12 ModuleType values.

---

### User Story 2 - Module Placement and Type Validation (Priority: P1)

A musician can place typed content modules on a lesson page at any grid position and size. The system enforces a minimum size per module type and tracks which building block types are valid for each module type. These rules are defined in a single authoritative lookup table — no module-type logic may be scattered across multiple places.

**Why this priority**: Module placement and content-type constraints are the core business rules of the application. The domain models for Module, LessonPage, and Lesson, together with the ModuleTypeConstraints lookup, must exist before any content-authoring feature can be implemented or tested.

**Independent Test**: Can be fully tested by constructing Module domain model instances with different ModuleType values and querying ModuleTypeConstraints to verify allowed block types and minimum dimensions for every ModuleType.

**Acceptance Scenarios**:

1. **Given** any ModuleType value, **When** ModuleTypeConstraints is queried, **Then** it returns the exact set of allowed BuildingBlockType values and the minimum GridWidth and GridHeight for that type — and all 12 ModuleType values have entries.
2. **Given** a Module domain model, **When** constructed with GridX=3, GridY=5, GridWidth=8, GridHeight=4, **Then** all four values are stored as non-negative integers in 5mm grid units.
3. **Given** the Breadcrumb ModuleType, **When** ModuleTypeConstraints.AllowedBlocks is queried, **Then** the returned set is empty (reflecting the rule that Breadcrumb content is always an empty array).

---

### User Story 3 - Module Content Authoring (Priority: P2)

A musician can fill a module with structured content: plain-text paragraphs (with optional bold), bulleted and numbered lists, tables, musical note sequences, chord progressions with beat counts, and chord tablature groups. Every content item is typed and self-describing so the frontend can render it correctly. Text throughout the application is limited to plain text with optional bold — no italic, underline, colour, or size.

**Why this priority**: Building block content is what musicians actually create. The building block hierarchy is the application's central data contract for module content.

**Independent Test**: Can be fully tested by constructing each of the 10 building block types with valid data and verifying the type discriminator, nested structure, and that no forbidden formatting attributes exist.

**Acceptance Scenarios**:

1. **Given** a TextSpan, **When** constructed with a text string and bold flag, **Then** both values are retrievable and no other formatting attributes (italic, underline, colour, size) exist on the type.
2. **Given** a ChordProgressionBlock, **When** constructed with a time signature, sections, and nested chord beats, **Then** all levels (section → measure → chord beat) are accessible and the type discriminator matches the BuildingBlockType enum value "ChordProgression".
3. **Given** a BulletListBlock or NumberedListBlock, **When** constructed with items, **Then** each item is an ordered list of TextSpan values and the item order is preserved. **Given** a CheckboxListBlock, **When** constructed with items, **Then** each item holds an ordered list of TextSpan values and an `IsChecked` boolean, and the item order is preserved.
4. **Given** a TableBlock, **When** constructed with columns and rows, **Then** each column holds a header as a list of TextSpan values and each row cell holds a list of TextSpan values.

---

### User Story 4 - Chord and Instrument Reference Data (Priority: P3)

The system maintains an immutable chord library seeded at startup. A musician can reference any chord by its identifier inside a chord progression or tablature group block. The chord's display name is stored directly in the block so the frontend can render it without a live database lookup. Chord and Instrument records can never be modified or deleted after seeding.

**Why this priority**: Chord data underpins the ChordProgression and ChordTablature module types. Accurate, immutable reference models are required before those module types can be authored or validated.

**Independent Test**: Can be fully tested by constructing Chord and Instrument domain model instances and verifying that all required attributes are present and that no mutation operations exist on these types.

**Acceptance Scenarios**:

1. **Given** an Instrument domain model with InstrumentKey Guitar6String, **When** inspected, **Then** the string count is 6 and the display name is present.
2. **Given** a Chord domain model, **When** referenced by ID from a ChordProgressionBlock, **Then** the block stores the chord's display name directly so it can be rendered without a separate chord lookup.

---

### User Story 5 - PDF Export Lifecycle (Priority: P3)

A musician can request a PDF export of a notebook and track its progress through four states: Pending (queued), Processing (rendering), Ready (file available), and Failed (error). The system records when the export was requested and when it completed. The storage location of the generated file is an internal reference — it is never exposed directly to the musician.

**Why this priority**: PDF export is a key user-facing feature with an asynchronous lifecycle. The PdfExport domain model is the state machine that coordinates the background job workflow.

**Independent Test**: Can be fully tested by constructing PdfExport instances in each ExportStatus state and verifying that status, timestamps, and the optional blob reference are correctly represented.

**Acceptance Scenarios**:

1. **Given** a new export request, **When** a PdfExport domain model is created, **Then** the status is Pending, the completion timestamp is absent, and the internal storage reference is absent.
2. **Given** a PdfExport in Processing state, **When** the export succeeds, **Then** the status becomes Ready, the completion timestamp is populated, and the internal storage reference is set.
3. **Given** a PdfExport in any non-terminal state, **When** the export fails, **Then** the status can be set to Failed.

---

### Edge Cases

- What happens if a building block's type discriminator is unrecognised during deserialization? The building block model must not throw on unknown types — unrecognised blocks are skipped; validation of unknown types is a service-layer concern.
- What happens when a ChordProgressionBlock has zero sections or a section has zero measures? Empty collections are permitted in the domain model — validation of meaningful content is a service-layer concern.
- What happens when a TableBlock has rows with fewer cells than defined columns? The domain model stores data as-is — structural consistency is a service-layer concern.
- What happens when a MusicalNotesBlock contains an unrecognised note name? The domain model stores note names as plain strings — validation against the chromatic scale is a service-layer concern.
- What happens if a Module's GridX or GridY is negative? The domain model stores the values; boundary enforcement (non-negative coordinates, no out-of-bounds placement, no overlap) is enforced in the service layer using ModuleTypeConstraints and PageSizeDimensions.

## Requirements *(mandatory)*

### Functional Requirements

**Enumerations**

- **FR-001**: The system MUST define a `ModuleType` enum with exactly 12 values: Title, Breadcrumb, Subtitle, Theory, Practice, Example, Important, Tip, Homework, Question, ChordTablature, FreeText.
- **FR-002**: The system MUST define a `BuildingBlockType` enum with exactly 10 values: SectionHeading, Date, Text, BulletList, NumberedList, CheckboxList, Table, MusicalNotes, ChordProgression, ChordTablatureGroup.
- **FR-003**: The system MUST define a `BorderStyle` enum with 4 values: None, Solid, Dashed, Dotted.
- **FR-004**: The system MUST define a `FontFamily` enum with 3 values: Default, Monospace, Serif.
- **FR-005**: The system MUST define a `PageSize` enum with 5 values: A4, A5, A6, B5, B6.
- **FR-006**: The system MUST define an `ExportStatus` enum with 4 values: Pending, Processing, Ready, Failed.
- **FR-007**: The system MUST define an `InstrumentKey` enum with 7 values: Guitar6String, Guitar7String, Bass4String, Bass5String, Ukulele4String, Banjo4String, Banjo5String.
- **FR-008**: The system MUST define a `ChordStringState` enum with 3 values: Open, Fretted, Muted — representing the state of a single string in a chord fingering diagram.
- **FR-009**: The system MUST define a `Language` enum with 2 values: English, Hungarian — used to determine the language for localised error messages.

**Domain Model Classes**

- **FR-010**: The system MUST define a `User` domain model with: unique identifier, email address, optional password hash (absent for Google OAuth accounts), optional Google account identifier, first name, last name, optional avatar URL, account creation timestamp (UTC), optional scheduled deletion timestamp (UTC), and preferred language (Language enum).
- **FR-011**: The system MUST define a `RefreshToken` domain model with: unique identifier, token string value, owning user identifier, expiry timestamp (UTC), creation timestamp (UTC), and a revocation flag.
- **FR-012**: The system MUST define a `UserSavedPreset` domain model with: unique identifier, owning user identifier, display name, and a style data payload (raw JSON string).
- **FR-013**: The system MUST define a `SystemStylePreset` domain model representing one of 5 built-in style presets (Classic, Colorful, Dark, Minimal, Pastel), with: unique identifier, display name, display order integer (1-based, unique across all 5 presets), default flag, and a style data payload (raw JSON string). Exactly one preset (`Classic`) has `IsDefault = true`.
- **FR-014**: The system MUST define an `Instrument` domain model with: unique identifier, instrument key (InstrumentKey enum), display name, and string count (`int`, stored at seeding time). The `StringCount` values are fixed per `InstrumentKey`: Guitar6String→6, Guitar7String→7, Bass4String→4, Bass5String→5, Ukulele4String→4, Banjo4String→4, Banjo5String→5.
- **FR-015**: The system MUST define a `Chord` domain model with: unique identifier, owning instrument identifier, chord name (e.g., "C"), chord suffix (e.g., "m7", "maj7"; an empty string `""` is a valid suffix representing a plain major chord), and a fingering positions payload stored in a property named `PositionsJson` (non-nullable raw JSON string — structure per A-002 and `STACCATO_FRONTEND_DOCUMENTATION.md` §9).
- **FR-016**: The system MUST define a `Notebook` domain model with: unique identifier, owning user identifier, title, instrument identifier, page size (PageSize enum), creation timestamp (UTC), and last-updated timestamp (UTC). The `InstrumentId` and `PageSize` properties MUST be declared with init-only setters (`{ get; init; }`) — they are set once at construction and cannot be mutated thereafter by any caller.
- **FR-017**: The system MUST define a `NotebookModuleStyle` domain model with: unique identifier, owning notebook identifier, module type (ModuleType enum), and a style data payload (raw JSON string). Every notebook has exactly 12 of these — one per ModuleType value.
- **FR-018**: The system MUST define a `Lesson` domain model with: unique identifier, owning notebook identifier, title, creation timestamp (UTC), and last-updated timestamp (UTC). The service layer MUST update `UpdatedAt` whenever any content change occurs within the lesson, including: title edits, and any module addition, deletion, repositioning, or content update on any page of that lesson.
- **FR-019**: The system MUST define a `LessonPage` domain model with: unique identifier, owning lesson identifier, and page number (1-based integer).
- **FR-020**: The system MUST define a `Module` domain model with: unique identifier, owning lesson-page identifier, module type (ModuleType enum), grid X position, grid Y position, grid width, grid height (all non-negative `int` values measured in 5mm grid units), and a raw content payload stored in a property named `ContentJson` (non-nullable `string` — serialized array of building blocks; initialized to `"[]"` when a module is first created). Module has no `CreatedAt` or `UpdatedAt` timestamps; change tracking for page content is captured at the Lesson level.
- **FR-021**: The system MUST define a `PdfExport` domain model with: unique identifier, owning notebook identifier, owning user identifier, export status (ExportStatus enum), creation timestamp (UTC), optional completion timestamp (UTC), and an optional internal blob storage reference stored in a property named `BlobReference` (nullable `string?` — never exposed to clients).

**Building Block Content Models**

> Throughout this section, "ordered list" refers to an insertion-ordered `List<T>` in C#. Items are maintained in the order they were added and are not automatically sorted by any property.

- **FR-022**: The system MUST define `TextSpan` as a `record` with exactly two properties: `Text` (non-nullable `string`) and `Bold` (`bool`). `Text` MUST be a non-empty string — a `TextSpan` with `Text = ""` is invalid. No italic, underline, colour, size, or any other formatting property may exist.
- **FR-023**: The system MUST define an abstract `BuildingBlock` base type with a single type discriminator `property` named `Type` whose value identifies which `BuildingBlockType` the block represents. This is a C# property, not a C# attribute annotation. Concrete subclasses set `Type` in their constructor; the property has no setter.
- **FR-024**: The system MUST define `SectionHeadingBlock`, `DateBlock`, and `TextBlock` — each a BuildingBlock holding an ordered list of TextSpan values.
- **FR-025**: The system MUST define `BulletListBlock` and `NumberedListBlock` — each a BuildingBlock holding an ordered list of items, where each item is an ordered list of TextSpan values representing that list entry's text; each item list MUST contain at least one TextSpan. The system MUST define a standalone named `CheckboxListItem` type with two properties: `List<TextSpan> Spans` (the item's text content — at least one span required) and `bool IsChecked` (the persisted completion state). The system MUST define `CheckboxListBlock` — a BuildingBlock holding `List<CheckboxListItem> Items`. Item order is preserved by the domain model and is not affected by toggling `IsChecked`.
- **FR-026**: The system MUST define a standalone named `TableColumn` type with one property: `List<TextSpan> Header` (the column's header text — at least one span required). The system MUST define `TableBlock` — a BuildingBlock with `List<TableColumn> Columns` and `List<List<List<TextSpan>>> Rows` (row → cell → text spans). A `TableBlock` with an empty `Columns` list is invalid. A `TableBlock` with an empty `Rows` list when `Columns` is non-empty is also invalid. Each row list MUST have the same number of cell lists as `Columns.Count`.
- **FR-027**: The system MUST define `MusicalNotesBlock` — a BuildingBlock holding an ordered sequence of note name strings from the chromatic scale. The `Notes` list MUST contain at least one entry — an empty `MusicalNotesBlock` is invalid.
- **FR-028**: The system MUST define `ChordProgressionBlock` — a BuildingBlock with: a time signature string, and an ordered list of `ChordProgressionSection` values. The system MUST define `ChordProgressionSection` as a standalone named top-level model with: a label string, a `Repeat` count (`int`) that MUST be ≥ 1 (a section with Repeat ≤ 0 is invalid), and an ordered list of `ChordMeasure` values. The system MUST define `ChordMeasure` as a standalone named top-level model with an ordered list of `ChordBeat` values. The system MUST define `ChordBeat` as a standalone named top-level model with: a `ChordId` (`Guid`), a `DisplayName` string (stored at authoring time so the chord name can be rendered without a live database query — "offline rendering" means no DB round-trip at render time), and a `Beats` count (`int`) that MUST be ≥ 1 (a ChordBeat with Beats ≤ 0 is invalid). `ChordBeat.DisplayName` is the chord's contextual name within a progression; it is semantically distinct from `ChordTablatureItem.Label`, which is the user-assigned label beneath a chord diagram in a tablature group. All four types are top-level types in the DomainModels project — none are nested inner classes.
- **FR-029**: The system MUST define a standalone named `ChordTablatureItem` type with two properties: `Guid ChordId` (FK → Chord.Id) and `string Label` (user-defined display label for the chord diagram — distinct from the chord's canonical name). The system MUST define `ChordTablatureGroupBlock` — a BuildingBlock holding `List<ChordTablatureItem> Items` in insertion order.

**Constraint and Dimension Tables**

- **FR-030**: The system MUST define a static `ModuleTypeConstraints` class that maps every ModuleType value to: (a) the set of BuildingBlockType values permitted in modules of that type, and (b) the minimum GridWidth and GridHeight in grid units. This class MUST be the single source of truth for module-type constraints — no other class may embed or duplicate these mappings.
- **FR-031**: The ModuleTypeConstraints mapping MUST include all 12 ModuleType values with no gaps. The `Breadcrumb` type MUST map to an empty allowed-block set. `ChordTablatureGroup` blocks MUST be permitted in `ChordTablature`, `Practice`, and `FreeText` module types, and MUST NOT be permitted in any other type (Title, Breadcrumb, Subtitle, Theory, Example, Important, Tip, Homework, Question). `FreeText` accepts all 10 `BuildingBlockType` values. The authoritative per-type allowed-block mapping is defined in `STACCATO_FRONTEND_DOCUMENTATION.md` §5.4 and recorded in `data-model.md → ModuleTypeConstraints.AllowedBlocks`.
- **FR-032**: The system MUST define a static `PageSizeDimensions` class that maps every PageSize enum value to: (a) physical dimensions in millimetres, and (b) grid dimensions as width × height in 5mm-spaced dot units (A4=42×59, A5=29×42, A6=21×29, B5=35×50, B6=25×35). This class MUST be the single source of truth — no other class may embed these values.

**Project Constraints**

- **FR-033**: All enums, domain model classes, building block models, and static constraint classes MUST reside in the `DomainModels` project, which MUST reference no other project in the solution.
- **FR-034**: Every type in the DomainModels project — domain model classes, building block classes, support types (`CheckboxListItem`, `TableColumn`, `ChordBeat`, `ChordMeasure`, `ChordProgressionSection`, `ChordTablatureItem`), and static constant classes — MUST carry no persistence, serialization, or validation framework attributes. All types are plain data containers.
- **FR-035**: This requirement applies to all types in the DomainModels project. Optional string properties (e.g., `AvatarUrl`, `GoogleId`, `BlobReference`) MUST be typed as nullable `string?`; required string properties (e.g., `Email`, `Title`, `ContentJson`, `StylesJson`, `PositionsJson`) MUST be non-nullable `string`.

### Key Entities

- **User**: Represents a registered musician. Owns Notebooks. Authenticates via email/password or Google OAuth. Supports soft delete with a 30-day grace period.
- **Notebook**: Top-level workspace, immutably bound to a PageSize and Instrument. Contains Lessons and exactly 12 NotebookModuleStyle records.
- **NotebookModuleStyle**: Visual style record for one module type within a notebook. One per ModuleType, created atomically with the Notebook.
- **Lesson**: An ordered unit of study within a Notebook. Contains one or more LessonPages (first page auto-created).
- **LessonPage**: A single page within a Lesson, identified by a 1-based page number. Contains Modules on a 2D grid.
- **Module**: A typed content container placed at a specific grid position. Stores building block content as a serialized JSON array.
- **BuildingBlock hierarchy**: The 10 typed content element types placed inside a Module. TextSpan is the universal leaf unit for all user-entered text.
- **ModuleTypeConstraints**: A static lookup. The sole authority on which building block types are valid per module type and the minimum grid dimensions each module type requires.
- **PageSizeDimensions**: A static lookup. The sole authority on the physical and grid dimensions for each page size.
- **Chord / Instrument**: Immutable reference data seeded at startup. A Chord belongs to one Instrument. Neither can be modified or deleted after seeding.
- **PdfExport**: Tracks the full lifecycle of an async PDF export from queued through to ready or failed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every enum and domain model class in the DomainModels project compiles with nullable reference types enabled and zero warnings or errors, with no external project references in the `.csproj` — confirmed by `dotnet build DomainModels/DomainModels.csproj`.
- **SC-002**: ModuleTypeConstraints returns a non-null result for all 12 ModuleType values, with defined minimum dimensions and an allowed-block set for each — confirmed by an automated check iterating over every enum member.
- **SC-003**: PageSizeDimensions returns correct grid dimensions for all 5 PageSize values, satisfying the 5mm-spacing relationship using floor division (`floor(mm ÷ 5) = dot units`; e.g., 297mm → 59, 148mm → 29) — confirmed by automated assertions on all 5 values.
- **SC-004**: All 10 building block types can be instantiated with valid data and their `Type` property value matches the corresponding `BuildingBlockType` enum member name exactly (case-sensitive string equality, e.g., the serialized discriminator for `BuildingBlockType.ChordProgression` is `"ChordProgression"`) — confirmed by a round-trip test for each type.
- **SC-005**: No type in the DomainModels project — including domain model classes and building block support types — exposes a property or attribute that would introduce a dependency on an EF Core, FluentValidation, or JSON serialization namespace — confirmed by a project reference scan showing zero such framework references.
- **SC-006**: The Breadcrumb ModuleType maps to an empty allowed-block set in ModuleTypeConstraints, and the ChordTablature ModuleType maps to a set containing ChordTablatureGroup — confirmed by direct assertions on those two entries.
- **SC-007**: No type in the DomainModels project carries an EF Core, FluentValidation, JSON serialization, or other framework annotation as a C# attribute on any class or property declaration — confirmed by a source scan showing no `[JsonConverter]`, `[JsonPolymorphic]`, `[JsonDerivedType]`, `[Key]`, `[Column]`, `[Required]`, or equivalent attribute usages anywhere in the project.

## Assumptions

- **A-001**: The style data payload for `NotebookModuleStyle`, `UserSavedPreset`, and `SystemStylePreset` is stored as a non-nullable raw JSON string (`StylesJson`) in the domain model; its structured interpretation (border style, font family, colours) is handled at the serialization and service layers. A `null` value is never valid for `StylesJson`. The style property schema is confirmed by `STACCATO_FRONTEND_DOCUMENTATION.md` §6.
- **A-002**: The chord fingering payload in the `Chord` domain model is a raw JSON string (`PositionsJson`); its structure is defined by the chords-db seed format and is documented in `STACCATO_FRONTEND_DOCUMENTATION.md` §9 (`ChordPosition`, `ChordBarre`, `ChordString` structures). The format is confirmed and accessible.
- **A-003**: The 5 system style preset names (Classic, Colorful, Dark, Minimal, Pastel) are fixed at design time and seeded into the database; the `SystemStylePreset` domain model represents one seeded record per preset name.
- **A-004**: Minimum grid dimensions per ModuleType, allowed building block sets per ModuleType, and page size grid dimensions are fixed constants taken from `STACCATO_FRONTEND_DOCUMENTATION.md` §4 and §5. The file is present in the repository and is the authoritative source. These values are not configurable at runtime.
- **A-005**: The Language enum values correspond to IETF language tags: English → "en", Hungarian → "hu", for use with the IStringLocalizer localization infrastructure.
- **A-006**: The exact per-type allowed-block mappings are finalized and recorded in `data-model.md → ModuleTypeConstraints.AllowedBlocks`, sourced directly from `STACCATO_FRONTEND_DOCUMENTATION.md` §5.4. Key rules: `Breadcrumb` has an empty set; `ChordTablatureGroup` is permitted only in `ChordTablature`, `Practice`, and `FreeText`; `FreeText` accepts all 10 building block types. `ChordProgression` is permitted only in `Practice`, `Example`, and `FreeText`.

## Clarifications

### Session 2026-03-02

- Q: Should each CheckboxListBlock item store a persisted `IsChecked` boolean state alongside its text, or are checkboxes purely a visual rendering choice with no stored completion state? → A: Add `IsChecked` boolean per item — each item is `{ spans: TextSpan[], isChecked: bool }`. Confirmed by `STACCATO_FRONTEND_DOCUMENTATION.md` §5.2 (checkboxes are functional, not visual-only).
- Q: Should the nested structures in `ChordProgressionBlock` (section, measure, chord beat) be standalone named model classes or nested inner classes? → A: Standalone named top-level classes — `ChordProgressionSection`, `ChordMeasure`, `ChordBeat`.
- Q: Should the `Module` domain model include `CreatedAt` or `UpdatedAt` timestamp fields? → A: No timestamps — Module has no `CreatedAt` or `UpdatedAt`; change tracking is at the Lesson level.
- Q: Should `TextSpan` be a `class`, `record`, or `struct`? → A: `record` — consistent null-safety handling with value semantics.
- Q: How should `Notebook.InstrumentId` and `Notebook.PageSize` immutability be enforced in the domain model? → A: Init-only setters (`{ get; init; }`) — set once at construction, cannot be mutated by any caller.
- Q: Is `Instrument.StringCount` derived at runtime from `InstrumentKey` or stored as an independent field? → A: Stored `int` field — set by the seeder; not computed at runtime.
- Q: Should `Lesson.UpdatedAt` be updated only on lesson title edits, or also on any module change on any lesson page? → A: On any content change — title edits and any module addition, deletion, repositioning, or content update.
- Q: Should the type discriminator string match `BuildingBlockType` enum member names case-sensitively? → A: Yes — exact case-sensitive match (e.g., `"ChordProgression"`).
- Q: Are empty/zero states valid in the domain model for TextSpan.Text, ChordBeat.Beats, ChordProgressionSection.Repeat, MusicalNotesBlock.Notes, TableBlock.Rows, and TableBlock.Columns? → A: All invalid — spec adds explicit non-empty/positive invariants for each.
