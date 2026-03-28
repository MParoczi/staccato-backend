# Feature Specification: User Profile Management

**Feature Branch**: `006-user-profile-management`
**Created**: 2026-03-28
**Status**: Draft
**Input**: User description: "Implement user profile management, account soft deletion with 30-day grace period, avatar upload, and user-saved style presets for the Staccato API."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View and Update Profile (Priority: P1)

A logged-in user wants to view their current profile information and update personal preferences such as their display name, preferred language, default notebook page size, and default instrument. This is the foundational profile feature all other stories depend on.

**Why this priority**: Profile data is the backbone of personalisation. Without it, no other user-facing customisation is meaningful. It is the most frequently accessed user endpoint.

**Independent Test**: Can be fully tested by authenticating a user, calling GET /users/me to verify profile data, then calling PUT /users/me to modify preferences, and confirming changes persist on a subsequent GET.

**Acceptance Scenarios**:

1. **Given** an authenticated user, **When** they call GET /users/me, **Then** the response returns their full profile including id, email, firstName, lastName, language, defaultPageSize, defaultInstrumentId, avatarUrl, and scheduledDeletionAt (null if not scheduled).
2. **Given** an authenticated user, **When** they call PUT /users/me with valid values for firstName, lastName, language, defaultPageSize, and defaultInstrumentId, **Then** the response returns 200 with the updated profile.
3. **Given** an authenticated user, **When** they call PUT /users/me with an invalid defaultPageSize value or with any of the five required fields missing, **Then** the response returns 400 with field-level validation errors.
4. **Given** an authenticated user, **When** they call PUT /users/me with a defaultInstrumentId that does not exist, **Then** the response returns 404 with error code INSTRUMENT_NOT_FOUND.
5. **Given** an unauthenticated request, **When** either endpoint is called, **Then** the response returns 401.

---

### User Story 2 - Schedule and Cancel Account Deletion (Priority: P1)

A user who wishes to leave the platform can schedule their account for deletion. The system delays the actual deletion by 30 days to allow recovery. A user who changed their mind can cancel the deletion during the grace period.

**Why this priority**: Account lifecycle management is a security and compliance requirement. Soft deletion prevents accidental permanent data loss and supports user trust.

**Independent Test**: Can be fully tested by calling DELETE /users/me, verifying ScheduledDeletionAt is set 30 days in the future, then calling POST /users/me/cancel-deletion to clear it, and confirming ScheduledDeletionAt is null again.

**Acceptance Scenarios**:

1. **Given** an authenticated user with no scheduled deletion, **When** they call DELETE /users/me, **Then** the response returns 204 and their account's ScheduledDeletionAt is set to exactly 30 days from now (UTC).
2. **Given** an authenticated user with a scheduled deletion, **When** they call DELETE /users/me again, **Then** the response returns 409 with error code ACCOUNT_DELETION_ALREADY_SCHEDULED.
3. **Given** an authenticated user with a scheduled deletion, **When** they call POST /users/me/cancel-deletion, **Then** the response returns 204 and ScheduledDeletionAt is cleared to null.
4. **Given** an authenticated user with no scheduled deletion, **When** they call POST /users/me/cancel-deletion, **Then** the response returns 400 with error code ACCOUNT_DELETION_NOT_SCHEDULED.
5. **Given** an unauthenticated request, **When** either endpoint is called, **Then** the response returns 401.

---

### User Story 3 - Upload Avatar (Priority: P2)

A user wants to personalise their profile with a photo or avatar image. The system accepts a file upload, validates its format and size, stores it securely, and associates it with the user's profile.

**Why this priority**: Avatar upload is a secondary personalisation feature. It enhances user identity but does not block core functionality.

**Independent Test**: Can be fully tested by uploading a valid image via PUT /users/me/avatar, confirming the response returns 200 and the user's profile now includes an updated avatarUrl.

**Acceptance Scenarios**:

1. **Given** an authenticated user, **When** they upload a valid JPG, PNG, or WebP image under 2MB via PUT /users/me/avatar, **Then** the response returns 200, the image is stored, and the user's profile reflects the new avatar.
2. **Given** an authenticated user with an existing avatar, **When** they upload a new valid image, **Then** the old avatar is replaced and the previous blob is removed from storage.
3. **Given** an authenticated user, **When** they upload a file exceeding 2MB, **Then** the response returns 400 with a validation error indicating the size limit.
4. **Given** an authenticated user, **When** they upload a file with an unsupported format (e.g., GIF, BMP, PDF), **Then** the response returns 400 with a validation error indicating the allowed formats.
5. **Given** an authenticated user, **When** the upload request does not contain a file, **Then** the response returns 400 with a validation error.
6. **Given** an authenticated user with an existing avatar, **When** they call `DELETE /users/me/avatar`, **Then** the response returns 204, the blob is removed from storage, and `GET /users/me` returns null for `avatarUrl`.
7. **Given** an authenticated user with no avatar, **When** they call `DELETE /users/me/avatar`, **Then** the response returns 204 (idempotent — no error).
8. **Given** an unauthenticated request, **When** any avatar endpoint is called, **Then** the response returns 401.

---

### User Story 4 - Manage Style Presets (Priority: P2)

A user wants to save named collections of module styling configurations so they can quickly apply consistent visual themes when creating or editing notebooks. They can create, view, update, and delete their saved presets.

**Why this priority**: Style presets are a power-user feature that improves efficiency for users managing multiple notebooks. They have no dependencies on other stories beyond the profile system.

**Independent Test**: Can be fully tested by creating a preset via POST /users/me/presets, retrieving it via GET /users/me/presets, updating its name via PUT /users/me/presets/{id}, and deleting it via DELETE /users/me/presets/{id}.

**Acceptance Scenarios**:

1. **Given** an authenticated user, **When** they call GET /users/me/presets, **Then** the response returns 200 with an array of all their saved presets (empty array if none).
2. **Given** an authenticated user, **When** they call POST /users/me/presets with a valid name and exactly 12 module-type style definitions (one per ModuleType), **Then** the response returns 201 with the created preset.
3. **Given** an authenticated user, **When** they call POST /users/me/presets with fewer than 12 style definitions or duplicate module types, **Then** the response returns 400 with validation errors.
4. **Given** an authenticated user, **When** they call PUT /users/me/presets/{id} with an updated name or style definitions, **Then** the response returns 200 with the updated preset.
5. **Given** an authenticated user, **When** they call DELETE /users/me/presets/{id}, **Then** the response returns 204 and the preset is removed.
6. **Given** an authenticated user, **When** they call POST /users/me/presets with a name they have already used for another preset, **Then** the response returns 409 with error code DUPLICATE_PRESET_NAME.
7. **Given** an authenticated user, **When** they call PUT or DELETE /users/me/presets/{id} with an id that does not exist, **Then** the response returns 404.
8. **Given** an authenticated user, **When** they reference a preset that belongs to another user, **Then** the response returns 403.
9. **Given** an unauthenticated request, **When** any preset endpoint is called, **Then** the response returns 401.

---

### User Story 5 - Automatic Account Deletion Cleanup (Priority: P3)

The system automatically processes accounts whose 30-day deletion grace period has expired. All user data, notebooks, lessons, and avatar files are permanently removed.

**Why this priority**: This background process is essential for data privacy compliance and storage hygiene, but it runs without user interaction and is operationally invisible to end users.

**Independent Test**: Can be fully tested by seeding a user with ScheduledDeletionAt set to a past date, triggering the cleanup service, and verifying the user record and all associated data no longer exist in the system.

**Acceptance Scenarios**:

1. **Given** one or more user accounts with ScheduledDeletionAt in the past, **When** the cleanup service runs, **Then** each such account and all its associated data are permanently deleted.
2. **Given** a user with an avatar blob, **When** their account is hard-deleted by the cleanup service, **Then** the avatar blob is also removed from storage.
3. **Given** a user whose ScheduledDeletionAt is in the future, **When** the cleanup service runs, **Then** that account is NOT deleted.
4. **Given** multiple expired accounts where one deletion fails, **When** the cleanup service runs, **Then** the failure is logged and the remaining accounts continue to be processed.

---

### Edge Cases

- What happens when GET /users/me is called with a valid JWT for a user whose account has been hard-deleted? The response returns 401 (token valid but user no longer exists).
- What happens when a user tries to update their profile while their account is scheduled for deletion? Updates are permitted during the grace period — only hard deletion is deferred. Active JWT sessions also remain fully valid throughout the grace period.
- What happens when the avatar blob deletion fails during account cleanup? The failure is logged but the user record is still hard-deleted to avoid stale accounts.
- What happens when two cleanup service invocations run concurrently? Each invocation independently queries for expired accounts; duplicate deletion attempts on missing records are handled gracefully (no-op or idempotent).
- What happens when two concurrent `DELETE /users/me` calls arrive for the same user? The first call succeeds (204, sets ScheduledDeletionAt); the second call finds ScheduledDeletionAt already set and returns 409 (ACCOUNT_DELETION_ALREADY_SCHEDULED).
- What happens when the cleanup service fires at the same moment a user calls `POST /users/me/cancel-deletion`? Given the 30-day grace period, this window is extremely narrow. The last writer wins at the DB level; if the cleanup commits first, the cancel-deletion will fail with a not-found error on the now-deleted user record (returned as 401 — token valid but user gone).
- What happens when PUT /users/me/avatar is called and the storage service is unavailable? The response returns 500; the user's existing avatarBlobPath is not changed.
- What happens when PUT /users/me/presets/{id} references a preset that does not exist? The response returns 404.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow authenticated users to retrieve their full profile (id, email, firstName, lastName, language, defaultPageSize, defaultInstrumentId, avatarUrl, scheduledDeletionAt) via GET /users/me.
- **FR-002**: System MUST allow authenticated users to update their firstName, lastName, language, defaultPageSize, and defaultInstrumentId via PUT /users/me. All five fields are required on every call (full replacement semantics); omitting any field MUST return 400. `firstName` MUST be non-empty and at most 100 characters. `lastName` MAY be an empty string and MUST be at most 100 characters. `language` MUST be one of the two supported locale codes (`en` or `hu`). Sending `null` for `defaultPageSize` or `defaultInstrumentId` clears the preference (sets the value to null in storage).
- **FR-003**: System MUST validate that defaultInstrumentId references an existing instrument; updates with a non-existent instrument MUST be rejected with a 404 error (INSTRUMENT_NOT_FOUND).
- **FR-004**: System MUST validate that defaultPageSize is one of the supported page size values (A4, A5, A6, B5, B6).
- **FR-005**: System MUST allow authenticated users to schedule account deletion by setting ScheduledDeletionAt to `DateTime.UtcNow.AddDays(30)` (exactly 30 × 24 hours from the moment of the request) via DELETE /users/me; the response MUST be 204.
- **FR-006**: System MUST reject repeated deletion scheduling for already-scheduled accounts with a 409 error using code ACCOUNT_DELETION_ALREADY_SCHEDULED.
- **FR-007**: System MUST allow authenticated users to cancel a pending deletion via POST /users/me/cancel-deletion, clearing ScheduledDeletionAt; the response MUST be 204.
- **FR-008**: System MUST reject cancel-deletion requests for accounts without a scheduled deletion with a 400 error using code ACCOUNT_DELETION_NOT_SCHEDULED.
- **FR-009**: System MUST allow authenticated users to upload an avatar image via PUT /users/me/avatar as multipart/form-data with the file in a field named `file`; the response MUST be 200 with the updated profile.
- **FR-010**: System MUST reject avatar uploads strictly exceeding 2,097,152 bytes (2 MiB) with a 400 validation error. Files of exactly 2,097,152 bytes MUST be accepted.
- **FR-011**: System MUST reject avatar uploads whose MIME type (Content-Type) is not one of `image/jpeg`, `image/png`, or `image/webp` with a 400 validation error. Format validation is performed on the MIME type, not the file extension.
- **FR-012**: System MUST store uploaded avatars in external file storage under the path avatars/{userId}, record the full public blob URL in the user's profile, and return that URL in `GET /users/me` responses so clients can render it directly without a proxy endpoint.
- **FR-013**: System MUST delete the previous avatar from storage when a user uploads a new avatar.
- **FR-013a**: System MUST allow authenticated users to remove their avatar entirely via `DELETE /users/me/avatar`; the response MUST be 204, the blob MUST be deleted from storage, and `avatarUrl` MUST be set to null. Calling this endpoint when no avatar is set MUST return 204 (idempotent). If `avatarUrl` is set in the database but the blob no longer exists in storage, the blob deletion MUST be treated as a no-op (idempotent delete) and `avatarUrl` MUST still be cleared.
- **FR-014**: System MUST allow authenticated users to retrieve all their saved style presets via GET /users/me/presets; the response MUST return an empty array when no presets exist. Presets MUST be ordered alphabetically by name ascending.
- **FR-015**: System MUST allow authenticated users to create a named style preset containing exactly 12 module-type style definitions (one per ModuleType) via POST /users/me/presets; the response MUST be 201.
- **FR-015a**: Preset names MUST be unique among a user's own saved presets; attempting to create a preset with a name already used by the same user MUST return 409 with error code DUPLICATE_PRESET_NAME. User preset names MAY match system preset names (Classic, Colorful, Dark, Minimal, Pastel) — the two are separate namespaces.
- **FR-016**: System MUST reject preset creation with fewer than 12 style definitions, more than 12, or duplicate module types, with a 400 validation error. `moduleType` values are matched case-insensitively against the `ModuleType` enum; unrecognised values MUST return 400. Each style entry's `stylesJson` field MUST be a non-empty string; an empty string MUST return 400.
- **FR-017**: System MUST allow authenticated users to update the name or style definitions of their own preset via PUT /users/me/presets/{id}; the response MUST be 200. At least one of `name` or `styles` MUST be present in the request body; a request containing neither MUST return 400. Renaming a preset to a name already used by *another* of the user's presets MUST return 409 with error code DUPLICATE_PRESET_NAME. Renaming a preset to its own current name MUST be accepted (no conflict — returns 200).
- **FR-018**: System MUST allow authenticated users to delete their own preset via DELETE /users/me/presets/{id}; the response MUST be 204.
- **FR-019**: System MUST return 403 when a user attempts to access or modify a preset that belongs to another user.
- **FR-020**: System MUST return 404 when a user references a preset ID that does not exist.
- **FR-021**: All endpoints MUST require valid JWT authentication; unauthenticated requests MUST return 401.
- **FR-022**: System MUST run a background cleanup process on a daily schedule that permanently deletes all user accounts whose ScheduledDeletionAt is in the past, along with all cascade-associated data (Notebooks, Lessons, LessonPages, and Modules via EF Core cascade delete) and avatar blobs.
- **FR-023**: Background cleanup MUST log failures on individual account deletions and continue processing remaining accounts; a single failure MUST NOT halt the entire cleanup run. If the database commit fails after processing a batch, the failure MUST be logged and the batch MUST be retried on the next scheduled run (accounts remain with their ScheduledDeletionAt set and will be selected again).

### Key Entities

- **User**: Represents a registered account. Key attributes: unique identifier, email address, display name, preferred language, default page size preference, default instrument preference, avatar public URL (nullable — full URL returned directly to clients for rendering), scheduled deletion timestamp (nullable).
- **UserStylePreset**: A named collection of 12 module-type style configurations saved by a user for reuse across notebooks. Key attributes: unique identifier, owner user reference, preset name, ordered collection of 12 style definitions.
- **StyleDefinition** *(within a preset)*: One of the 12 entries in a style preset, corresponding to a specific module type. Contains all visual styling properties for that module type (font, colours, etc.) — mirrors the structure of NotebookModuleStyle on a notebook.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can view their complete profile and update all editable preferences in under 2 seconds under normal load conditions.
- **SC-002**: Account deletion scheduling and cancellation each complete in a single request-response cycle with no additional user steps.
- **SC-003**: Avatar uploads for valid files under 2 MiB complete successfully 100% of the time when blob storage is available and the network is not degraded; invalid uploads are rejected immediately with actionable error messages.
- **SC-004**: Style presets are created, retrieved, updated, and deleted correctly with no data loss or cross-user leakage; users cannot view, modify, or delete presets they do not own.
- **SC-005**: All accounts whose grace period has expired are deleted within 24 hours of their scheduled deletion timestamp, with zero false positives (active accounts not deleted).
- **SC-006**: All profile endpoints reject unauthenticated requests 100% of the time; no user data is accessible without a valid token.

## Assumptions

- A user may have at most one avatar at a time. Uploading a new avatar replaces the previous one.
- There is no enforced maximum number of style presets a user can save (bounded only by practical storage limits).
- Profile updates do not require re-authentication (email and password are not updatable through these endpoints — those are managed via the auth system).
- The background cleanup service is considered a single instance; no distributed locking is required since it runs in-process.
- When the avatar blob deletion fails during account cleanup, the hard delete of the user record still proceeds; blob cleanup failure is logged as a warning only.
- The `language` field accepts only the two supported locale codes: `en` and `hu` (see FR-002).
- `firstName` is required and has a maximum length of 100 characters. `lastName` may be an empty string and has a maximum length of 100 characters (see FR-002).
- Style preset names must be non-empty strings; a maximum length of 100 characters is enforced (see FR-015).
- Instrument seed data is a hard prerequisite: if instruments are not seeded before the service starts, any `defaultInstrumentId` set on a user record will fail FK validation. The `defaultInstrumentId` field will be null for all users until instruments are seeded.

## Dependencies

- Auth system (feature 005): JWT bearer authentication must be in place; user identity is resolved from JWT claims.
- Domain models (feature 002): PageSize enum and ModuleType enum must be defined in DomainModels.
- Instrument seed data (feature 003): Instruments must be seeded before defaultInstrumentId validation can pass.
- Azure Blob Storage configuration: Connection string and container name must be present in application settings.

## Clarifications

### Session 2026-03-28

- Q: Should clients access avatar images via a public blob URL returned in the profile, or through a backend proxy endpoint? → A: Return full public blob URL in `GET /users/me`; clients render it directly with no proxy endpoint needed.
- Q: After scheduling deletion, do active JWT sessions remain valid for the 30-day grace period? → A: Yes — sessions remain fully valid throughout the grace period so the user can continue using the app and cancel deletion if they change their mind.
- Q: Must preset names be unique per user? → A: Yes — duplicate names within the same user's presets are rejected with 409 and error code DUPLICATE_PRESET_NAME (applies to both creation and rename).
- Q: Can users remove their avatar without uploading a replacement? → A: Yes — add `DELETE /users/me/avatar`; deletes the blob, sets avatarBlobPath to null, returns 204 (idempotent when no avatar is set).
- Q: Does `PUT /users/me` require all five fields or allow partial updates? → A: All five fields required on every call (firstName, lastName, language, defaultPageSize, defaultInstrumentId — full replacement / standard PUT semantics); omitting any returns 400.
- Q: Status code for ACCOUNT_DELETION_ALREADY_SCHEDULED? → A: 409 Conflict (state conflict semantics). Spec acceptance scenario and FR-006 updated accordingly.
- Q: "30 days" grace period definition? → A: `DateTime.UtcNow.AddDays(30)` — exactly 30 × 24 hours in UTC.
- Q: 2 MB file size — SI or binary? → A: 2,097,152 bytes (binary/MiB). Boundary value (exactly 2,097,152 bytes) is accepted.
- Q: Multipart form field name for avatar upload? → A: `file`.
- Q: `null` for `defaultInstrumentId` in PUT /users/me — clears or keeps? → A: Clears the preference (full-replacement semantics).
- Q: Preset list ordering in GET /users/me/presets? → A: Alphabetical by name ascending.
- Q: Can user preset names match system preset names? → A: Yes — separate namespaces, no cross-table uniqueness check.
- Q: Renaming a preset to its current name — conflict or accepted? → A: Accepted (200). Uniqueness check excludes the preset being updated.
- Q: Empty body on PUT /users/me/presets/{id}? → A: Rejected with 400 — at least one of name or styles must be present.
- Q: moduleType case sensitivity in preset styles? → A: Case-insensitive matching against ModuleType enum.
