# Spec Review Checklist: User Profile Management

**Purpose**: Author self-review before opening PR — validates spec completeness, clarity, and consistency across all four feature domains (profile, deletion lifecycle, avatar, style presets)
**Created**: 2026-03-28
**Feature**: [spec.md](../spec.md) · [plan.md](../plan.md) · [contracts/api.md](../contracts/api.md)

---

## Requirement Completeness — Profile

- [x] CHK001 — Are all fields returned by `GET /users/me` listed individually in FR-001, or does the spec rely on implicit inclusion? [Completeness, Spec §FR-001]
  > **Resolved**: FR-001 updated to list all 9 fields explicitly: `id`, `email`, `firstName`, `lastName`, `language`, `defaultPageSize`, `defaultInstrumentId`, `avatarUrl`, `scheduledDeletionAt`.
- [x] CHK002 — Are the exact fields that `PUT /users/me` accepts listed exhaustively in FR-002, with no ambiguity about whether additional fields (e.g., email) may be submitted and silently ignored? [Completeness, Spec §FR-002]
  > **Resolved**: FR-002 updated to list all five fields (`firstName`, `lastName`, `language`, `defaultPageSize`, `defaultInstrumentId`) with "All five fields are required on every call."
- [x] CHK003 — Is the behaviour of `PUT /users/me` when `defaultInstrumentId` is null explicitly covered — does null clear the preference or leave it unchanged? [Clarity, Spec §FR-002]
  > **Resolved**: FR-002 explicitly states "Sending `null` for `defaultPageSize` or `defaultInstrumentId` clears the preference (sets the value to null in storage)." Confirmed in Clarifications.
- [x] CHK004 — Is the validation rule for `firstName` maximum length (100 chars) stated in the spec, or does it only appear in the plan/quickstart? [Completeness, Gap]
  > **Resolved**: FR-002 updated to state "`firstName` MUST be non-empty and at most 100 characters." Also documented in Assumptions.
- [x] CHK005 — Is `lastName` allowed to be an empty string (as the plan permits), and is this explicitly documented in the spec? [Clarity, Spec §FR-002]
  > **Resolved**: FR-002 updated to state "`lastName` MAY be an empty string and MUST be at most 100 characters." Also documented in Assumptions.
- [x] CHK006 — Does the spec define what `language` values are valid (`en`/`hu`) and where this is enforced (request validation vs. service layer)? [Completeness, Spec §FR-002]
  > **Resolved**: Assumptions section states "The `preferredLanguage` field accepts only the two supported locales: `en` and `hu`." Enforcement at request validation is established by the validator in `contracts/api.md`.

---

## Requirement Completeness — Deletion Lifecycle

- [x] CHK007 — Is the exact duration of the grace period ("30 days") defined in days, hours, or as a specific `DateTime.UtcNow.AddDays(30)` expression, or is it left open to interpretation? [Clarity, Spec §FR-005]
  > **Resolved**: FR-005 specifies "`DateTime.UtcNow.AddDays(30)` (exactly 30 × 24 hours from the moment of the request)."
- [x] CHK008 — Does the spec define whether "30 days" means calendar days, exactly 720 hours, or end-of-day on the 30th day? [Ambiguity, Spec §FR-005]
  > **Resolved**: FR-005 states "exactly 30 × 24 hours from the moment of the request" — unambiguous UTC duration.
- [x] CHK009 — Is the error code `ACCOUNT_DELETION_NOT_SCHEDULED` listed in the spec's canonical error code inventory (or CLAUDE.md), or does it exist only in the clarifications section? [Consistency, Gap]
  > **Resolved**: `ACCOUNT_DELETION_NOT_SCHEDULED`, `DUPLICATE_PRESET_NAME`, and `INSTRUMENT_NOT_FOUND` added to the canonical error code list in `CLAUDE.md`.
- [x] CHK010 — Is the error code `ACCOUNT_DELETION_ALREADY_SCHEDULED` confirmed to return 409 (as the plan documents) rather than 400 (as the original spec stated)? Is this deviation explicitly recorded? [Consistency, Conflict, Plan §Spec Alignment Decisions]
  > **Resolved**: FR-006, US2 scenario 2, and `contracts/api.md` all consistently use 409. Deviation recorded in Clarifications ("Status code for ACCOUNT_DELETION_ALREADY_SCHEDULED? → A: 409 Conflict"). `ACCOUNT_DELETION_ALREADY_SCHEDULED` is already in `CLAUDE.md`.
- [x] CHK011 — Does the spec explicitly state that all 4 types of associated data (notebooks, lessons, lesson pages, modules) are cascade-deleted when a user is hard-deleted, or does it rely on "cascade" as an implicit assumption? [Completeness, Spec §FR-022]
  > **Resolved**: FR-022 updated to enumerate entity types: "Notebooks, Lessons, LessonPages, and Modules via EF Core cascade delete."
- [x] CHK012 — Is the behaviour specified when the account cleanup service encounters a DB commit failure (partial batch) — does it retry, skip, or abort the run? [Coverage, Spec §FR-023]
  > **Resolved**: FR-023 updated to state that a commit failure is logged and the batch is retried on the next scheduled run (accounts retain their ScheduledDeletionAt and are re-selected).

---

## Requirement Completeness — Avatar

- [x] CHK013 — Is the file field name in the `multipart/form-data` request body specified (e.g., `file`)? Without this, client and server implementations may diverge. [Completeness, Gap]
  > **Resolved**: FR-009 states "with the file in a field named `file`." Confirmed in Clarifications and `contracts/api.md`.
- [x] CHK014 — Does the spec define whether avatar format validation is performed on MIME type (Content-Type header), file extension, or magic bytes? Each has different spoofing implications. [Clarity, Gap]
  > **Resolved**: `contracts/api.md` specifies "Allowed MIME types: `image/jpeg`, `image/png`, `image/webp`" and the implementation validates `file.ContentType`. MIME-type validation is established at the contracts layer.
- [x] CHK015 — Is the maximum file size (2 MB) defined as 2,000,000 bytes (SI) or 2,097,152 bytes (binary)? [Ambiguity, Spec §FR-010]
  > **Resolved**: FR-010 explicitly states "2,097,152 bytes (2 MiB)" and Clarifications confirm this is the binary definition.
- [x] CHK016 — Is the behaviour specified when `DELETE /users/me/avatar` is called and the blob no longer exists in storage (e.g., manually removed)? Does the spec guarantee 204 in this case? [Edge Case, Spec §FR-013a]
  > **Resolved**: FR-013a updated to state "If `avatarUrl` is set in the database but the blob no longer exists in storage, the blob deletion MUST be treated as a no-op (idempotent delete) and `avatarUrl` MUST still be cleared."
- [x] CHK017 — Does the spec define what `avatarUrl` looks like in `GET /users/me` after `DELETE /users/me/avatar` — specifically that it returns `null`, not an empty string? [Clarity, Spec §FR-013a]
  > **Resolved**: FR-013a states "`avatarBlobPath` MUST be set to null" (naming is stale but intent is clear). `contracts/api.md` confirms `avatarUrl` is `null` in the response shape.
- [x] CHK018 — Are the allowed MIME types (`image/jpeg`, `image/png`, `image/webp`) the authoritative list, and is the spec clear that the backend validates MIME type, not file extension? [Completeness, Spec §FR-011]
  > **Resolved**: FR-011 updated to state "whose MIME type (Content-Type) is not one of `image/jpeg`, `image/png`, or `image/webp`" and "Format validation is performed on the MIME type, not the file extension."

---

## Requirement Completeness — Style Presets

- [x] CHK019 — Does the spec define the canonical list of 12 `ModuleType` values that must be present in a preset? Or does it rely on an external enum definition without cross-referencing? [Completeness, Spec §FR-015]
  > **Resolved**: The spec cross-references the `ModuleType` enum via the Dependencies section ("feature 002: ModuleType enum must be defined in DomainModels"). The full list of 12 values is enumerated in `contracts/api.md` (Title, Breadcrumb, Text, BulletList, NumberedList, CheckboxList, Table, MusicalNotes, ChordProgression, ChordTablatureGroup, Date, SectionHeading).
- [x] CHK020 — Is the maximum length of a preset `name` (100 chars) specified in the spec, or only in the plan/quickstart? [Completeness, Gap]
  > **Resolved**: Assumptions section states "a maximum length of 100 characters is assumed as a reasonable default." Also in `contracts/api.md` ("max 100 chars").
- [x] CHK021 — Does the spec define whether `PUT /users/me/presets/{id}` requires at least one field (name or styles), or is an empty body a valid no-op? [Completeness, Spec §FR-017]
  > **Resolved**: FR-017 states "At least one of `name` or `styles` MUST be present in the request body; a request containing neither MUST return 400." Confirmed in Clarifications.
- [x] CHK022 — Is the `stylesJson` field within each style entry defined as non-empty, or can it be an empty string/object? [Clarity, Spec §FR-015]
  > **Resolved**: FR-016 updated to state "Each style entry's `stylesJson` field MUST be a non-empty string; an empty string MUST return 400."
- [x] CHK023 — Does the spec define the ordering of items in `GET /users/me/presets` responses (e.g., by creation date, alphabetical, or unspecified)? [Completeness, Gap]
  > **Resolved**: FR-014 states "Presets MUST be ordered alphabetically by name ascending." Confirmed in Clarifications.
- [x] CHK024 — Is it specified whether a user-saved preset can share a name with a system style preset (Classic, Colorful, etc.) — or only with other user presets? [Clarity, Spec §FR-015a]
  > **Resolved**: FR-015a states "User preset names MAY match system preset names (Classic, Colorful, Dark, Minimal, Pastel) — the two are separate namespaces." Confirmed in Clarifications.

---

## Requirement Clarity

- [x] CHK025 — Is "full user profile" (used in FR-001 and User Story 1) explicitly enumerated, or could a reader disagree about which fields constitute "full"? [Clarity, Spec §FR-001]
  > **Resolved**: FR-001 and US1 scenario 1 now enumerate all 9 fields by their correct names (`id`, `email`, `firstName`, `lastName`, `language`, `defaultPageSize`, `defaultInstrumentId`, `avatarUrl`, `scheduledDeletionAt`). No ambiguity remains.
- [x] CHK026 — Is "valid JWT authentication" (FR-021) specific enough — does the spec reference the token type (Bearer HS256) and claim used for identity (`ClaimTypes.NameIdentifier`)? [Clarity, Spec §FR-021]
  > **Resolved**: The spec is intentionally technology-agnostic. Implementation specifics (Bearer HS256, `ClaimTypes.NameIdentifier`) are documented in `plan.md` and `CLAUDE.md` as appropriate for the planning layer.
- [x] CHK027 — Is "normal conditions" (used in SC-003 for avatar upload success rate) defined or bounded in any way? [Ambiguity, Spec §SC-003]
  > **Resolved**: SC-003 updated to "when blob storage is available and the network is not degraded" — replaces the undefined "normal conditions" with a concrete, bounded qualifier.
- [x] CHK028 — Does FR-013a define "idempotent" precisely enough — is it clear that repeated calls with no avatar set return 204 with no side effects, not 404? [Clarity, Spec §FR-013a]
  > **Resolved**: FR-013a states "Calling this endpoint when no avatar is set MUST return 204 (idempotent)" — 204 is explicitly required, not 404.
- [x] CHK029 — Is the phrase "hard-delete them along with all their data via cascade" (FR-022) specific enough for an implementer to know exactly which tables are affected, or should it reference the data model hierarchy? [Clarity, Spec §FR-022]
  > **Resolved**: Same fix as CHK011 — FR-022 now enumerates "Notebooks, Lessons, LessonPages, and Modules via EF Core cascade delete."

---

## Requirement Consistency

- [x] CHK030 — Does the error code list in CLAUDE.md include all new codes introduced by this feature (`ACCOUNT_DELETION_NOT_SCHEDULED`, `DUPLICATE_PRESET_NAME`)? [Consistency, Gap]
  > **Resolved**: `ACCOUNT_DELETION_NOT_SCHEDULED`, `DUPLICATE_PRESET_NAME`, and `INSTRUMENT_NOT_FOUND` added to `CLAUDE.md` error code list. All four feature codes now present.
- [x] CHK031 — Is the HTTP status for `POST /users/me/cancel-deletion` consistently 204 across User Story 2 acceptance scenarios, FR-007, and contracts/api.md? [Consistency, Spec §FR-007]
  > **Resolved**: US2 scenario 3, FR-007, and `contracts/api.md` all specify 204 for successful cancel-deletion.
- [x] CHK032 — Does the spec consistently use `avatarUrl` (not `avatarBlobPath` — the original name before clarification Q1) across all sections and the contracts document? [Consistency, Clarification §Q1]
  > **Resolved**: FR-001, FR-013a, US3 independent test, and US3 scenario 6 all updated to use `avatarUrl`. Spec and `contracts/api.md` are now consistent.
- [x] CHK033 — Is the 409 status for `ACCOUNT_DELETION_ALREADY_SCHEDULED` reflected consistently across the spec's acceptance scenarios, FR-006, and contracts/api.md — or does the spec still say 400 in some places? [Consistency, Conflict, Plan §Spec Alignment]
  > **Resolved**: 409 is consistent in US2 scenario 2, FR-006, and `contracts/api.md`. No 400 remains for this code.
- [x] CHK034 — Do the acceptance scenarios for preset operations (User Story 4) align with the FR numbering for presets (FR-015 through FR-020) — are all business rules covered by at least one scenario? [Consistency, Spec §User Story 4]
  > **Resolved**: US4 scenarios 6 and 7 added to cover FR-015a (409 DUPLICATE_PRESET_NAME on create) and FR-020 (404 for non-existent preset id). All FRs now have at least one acceptance scenario.

---

## Scenario Coverage

- [x] CHK035 — Is there an acceptance scenario covering `GET /users/me` when the requesting user's account has `scheduledDeletionAt` set (i.e., is the response shape defined for scheduled-for-deletion accounts)? [Coverage, Gap]
  > **Resolved**: `scheduledDeletionAt` is part of the `UserResponse` shape in both `contracts/api.md` and FR-001. The Edge Cases section clarifies that profile updates (and by extension GET) remain available during the grace period.
- [x] CHK036 — Is there a requirement or scenario for what happens when `PUT /users/me` is called with a `defaultInstrumentId` belonging to a valid but non-guitar instrument (if instruments are multi-type)? [Coverage, Gap]
  > **Resolved**: The spec does not restrict `defaultInstrumentId` to guitar instruments. FR-003 validates only existence, not instrument type — any seeded instrument ID is valid.
- [x] CHK037 — Is there a scenario covering `PUT /users/me/presets/{id}` where only `name` is updated (styles omitted) — verifying partial update semantics are documented? [Coverage, Spec §FR-017]
  > **Resolved**: FR-017 explicitly states "update the name or style definitions" (either or both), and "At least one of `name` or `styles` MUST be present." Name-only update is a valid, documented case.
- [x] CHK038 — Is there a scenario covering `PUT /users/me/presets/{id}` where the user renames the preset to its *current* name — should this be accepted (no conflict) or rejected (duplicate)? [Edge Case, Spec §FR-017]
  > **Resolved**: FR-017 states "Renaming a preset to its own current name MUST be accepted (no conflict — returns 200)." Confirmed in Clarifications.
- [x] CHK039 — Does the spec cover what happens if `AccountDeletionCleanupService` runs while a user is mid-request (e.g., the user is in the middle of cancelling deletion when the cleanup fires)? [Coverage, Concurrency, Gap]
  > **Resolved**: Edge Cases section updated to document this race condition: the last writer wins at the DB level; if cleanup commits first, the cancel-deletion attempt will fail with a not-found error returned as 401 (token valid but user gone).
- [x] CHK040 — Is there a requirement for what `GET /users/me/presets` returns if the user's account is scheduled for deletion — are presets still accessible during the grace period? [Coverage, Gap]
  > **Resolved**: The Edge Cases section states "Updates are permitted during the grace period — only hard deletion is deferred. Active JWT sessions also remain fully valid throughout the grace period." All endpoints including preset endpoints remain accessible.

---

## Edge Case Coverage

- [x] CHK041 — Is the behaviour defined when `PUT /users/me/avatar` is called with a valid image that happens to be exactly 2 MB (boundary value)? Is 2 MB allowed or rejected? [Edge Case, Spec §FR-010]
  > **Resolved**: FR-010 states "Files of exactly 2,097,152 bytes MUST be accepted" — boundary value is explicitly allowed.
- [x] CHK042 — Is there a requirement covering what happens when two concurrent `DELETE /users/me` calls arrive for the same user simultaneously? [Edge Case, Concurrency, Gap]
  > **Resolved**: Edge Cases section updated to document concurrent `DELETE /users/me` behavior: the first call succeeds (204); the second finds `ScheduledDeletionAt` already set and returns 409 (ACCOUNT_DELETION_ALREADY_SCHEDULED).
- [x] CHK043 — Does the spec address the case where `AccountDeletionCleanupService` runs and a user's `AvatarUrl` contains a URL pointing to a different storage account or container than the configured one? [Edge Case, Gap]
  > **Resolved (by design)**: Blob paths are always `avatars/{userId}` in the configured container; this state cannot occur through the normal API. The cleanup service uses the deterministic blob path, not the stored URL, for deletion — no spec change required.
- [x] CHK044 — Is the behaviour specified when `POST /users/me/presets` is called with a `styles` array where `moduleType` values are valid enum names but with incorrect casing (e.g., `"title"` vs `"Title"`)? [Edge Case, Spec §FR-015]
  > **Resolved**: FR-016 states "`moduleType` values are matched case-insensitively against the `ModuleType` enum." Confirmed in Clarifications.
- [x] CHK045 — Does the spec define what happens when a user's `defaultInstrumentId` preference references an instrument that was seeded but later restricted-deleted — can this state occur, and how is it handled on `GET /users/me`? [Edge Case, Spec §FR-001, data-model.md]
  > **Resolved**: `CLAUDE.md` specifies `DeleteBehavior.Restrict` for Instrument entities — they cannot be deleted after seeding. This state cannot occur.

---

## Dependencies & Assumptions

- [x] CHK046 — Is the assumption that `Azure.Storage.Blobs` is already referenced in `Application.csproj` validated against the actual `.csproj` file, not just the DI registration code? [Assumption, Plan §Technical Context]
  > **Resolved**: `Application.csproj` confirmed to contain `<PackageReference Include="Azure.Storage.Blobs" Version="12.27.0" />`.
- [x] CHK047 — Is the dependency on `PageSize` and `ModuleType` enums being defined in `DomainModels` (feature 002) documented with a hard prerequisite, not just an assumption? [Dependency, Spec §Dependencies]
  > **Resolved**: Dependencies section states "Domain models (feature 002): PageSize enum and ModuleType enum must be defined in DomainModels" as an explicit prerequisite.
- [x] CHK048 — Is the assumption that instruments are always seeded before the service starts documented — what happens if `GET /users/me` is called with a `defaultInstrumentId` set but no instrument seed data exists? [Assumption, Spec §Assumptions]
  > **Resolved**: Assumptions section updated: "Instrument seed data is a hard prerequisite: if instruments are not seeded before the service starts, any `defaultInstrumentId` set on a user record will fail FK validation. The `defaultInstrumentId` field will be null for all users until instruments are seeded."
- [x] CHK049 — Is it documented whether `IUserSavedPresetRepository.GetByUserIdAsync` returns presets in insertion order, and whether the spec's ordering assumption (CHK023) relies on this? [Assumption, data-model.md]
  > **Resolved**: FR-014 explicitly requires alphabetical ordering by name ascending — the spec does not rely on insertion order from the repository. The repository implementation will apply `ORDER BY Name`.
- [x] CHK050 — Does the plan document that `AccountDeletionCleanupService` is already registered in `AddBackgroundWorkers()` as a pre-existing hook — and that no new DI registration is needed for it? [Completeness, Plan §Modified Files]
  > **Resolved**: `quickstart.md` Step 11 states "Already registered in `AddBackgroundWorkers()` — no DI change needed." Confirmed in `tasks.md` T049 note and verified in `Application/Extensions/ServiceCollectionExtensions.cs` line 173.

---

## Notes

- Check items off as completed: `[x]`
- Add findings inline next to the item (e.g., `[x] CHK003 — confirmed: null clears preference, documented in FR-002`)
- Items marked `[Gap]` indicate a requirement that may be absent from the spec — update spec if the gap is real
- Items marked `[Conflict]` indicate an inconsistency — resolve before proceeding to `/speckit.tasks`
- Items marked `[Ambiguity]` indicate a requirement that needs clarification before implementation
- Run `/speckit.clarify` for any ambiguities surfaced here that materially affect implementation
