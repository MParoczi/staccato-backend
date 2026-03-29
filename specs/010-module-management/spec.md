# Feature Specification: Module Management

**Feature Branch**: `010-module-management`
**Created**: 2026-03-29
**Status**: Draft
**Input**: User description: "Implement module management with full server-side 2D grid validation for the Staccato API."

## Clarifications

### Session 2026-03-29

- Q: Should the PUT request body include moduleType, and how should mismatches be handled? → A: PUT includes moduleType; server rejects with a 400 error if it differs from the stored value.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Place a New Module on a Lesson Page (Priority: P1)

A user opens a lesson page in their notebook and adds a new content module (e.g., Theory, Practice, Homework) at a specific position on the dotted grid. The system validates that the module fits within the page, doesn't overlap existing modules, and meets minimum size requirements for its type.

**Why this priority**: Module creation is the foundational interaction — without it, no content can be added to a lesson page. Every other module operation depends on modules existing first.

**Independent Test**: Can be fully tested by creating a module on an empty lesson page via POST and verifying the module appears with correct position, type, and empty content. Delivers the core value of placing content blocks on the grid canvas.

**Acceptance Scenarios**:

1. **Given** an authenticated user with a notebook containing a lesson page, **When** the user creates a Theory module at position (2, 5) with dimensions 18x10, **Then** the system returns 201 with the created module including a server-generated ID, and the module appears at the specified grid coordinates.
2. **Given** a lesson page with an existing module occupying grid cells (0, 0) to (20, 10), **When** the user attempts to create a module at (10, 5) with dimensions 15x8, **Then** the system returns 422 with error code `MODULE_OVERLAP`.
3. **Given** a lesson page on an A4 notebook (42x59 grid), **When** the user attempts to create a module at position (40, 0) with width 5, **Then** the system returns 422 with error code `MODULE_OUT_OF_BOUNDS`.
4. **Given** a Theory module type with minimum dimensions 8x5, **When** the user attempts to create a Theory module with dimensions 6x3, **Then** the system returns 422 with error code `MODULE_TOO_SMALL`.
5. **Given** a Breadcrumb module type, **When** the user creates a Breadcrumb module with non-empty content, **Then** the system returns 422 with error code `BREADCRUMB_CONTENT_NOT_EMPTY`.
6. **Given** a lesson that already has a Title module on page 1, **When** the user attempts to create another Title module on page 2, **Then** the system returns 409 with error code `DUPLICATE_TITLE_MODULE`.

---

### User Story 2 - Edit Module Content (Priority: P2)

A user selects an existing module and updates its content by adding building blocks (text, lists, tables, chord progressions, etc.) along with optionally repositioning it. The system validates that the building block types are allowed for the module's type and that the new position remains valid.

**Why this priority**: After placing modules, users need to fill them with content. Full module updates enable the core editing workflow.

**Independent Test**: Can be fully tested by creating a module, then updating it via PUT with building block content and verifying the updated content is persisted and returned correctly.

**Acceptance Scenarios**:

1. **Given** an existing Theory module, **When** the user updates it with a SectionHeading and a Text building block, **Then** the system returns 200 with the updated module containing the new content.
2. **Given** an existing Theory module, **When** the user attempts to add a ChordProgression building block (not allowed for Theory), **Then** the system returns 422 with error code `INVALID_BUILDING_BLOCK`.
3. **Given** an existing module, **When** the user updates its position to overlap with another module, **Then** the system returns 422 with error code `MODULE_OVERLAP` (the overlap check excludes the module being updated).
4. **Given** an existing Breadcrumb module, **When** the user attempts to PUT content (non-empty array) into it, **Then** the system returns 422 with error code `BREADCRUMB_CONTENT_NOT_EMPTY`.
5. **Given** an existing Theory module, **When** the user sends a PUT with moduleType set to "Practice", **Then** the system returns 400 with error code `MODULE_TYPE_IMMUTABLE`.

---

### User Story 3 - Drag and Resize Modules on the Grid (Priority: P3)

A user drags a module to a new position or resizes it by dragging its handles. The frontend sends a lightweight layout-only update (PATCH) with the new grid coordinates and dimensions. The system validates the new layout without requiring the full content payload.

**Why this priority**: Drag-and-resize is critical for the free-form grid editing experience, but it builds on top of the module creation and content editing capabilities.

**Independent Test**: Can be fully tested by creating a module and then updating its layout via PATCH, verifying the new position is saved while content remains unchanged.

**Acceptance Scenarios**:

1. **Given** a module at position (0, 0) with dimensions 10x10, **When** the user drags it to (5, 5) via PATCH, **Then** the system returns 200 with the updated position and all other module data unchanged.
2. **Given** a module being resized, **When** the new dimensions would extend beyond the page boundary, **Then** the system returns 422 with error code `MODULE_OUT_OF_BOUNDS`.
3. **Given** a module being moved, **When** the new position would overlap with another module, **Then** the system returns 422 with error code `MODULE_OVERLAP`.
4. **Given** a module being resized, **When** the new dimensions are below the minimum for its type, **Then** the system returns 422 with error code `MODULE_TOO_SMALL`.

---

### User Story 4 - View All Modules on a Page (Priority: P4)

A user opens a lesson page and the frontend fetches all modules placed on that page to render them on the grid canvas.

**Why this priority**: Retrieving modules is essential for rendering the page, but it is a simpler read operation that supports the other CRUD stories.

**Independent Test**: Can be fully tested by creating multiple modules on a page and then fetching them via GET, verifying all modules are returned with correct data.

**Acceptance Scenarios**:

1. **Given** a lesson page with 3 modules, **When** the user requests all modules for that page, **Then** the system returns 200 with an array of 3 module objects including their positions, types, and content.
2. **Given** an empty lesson page, **When** the user requests all modules, **Then** the system returns 200 with an empty array.

---

### User Story 5 - Delete a Module (Priority: P5)

A user removes a module from a lesson page. The module and all its content are permanently deleted.

**Why this priority**: Deletion is a supporting operation that allows users to undo module placement mistakes or clear unwanted content.

**Independent Test**: Can be fully tested by creating a module, deleting it via DELETE, and verifying it no longer appears in the page's module list.

**Acceptance Scenarios**:

1. **Given** an existing module, **When** the user deletes it, **Then** the system returns 204 and the module no longer appears in subsequent GET requests.
2. **Given** a module that belongs to another user, **When** the user attempts to delete it, **Then** the system returns 403.

---

### Edge Cases

- What happens when a user tries to create a module with zIndex < 0? The system returns a 400 FluentValidation error (field-level), not a business rule exception.
- What happens when a user tries to update a module that doesn't exist? The system returns 404.
- What happens when a user tries to create a module on a lesson page belonging to another user's notebook? The system returns 403.
- What happens when the content JSON contains building blocks with an unrecognized type discriminator? The system returns 422 with `INVALID_BUILDING_BLOCK`.
- What happens when a user creates a module exactly at the page boundary (gridX + gridWidth == pageGridWidth)? This is valid — the module fits within the page.
- What happens when two modules are adjacent (touching edges) but not overlapping? This is valid — only actual overlap is rejected.
- What happens when a PUT request sends a different moduleType than the module's stored type? The system returns 400 with `MODULE_TYPE_IMMUTABLE` — module type is immutable after creation.
- What happens when a Title module is deleted? The uniqueness slot is freed — a new Title module can be created on any page of that lesson.
- What happens when PUT sends malformed (unparseable) JSON in the content field? The system returns 400.
- What happens when PUT content contains a building block with an unrecognized type discriminator? The system returns 422 with `INVALID_BUILDING_BLOCK`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow authenticated users to create a module on a lesson page they own, specifying module type, grid position, grid dimensions, z-index, and content.
- **FR-002**: System MUST allow authenticated users to retrieve all modules on a lesson page they own, ordered by GridY ascending then GridX ascending.
- **FR-003**: System MUST allow authenticated users to perform a full update of a module they own, including content, grid position, dimensions, and z-index. The request MUST include moduleType; the system MUST reject with a 400 error if the provided moduleType differs from the module's stored type.
- **FR-004**: System MUST allow authenticated users to delete a module they own, permanently removing it and its content.
- **FR-005**: System MUST allow authenticated users to update only the layout properties (gridX, gridY, gridWidth, gridHeight, zIndex) of a module they own, without requiring the content payload.
- **FR-006**: System MUST enforce minimum grid dimensions per module type: gridWidth >= MinWidth and gridHeight >= MinHeight as defined in ModuleTypeConstraints.
- **FR-007**: System MUST enforce page boundary constraints: gridX >= 0, gridY >= 0, gridX + gridWidth <= page grid width, gridY + gridHeight <= page grid height. Page dimensions are derived from the notebook's page size.
- **FR-008**: System MUST reject module placements that overlap with any existing module on the same page. Two rectangles A and B overlap when: `A.gridX < B.gridX + B.gridWidth AND A.gridX + A.gridWidth > B.gridX AND A.gridY < B.gridY + B.gridHeight AND A.gridY + A.gridHeight > B.gridY` (AABB intersection test). The module being updated is excluded from this check.
- **FR-009**: System MUST validate on PUT that all building blocks in a module's content are of types allowed for that module's type, as defined in ModuleTypeConstraints. (POST requires empty content per FR-015; PATCH does not include content.)
- **FR-010**: System MUST enforce that Breadcrumb modules always have empty content ([]) on both POST and PUT.
- **FR-011**: System MUST enforce that only one Title module exists per lesson across all of its pages. Attempting to create a second Title module returns a conflict error.
- **FR-012**: System MUST enforce that zIndex is >= 0.
- **FR-013**: System MUST return distinct, machine-readable error codes for each validation violation: `MODULE_TOO_SMALL`, `MODULE_OUT_OF_BOUNDS`, `MODULE_OVERLAP`, `INVALID_BUILDING_BLOCK`, `BREADCRUMB_CONTENT_NOT_EMPTY`, `DUPLICATE_TITLE_MODULE`, `MODULE_TYPE_IMMUTABLE`, `MALFORMED_CONTENT_JSON`.
- **FR-014**: System MUST enforce ownership — users can only access and modify modules within their own notebooks. Accessing another user's modules returns a 403 error.
- **FR-015**: System MUST validate that content is an empty array ([]) when creating a new module (POST).
- **FR-016**: System MUST return a 400 error with code `MALFORMED_CONTENT_JSON` when ContentJson on PUT is malformed or unparseable JSON. System MUST return 422 with `INVALID_BUILDING_BLOCK` when a building block contains an unrecognized type discriminator.
- **FR-017**: System MUST provide localized error messages (en, hu) for all error codes introduced by this feature.
- **FR-018**: System MUST always run grid validation (minimum size, page boundary, overlap) on PUT, regardless of whether position fields changed from their current values.

### Validation Rule Applicability by Endpoint

| Rule | POST | PUT | PATCH |
|------|------|-----|-------|
| Minimum size (FR-006) | Yes | Yes | Yes |
| Page boundary (FR-007) | Yes | Yes | Yes |
| No overlap (FR-008) | Yes | Yes | Yes |
| Allowed blocks (FR-009) | No (empty) | Yes | No (no content) |
| Breadcrumb empty (FR-010) | Yes | Yes | No (no content) |
| Title unique (FR-011) | Yes | No | No |
| ZIndex >= 0 (FR-012) | Yes | Yes | Yes |
| Type immutable (FR-003) | N/A | Yes | N/A |
| Malformed JSON (FR-016) | N/A | Yes | N/A |

### Key Entities

- **Module**: A content block placed on a lesson page's 2D grid. Key attributes: type, grid position (x, y), grid dimensions (width, height), z-index for visual stacking, and structured content as an ordered array of building blocks.
- **LessonPage**: The parent surface where modules are placed. Provides the grid boundary constraints derived from the notebook's page size.
- **Lesson**: The parent of lesson pages. Enforces the one-Title-module-per-lesson rule across all its pages.
- **Notebook**: The grandparent entity that determines page size (and thus grid dimensions) for boundary validation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can place a new module on a lesson page and see it confirmed within 1 second (server-side p95, normal load).
- **SC-002**: Users receive clear, specific error feedback when a module placement violates any validation rule, enabling immediate correction without guesswork. All 8 error codes return distinct, machine-readable identifiers.
- **SC-003**: Drag-and-resize layout updates complete within 500ms server-side (p95, normal load) to support smooth, real-time grid editing with debounced auto-save.
- **SC-004**: Server-side validation rules are enforced per the Validation Rule Applicability table — grid placement rules (minimum size, boundary, overlap) apply to all mutation endpoints; content rules apply to PUT only; title uniqueness applies to POST only. No rule can be bypassed by using a different endpoint.
- **SC-005**: Users can only access and modify modules within their own notebooks — unauthorized access is blocked 100% of the time.
- **SC-006**: The one-Title-module-per-lesson rule is enforced across all pages of a lesson with zero false positives or false negatives.

## Assumptions

- The frontend will debounce PATCH layout calls (minimum 500ms) to avoid excessive server requests during drag/resize operations.
- Module creation always starts with empty content ([]). Content is populated via subsequent PUT updates.
- ZIndex is used for visual stacking order on the frontend; the backend stores and returns it but does not use it for any business logic beyond the >= 0 constraint.
- Building block content validation on POST (empty array check) and PUT (type constraint check) are separate concerns — POST enforces empty content, PUT validates the building block types.
- The Breadcrumb module's content is always empty because its display content is dynamically derived from Subtitle modules; the server enforces this invariant.
- Module type is immutable after creation. PUT requires moduleType in the request body and rejects mismatches with 400, making the constraint explicit to callers.
- Concurrent layout updates (PATCH) use last-write-wins semantics. No optimistic concurrency control is implemented, consistent with the rest of the project. The frontend debounce (500ms) naturally reduces conflict frequency.
- Module count per page is expected to stay under 50 in practice. The overlap check is O(n) against page modules, which is acceptable at this scale.
- Malformed JSON in the `content` field is caught at two levels: (1) ASP.NET Core model binding rejects structurally invalid JSON with a standard 400 response before reaching business logic; (2) the service layer validates semantically invalid content (e.g., unrecognized type discriminator) after model binding succeeds. FR-016 covers both levels — the framework handles structural malformation, the service handles semantic validation.
- Performance targets SC-001 and SC-003 are validated via manual load testing or production monitoring, not automated unit/integration tests. The expected module count per page (<50) and simple CRUD operations make these targets easily achievable without dedicated performance test infrastructure at this stage.
