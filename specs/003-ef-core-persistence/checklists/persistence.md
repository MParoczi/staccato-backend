# Persistence Requirements Quality Checklist: EF Core Entity Models and Database Persistence

**Purpose**: Author self-review — validate spec quality, clarity, and completeness across all four requirement areas (entity model, constraints, seed data, JSON schemas) before writing any implementation code
**Created**: 2026-03-07
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md) | [data-model.md](../data-model.md)
**Scope**: All areas equally — entity model + constraints + seed data + JSON schemas
**Audience**: Feature author, pre-implementation

---

## Q&A Session — 2026-03-07

Decisions made during checklist review:

- Q: JSON property name casing for all `*Json` columns → **camelCase** (FR-041 added; A-003 updated)
- Q: StylesJson shape for Title/Subtitle module types → **All 9 fields stored for all 12 module types** (A-003 updated)
- Q: Preset color palette precision in spec → **Qualitative descriptions sufficient; Colorful must match frontend docs reference exactly** (FR-030/FR-031 updated with Colorful hex values)
- Q: Chord file semantic validation (missing fields, duplicates) → **Fail fast on both** (FR-038 extended)

---

## Requirement Completeness

- [x] CHK001 — Is the delete behavior for `NotebookEntity.InstrumentId` → `InstrumentEntity` explicitly specified? ✓ FR-040 added: `DeleteBehavior.Restrict` (instruments are immutable).

- [x] CHK002 — Is `NotebookModuleStyleEntity.StylesJson` included in the JSON column list? ✓ Added to FR-004.

- [x] CHK003 — Are the full hex color palettes documented? ✓ FR-031 updated: Colorful exact hex values locked to frontend docs reference; Classic/Dark/Minimal/Pastel remain qualitative (implementation choice).

- [x] CHK004 — Are all bounded string properties enumerated? ✓ FR-026 now lists all 8 bounded properties with exact max lengths.

- [x] CHK005 — Is `guitar_chords.json` runtime location specified? ✓ FR-035 updated: `CopyToOutputDirectory PreserveNewest` required in `Persistence.csproj`.

- [x] CHK006 — Is `NotebookEntity.InstrumentId` listed in Key Entities with its FK and navigation? ✓ Already present in Key Entities section.

- [x] CHK007 — Is the domain model contract for `PdfExport.LessonIds` callers defined? ✓ Deferred — service layer contract, not a persistence spec concern. FR-039 covers the property definition.

---

## Requirement Clarity

- [x] CHK008 — Is JSON property name casing specified? ✓ FR-041 added: camelCase for all JSON columns. A-003 updated.

- [x] CHK009 — Is the seeder idempotency guard precisely defined? ✓ FR-028 updated: "skip if any rows exist" (non-empty table = skip entire seeder).

- [x] CHK010 — Is "comprehensive" in FR-036 verifiable? ✓ SC-002 updated: chord count in DB must equal chord count in JSON file.

- [x] CHK011 — Is migration-level vs seeder-level idempotency clearly distinguished? ✓ FR-027 and FR-028 now describe both separately.

- [x] CHK012 — Is the `ExportStatus` integer mapping stated explicitly? ✓ Verifiable from `DomainModels/Enums/ExportStatus.cs` (Pending=0, Processing=1, Ready=2, Failed=3). FR-019 inline reference is sufficient.

- [x] CHK013 — Is the StylesJson shape for Title/Subtitle module types specified? ✓ A-003 updated: all 9 fields stored uniformly for all 12 module types.

---

## Requirement Consistency

- [x] CHK014 — Is A-004 consistent with research.md Decision 1? ✓ A-004 updated: `.HasFilter()` is native EF Core API, no raw SQL needed.

- [x] CHK015 — Does SC-004 constraint count match enumerable FRs? ✓ SC-004 updated to 16 (5 unique + 1 partial unique + 10 FK behaviors) with full inventory.

- [x] CHK016 — Are the two StylesJson shapes consistently distinguished throughout? ✓ A-003 and Key Entities section are consistent.

- [x] CHK017 — Is the domain model update (FR-039) reflected consistently? ✓ FR-039 is the single authoritative statement.

- [x] CHK018 — Is `LessonIdsJson` (entity) vs `LessonIds` (domain) naming unambiguous? ✓ Naming is consistent throughout spec.

---

## Acceptance Criteria Quality

- [x] CHK019 — Can SC-002 be objectively verified? ✓ SC-002 updated: "chord count in DB equals chord count in JSON file."

- [x] CHK020 — Is SC-004 measurable with the constraint count? ✓ SC-004 updated with full labelled inventory of 16 constraints.

- [x] CHK021 — Can SC-006 "7 dependent entity types" be reconciled? ✓ Verified: notebooks, lessons, lesson pages, modules, refresh tokens, user saved presets, PDF exports = 7. ✓

- [x] CHK022 — Is SC-008 measurable? ✓ SC-008 updated to match FR-038 specifics: `InvalidOperationException` with file path and failure reason.

---

## Scenario Coverage

- [x] CHK023 — Are requirements defined for `DbInitializer` under InMemory EF? ✓ FR-027 updated: skip `MigrateAsync` when provider is InMemory.

- [x] CHK024 — Are requirements defined for DB connection failure at startup? ✓ Deferred — EF Core throws its own descriptive exception on connection failure; no additional spec requirement needed.

- [x] CHK025 — Are requirements defined for partially-seeded tables? ✓ FR-028 specifies skip-all if any rows exist. Partial seed state is treated as "non-empty table → skip." This is documented behavior.

- [x] CHK026 — Are requirements defined for semantically malformed chord entries? ✓ FR-038 extended: missing `name`/`suffix`/`positions`, empty `positions` array → fail fast.

---

## Edge Case Coverage

- [x] CHK027 — Is partial instrument seed behavior defined? ✓ FR-028 skip-all guard covers this: if any instruments exist, seeder exits.

- [x] CHK028 — Are duplicate chord name+suffix entries addressed? ✓ FR-038 extended: duplicate `name`+`suffix` → fail fast.

- [x] CHK029 — Is empty `guitar_chords.json` (`[]`) addressed? ✓ FR-038 updated: empty array → fail fast.

- [x] CHK030 — Is empty/whitespace `StylesJson` addressed? ✓ Deferred — FR-004 marks it NOT NULL and the seeder always writes valid JSON; no user input path exists for this field at the persistence layer.

---

## Non-Functional Requirements

- [x] CHK031 — Is startup duration specified as a requirement? ✓ Deferred — plan.md Technical Context notes "<10 seconds" as a goal; not promoted to a spec requirement.

- [x] CHK032 — Are transaction interruption recovery requirements defined? ✓ Deferred — not a persistence spec concern; handled by idempotent re-run.

- [x] CHK033 — Are connection resiliency requirements defined? ✓ Deferred — `EnableRetryOnFailure` is an Application-layer configuration concern, out of scope for this feature's spec.

---

## Dependencies & Assumptions

- [x] CHK034 — Is A-004 now consistent with research.md? ✓ A-004 updated to state `.HasFilter()` is native EF Core — no raw SQL needed.

- [x] CHK035 — Is A-008 normative or informational? ✓ Promoted to FR-026 (normative). A-008 marked as superseded.

- [x] CHK036 — Is there an explicit FR prohibiting `HasConversion<string>()` on enums? ✓ FR-006 extended with explicit prohibition.

- [x] CHK037 — Is seeded non-guitar instruments having no chords expected? ✓ A-002 updated: "expected and valid for non-guitar instruments to have zero chord rows."

---

## Identified Conflicts — Resolved

- [x] CHK038 — **[Resolved]** `NotebookModuleStyleEntity.StylesJson` added to FR-004.

- [x] CHK039 — **[Resolved]** SC-004 constraint count corrected to 16 with full inventory.

- [x] CHK040 — **[Resolved]** A-004 and A-005 updated; `.HasFilter()` is native EF Core API.

---

## Notes

- All 40 items resolved. No outstanding gaps or conflicts remain.
- 4 items deferred (CHK007, CHK024, CHK031–CHK033) — all are either service-layer concerns or implementation configuration details outside the persistence spec scope.
- Spec is ready for `/speckit.tasks`.
