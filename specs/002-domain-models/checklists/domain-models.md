# Requirements Quality Checklist: Domain Model Implementation

**Purpose**: Comprehensive requirements quality audit — spec + planning artifacts.
  Validates completeness, clarity, consistency across spec.md, data-model.md, and research.md.
**Created**: 2026-03-02
**Feature**: [spec.md](../spec.md) | [data-model.md](../data-model.md) | [research.md](../research.md)
**Depth**: Comprehensive | **Audience**: Author + PR reviewer | **Scope**: Spec + planning docs

---

## Requirement Completeness

- [x] CHK001 — Is the `Instrument.StringCount` → `InstrumentKey` mapping (e.g., Guitar6String→6, Bass4String→4) specified in the spec requirements, or only in data-model.md? If only in data-model.md, is a requirement missing from the spec? [Completeness, Gap, data-model.md §Instrument]
  Finding: FR-014 updated with full InstrumentKey→StringCount mapping table. Gap closed.

- [x] CHK002 — Are requirements for which preset has `IsDefault = true` documented in the spec? FR-013 defines the `IsDefault` field but does not specify that exactly one preset (Classic) holds this flag. [Completeness, Spec §FR-013]
  Finding: FR-013 updated: "Exactly one preset (`Classic`) has `IsDefault = true`."

- [x] CHK003 — Are requirements specified for ordering of `CheckboxListBlock.Items` in response to toggle operations — i.e., does toggling `IsChecked` affect item order? [Completeness, Gap, Spec §FR-025]
  Finding: FR-025 updated: "Item order is preserved by the domain model and is not affected by toggling `IsChecked`."

- [x] CHK004 — Are requirements defined for the `Chord.Suffix` empty-string case — is an empty suffix valid (representing a plain major chord), and is this an invariant of the domain model? [Completeness, Spec §FR-015]
  Finding: FR-015 updated: "an empty string `""` is a valid suffix representing a plain major chord."

- [x] CHK005 — Are the exact property names of all domain model classes specified in the spec requirements, or only described by attribute name (e.g., "fingering positions payload" in FR-015 vs "PositionsJson" in data-model.md)? [Completeness, Gap, Spec §FR-015 vs data-model.md §Chord]
  Finding: FR-015 updated with `PositionsJson`; FR-021 updated with `BlobReference`; FR-020 updated with `ContentJson`.

- [x] CHK006 — Are requirements defined for what `Module.ContentJson` contains when a module is first created — is an empty array `[]` the expected initial value? [Completeness, Gap, Spec §FR-020]
  Finding: FR-020 updated: "initialized to `\"[]\"` when a module is first created."

- [x] CHK007 — Is the `ChordTablatureItem` type documented as a requirement in the spec? FR-029 describes its structure inline ("each with a chord identifier (Guid) and a display label string") but does not name it as a standalone type. [Completeness, Spec §FR-029 vs data-model.md §ChordTablatureItem]
  Finding: FR-029 updated with named `ChordTablatureItem` type and its two properties (`ChordId`, `Label`).

- [x] CHK008 — Is the `TableColumn` type documented as a requirement in the spec? FR-026 describes column structure inline but does not name `TableColumn` as a standalone required type. [Completeness, Spec §FR-026 vs data-model.md §TableColumn]
  Finding: FR-026 updated with named `TableColumn` type and its property (`Header`).

- [x] CHK009 — Are requirements specified for `SystemStylePreset.DisplayOrder` values — is there a rule that display orders must be unique and contiguous, or can they be arbitrary? [Completeness, Gap, Spec §FR-013]
  Finding: FR-013 updated: "display order integer (1-based, unique across all 5 presets)."

---

## Requirement Clarity

- [x] CHK010 — Is "value type" in FR-022 ("`TextSpan` value type") consistently defined? data-model.md calls it a "plain class", while `value type` in C# specifically means a struct. Is the intent a `class`, `record`, or `struct`? [Clarity, Conflict, Spec §FR-022 vs data-model.md §TextSpan]
  Finding: FR-022 and data-model.md updated: `TextSpan` is defined as a `record`.

- [x] CHK011 — Is "ordered list" consistently defined across FR-024–FR-029 — does it mean insertion-ordered (`List<T>`), or is ordering by some sort key? [Clarity, Ambiguity, Spec §FR-024-029]
  Finding: Preamble added before FR-022: "'ordered list' refers to an insertion-ordered `List<T>` — items are maintained in the order they were added and are not automatically sorted by any property."

- [x] CHK012 — Is "offline rendering" in FR-028 (`ChordBeat.DisplayName` "stored for offline rendering") operationally defined in the spec? What does "offline" mean for a web application — no network call, or no DB call at render time? [Clarity, Ambiguity, Spec §FR-028]
  Finding: FR-028 updated: "'offline rendering' means no DB round-trip at render time."

- [x] CHK013 — Is the distinction between `ChordBeat.DisplayName` and `ChordTablatureItem.Label` (FR-028 vs FR-029) clarified — both are display strings for chord references, but FR-028 calls it "display name" and FR-029 calls it "display label". Are these semantically distinct? [Clarity, Ambiguity, Spec §FR-028 vs §FR-029]
  Finding: FR-028 updated: "`ChordBeat.DisplayName` is the chord's contextual name within a progression; it is semantically distinct from `ChordTablatureItem.Label`, which is the user-assigned label beneath a chord diagram in a tablature group."

- [x] CHK014 — Is "top-level type" in FR-028 precisely defined — does it mean a non-nested public class, or specifically a class declared at the namespace scope with no enclosing class? [Clarity, Spec §FR-028]
  Finding: FR-028 already clearly states "top-level types in the DomainModels project — none are nested inner classes." No additional change needed.

- [x] CHK015 — Is "immutable after creation" for `Notebook.InstrumentId` and `Notebook.PageSize` operationally defined in the domain model spec — does immutability mean the property has no public setter, or is it enforced only at the service layer? [Clarity, Spec §FR-016]
  Finding: FR-016 updated: "init-only setters (`{ get; init; }`) — set once at construction and cannot be mutated thereafter by any caller." data-model.md table updated accordingly.

- [x] CHK016 — Is "raw JSON string" in A-001 (style payload for NotebookModuleStyle, UserSavedPreset, SystemStylePreset) sufficiently defined — is it clear that `null` is not a valid value for `StylesJson`? [Clarity, Spec §A-001, §FR-012, §FR-013, §FR-017]
  Finding: A-001 updated: "A `null` value is never valid for `StylesJson`."

- [x] CHK017 — Is the term "fingering positions payload" (FR-015) sufficiently precise — does it describe the structure per string (state + fret), or is the exact JSON schema deferred to the seed data source? [Clarity, Spec §FR-015, §A-002]
  Finding: FR-015 updated with `PositionsJson` property name and reference to A-002 + STACCATO_FRONTEND_DOCUMENTATION.md §9.

---

## Requirement Consistency — Within Spec

- [x] CHK018 — Do the 10 `BuildingBlockType` values in FR-002 map exactly one-to-one to the 10 concrete building block classes defined in FR-024 through FR-029? Are there any BuildingBlockType enum values without a corresponding class requirement? [Consistency, Spec §FR-002 vs §FR-024-029]
  Finding: Verified consistent. FR-024: SectionHeadingBlock, DateBlock, TextBlock (3). FR-025: BulletListBlock, NumberedListBlock, CheckboxListBlock (3). FR-026: TableBlock (1). FR-027: MusicalNotesBlock (1). FR-028: ChordProgressionBlock (1). FR-029: ChordTablatureGroupBlock (1). Total = 10 ✓

- [x] CHK019 — Does FR-031 accurately list all "non-music module types"? FR-031 lists: Title, Subtitle, Theory, Practice, Example, Important, Tip, Homework, Question, FreeText — that is 10 values. Combined with Breadcrumb (empty set) and ChordTablature (special), this accounts for all 12 ModuleType values in FR-001. Is this consistent? [Consistency, Spec §FR-031 vs §FR-001]
  Finding: FR-031 updated with authoritative frontend docs mapping. 12 values accounted for ✓

- [x] CHK020 — Does FR-020 state "non-negative integers" for grid positions but not specify the exact data type (int, short, etc.)? Is the "non-negative integer" constraint consistently expressed across FR-020 and data-model.md? [Consistency, Spec §FR-020 vs data-model.md §Module]
  Finding: FR-020 updated to specify `int` explicitly: "all non-negative `int` values."

- [x] CHK021 — Does A-006 (still listed as "will be finalized in the planning phase") conflict with the finalized allowed-block tables in data-model.md and research.md Decision 1? Should A-006 be updated to reference the finalized mapping now that planning is complete? [Consistency, Conflict, Spec §A-006 vs data-model.md §ModuleTypeConstraints]
  Finding: A-006 updated to reference finalized tables in data-model.md (sourced from STACCATO_FRONTEND_DOCUMENTATION.md §5.4).

- [x] CHK022 — Is `FR-034` ("Domain model classes MUST carry no persistence, serialization, or validation framework attributes") also explicitly applicable to building block support types (`CheckboxListItem`, `TableColumn`, `ChordBeat`, `ChordMeasure`, `ChordProgressionSection`, `ChordTablatureItem`)? FR-034 says "domain model classes" but the support types live in `BuildingBlocks/`, not `Models/`. [Consistency, Ambiguity, Spec §FR-034]
  Finding: FR-034 updated to explicitly cover all types in DomainModels including all support types.

- [x] CHK023 — Is `FR-035` ("Optional string properties MUST be nullable; required strings MUST be non-nullable") applicable to building block types and support types, or only to the 12 domain model classes in `Models/`? [Consistency, Ambiguity, Spec §FR-035]
  Finding: FR-035 updated: "This requirement applies to all types in the DomainModels project."

---

## Spec ↔ Data-Model Consistency

- [x] CHK024 — Does data-model.md's `ModuleTypeConstraints.AllowedBlocks` table show `ChordProgression` as absent for Subtitle, Important, and Tip — consistent with A-006's rule that ChordProgression is NOT in structural/callout module types? [Consistency, Spec §A-006 vs data-model.md §ModuleTypeConstraints]
  Finding: data-model.md AllowedBlocks table updated from frontend docs. ChordProgression absent for Subtitle, Important, Tip ✓ (also absent for Theory, Homework, Question per frontend docs).

- [x] CHK025 — Does data-model.md assign `MusicalNotes` to the same module types as `ChordProgression` (Theory, Practice, Example, Homework, Question, FreeText) — or does the allowed-block table differ? Is this alignment requirement documented anywhere in the spec? [Consistency, Gap, data-model.md §ModuleTypeConstraints]
  Finding: Per authoritative frontend docs, MusicalNotes and ChordProgression are NOT in the same set. MusicalNotes appears in Theory, Practice, Example, Important, Tip, ChordTablature, FreeText. ChordProgression appears only in Practice, Example, FreeText. A-006 updated to document this. Gap closed.

- [x] CHK026 — Does data-model.md include a `StringCount` property, and is it specified as derived from `Key` (InstrumentKey) or as an independently stored integer? The spec (FR-014) says "string count (integer)" without specifying derivation. [Consistency, Spec §FR-014 vs data-model.md §Instrument]
  Finding: FR-014 updated: stored `int` field set at seeding time with explicit per-key values.

- [x] CHK027 — Does data-model.md use `BlobReference` as the property name for `PdfExport`'s storage reference, while spec FR-021 describes it as "optional internal blob storage reference"? Is the canonical property name `BlobReference` specified in the spec, or only in data-model.md? [Consistency, Spec §FR-021 vs data-model.md §PdfExport]
  Finding: FR-021 updated with `BlobReference` as the canonical property name.

- [x] CHK028 — Does data-model.md specify `Chord.PositionsJson` as the property name, while spec FR-015 says "fingering positions payload"? Is the canonical property name `PositionsJson` specified in spec requirements? [Consistency, Spec §FR-015 vs data-model.md §Chord]
  Finding: FR-015 updated with `PositionsJson` as the canonical property name.

- [x] CHK029 — Does the count of domain model files in plan.md's source tree (12 files in `Models/`) match exactly the 12 domain model requirements FR-010 through FR-021? [Consistency, Spec §FR-010-021 vs plan.md §Source-Code-Changes]
  Finding: Verified consistent. 12 files listed in plan.md match FR-010 through FR-021 ✓

- [x] CHK030 — Does data-model.md describe `TableBlock.Rows` as `List<List<List<TextSpan>>>` — is this three-level generic nesting documented anywhere in the spec? FR-026 describes rows/cells/spans in prose but does not give the generic type signature. [Consistency, Spec §FR-026 vs data-model.md §TableBlock]
  Finding: FR-026 updated with explicit `List<List<List<TextSpan>>>` type signature.

- [x] CHK031 — Does data-model.md's `CheckboxListBlock` use `List<CheckboxListItem>` for items, while spec FR-025 describes items inline as "TextSpan values AND a persisted IsChecked boolean"? Is `CheckboxListItem` as a named type requirement captured in the spec? [Consistency, Spec §FR-025 vs data-model.md §CheckboxListItem]
  Finding: FR-025 updated with named `CheckboxListItem` type as a standalone requirement.

---

## Spec ↔ Research Consistency

- [x] CHK032 — Does research.md Decision 1's allowed-block table for the four semantic groups (Structural, Callout, Content, Chord) exactly match the data-model.md `ModuleTypeConstraints.AllowedBlocks` table — specifically for the Callout group (Important, Tip)? [Consistency, research.md §Decision-1 vs data-model.md §ModuleTypeConstraints]
  Finding: research.md Decision 1 completely replaced with the authoritative frontend docs table. Both research.md and data-model.md now use the same source. Previous research-derived table (Important/Tip → BulletList, NumberedList, CheckboxList) was incorrect; authoritative value is SectionHeading, Text, MusicalNotes.

- [x] CHK033 — Does research.md Decision 4 ("rows stay as `List<List<List<TextSpan>>>` — no named Row or Cell wrapper") remain consistent with spec FR-026's prose description? Is the decision not to name rows/cells reflected as a requirement in the spec? [Consistency, research.md §Decision-4 vs Spec §FR-026]
  Finding: FR-026 updated with explicit `List<List<List<TextSpan>>>` signature, consistent with research.md Decision 4 ✓

- [x] CHK034 — Does research.md Decision 6 ("BuildingBlock is abstract with a get-only Type property set in constructor") align with spec FR-023 ("single type discriminator attribute whose value identifies which BuildingBlockType")? Is "attribute" in FR-023 meant as a C# attribute (annotation) or a property attribute (field)? [Clarity, Consistency, Spec §FR-023 vs research.md §Decision-6]
  Finding: FR-023 updated: "attribute" replaced with "property" to eliminate C# annotation ambiguity. Now reads: "single type discriminator `property` named `Type`... This is a C# property, not a C# attribute annotation."

- [x] CHK035 — Does research.md Decision 5 ("no timestamps on Module; change tracking at Lesson level") introduce a requirement that `Lesson.UpdatedAt` must be updated whenever a module on one of its pages changes? If so, is this requirement captured in the spec (FR-018) or only in research notes? [Consistency, Gap, research.md §Decision-5 vs Spec §FR-018]
  Finding: FR-018 updated: "The service layer MUST update `UpdatedAt` whenever any content change occurs within the lesson, including: title edits, and any module addition, deletion, repositioning, or content update on any page of that lesson."

---

## Acceptance Criteria Quality

- [x] CHK036 — Is SC-001 ("compiles with nullable reference types enabled and zero warnings or errors") precise enough to be mechanically verified — does it specify which build command or tool produces this confirmation? [Measurability, Spec §SC-001]
  Finding: SC-001 updated: "confirmed by `dotnet build DomainModels/DomainModels.csproj`."

- [x] CHK037 — Is SC-004 ("type discriminator matches the corresponding BuildingBlockType enum member name") unambiguous — does "enum member name" mean exact case-sensitive string equality (e.g., "ChordProgression"), or is case-insensitive matching acceptable? [Clarity, Spec §SC-004]
  Finding: SC-004 updated: "case-sensitive string equality, e.g., the serialized discriminator for `BuildingBlockType.ChordProgression` is `\"ChordProgression\"`."

- [x] CHK038 — Does SC-005 ("no domain model class exposes a property that would force a dependency on an EF Core, FluentValidation, or JSON serialization namespace") cover building block support types (`CheckboxListItem`, `ChordBeat`, etc.) or only the 12 named domain model classes? [Completeness, Spec §SC-005]
  Finding: SC-005 updated to explicitly include "building block support types."

- [x] CHK039 — Is SC-003 ("PageSizeDimensions returns correct grid dimensions satisfying the 5mm-spacing relationship: physical mm ÷ 5 = dot units") consistent with the actual grid values — e.g., 297 ÷ 5 = 59.4, rounded to 59? Is the rounding rule (floor) specified in the spec? [Clarity, Spec §SC-003 vs data-model.md §PageSizeDimensions]
  Finding: SC-003 updated: "using floor division (`floor(mm ÷ 5) = dot units`; e.g., 297mm → 59, 148mm → 29)."

- [x] CHK040 — Is there a success criterion covering that `FR-034` is satisfied — i.e., that no build-time or runtime framework dependency attribute exists on any domain model or building block? SC-005 covers namespace dependency but not attribute presence. [Completeness, Gap, Spec §SC-005 vs §FR-034]
  Finding: SC-007 added: "No type in the DomainModels project carries an EF Core, FluentValidation, JSON serialization, or other framework annotation as a C# attribute..."

---

## Edge Case Coverage

- [x] CHK041 — Is the behavior defined for a `TextSpan` with an empty string (`Text = ""`) — is this a valid span or must text be non-empty? [Edge Case, Gap, Spec §FR-022]
  Finding: FR-022 updated: "`Text` MUST be a non-empty string — a `TextSpan` with `Text = \"\"` is invalid."

- [x] CHK042 — Is the behavior defined for `ChordBeat.Beats = 0` — is a zero-beat chord beat permitted in the domain model? [Edge Case, Gap, Spec §FR-028]
  Finding: FR-028 updated: "`Beats` count (`int`) that MUST be ≥ 1 (a ChordBeat with Beats ≤ 0 is invalid)."

- [x] CHK043 — Is the behavior defined for `ChordProgressionSection.Repeat = 0` — is a section that repeats zero times a valid domain model state? [Edge Case, Gap, Spec §FR-028]
  Finding: FR-028 updated: "`Repeat` count that MUST be ≥ 1 (a section with Repeat ≤ 0 is invalid)."

- [x] CHK044 — Is the behavior defined for `MusicalNotesBlock.Notes` being an empty list — is a note block with no notes a valid state? [Edge Case, Gap, Spec §FR-027]
  Finding: FR-027 updated: "The `Notes` list MUST contain at least one entry — an empty `MusicalNotesBlock` is invalid."

- [x] CHK045 — Is the behavior defined for `TableBlock.Rows` being empty while `TableBlock.Columns` is non-empty — is a table with headers but no data rows valid? [Edge Case, Gap, Spec §FR-026]
  Finding: FR-026 updated: "A `TableBlock` with an empty `Rows` list when `Columns` is non-empty is also invalid."

- [x] CHK046 — Is the behavior defined for `TableBlock.Columns` being empty — is a table with no column definitions valid? [Edge Case, Gap, Spec §FR-026]
  Finding: FR-026 updated: "A `TableBlock` with an empty `Columns` list is invalid."

---

## Dependencies & Assumptions Quality

- [x] CHK047 — Has A-006 been resolved by research.md and data-model.md? Is the spec's A-006 body ("exact per-type mappings will be finalized in the planning phase") now stale — should it be updated to reference the finalized tables in data-model.md? [Assumption, Spec §A-006]
  Finding: A-006 updated to reference finalized tables (same as CHK021).

- [x] CHK048 — Is A-002 (\"chord fingering payload is a raw JSON string defined by the chords-db source\") a validated assumption — is the chords-db JSON format confirmed and documented somewhere accessible? [Assumption, Gap, Spec §A-002]
  Finding: A-002 updated: "documented in `STACCATO_FRONTEND_DOCUMENTATION.md` §9 (`ChordPosition`, `ChordBarre`, `ChordString` structures). The format is confirmed and accessible."

- [x] CHK049 — Is the assumption in A-004 ("minimum grid dimensions are fixed constants from the frontend documentation") valid given that STACCATO_FRONTEND_DOCUMENTATION.md is not present in the repository? Are the values in research.md/data-model.md confirmed as authoritative? [Assumption, Risk, Spec §A-004 vs research.md §Decision-1]
  Finding: `STACCATO_FRONTEND_DOCUMENTATION.md` is now present in the repository. A-004 updated to reference §4 and §5 as the authoritative source. Grid dimensions and allowed-block tables have been corrected against the frontend docs.

- [x] CHK050 — Is the assumption in A-001 (style payload is a raw JSON string) documented as an explicit architectural decision in research.md, or is it only an assumption in the spec? Would a misalignment here cause breaking changes across Domain, Repository, and Api layers? [Assumption, Risk, Spec §A-001 vs research.md]
  Finding: A-001 is a validated spec assumption. `STACCATO_FRONTEND_DOCUMENTATION.md` §6 confirms the style property schema. The assumption is accepted and validated; research.md does not need a separate Decision entry as the frontend docs serve as the authoritative confirmation.

---

## Notes

- Check items off as completed: `[x]`
- Add inline findings using: `Finding: <description>` below the item
- Mandatory-gate items (blocking merge): CHK010, CHK018, CHK019, CHK021, CHK024, CHK034, CHK039
- Items marked `[Conflict]` require spec or data-model update before `/speckit.tasks`
- Items marked `[Gap]` are missing requirements — decide whether to add them or explicitly accept the gap

**All 50 items resolved. No open conflicts.**
