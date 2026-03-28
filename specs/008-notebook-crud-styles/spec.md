# Feature Specification: Notebook CRUD and Style Management

**Feature Branch**: `008-notebook-crud-styles`
**Created**: 2026-03-28
**Status**: Draft
**Input**: User description: "Implement notebook CRUD and notebook module style management for Staccato."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create and Browse Notebooks (Priority: P1)

A logged-in user wants to start a new learning journey. They open the notebooks dashboard, see a list of their existing notebooks (with title, instrument, page size, cover colour, and lesson count), and create a new notebook by supplying a title, instrument, page size, and cover colour. On creation the system immediately applies the Colorful style preset so the notebook is ready to use. The user can later open the notebook to see its full detail including all 12 module type style configurations.

**Why this priority**: Without the ability to create and list notebooks there is nothing else to do in the application. Every other feature depends on a notebook existing.

**Independent Test**: Can be fully tested by creating a notebook, then listing notebooks and fetching the detail — the new entry appears in the list and its detail includes all 12 styles using the Colorful preset.

**Acceptance Scenarios**:

1. **Given** a logged-in user with no notebooks, **When** they request their notebook list, **Then** the response is an empty array.
2. **Given** a logged-in user, **When** they create a notebook with a valid title, instrument ID, page size, and cover colour with no styles provided, **Then** a new notebook is created with all 12 module type styles set to the Colorful system preset and the full detail (including styles) is returned with status 201.
3. **Given** a logged-in user, **When** they create a notebook and provide an explicit array of 12 style objects, **Then** those styles are used instead of the default preset.
4. **Given** a logged-in user with two notebooks, **When** they list their notebooks, **Then** both notebooks appear in the response, each with id, title, instrument name, page size, cover colour, lesson count, and timestamps.
5. **Given** a logged-in user, **When** they request the detail of one of their notebooks, **Then** the full notebook detail is returned including all 12 style records.
6. **Given** user A and user B each with their own notebooks, **When** user A lists their notebooks, **Then** only user A's notebooks appear.

---

### User Story 2 - Edit and Delete Notebooks (Priority: P2)

A logged-in user wants to rename a notebook or update its cover colour. They also want to be able to delete a notebook they no longer need, removing all its content permanently.

**Why this priority**: Editing and deleting are core CRUD operations; without them notebooks are static once created.

**Independent Test**: Can be fully tested by creating a notebook, updating its title and cover colour, verifying the changes in the response, then deleting it and confirming it no longer appears in the list.

**Acceptance Scenarios**:

1. **Given** a logged-in user with an existing notebook, **When** they update it with a new title and cover colour, **Then** the updated notebook detail (with the new values) is returned with status 200.
2. **Given** a logged-in user, **When** they attempt to update a notebook's instrument or page size, **Then** the request is rejected with status 400 and error code `NOTEBOOK_INSTRUMENT_IMMUTABLE` or `NOTEBOOK_PAGE_SIZE_IMMUTABLE` respectively.
3. **Given** a logged-in user, **When** they delete one of their notebooks, **Then** status 204 is returned and the notebook (and all its lessons, pages, and modules) is permanently removed.
4. **Given** user A tries to update or delete user B's notebook, **Then** the operation is rejected with status 403.
5. **Given** a non-existent notebook ID, **When** any user requests its detail, **Then** status 404 is returned.

---

### User Story 3 - View and Bulk-Update Module Styles (Priority: P3)

A logged-in user wants to customise how module types look inside their notebook. They can view the current 12 style configurations and replace all of them in a single operation.

**Why this priority**: Style management enhances the personalisation experience. A notebook works fully without custom styles, so this is lower priority than basic CRUD.

**Independent Test**: Can be fully tested by fetching styles for a notebook, submitting a replacement set of 12, and confirming the updated styles are returned.

**Acceptance Scenarios**:

1. **Given** a logged-in user with an existing notebook, **When** they request its styles, **Then** exactly 12 `NotebookModuleStyle` records are returned, one per module type.
2. **Given** a logged-in user, **When** they submit a bulk-replace request containing exactly 12 style objects covering all module types, **Then** all 12 records are updated and returned with status 200.
3. **Given** a logged-in user, **When** they submit a bulk-replace request with fewer or more than 12 items, or with a missing module type, **Then** the request is rejected with a validation error (status 400).
4. **Given** user A tries to view or update user B's notebook styles, **Then** the operation is rejected with status 403.

---

### User Story 4 - Apply a Preset to a Notebook (Priority: P4)

A logged-in user wants to quickly reset or restyle their notebook by applying a system preset (e.g. "Dark" or "Pastel") or one of their own saved presets. The selected preset replaces all 12 module type styles in one action.

**Why this priority**: Applying presets is a time-saving shortcut on top of the bulk-update capability, so it follows naturally once styles are editable.

**Independent Test**: Can be fully tested by applying a system preset to a notebook and confirming that all 12 returned styles match the preset's definition.

**Acceptance Scenarios**:

1. **Given** a logged-in user with an existing notebook and a valid system preset ID, **When** they apply that preset, **Then** all 12 notebook module styles are replaced with the preset's values and the updated styles are returned with status 200.
2. **Given** a logged-in user with an existing notebook and a valid user-saved preset ID that belongs to them, **When** they apply that preset, **Then** all 12 styles are replaced and returned with status 200.
3. **Given** a logged-in user supplies a preset ID that does not exist in either system or user-saved tables, **Then** status 404 is returned.
4. **Given** user A tries to apply user B's saved preset to user A's notebook, **Then** status 403 is returned.
5. **Given** user A tries to apply a preset to user B's notebook, **Then** status 403 is returned.

---

### User Story 5 - Browse System Style Presets (Priority: P5)

Any visitor (no login required) can retrieve the list of system style presets to preview what styles are available before or without creating an account.

**Why this priority**: Useful for the onboarding and creation flows but not critical to the core notebook workflow.

**Independent Test**: Can be fully tested by calling the presets endpoint without a token and confirming exactly 5 system presets are returned.

**Acceptance Scenarios**:

1. **Given** an unauthenticated request to the presets endpoint, **When** the request is made, **Then** all 5 system presets (Classic, Colorful, Dark, Minimal, Pastel) are returned with status 200 — no authentication token is required.
2. **Given** the presets endpoint is called, **Then** each preset includes its name, display order, whether it is the default, and all 12 module type style definitions.

---

### Edge Cases

- What happens when a notebook creation request references an instrument ID that does not exist? → Rejected with status 422 and error code `INSTRUMENT_NOT_FOUND`.
- What happens when `coverColor` is not a valid hex string? → Rejected with status 400 (FluentValidation error).
- What happens when `title` exceeds 200 characters? → Rejected with status 400 (FluentValidation error).
- What happens when `pageSize` is not one of the five allowed values? → Rejected with status 400.
- What happens when a `POST /notebooks` request includes an `instrumentId` or `pageSize` field AND a `PUT /notebooks/{id}` request also includes them? The POST always accepts these fields (they are required); the PUT rejects them if present at all, regardless of whether the submitted value matches the stored value.
- What happens if `styles` is provided in `POST /notebooks` but contains fewer or more than 12 items, duplicate module types, or is missing a module type? → Rejected with status 400 (must be exactly 12, one per module type, no duplicates — same rules as bulk style update).
- What happens when applying a preset to a notebook that belongs to another user? → 403, never 404.
- What happens when applying a user-saved preset that belongs to another user? → 403, never 404.
- What happens when both system and user-saved preset tables contain a record with the same ID? → The system preset table is checked first; if not found there, the user-saved preset table is checked.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow authenticated users to list all notebooks belonging to them in a single unpaginated response, returning id, title, instrument name, page size, cover colour, lesson count, and creation/update timestamps for each, ordered by `createdAt` ascending (oldest first).
- **FR-002**: The system MUST allow authenticated users to create a notebook by supplying a title (max 200 characters), a valid instrument ID, a page size (one of A4, A5, A6, B5, B6), and a valid hex cover colour.
- **FR-003**: On notebook creation, the system MUST atomically create exactly 12 `NotebookModuleStyle` records (one per module type). If styles are not provided in the request, the Colorful system preset is used. If styles are explicitly provided, the same validation rules apply as for bulk style update: exactly 12 entries, one per `ModuleType`, no duplicate types — otherwise the request is rejected with status 400.
- **FR-004**: The system MUST allow authenticated users to fetch the full detail of any of their notebooks, including all 12 style records.
- **FR-005**: The system MUST allow authenticated users to update the title and cover colour of their notebooks. Both `title` and `coverColor` are required in every update request. If `instrumentId` or `pageSize` are present in the request body (even with a matching value), the request MUST be rejected with status 400 and error codes `NOTEBOOK_INSTRUMENT_IMMUTABLE` or `NOTEBOOK_PAGE_SIZE_IMMUTABLE` respectively. Enforcement is achieved by including optional `instrumentId?` and `pageSize?` fields in the update request DTO and rejecting them via validation if non-null.
- **FR-006**: The system MUST allow authenticated users to permanently delete any of their notebooks; deletion cascades to all lessons, lesson pages, modules, and styles. If the notebook has an active (in-progress) PDF export, the deletion MUST be rejected with status 409 and error code `ACTIVE_EXPORT_EXISTS`.
- **FR-007**: The system MUST reject requests to view, edit, or delete notebooks that belong to a different user with status 403 (never 404).
- **FR-008**: The system MUST allow authenticated users to retrieve the 12 module type style records for any of their notebooks.
- **FR-009**: The system MUST allow authenticated users to bulk-replace all 12 module type style records for any of their notebooks in a single transactional operation. The request body MUST contain exactly 12 entries, one per module type. Existing `NotebookModuleStyle` records are updated in place — their `Id` values are preserved. Both `PUT /notebooks/{id}/styles` and `POST .../apply-preset` MUST refresh `Notebook.UpdatedAt` to the current UTC time upon successful completion.
- **FR-010**: The system MUST allow authenticated users to apply a system preset or a user-owned preset to any of their notebooks, replacing all 12 styles atomically. `Notebook.UpdatedAt` MUST be refreshed to the current UTC time upon successful preset application.
- **FR-011**: When applying a preset, the system MUST look up the given ID first in the system preset table, then in the user-saved preset table. If found in neither, it MUST return 404. If found in the user-saved table and the preset belongs to another user, it MUST return 403.
- **FR-012**: The system MUST expose a public endpoint that returns all 5 system style presets without requiring authentication, ordered by `displayOrder` ascending.
- **FR-013**: Every user can only access their own notebooks and styles; no cross-user data leakage is permitted.

### Key Entities

- **Notebook**: Top-level learning container belonging to a user. Has a title (mutable), instrument reference (immutable), page size (immutable), cover colour (mutable), and an ordered list of lessons. Always has exactly 12 associated module type styles.
- **NotebookModuleStyle**: Style configuration for one module type within one notebook. Covers background colour, border appearance (style, colour, width, radius), header colours, body text colour, and font family. Exactly 12 exist per notebook (one per module type: Title, Breadcrumb, Subtitle, Theory, Practice, Example, Important, Tip, Homework, Question, ChordTablature, FreeText).
- **SystemStylePreset**: A read-only, seeded collection of 12 module type style definitions. Five exist: Classic, Colorful, Dark, Minimal, Pastel. Colorful is the default on notebook creation.
- **UserSavedPreset**: A user-owned collection of 12 module type style definitions saved from a notebook's current configuration. Stored at user-account level, applicable to any of the user's notebooks.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can create, view, update, and delete a notebook in under 5 seconds each under typical network conditions.
- **SC-002**: The notebook list loads with all summary fields present for 100% of the user's notebooks, regardless of how many notebooks exist.
- **SC-003**: Applying a style preset replaces all 12 module type styles in a single round-trip; no partial updates occur under any condition.
- **SC-004**: Immutability of instrument and page size is enforced 100% of the time — no update ever changes these fields.
- **SC-005**: Ownership isolation is maintained 100% of the time — no user ever reads or modifies another user's notebooks or styles through any endpoint in this feature.
- **SC-006**: Every new notebook has exactly 12 module type style records immediately after creation, with no manual follow-up step required.
- **SC-007**: The system presets endpoint is accessible without authentication 100% of the time and always returns exactly 5 presets.

## Assumptions

- An `Instrument` entity already exists in the system, is immutable, and was seeded at startup; the notebook creation endpoint validates the provided `instrumentId` against this seeded data.
- The `CoverColor` field is validated as a CSS hex colour string (e.g. `#8B4513`); the exact format (3-digit vs 6-digit hex) is accepted as-is from the client.
- When `styles` is `null` or omitted in `POST /notebooks`, "Colorful" is identified programmatically as the system preset with `IsDefault = true`. If `GetDefaultAsync()` returns null (no preset has `IsDefault = true`), this is a server-side configuration error — the service throws an unhandled exception and the client receives **500 Problem Details**. No specific error code is defined for this case; it should never occur in a correctly seeded environment.
- The `PUT /notebooks/{id}` endpoint receives only `title` and `coverColor` fields. The presence of any other field (including `instrumentId` or `pageSize`) causes a 400 error regardless of whether the submitted value matches the stored value.
- `NotebookSummary` includes an `updatedAt` field (present in the TypeScript interface) even though the feature description lists only `createdAt`; this matches the established data model.
- User-saved presets are out of scope for creation/saving in this feature (managed elsewhere); this feature only supports *applying* existing user-saved presets to a notebook.
- The `StylesJson` column on `NotebookModuleStyle` stores a flat JSON object of all style properties for that module type entry.

## Clarifications

### Session 2026-03-28

- Q: What ordering should be applied to `GET /notebooks`? → A: `createdAt` ascending (oldest first)
- Q: Does `PUT /notebooks/{id}` support partial updates (either field omittable), or must both `title` and `coverColor` always be provided? → A: Both fields are required in every update request.
- Q: Should `GET /notebooks` support pagination? → A: No pagination — always return all of the user's notebooks in a single response.
- Q: When `styles` is explicitly provided in `POST /notebooks`, are the same validation rules applied as for bulk style update? → A: Yes — exactly 12 entries, one per `ModuleType`, no duplicate types; any violation is rejected with 400.
- Q: Should `GET /presets` return presets sorted by `displayOrder` ascending? → A: Yes — sorted by `displayOrder` ascending.
