# Research: Domain Model Implementation

**Branch**: `002-domain-models` | **Date**: 2026-03-02

---

## Decision 1: Allowed BuildingBlockType Set per ModuleType

**Context**: Spec A-006 deferred exact per-ModuleType allowed-block mappings to this phase.
`STACCATO_FRONTEND_DOCUMENTATION.md` §5.4 is now present in the repository and is
the authoritative source. The semantic-grouping table derived in the original research
(without the frontend docs) has been **superseded** by the frontend documentation.

**Decision**: Exact mapping from `STACCATO_FRONTEND_DOCUMENTATION.md` §5.4 — see
`data-model.md → ModuleTypeConstraints.AllowedBlocks` for the full table.

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

**Key differences from the original research-derived table**:
- SectionHeading is NOT in Title or Subtitle (Title also loses SectionHeading).
- SectionHeading IS in the content group (Theory, Practice, Example, Important, Tip, Homework, Question).
- Important and Tip have MusicalNotes but NOT BulletList, NumberedList, CheckboxList.
- Theory and Homework have lists but NOT ChordProgression.
- Practice and Example have ChordProgression (and Practice also has ChordTablatureGroup).
- Question has only SectionHeading + Text.
- ChordTablature also permits MusicalNotes.
- FreeText = all 10 types (not just 7).
- MusicalNotes and ChordProgression are NOT in the same module type set.

**Rationale**: Frontend documentation is the single authoritative source per A-004.
The original research derivation was a best-effort inference made without the docs.

---

## Decision 2: CheckboxListItem Structure

**Context**: The original feature description defined CheckboxListBlock items as
"lists of TextSpans", identical to BulletListBlock. Clarification Q1 confirmed
that each item requires a persisted `IsChecked` boolean.

**Decision**: Introduce a named `CheckboxListItem` class in `DomainModels/BuildingBlocks/`
with two properties: `List<TextSpan> Spans` and `bool IsChecked`. CheckboxListBlock
holds `List<CheckboxListItem> Items`. This is a standalone top-level type.

**Rationale**: Named type is necessary because each item has two distinct fields
(text spans + checked state). Using an anonymous tuple or a pair would be
harder to serialize/deserialize and less readable.

**Alternatives considered**: Using `(List<TextSpan> Spans, bool IsChecked)` tuple
— rejected; not easily JSON-serialisable and has no canonical property names.

---

## Decision 3: ChordProgressionBlock Sub-Type Naming

**Context**: Clarification Q2 confirmed standalone top-level named classes.

**Decision**: Four standalone top-level classes in `DomainModels/BuildingBlocks/`:
- `ChordProgressionBlock` — the BuildingBlock itself
- `ChordProgressionSection` — label + repeat + ordered list of ChordMeasure
- `ChordMeasure` — ordered list of ChordBeat
- `ChordBeat` — chord identifier (Guid) + display name (string) + beat count (int)

**Rationale**: Standalone types are independently referenceable from services and
repositories without qualifying via the parent class. Consistent with project's
zero-inner-class convention.

---

## Decision 4: TableBlock Supporting Types

**Context**: FR-026 defines TableBlock with columns (each having a header as
`List<TextSpan>`) and rows (each row is `List<List<TextSpan>>`). A named column
type keeps the API clean and self-documenting.

**Decision**: Introduce `TableColumn` as a standalone top-level class with property
`List<TextSpan> Header`. TableBlock holds `List<TableColumn> Columns` and
`List<List<List<TextSpan>>> Rows` (row → cell → spans). No named Row or Cell
wrapper is created — rows are `List<List<TextSpan>>` directly — because rows
have no properties other than their cells, and cells have no properties other
than their spans.

**Rationale**: Named `TableColumn` avoids the ambiguity of "what does the first
list in a list-of-lists represent". The row/cell levels have no metadata, so
naming them adds files without adding clarity.

**Alternatives considered**: Named `TableRow` and `TableCell` wrappers — rejected;
both are single-property wrappers that add indirection without expressiveness.

---

## Decision 5: Module Has No Timestamps

**Context**: Clarification Q3 confirmed no `CreatedAt` or `UpdatedAt` on Module.

**Decision**: `Module` domain model has no timestamp properties. Parent `Lesson`
already has `UpdatedAt`; any content change in a module should update the lesson's
`UpdatedAt` at the service layer. No API endpoint returns a per-module timestamp.

**Rationale**: Matches CLAUDE.md's Module field list (GridX, GridY, GridWidth,
GridHeight, ContentJson — no timestamps). Avoids unnecessary DB columns.

---

## Decision 6: Abstract BuildingBlock Base and Type Discriminator

**Context**: FR-023 requires an abstract base with a type discriminator.
FR-034 forbids serialization framework attributes on domain models.

**Decision**: `BuildingBlock` is an `abstract` class with a single property:
`BuildingBlockType Type { get; }`. Each concrete subclass sets this in its
constructor via `Type = BuildingBlockType.X`. The property has no setter;
the value is established at construction time.

Serialization of the discriminator (e.g., as a `"type"` JSON field using
`JsonStringEnumConverter`) is configured at the Application layer, not in
DomainModels. DomainModels carries no `[JsonConverter]`, `[JsonPolymorphic]`,
or similar attributes.

**Rationale**: Keeps DomainModels framework-agnostic (FR-034). The `Type` property
allows runtime type identification without reflection, which services need for
validation (e.g., checking allowed blocks for a ModuleType).

**Alternatives considered**: Using a `[JsonDerivedType]` attribute hierarchy —
rejected; violates FR-034 (no serialization attributes). Using an enum property
with a setter — rejected; a writable discriminator allows invalid states.

---

## Decision 7: Namespace Conventions for DomainModels

**Decision**: Namespaces follow folder path per the constitution:

| Folder | Namespace |
|---|---|
| `DomainModels/Enums/` | `DomainModels.Enums` |
| `DomainModels/Models/` | `DomainModels.Models` |
| `DomainModels/BuildingBlocks/` | `DomainModels.BuildingBlocks` |
| `DomainModels/Constants/` | `DomainModels.Constants` |

File-scoped namespaces are used throughout (Principle IX).

---

## Decision 8: ModuleTypeConstraints and PageSizeDimensions — Static vs Instance

**Decision**: Both are `static` classes (not singletons, not injected). They
contain only `static readonly` dictionary fields. Consumers reference them
directly by class name: `ModuleTypeConstraints.AllowedBlocks[type]`.

**Rationale**: These are compile-time constants — there is no runtime variation,
no configuration, and no testing scenario where a different mapping is needed.
Making them injectable would add complexity without benefit. A single integration
test asserts correctness of every entry.

**Alternatives considered**: `IModuleTypeConstraints` interface injected via DI —
rejected; unnecessary abstraction for pure constant data. `readonly struct` record
values — rejected; the data is tabular (dictionary lookups), not value-type.
