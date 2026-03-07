# Data Model: Domain Model Implementation

**Branch**: `002-domain-models` | **Date**: 2026-03-02

All types reside in the `DomainModels` project. Zero project references.
Nullable reference types enabled throughout.

---

## Enums (`DomainModels/Enums/`)

### ModuleType

12 values representing the semantic type of a content module on a lesson page.

```
Title, Breadcrumb, Subtitle, Theory, Practice, Example,
Important, Tip, Homework, Question, ChordTablature, FreeText
```

### BuildingBlockType

10 values identifying the type of a building block within a module's content.

```
SectionHeading, Date, Text, BulletList, NumberedList, CheckboxList,
Table, MusicalNotes, ChordProgression, ChordTablatureGroup
```

### BorderStyle

4 values for module border rendering.

```
None, Solid, Dashed, Dotted
```

### FontFamily

3 values for module font rendering.

```
Default, Monospace, Serif
```

### PageSize

5 values representing the physical page size of a notebook.

```
A4, A5, A6, B5, B6
```

### ExportStatus

4 values for the PDF export lifecycle state machine.

```
Pending, Processing, Ready, Failed
```

### InstrumentKey

7 values identifying a supported instrument type.

```
Guitar6String, Guitar7String, Bass4String, Bass5String,
Ukulele4String, Banjo4String, Banjo5String
```

### ChordStringState

3 values for a single string's state in a chord fingering diagram.

```
Open, Fretted, Muted
```

### Language

2 values for the user's preferred language for localised messages.

```
English, Hungarian
```

---

## Domain Models (`DomainModels/Models/`)

### User

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `Guid` | No | App-generated (`Guid.NewGuid()`) |
| `Email` | `string` | No | Unique across all users |
| `PasswordHash` | `string?` | Yes | Null for Google OAuth-only accounts |
| `GoogleId` | `string?` | Yes | Null for email/password-only accounts |
| `FirstName` | `string` | No | |
| `LastName` | `string` | No | |
| `AvatarUrl` | `string?` | Yes | External URL or null |
| `CreatedAt` | `DateTime` | No | UTC |
| `ScheduledDeletionAt` | `DateTime?` | Yes | UTC; non-null = soft-delete scheduled |
| `Language` | `Language` | No | Default: English |

---

### RefreshToken

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `Guid` | No | App-generated |
| `Token` | `string` | No | Opaque token value |
| `UserId` | `Guid` | No | FK → User.Id |
| `ExpiresAt` | `DateTime` | No | UTC; encodes rememberMe choice |
| `CreatedAt` | `DateTime` | No | UTC |
| `IsRevoked` | `bool` | No | True = token has been rotated or invalidated |

---

### UserSavedPreset

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `Guid` | No | App-generated |
| `UserId` | `Guid` | No | FK → User.Id |
| `Name` | `string` | No | User-defined preset name |
| `StylesJson` | `string` | No | Raw JSON; structured interpretation at mapping layer |

---

### SystemStylePreset

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `Guid` | No | App-generated; seeded at startup |
| `Name` | `string` | No | Classic, Colorful, Dark, Minimal, or Pastel |
| `DisplayOrder` | `int` | No | 1-based sort order in UI |
| `IsDefault` | `bool` | No | True for exactly one preset (Classic) |
| `StylesJson` | `string` | No | Raw JSON; structured interpretation at mapping layer |

---

### Instrument

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `Guid` | No | App-generated; seeded at startup, immutable |
| `Key` | `InstrumentKey` | No | Identifies the instrument type |
| `DisplayName` | `string` | No | Human-readable name (e.g., "6-String Guitar") |
| `StringCount` | `int` | No | Derived from Key; Guitar6String→6, Bass4String→4, etc. |

---

### Chord

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `Guid` | No | App-generated; seeded at startup, immutable |
| `InstrumentId` | `Guid` | No | FK → Instrument.Id |
| `Name` | `string` | No | Root note name (e.g., "C", "F#") |
| `Suffix` | `string` | No | Chord quality suffix (e.g., "m7", "maj7", "" for major) |
| `PositionsJson` | `string` | No | Raw JSON; array of per-string `{ state, fret }` objects |

---

### Notebook

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `Guid` | No | App-generated |
| `UserId` | `Guid` | No | FK → User.Id |
| `Title` | `string` | No | |
| `InstrumentId` | `Guid` | No | FK → Instrument.Id; init-only (`{ get; init; }`) — immutable after construction |
| `PageSize` | `PageSize` | No | Init-only (`{ get; init; }`) — immutable after construction |
| `CreatedAt` | `DateTime` | No | UTC |
| `UpdatedAt` | `DateTime` | No | UTC |

---

### NotebookModuleStyle

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `Guid` | No | App-generated |
| `NotebookId` | `Guid` | No | FK → Notebook.Id |
| `ModuleType` | `ModuleType` | No | Each notebook has exactly one record per ModuleType (12 total) |
| `StylesJson` | `string` | No | Raw JSON; structured interpretation at mapping layer |

---

### Lesson

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `Guid` | No | App-generated |
| `NotebookId` | `Guid` | No | FK → Notebook.Id |
| `Title` | `string` | No | |
| `CreatedAt` | `DateTime` | No | UTC; ordering by CreatedAt ascending |
| `UpdatedAt` | `DateTime` | No | UTC |

---

### LessonPage

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `Guid` | No | App-generated |
| `LessonId` | `Guid` | No | FK → Lesson.Id |
| `PageNumber` | `int` | No | 1-based; page 1 auto-created with the lesson |

---

### Module

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `Guid` | No | App-generated |
| `LessonPageId` | `Guid` | No | FK → LessonPage.Id |
| `ModuleType` | `ModuleType` | No | Determines allowed blocks and minimum grid size |
| `GridX` | `int` | No | Column in 5mm grid units; ≥ 0 |
| `GridY` | `int` | No | Row in 5mm grid units; ≥ 0 |
| `GridWidth` | `int` | No | Width in 5mm grid units; ≥ MinWidth for ModuleType |
| `GridHeight` | `int` | No | Height in 5mm grid units; ≥ MinHeight for ModuleType |
| `ContentJson` | `string` | No | Serialised `BuildingBlock[]` array |

> No `CreatedAt` or `UpdatedAt` — change tracking is at the Lesson level (clarification Q3).

---

### PdfExport

| Property | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `Guid` | No | App-generated |
| `NotebookId` | `Guid` | No | FK → Notebook.Id |
| `UserId` | `Guid` | No | FK → User.Id |
| `Status` | `ExportStatus` | No | Initial: Pending |
| `CreatedAt` | `DateTime` | No | UTC |
| `CompletedAt` | `DateTime?` | Yes | UTC; set when status → Ready or Failed |
| `BlobReference` | `string?` | Yes | Internal Azure Blob path; **never exposed to clients** |

---

## Building Block Types (`DomainModels/BuildingBlocks/`)

### TextSpan

Leaf unit for all user-entered text throughout the application. Defined as a `record`.

| Property | Type | Notes |
|---|---|---|
| `Text` | `string` | Plain text; no inline formatting. MUST be non-empty (`""` is invalid). |
| `Bold` | `bool` | True = bold rendering |

> Exactly two properties. No italic, underline, colour, size, or any other formatting.

---

### BuildingBlock (abstract base)

| Property | Type | Notes |
|---|---|---|
| `Type` | `BuildingBlockType` | Get-only; set in concrete subclass constructor |

---

### SectionHeadingBlock : BuildingBlock

`Type = BuildingBlockType.SectionHeading`

| Property | Type |
|---|---|
| `Spans` | `List<TextSpan>` |

---

### DateBlock : BuildingBlock

`Type = BuildingBlockType.Date`

| Property | Type |
|---|---|
| `Spans` | `List<TextSpan>` |

---

### TextBlock : BuildingBlock

`Type = BuildingBlockType.Text`

| Property | Type |
|---|---|
| `Spans` | `List<TextSpan>` |

---

### BulletListBlock : BuildingBlock

`Type = BuildingBlockType.BulletList`

| Property | Type | Notes |
|---|---|---|
| `Items` | `List<List<TextSpan>>` | Each inner list = one bullet item's text spans |

---

### NumberedListBlock : BuildingBlock

`Type = BuildingBlockType.NumberedList`

| Property | Type | Notes |
|---|---|---|
| `Items` | `List<List<TextSpan>>` | Each inner list = one numbered item's text spans |

---

### CheckboxListItem

Standalone support type for CheckboxListBlock.

| Property | Type | Notes |
|---|---|---|
| `Spans` | `List<TextSpan>` | The item's text content |
| `IsChecked` | `bool` | Persisted completion state |

### CheckboxListBlock : BuildingBlock

`Type = BuildingBlockType.CheckboxList`

| Property | Type |
|---|---|
| `Items` | `List<CheckboxListItem>` |

---

### TableColumn

Standalone support type for TableBlock.

| Property | Type | Notes |
|---|---|---|
| `Header` | `List<TextSpan>` | Column header text |

### TableBlock : BuildingBlock

`Type = BuildingBlockType.Table`

| Property | Type | Notes |
|---|---|---|
| `Columns` | `List<TableColumn>` | Column definitions with headers |
| `Rows` | `List<List<List<TextSpan>>>` | Row → Cell → Spans |

---

### MusicalNotesBlock : BuildingBlock

`Type = BuildingBlockType.MusicalNotes`

| Property | Type | Notes |
|---|---|---|
| `Notes` | `List<string>` | Ordered chromatic note names (e.g., "C", "D#", "Bb") |

---

### ChordBeat

Standalone support type.

| Property | Type | Notes |
|---|---|---|
| `ChordId` | `Guid` | FK → Chord.Id (for lookup) |
| `DisplayName` | `string` | Stored display name for offline rendering |
| `Beats` | `int` | Number of beats this chord occupies in the measure |

### ChordMeasure

Standalone support type.

| Property | Type |
|---|---|
| `Chords` | `List<ChordBeat>` |

### ChordProgressionSection

Standalone support type.

| Property | Type | Notes |
|---|---|---|
| `Label` | `string` | Section label (e.g., "Verse", "Chorus") |
| `Repeat` | `int` | Number of times this section repeats |
| `Measures` | `List<ChordMeasure>` | |

### ChordProgressionBlock : BuildingBlock

`Type = BuildingBlockType.ChordProgression`

| Property | Type | Notes |
|---|---|---|
| `TimeSignature` | `string` | Time signature string (e.g., "4/4", "3/4", "6/8") |
| `Sections` | `List<ChordProgressionSection>` | |

---

### ChordTablatureGroupBlock : BuildingBlock

`Type = BuildingBlockType.ChordTablatureGroup`

| Property | Type | Notes |
|---|---|---|
| `Items` | `List<ChordTablatureItem>` | Ordered chord references |

### ChordTablatureItem

Standalone support type.

| Property | Type | Notes |
|---|---|---|
| `ChordId` | `Guid` | FK → Chord.Id |
| `Label` | `string` | Display label for this chord in the group |

---

## Static Constraint Classes (`DomainModels/Constants/`)

### ModuleTypeConstraints

Two `static readonly` dictionaries; both keyed by `ModuleType`.

**`AllowedBlocks`: `IReadOnlyDictionary<ModuleType, IReadOnlySet<BuildingBlockType>>`**

> Source: `STACCATO_FRONTEND_DOCUMENTATION.md` §5.4 (authoritative). Previous research-derived table has been superseded.

| ModuleType | Allowed BuildingBlockTypes |
|---|---|
| Title | Date, Text |
| Breadcrumb | *(empty set)* |
| Subtitle | Text |
| Theory | SectionHeading, Text, BulletList, NumberedList, Table, MusicalNotes |
| Practice | SectionHeading, Text, ChordProgression, ChordTablatureGroup, MusicalNotes |
| Example | SectionHeading, Text, ChordProgression, MusicalNotes |
| Important | SectionHeading, Text, MusicalNotes |
| Tip | SectionHeading, Text, MusicalNotes |
| Homework | SectionHeading, Text, BulletList, NumberedList, CheckboxList |
| Question | SectionHeading, Text |
| ChordTablature | ChordTablatureGroup, MusicalNotes |
| FreeText | *(all 10 BuildingBlockTypes)* |

**`MinimumSizes`: `IReadOnlyDictionary<ModuleType, (int MinWidth, int MinHeight)>`**

| ModuleType | MinWidth | MinHeight |
|---|---|---|
| Title | 20 | 4 |
| Breadcrumb | 20 | 3 |
| Subtitle | 10 | 3 |
| Theory | 8 | 5 |
| Practice | 8 | 5 |
| Example | 8 | 5 |
| Important | 8 | 4 |
| Tip | 8 | 4 |
| Homework | 8 | 5 |
| Question | 8 | 4 |
| ChordTablature | 8 | 10 |
| FreeText | 4 | 4 |

---

### PageSizeDimensions

One `static readonly` dictionary keyed by `PageSize`.

**`Dimensions`: `IReadOnlyDictionary<PageSize, (int WidthMm, int HeightMm, int GridWidth, int GridHeight)>`**

| PageSize | WidthMm | HeightMm | GridWidth | GridHeight |
|---|---|---|---|---|
| A4 | 210 | 297 | 42 | 59 |
| A5 | 148 | 210 | 29 | 42 |
| A6 | 105 | 148 | 21 | 29 |
| B5 | 176 | 250 | 35 | 50 |
| B6 | 125 | 176 | 25 | 35 |

> Grid units = floor(mm / 5). Verified: 210÷5=42 ✓, 297÷5=59.4→59 ✓,
> 148÷5=29.6→29 ✓, 210÷5=42 ✓, 105÷5=21 ✓, 148÷5=29.6→29 ✓,
> 176÷5=35.2→35 ✓, 250÷5=50 ✓, 125÷5=25 ✓, 176÷5=35.2→35 ✓.
