# Spec Review Checklist: User Profile Management

**Purpose**: Author self-review before opening PR — validates spec completeness, clarity, and consistency across all four feature domains (profile, deletion lifecycle, avatar, style presets)
**Created**: 2026-03-28
**Feature**: [spec.md](../spec.md) · [plan.md](../plan.md) · [contracts/api.md](../contracts/api.md)

---

## Requirement Completeness — Profile

- [ ] CHK001 — Are all fields returned by `GET /users/me` listed individually in FR-001, or does the spec rely on implicit inclusion? [Completeness, Spec §FR-001]
- [ ] CHK002 — Are the exact fields that `PUT /users/me` accepts listed exhaustively in FR-002, with no ambiguity about whether additional fields (e.g., email) may be submitted and silently ignored? [Completeness, Spec §FR-002]
- [ ] CHK003 — Is the behaviour of `PUT /users/me` when `defaultInstrumentId` is null explicitly covered — does null clear the preference or leave it unchanged? [Clarity, Spec §FR-002]
- [ ] CHK004 — Is the validation rule for `firstName` maximum length (100 chars) stated in the spec, or does it only appear in the plan/quickstart? [Completeness, Gap]
- [ ] CHK005 — Is `lastName` allowed to be an empty string (as the plan permits), and is this explicitly documented in the spec? [Clarity, Spec §FR-002]
- [ ] CHK006 — Does the spec define what `language` values are valid (`en`/`hu`) and where this is enforced (request validation vs. service layer)? [Completeness, Spec §FR-002]

---

## Requirement Completeness — Deletion Lifecycle

- [ ] CHK007 — Is the exact duration of the grace period ("30 days") defined in days, hours, or as a specific `DateTime.UtcNow.AddDays(30)` expression, or is it left open to interpretation? [Clarity, Spec §FR-005]
- [ ] CHK008 — Does the spec define whether "30 days" means calendar days, exactly 720 hours, or end-of-day on the 30th day? [Ambiguity, Spec §FR-005]
- [ ] CHK009 — Is the error code `ACCOUNT_DELETION_NOT_SCHEDULED` listed in the spec's canonical error code inventory (or CLAUDE.md), or does it exist only in the clarifications section? [Consistency, Gap]
- [ ] CHK010 — Is the error code `ACCOUNT_DELETION_ALREADY_SCHEDULED` confirmed to return 409 (as the plan documents) rather than 400 (as the original spec stated)? Is this deviation explicitly recorded? [Consistency, Conflict, Plan §Spec Alignment Decisions]
- [ ] CHK011 — Does the spec explicitly state that all 4 types of associated data (notebooks, lessons, lesson pages, modules) are cascade-deleted when a user is hard-deleted, or does it rely on "cascade" as an implicit assumption? [Completeness, Spec §FR-022]
- [ ] CHK012 — Is the behaviour specified when the account cleanup service encounters a DB commit failure (partial batch) — does it retry, skip, or abort the run? [Coverage, Spec §FR-023]

---

## Requirement Completeness — Avatar

- [ ] CHK013 — Is the file field name in the `multipart/form-data` request body specified (e.g., `file`)? Without this, client and server implementations may diverge. [Completeness, Gap]
- [ ] CHK014 — Does the spec define whether avatar format validation is performed on MIME type (Content-Type header), file extension, or magic bytes? Each has different spoofing implications. [Clarity, Gap]
- [ ] CHK015 — Is the maximum file size (2 MB) defined as 2,000,000 bytes (SI) or 2,097,152 bytes (binary)? [Ambiguity, Spec §FR-010]
- [ ] CHK016 — Is the behaviour specified when `DELETE /users/me/avatar` is called and the blob no longer exists in storage (e.g., manually removed)? Does the spec guarantee 204 in this case? [Edge Case, Spec §FR-013a]
- [ ] CHK017 — Does the spec define what `avatarUrl` looks like in `GET /users/me` after `DELETE /users/me/avatar` — specifically that it returns `null`, not an empty string? [Clarity, Spec §FR-013a]
- [ ] CHK018 — Are the allowed MIME types (`image/jpeg`, `image/png`, `image/webp`) the authoritative list, and is the spec clear that the backend validates MIME type, not file extension? [Completeness, Spec §FR-011]

---

## Requirement Completeness — Style Presets

- [ ] CHK019 — Does the spec define the canonical list of 12 `ModuleType` values that must be present in a preset? Or does it rely on an external enum definition without cross-referencing? [Completeness, Spec §FR-015]
- [ ] CHK020 — Is the maximum length of a preset `name` (100 chars) specified in the spec, or only in the plan/quickstart? [Completeness, Gap]
- [ ] CHK021 — Does the spec define whether `PUT /users/me/presets/{id}` requires at least one field (name or styles), or is an empty body a valid no-op? [Completeness, Spec §FR-017]
- [ ] CHK022 — Is the `stylesJson` field within each style entry defined as non-empty, or can it be an empty string/object? [Clarity, Spec §FR-015]
- [ ] CHK023 — Does the spec define the ordering of items in `GET /users/me/presets` responses (e.g., by creation date, alphabetical, or unspecified)? [Completeness, Gap]
- [ ] CHK024 — Is it specified whether a user-saved preset can share a name with a system style preset (Classic, Colorful, etc.) — or only with other user presets? [Clarity, Spec §FR-015a]

---

## Requirement Clarity

- [ ] CHK025 — Is "full user profile" (used in FR-001 and User Story 1) explicitly enumerated, or could a reader disagree about which fields constitute "full"? [Clarity, Spec §FR-001]
- [ ] CHK026 — Is "valid JWT authentication" (FR-021) specific enough — does the spec reference the token type (Bearer HS256) and claim used for identity (`ClaimTypes.NameIdentifier`)? [Clarity, Spec §FR-021]
- [ ] CHK027 — Is "normal conditions" (used in SC-003 for avatar upload success rate) defined or bounded in any way? [Ambiguity, Spec §SC-003]
- [ ] CHK028 — Does FR-013a define "idempotent" precisely enough — is it clear that repeated calls with no avatar set return 204 with no side effects, not 404? [Clarity, Spec §FR-013a]
- [ ] CHK029 — Is the phrase "hard-delete them along with all their data via cascade" (FR-022) specific enough for an implementer to know exactly which tables are affected, or should it reference the data model hierarchy? [Clarity, Spec §FR-022]

---

## Requirement Consistency

- [ ] CHK030 — Does the error code list in CLAUDE.md include all new codes introduced by this feature (`ACCOUNT_DELETION_NOT_SCHEDULED`, `DUPLICATE_PRESET_NAME`)? [Consistency, Gap]
- [ ] CHK031 — Is the HTTP status for `POST /users/me/cancel-deletion` consistently 204 across User Story 2 acceptance scenarios, FR-007, and contracts/api.md? [Consistency, Spec §FR-007]
- [ ] CHK032 — Does the spec consistently use `avatarUrl` (not `avatarBlobPath` — the original name before clarification Q1) across all sections and the contracts document? [Consistency, Clarification §Q1]
- [ ] CHK033 — Is the 409 status for `ACCOUNT_DELETION_ALREADY_SCHEDULED` reflected consistently across the spec's acceptance scenarios, FR-006, and contracts/api.md — or does the spec still say 400 in some places? [Consistency, Conflict, Plan §Spec Alignment]
- [ ] CHK034 — Do the acceptance scenarios for preset operations (User Story 4) align with the FR numbering for presets (FR-015 through FR-020) — are all business rules covered by at least one scenario? [Consistency, Spec §User Story 4]

---

## Scenario Coverage

- [ ] CHK035 — Is there an acceptance scenario covering `GET /users/me` when the requesting user's account has `scheduledDeletionAt` set (i.e., is the response shape defined for scheduled-for-deletion accounts)? [Coverage, Gap]
- [ ] CHK036 — Is there a requirement or scenario for what happens when `PUT /users/me` is called with a `defaultInstrumentId` belonging to a valid but non-guitar instrument (if instruments are multi-type)? [Coverage, Gap]
- [ ] CHK037 — Is there a scenario covering `PUT /users/me/presets/{id}` where only `name` is updated (styles omitted) — verifying partial update semantics are documented? [Coverage, Spec §FR-017]
- [ ] CHK038 — Is there a scenario covering `PUT /users/me/presets/{id}` where the user renames the preset to its *current* name — should this be accepted (no conflict) or rejected (duplicate)? [Edge Case, Spec §FR-015a]
- [ ] CHK039 — Does the spec cover what happens if `AccountDeletionCleanupService` runs while a user is mid-request (e.g., the user is in the middle of cancelling deletion when the cleanup fires)? [Coverage, Concurrency, Gap]
- [ ] CHK040 — Is there a requirement for what `GET /users/me/presets` returns if the user's account is scheduled for deletion — are presets still accessible during the grace period? [Coverage, Gap]

---

## Edge Case Coverage

- [ ] CHK041 — Is the behaviour defined when `PUT /users/me/avatar` is called with a valid image that happens to be exactly 2 MB (boundary value)? Is 2 MB allowed or rejected? [Edge Case, Spec §FR-010]
- [ ] CHK042 — Is there a requirement covering what happens when two concurrent `DELETE /users/me` calls arrive for the same user simultaneously? [Edge Case, Concurrency, Gap]
- [ ] CHK043 — Does the spec address the case where `AccountDeletionCleanupService` runs and a user's `AvatarUrl` contains a URL pointing to a different storage account or container than the configured one? [Edge Case, Gap]
- [ ] CHK044 — Is the behaviour specified when `POST /users/me/presets` is called with a `styles` array where `moduleType` values are valid enum names but with incorrect casing (e.g., `"title"` vs `"Title"`)? [Edge Case, Spec §FR-015]
- [ ] CHK045 — Does the spec define what happens when a user's `defaultInstrumentId` preference references an instrument that was seeded but later restricted-deleted — can this state occur, and how is it handled on `GET /users/me`? [Edge Case, Spec §FR-001, data-model.md]

---

## Dependencies & Assumptions

- [ ] CHK046 — Is the assumption that `Azure.Storage.Blobs` is already referenced in `Application.csproj` validated against the actual `.csproj` file, not just the DI registration code? [Assumption, Plan §Technical Context]
- [ ] CHK047 — Is the dependency on `PageSize` and `ModuleType` enums being defined in `DomainModels` (feature 002) documented with a hard prerequisite, not just an assumption? [Dependency, Spec §Dependencies]
- [ ] CHK048 — Is the assumption that instruments are always seeded before the service starts documented — what happens if `GET /users/me` is called with a `defaultInstrumentId` set but no instrument seed data exists? [Assumption, Spec §Assumptions]
- [ ] CHK049 — Is it documented whether `IUserSavedPresetRepository.GetByUserIdAsync` returns presets in insertion order, and whether the spec's ordering assumption (CHK023) relies on this? [Assumption, data-model.md]
- [ ] CHK050 — Does the plan document that `AccountDeletionCleanupService` is already registered in `AddBackgroundWorkers()` as a pre-existing hook — and that no new DI registration is needed for it? [Completeness, Plan §Modified Files]

---

## Notes

- Check items off as completed: `[x]`
- Add findings inline next to the item (e.g., `[x] CHK003 — confirmed: null clears preference, documented in FR-002`)
- Items marked `[Gap]` indicate a requirement that may be absent from the spec — update spec if the gap is real
- Items marked `[Conflict]` indicate an inconsistency — resolve before proceeding to `/speckit.tasks`
- Items marked `[Ambiguity]` indicate a requirement that needs clarification before implementation
- Run `/speckit.clarify` for any ambiguities surfaced here that materially affect implementation
