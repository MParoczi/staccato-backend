# Feature Specification: Lesson & Lesson Page Management

**Feature Branch**: `009-lesson-page-management`
**Created**: 2026-03-29
**Status**: Ready for Implementation
**Input**: User description: "Implement lesson and lesson page management for Staccato, including CRUD endpoints for lessons and lesson pages, plus an auto-generated notebook index."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Create and Manage Lessons (Priority: P1)

A user opens one of their notebooks and creates a new lesson to record what they learned during a practice session. They give the lesson a title (e.g., "1. Guitar Basics"). The system automatically provides a first blank page within that lesson. Later, the user renames the lesson to correct a typo. When they no longer need the lesson, they delete it entirely.

**Why this priority**: Lessons are the fundamental content unit inside a notebook. Without the ability to create, view, rename, and delete lessons, no other notebook content features can function.

**Independent Test**: Can be fully tested by creating a notebook, then creating/listing/updating/deleting lessons within it — delivers the core data-entry capability of the application.

**Acceptance Scenarios**:

1. **Given** an authenticated user owns a notebook, **When** they request to create a lesson with a valid title, **Then** the system creates the lesson, automatically creates the first page (page number 1), and returns the lesson detail with the page included.
2. **Given** a notebook has three lessons, **When** the user requests all lessons for that notebook, **Then** the system returns all three lessons ordered by creation date ascending, each with id, title, createdAt, and pageCount.
3. **Given** a lesson exists, **When** the user requests the lesson by its id, **Then** the system returns the lesson detail including its list of pages.
4. **Given** a lesson exists, **When** the user updates its title with a valid new title, **Then** the system persists the change and returns the updated lesson detail.
5. **Given** a lesson exists with pages and modules, **When** the user deletes the lesson, **Then** the lesson, all its pages, and all modules on those pages are permanently removed.

---

### User Story 2 — Manage Lesson Pages (Priority: P2)

A user is working on a lesson and runs out of space on the current page. They add a new page to the lesson, which receives the next sequential page number. If they added a page by mistake, they can delete it — as long as it is not the only remaining page in the lesson.

**Why this priority**: Page management is the next layer of content organization after lessons. Users need to be able to expand their lessons across multiple pages and remove unnecessary ones.

**Independent Test**: Can be fully tested by creating a lesson, adding pages, listing them, and deleting non-last pages — delivers the multi-page layout capability.

**Acceptance Scenarios**:

1. **Given** a lesson with one page (page 1), **When** the user adds a new page, **Then** the system creates page 2 and returns it with a 201 status.
2. **Given** a lesson has pages 1 through 9, **When** the user adds a tenth page, **Then** the system creates page 10 and returns it with a 201 status (the soft limit threshold has been reached but not exceeded).
3. **Given** a lesson already has 10 or more pages, **When** the user adds another page, **Then** the system creates the page but returns a 200 status with the page data wrapped in a response that includes a warning message: "This lesson has reached the recommended maximum of 10 pages."
4. **Given** a lesson has pages 1 through 3, **When** the user requests all pages, **Then** the system returns all three pages ordered by page number ascending, each with id, pageNumber, and moduleCount.
5. **Given** a lesson has two pages, **When** the user deletes one of them, **Then** the page and all its modules are permanently removed.
6. **Given** a lesson has only one page remaining, **When** the user attempts to delete it, **Then** the system rejects the request with an error indicating the last page cannot be deleted.

---

### User Story 3 — View Notebook Index (Priority: P3)

A user opens a notebook and views the auto-generated index (table of contents). The index lists every lesson in creation order with its title and the global page number where it starts. This gives the user an overview of the notebook's structure and supports navigation to specific lessons.

**Why this priority**: The index is a read-only, derived view that depends on lessons and pages already existing. It adds significant navigation value but is not required for content creation.

**Independent Test**: Can be fully tested by creating a notebook with multiple lessons of varying page counts, then requesting the index and verifying each lesson's calculated start page number.

**Acceptance Scenarios**:

1. **Given** a notebook with three lessons (lesson A has 3 pages, lesson B has 2 pages, lesson C has 1 page), **When** the user requests the notebook index, **Then** the system returns three entries ordered by creation date with startPageNumber values of 2, 5, and 7 respectively (accounting for cover + index as pages before lesson content).
2. **Given** a notebook with no lessons, **When** the user requests the notebook index, **Then** the system returns an empty entries array.
3. **Given** a lesson is deleted from the middle of a notebook, **When** the user requests the index again, **Then** the remaining lessons' startPageNumber values are recalculated correctly.

---

### User Story 4 — Ownership Enforcement (Priority: P1)

An authenticated user attempts to access or modify a lesson, lesson page, or notebook index that belongs to another user's notebook. The system denies access without revealing whether the resource exists.

**Why this priority**: Security and data isolation are non-negotiable. Ownership enforcement must be present from the start alongside lesson creation.

**Independent Test**: Can be tested by creating resources under one user and attempting access from a different authenticated user — verifying 403 responses.

**Acceptance Scenarios**:

1. **Given** user A owns a notebook with a lesson, **When** user B attempts to view, update, or delete that lesson, **Then** the system returns a 403 Forbidden response.
2. **Given** user A owns a lesson with pages, **When** user B attempts to list, add, or delete pages in that lesson, **Then** the system returns a 403 Forbidden response.
3. **Given** user A owns a notebook, **When** user B requests the notebook index, **Then** the system returns a 403 Forbidden response.

---

### Edge Cases

- What happens when a user creates a lesson with a title at exactly 200 characters? The system accepts it successfully.
- What happens when a user creates a lesson with a title exceeding 200 characters? Validation rejects the request with a 400 error.
- What happens when a user creates a lesson with an empty or whitespace-only title? Validation rejects the request.
- What happens when a user tries to access a lesson or page with a non-existent ID? The system returns 404.
- What happens when a user tries to create a lesson in a non-existent notebook? The system returns 404.
- What happens when a user tries to add a page to a non-existent lesson? The system returns 404.
- What happens when a user deletes a page that has modules on it? The modules are hard-deleted as part of the cascade.
- What happens when page numbers have gaps (e.g., pages 1 and 3 exist after deletion)? The new page is numbered max + 1 based on existing page numbers, so it would be 4.
- What happens when DELETE /lessons/{lessonId}/pages/{pageId} is called with a pageId that belongs to a different lesson? The system returns 404 (page not found in this lesson context).
- What happens when DELETE is called on a page that was already deleted? The system returns 404 (resource no longer exists).

## Requirements *(mandatory)*

### Functional Requirements

**Lesson Management**

- **FR-001**: System MUST allow authenticated users to create a lesson within their own notebook, accepting a title (required, max 200 characters).
- **FR-002**: System MUST automatically create the first lesson page (page number 1) when a new lesson is created.
- **FR-003**: System MUST return all lessons for a given notebook ordered by creation date ascending, including each lesson's id, title, creation date, and page count.
- **FR-004**: System MUST return a lesson's full detail including its list of pages when requested by id.
- **FR-005**: System MUST allow the owner to update a lesson's title with a new valid title (required, max 200 characters).
- **FR-006**: System MUST hard-delete a lesson and cascade the deletion to all its pages and their modules.

**Lesson Page Management**

- **FR-007**: System MUST return all pages for a given lesson ordered by page number ascending, including each page's id, page number, and module count.
- **FR-008**: System MUST allow adding a new page to a lesson, assigning it a page number equal to the maximum existing page number plus one.
- **FR-009**: System MUST return a 200 status with a warning message when adding a page to a lesson that already has 10 or more pages. The response MUST include both the new page data and the warning text.
- **FR-010**: System MUST return a 201 status (without warning) when adding a page to a lesson that has fewer than 10 pages.
- **FR-011**: System MUST prevent deletion of the last remaining page in a lesson, returning an error.
- **FR-012**: System MUST hard-delete a page and cascade the deletion to all its modules.

**Notebook Index**

- **FR-013**: System MUST return an auto-generated index for a notebook containing all lessons ordered by creation date ascending.
- **FR-014**: Each index entry MUST include the lesson id, title, creation date, and a calculated start page number.
- **FR-015**: The start page number MUST be calculated as: 2 + (sum of page counts of all lessons created before the current lesson). This accounts for the cover page and index page occupying the first two positions.

**Security & Validation**

- **FR-016**: All endpoints MUST require a valid JWT bearer token.
- **FR-017**: System MUST return 403 Forbidden when a user attempts to access or modify resources belonging to another user's notebook. The system MUST NOT return 404 for ownership violations.
- **FR-018**: System MUST validate lesson titles as required and no longer than 200 characters, returning a 400 error for invalid input.

### Key Entities

- **Lesson**: A learning session record within a notebook. Has a title, belongs to a notebook, contains one or more pages. Ordered by creation date ascending within a notebook.
- **LessonPage**: A single page within a lesson. Identified by a sequential page number within its lesson. Contains zero or more modules placed on a 2D grid canvas.
- **NotebookIndexEntry**: A derived (non-persisted) data structure representing one lesson's position in the notebook table of contents. Contains the lesson reference and a calculated global start page number.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create a lesson and have a first blank page ready to use in a single action, with no additional steps required.
- **SC-002**: Users can add pages to a lesson and the system correctly assigns sequential page numbers without requiring manual input.
- **SC-003**: The notebook index accurately reflects the current state of all lessons and their page counts, recalculating start page numbers dynamically after any lesson or page change.
- **SC-004**: Unauthorized access attempts are rejected 100% of the time with 403 responses — no data leakage between users.
- **SC-005**: The system provides a proactive warning when a lesson exceeds the recommended 10-page limit, without blocking the user from adding more pages.
- **SC-006**: Deleting a lesson or page removes all associated child data completely, leaving no orphaned records.

## Assumptions

- Lesson titles do not need to be unique within a notebook — users may have similarly or identically named lessons.
- The 10-page soft limit is advisory only (a warning message). There is no hard upper limit on the number of pages per lesson.
- Page number gaps after deletion are acceptable. The system does not renumber existing pages.
- The notebook index is computed on-demand (not persisted). It is recalculated fresh on each request.
- Creating a page requires no request body — the page number is auto-assigned server-side.
