# Requirements Quality Checklist: Chord Library API

**Purpose**: Author self-review — validate that API contract, seeder/migration, and data model requirements are complete, clear, and consistent before opening a PR
**Created**: 2026-03-28
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md) | [data-model.md](../data-model.md) | [contracts/endpoints.md](../contracts/endpoints.md)
**Scope**: All three domains — API contracts · seeder/migration · data model schema
**Audience**: Author, pre-PR

---

## API Contract Completeness

- [ ] CHK001 — Are HTTP status codes documented for every scenario on `GET /instruments` (200, and the absence of 401 for unauthenticated callers)? [Completeness, Spec §User Story 3]
- [ ] CHK002 — Is response caching scope explicitly specified for all three endpoints — not just `GET /chords`? (`GET /chords/{id}` has it per plan; `GET /instruments` does not — is the omission intentional and stated?) [Completeness, plan.md §G2]
- [ ] CHK003 — Are all fields of `ChordSummaryResponse` documented with their data types and nullable status (especially `extension` and `alternation`, which are `null` when absent — not omitted)? [Completeness, data-model.md §Response DTOs]
- [ ] CHK004 — Are all fields of `ChordDetailResponse` and nested types (`ChordPositionResponse`, `ChordBarreResponse`, `ChordStringResponse`) documented with types and nullability? [Completeness, data-model.md §Response DTOs]
- [ ] CHK005 — Is the error response body format for a missing `instrument` param (400 via FluentValidation) specified and distinguished from an unknown instrument key (400 via model binding)? Both return 400 but through different mechanisms. [Completeness, Spec §FR-003/FR-004, contracts/endpoints.md]
- [ ] CHK006 — Is the `INSTRUMENT_NOT_FOUND` error (404) trigger condition — valid `InstrumentKey` enum value, but no matching row in the database — clearly distinguished from the model-binding 400 in the spec? [Completeness, Spec §FR-003/FR-004, contracts/endpoints.md]

---

## API Contract Clarity

- [ ] CHK007 — Is `previewPosition` selection criteria unambiguous? "First stored position" could mean insertion order, minimum `baseFret`, or index 0 in the JSON array. Does the spec make this unambiguous? [Clarity, Spec §FR-005, Assumptions]
- [ ] CHK008 — Is `instrumentKey` serialization format specified? The response uses `"Guitar6String"` (PascalCase enum name) — is this explicitly stated, or could an implementer choose a different serialization (e.g., camelCase `"guitar6String"`, or numeric)? [Clarity, data-model.md §Response DTOs]
- [ ] CHK009 — Is the `root` filter case-insensitive behavior specified with a concrete example that disambiguates case normalization (e.g., "f" matches "F", "f#" matches "F#")? [Clarity, Spec §FR-006]
- [ ] CHK010 — Is the `quality` filter case-insensitive behavior specified the same way? [Clarity, Spec §FR-006]
- [ ] CHK011 — Does the spec state whether `GET /chords` result ordering applies before or after filtering? (i.e., is the sorted set the filtered subset, or is a globally ordered set filtered?) [Clarity, Spec §FR-006a]
- [ ] CHK012 — Is it clear what `Chord.name` returns for extended chords? ("A maj7" vs "A Maj7" — is capitalisation of the quality component specified?) [Clarity, research.md §D1]
- [ ] CHK013 — Is the `barre: null` contract explicit — does the spec define exactly when barre is absent (i.e., no barre on this position) vs the field being omitted from the JSON? [Clarity, contracts/endpoints.md]
- [ ] CHK014 — Are the `fret` and `finger` nullability rules for each `ChordString.state` value explicitly documented? (null when state is `open` or `muted`; required when `fretted`) [Clarity, Spec §Key Entities, contracts/endpoints.md]

---

## API Contract Consistency

- [x] CHK015 — Are `quality` and `suffix` field semantics in `ChordSummaryResponse` clearly distinguished? Both currently carry the same value (e.g., "major") — does the spec explain why both fields exist and when they would differ? [Consistency/Ambiguity, Spec §Key Entities, data-model.md]
  - **Resolved**: `suffix` no longer exists. The schema uses `quality` (one of 13 named chord types), `extension` (optional symbolic extension, e.g. "add9"; null for most chords), and `alternation` (optional chromatic alteration, e.g. "#9"; null for all seeded chords). All three are documented in Spec §Key Entities, Spec §Clarifications, data-model.md §Entity Changes, and contracts/endpoints.md.
- [ ] CHK016 — Is the `string` field name in `ChordStringResponse` documented to match the JSON key name `"string"` (a C# reserved keyword)? Is the `[JsonPropertyName("string")]` requirement stated in the spec or data model? [Consistency, data-model.md §Response DTOs]
- [ ] CHK017 — Is `ChordBarre.fromString` / `toString` field directionality (1 = highest-pitched string) consistent between the spec, the frontend documentation, and the contracts file? [Consistency, contracts/endpoints.md, Spec §Key Entities]

---

## Seeder & Migration Requirements Completeness

- [ ] CHK018 — Is the embedded resource identifier (assembly-qualified name, e.g., `"Persistence.Data.guitar_chords.json"`) specified as a requirement, or is it left as an implementation detail that could be named differently? [Completeness, Spec §FR-009, research.md §D2]
- [ ] CHK019 — Is BOM handling for `guitar_chords.json` documented as a requirement? The file has a UTF-8 BOM; the spec/plan should state that the reader must strip it. [Completeness, research.md §Risk Register]
- [ ] CHK020 — Is the required execution order between `InstrumentSeeder` and `ChordSeeder` documented as a functional requirement? (Chords require the instrument row to exist.) [Completeness, Spec §FR-009]
- [ ] CHK021 — Is the natural key for chord deduplication in differential seeding fully specified with all four fields `(InstrumentId, Root, Quality, Suffix)`? Are all four needed, or could `(InstrumentId, Root, Quality)` be sufficient? [Completeness/Clarity, Spec §FR-009]
- [ ] CHK022 — Is the `InstrumentSeeder`'s migration to differential seeding (per-record) documented as a requirement in the spec? Currently the spec's FR-009 only describes the chord seeder. [Completeness, Spec §FR-009, plan.md §B4]
- [ ] CHK023 — Is the `ChordEntity.Name` semantic change (from root note letter to full display name) and the `Suffix` column removal both documented with explicit migration requirements? The migration `RestructureChordSchema` covers both. Is the order of steps (add, populate, drop) correctly specified? [Completeness, research.md §D1, data-model.md §EF Core Migration]
- [ ] CHK024 — Are the database migration steps specified in a safe dependency order — specifically: add columns with default, populate data, drop default, add index? [Completeness, data-model.md §EF Core Migration]
- [ ] CHK025 — Is it specified what happens at startup if the embedded resource name is wrong or the stream returns null? (The spec covers the missing-file case for the old file-path approach, but not the embedded-resource equivalent.) [Coverage/Gap, Spec §Edge Cases]

---

## Data Model Schema Requirements

- [ ] CHK026 — Are the max-length constraints for `Root` (50), `Quality` (50), `Extension` (50), and `Alternation` (50) justified? Do the actual values fit within these bounds? (Longest quality value is "Half-Diminished" = 15 chars; longest root is "Bb"/"F#" = 2 chars; longest extension in seed data is "add9" = 4 chars.) [Completeness, data-model.md §Entity Changes]
- [ ] CHK027 — Is the removal of `PositionsJson` from the `Chord` domain model documented with a note that callers previously accessing `Chord.PositionsJson` must migrate to `Chord.Positions`? [Completeness, data-model.md §Domain Model Changes]
- [ ] CHK028 — Is the `ChordString.StringNumber` C# property name to JSON `string` key name mapping documented as an explicit requirement? [Clarity, data-model.md §New Domain Models]
- [ ] CHK029 — Is `ChordStringState` enum serialization format (lowercase strings `"open"`, `"fretted"`, `"muted"` in JSON) specified as a requirement? Or could an implementer serialize as PascalCase `"Open"` without violating the spec? [Clarity, data-model.md §Response DTOs]
- [ ] CHK030 — Is the `ChordRepository.GetByIdAsync` override requirement (base class does not include navigation properties) documented? Without it, `Chord.InstrumentKey` would be unset in chord-detail responses. [Completeness, research.md §D4]
- [ ] CHK031 — Is it specified whether `ChordPosition.Strings` must contain exactly N entries — one per string on the instrument — or whether partial string arrays are valid? [Completeness/Clarity, Spec §Key Entities]
- [ ] CHK032 — Are valid range constraints for `ChordPosition.BaseFret` (e.g., minimum 1, no specified maximum) documented? [Completeness, data-model.md §New Domain Models]
- [ ] CHK033 — Is it specified whether a deserialization failure of `PositionsJson` at runtime (malformed stored JSON) surfaces as a 500 or is silently handled? [Coverage/Gap, Spec §Edge Cases]

---

## Scenario Coverage

- [ ] CHK034 — Is a zero-chord result scenario specified for `GET /chords?instrument=Guitar7String` (valid instrument key, but no chords seeded for it)? The spec covers zero results from filters but not from an entirely unseeded instrument. [Coverage, Spec §Edge Cases, Spec §Assumptions]
- [ ] CHK035 — Is the concurrent startup scenario addressed — what happens if two application instances run `DbInitializer` simultaneously and both attempt differential inserts for the same chord? [Coverage/Gap]
- [ ] CHK036 — Are requirements defined for the `GET /chords/{id}` scenario where the ID is a valid UUID format but not a Guid (e.g., all zeros)? Is that a 404 or a 400? [Clarity/Coverage, Spec §FR-008]

---

## Notes

- Check items off as completed: `[x]`
- Add findings inline, e.g. `[x] CHK007 — Confirmed: Spec §Assumptions para 5 states "first element (index 0)"`
- Items marked `[Gap]` indicate requirements that appear absent — if intentionally out of scope, note the decision
- Items in **Seeder & Migration** are highest risk for this feature; resolve those before API contract items if time-constrained
