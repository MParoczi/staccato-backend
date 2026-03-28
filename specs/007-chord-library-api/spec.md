# Feature Specification: Chord Library API

**Feature Branch**: `007-chord-library-api`
**Created**: 2026-03-28
**Status**: Draft
**Input**: User description: "Implement the chord library API and instrument management for Staccato."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse Chords by Instrument and Root (Priority: P1)

A frontend chord selector needs to display available chords for a given instrument. The user
opens the chord picker, selects an instrument (e.g. 6-string guitar), optionally filters by
root note (e.g. "F") and quality (e.g. "major"), and receives a list of matching chord
summaries — each including a preview fretboard position ready for thumbnail rendering.

**Why this priority**: This is the primary read path. All chord diagram features in the
frontend (ChordProgression, ChordTablatureGroup building blocks) depend on being able to
search and select chords. Without it, chord-related modules cannot be authored.

**Independent Test**: Can be fully tested by issuing `GET /chords?instrument=Guitar6String`,
`GET /chords?instrument=Guitar6String&root=F`, and
`GET /chords?instrument=Guitar6String&root=F&quality=major`, and verifying that each returns
a correctly-shaped array of chord summaries.

**Acceptance Scenarios**:

1. **Given** seeded chord data exists, **When** `GET /chords?instrument=Guitar6String` is
   called, **Then** the response is `200` with an array of `ChordSummary` objects, each
   containing `id`, `instrumentKey`, `name`, `root`, `quality`, `suffix`, and a
   `previewPosition` matching the first chord position.

2. **Given** seeded chord data exists, **When**
   `GET /chords?instrument=Guitar6String&root=F&quality=major` is called, **Then** only
   chord summaries whose `root` equals `"F"` and `quality` equals `"major"` are returned.

3. **Given** the request omits `instrument`, **When** `GET /chords` is called without the
   required query parameter, **Then** the response is `400` with a validation error.

4. **Given** the request specifies an unknown `instrument` key, **When**
   `GET /chords?instrument=Theremin` is called, **Then** the response is `400` with a
   validation error.

5. **Given** root and quality filters are provided that match no seeded chords, **When** the
   filtered endpoint is called, **Then** the response is `200` with an empty array.

---

### User Story 2 - Retrieve Full Chord Detail for Rendering (Priority: P1)

When the frontend renders a fretboard diagram for a chord the user has added to a lesson,
it needs the full chord data: all positions with complete string states, barre, and finger
assignments. The frontend calls `GET /chords/{id}` to get this data.

**Why this priority**: P1 alongside story 1 — the detail endpoint is required whenever a
lesson page containing a ChordTablatureGroup or ChordProgression is displayed. Without it,
chord diagrams cannot be rendered.

**Independent Test**: Can be fully tested by seeding one chord, retrieving its ID from the
list endpoint, calling `GET /chords/{id}`, and verifying the full `ChordDetail` shape
including all positions, barre data, and strings arrays.

**Acceptance Scenarios**:

1. **Given** a chord exists with the given `id`, **When** `GET /chords/{id}` is called,
   **Then** the response is `200` with a `ChordDetail` containing all positions, each with
   `label`, `baseFret`, `barre` (or `null`), and `strings` (one entry per string with
   `string`, `state`, `fret`, and `finger` fields).

2. **Given** no chord exists with the given `id`, **When** `GET /chords/{id}` is called,
   **Then** the response is `404`.

---

### User Story 3 - Retrieve Available Instruments (Priority: P2)

When a user creates a new notebook, the frontend must display a list of available instruments
to choose from. The instruments endpoint provides the full list of seeded instrument records.

**Why this priority**: Required for notebook creation, but independent from chord library
browsing — a user can browse chords without creating a notebook.

**Independent Test**: Can be fully tested by calling `GET /instruments` without any
authentication and verifying the response matches all seeded instrument records, each
containing `id`, `key`, `name`, and `stringCount`.

**Acceptance Scenarios**:

1. **Given** instruments are seeded, **When** `GET /instruments` is called, **Then** the
   response is `200` with an array of all instrument records, each containing `id`, `key`,
   `name`, and `stringCount`.

2. **Given** no authentication header is provided, **When** `GET /instruments` is called,
   **Then** the response is still `200` (endpoint is public, no authentication required).

---

### Edge Cases

- What happens when `GET /chords?instrument=Guitar6String&root=F` is called but no chords
  match the root filter? → `200` with an empty array.
- What happens when `GET /chords/{id}` is called with a well-formed UUID that does not
  match any seeded chord? → `404`.
- What happens when the `root` filter is provided as a lowercase value (e.g. `f` instead
  of `F`)? → Case-insensitive matching; both return the same results.
- What happens when the `quality` filter value does not match any seeded quality string?
  → `200` with an empty array (valid but empty filter result, not an error).
- What happens when the guitar_chords.json embedded resource is missing at startup? →
  Application startup fails with a clear error during `DbInitializer` execution.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST expose a public `GET /instruments` endpoint that returns all
  seeded instrument records with `id`, `key`, `name`, and `stringCount`.

- **FR-002**: The system MUST expose a public `GET /chords` endpoint that accepts a required
  `instrument` query parameter (matching an `InstrumentKey` enum value), and optional `root`
  and `quality` query parameters for filtering.

- **FR-003**: The system MUST return `400` when `GET /chords` is called without the
  `instrument` query parameter.

- **FR-004**: The system MUST return `400` when the `instrument` query parameter does not
  match a known `InstrumentKey` enum value.

- **FR-005**: Each chord summary in the `GET /chords` response MUST include a
  `previewPosition` populated from the first stored position for that chord.

- **FR-006**: `root` and `quality` filtering on `GET /chords` MUST be case-insensitive.

- **FR-006a**: Results from `GET /chords` MUST be ordered by `Root` ascending, then by
  `Quality` ascending — grouping all voicings of the same root note together.

- **FR-007**: The system MUST expose a public `GET /chords/{id}` endpoint that returns the
  full chord detail including all positions with complete `label`, `baseFret`, `barre`, and
  `strings` data.

- **FR-008**: `GET /chords/{id}` MUST return `404` when no chord with the given `id` exists.

- **FR-009**: The chord data MUST be seeded from an embedded JSON file
  (`Persistence/Data/guitar_chords.json`) during `DbInitializer` execution, read via
  `Assembly.GetManifestResourceStream` so the file is not required on disk at runtime.
  Seeding MUST be differential and additive: the seeder compares the JSON against existing
  database records and inserts only entries that are present in the JSON but absent from the
  database. Existing records are never modified or deleted; their `Guid` IDs remain stable
  across restarts and re-deploys. The natural key for `Instrument` deduplication is the
  `Key` enum value; the natural key for `Chord` deduplication is
  `(InstrumentId, Root, Quality, Extension)` where `null` Extension is treated as `""`
  for comparison.

- **FR-010**: The chord entity MUST store `Root`, `Quality`, `Extension`, and `Alternation`
  as dedicated columns (not buried in JSON) to support efficient server-side filtering and
  structured chord identity. `Suffix` is not stored — it has been superseded by this schema.

- **FR-011**: The chord entity configuration MUST define a composite index on
  `(InstrumentId, Root, Quality)` to support fast filtered list queries.

- **FR-012**: Both `GET /chords` and `GET /instruments` MUST be accessible without
  authentication.

- **FR-013**: Position data MUST be stored as a JSON array in a `NVARCHAR(MAX)` column and
  deserialised when building responses — not stored in relational rows.

- **FR-014**: The `Chord → Instrument` relationship MUST use restrict-on-delete behaviour;
  chord and instrument records must never be deleted after seeding.

### Key Entities

- **Instrument**: A seeded, immutable instrument type. Has a stable enum key (e.g.
  `Guitar6String`), a display name (e.g. "6-String Guitar"), and a string count. Never
  created or modified by users.

- **Chord**: A seeded, read-only chord record. Belongs to one `Instrument`. Has a `Name`
  (full display name, e.g. "F", "Am", "Gm7b5", "Gsus2"), a `Root` note (e.g. "F"), a
  `Quality` — one of 13 named qualities: `Major`, `Major 7`, `Sixth`, `Minor`, `Minor 7`,
  `Minor major 7`, `Seventh`, `Diminished`, `Half-Diminished`, `Diminished 7th`,
  `Suspended 4th`, `Suspended 2nd`, `Augmented` — an optional `Extension` (e.g. `"add9"`
  for chords where an extension is added beyond the base quality; null otherwise), an
  optional `Alternation` (e.g. `"#9"`, `"b5"` for chromatic alterations; null for all
  currently seeded chords), and a JSON array of `ChordPosition` values.

- **ChordPosition**: A single voicing of a chord, embedded in the chord's position JSON.
  Has a `Label` (e.g. "Barre 1st position"), a `BaseFret` (1 = nut position), an optional
  `Barre` descriptor (fret number, fromString, toString), and a `Strings` array — one entry
  per string — with state (`open`, `fretted`, or `muted`), fret number, and finger number.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Chord list queries filtered by instrument, root, and quality return only
  matching results with 100% accuracy — no false positives, no missing matches.

- **SC-002**: A developer can build and seed the database from scratch in one step, with all
  guitar chords immediately queryable after `DbInitializer` completes — zero manual data
  entry required.

- **SC-003**: Frontend chord diagrams can be rendered for every seeded chord — every
  `ChordPosition` in the API response provides complete, unambiguous rendering data with no
  missing fields required for display.

- **SC-004**: Both chord and instrument endpoints respond to unauthenticated requests —
  any client (including unauthenticated browsers) can retrieve chord and instrument data.

- **SC-005**: Filtered chord queries against the full seeded library are perceptibly instant
  for end users — the composite index ensures filtering does not degrade as the chord dataset
  grows.

## Clarifications

### Session 2026-03-28

- Q: When `DbInitializer` runs on a database that already has chord/instrument records, what should it do? → A: Differential additive seeding — compare the JSON against the database and insert only records that exist in the JSON but are not yet in the database. Existing records are never modified or deleted; their `Guid` IDs remain stable. New entries added to the JSON in future releases are automatically picked up on the next startup.
- Q: In what order should `GET /chords` results be returned? → A: By `Root` then `Quality` — groups all voicings of the same root note together, which aligns with the chord picker UI's root-first filtering flow.
- Q: How should the chord data model distinguish quality, extensions, and alterations — and what format should `guitar_chords.json` use? → A: The `Suffix` field is dropped entirely. The schema uses three semantic fields: `Quality` (one of 13 named qualities capturing the complete chord type), `Extension` (optional numeric/symbolic extension beyond the base quality, e.g. `"add9"`; null for most chords), and `Alternation` (optional chromatic alteration, e.g. `"#9"`, `"b5"`; null for all currently seeded chords). The JSON file stores all five fields explicitly: `name` (full display name), `root`, `quality`, `extension`, `alternation`. Example: `Gadd9` → quality `"Major"`, extension `"add9"`, alternation null. Example with alternation (future): `C7(#9)` → quality `"Seventh"`, extension null, alternation `"#9"`. The 12 seeded chord types map to this schema: major→(Major, null, null), minor→(Minor, null, null), 7→(Seventh, null, null), maj7→(Major 7, null, null), add9→(Major, add9, null), m7→(Minor 7, null, null), m7b5→(Half-Diminished, null, null), dim→(Diminished, null, null), dim7→(Diminished 7th, null, null), aug→(Augmented, null, null), sus2→(Suspended 2nd, null, null), sus4→(Suspended 4th, null, null).

## Assumptions

- The `guitar_chords.json` file is sourced from the `chords-db` open-source dataset and
  contains all standard guitar chord voicings for a 6-string guitar.
- Chords for other instruments (7-string guitar, bass, ukulele, banjo) are out of scope for
  this feature; only `Guitar6String` chords are seeded.
- The `InstrumentKey` enum already exists in `DomainModels` and includes `Guitar6String`;
  all other instrument keys are seeded as instrument records but have no associated chords
  until future features add them.
- No pagination is required for the chord list — the filtered result set is always returned
  in full. The dataset is bounded and small enough that pagination would add complexity
  without meaningful benefit.
- The `previewPosition` in `ChordSummary` is always the first element (index 0) of the
  chord's positions array; if a chord has only one position, that same object is used for
  both preview and full detail.
- Case-insensitive filtering applies to both `root` and `quality` parameters; the seed data
  stores values in canonical capitalisation (e.g. "F#", "major") but the API tolerates
  any casing from callers.
