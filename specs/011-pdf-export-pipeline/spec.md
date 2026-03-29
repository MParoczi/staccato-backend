# Feature Specification: PDF Export Pipeline

**Feature Branch**: `011-pdf-export-pipeline`
**Created**: 2026-03-29
**Status**: Draft
**Input**: User description: "Implement the asynchronous PDF export pipeline for Staccato using IHostedService + Channel, QuestPDF for rendering, Azure Blob Storage for file storage, and SignalR for completion notification."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Export Full Notebook as PDF (Priority: P1)

A user has completed several lessons in their notebook and wants to export the entire notebook as a printable PDF. They request an export, and the system processes it in the background. Once the PDF is ready, the user receives a real-time notification and can download it.

**Why this priority**: This is the core value proposition of the export feature. Without the ability to generate and download a full notebook PDF, no other export functionality is meaningful.

**Independent Test**: Can be fully tested by creating a notebook with lessons, pages, and modules, requesting an export, waiting for completion, and downloading the resulting PDF file.

**Acceptance Scenarios**:

1. **Given** a user has a notebook with at least one lesson, **When** they request an export without specifying lesson IDs, **Then** the system creates an export record with Pending status and returns a 202 response with the export ID.
2. **Given** an export has been queued, **When** the background processor picks it up, **Then** the status transitions from Pending to Processing, the PDF is rendered with a cover page, index page, and all lesson pages, and the status transitions to Ready.
3. **Given** an export has completed successfully, **When** the user is connected via real-time messaging, **Then** they receive a notification containing the export ID and file name.
4. **Given** an export has status Ready, **When** the user requests to download it, **Then** the system streams the PDF file with a filename matching the notebook title and a content-disposition header indicating attachment.

---

### User Story 2 - Export Selected Lessons as PDF (Priority: P2)

A user wants to export only specific lessons from a notebook rather than the whole thing, for example to share a subset of content with a student or print only certain topics.

**Why this priority**: Partial export builds on the full-export pipeline but adds user flexibility. It requires the same rendering infrastructure but with filtering logic.

**Independent Test**: Can be tested by creating a notebook with multiple lessons, requesting an export with a subset of lesson IDs, and verifying the downloaded PDF contains only the specified lessons.

**Acceptance Scenarios**:

1. **Given** a notebook with 5 lessons, **When** the user requests an export specifying 2 lesson IDs, **Then** only those 2 lessons appear in the generated PDF.
2. **Given** a user provides lesson IDs that do not belong to the specified notebook, **When** they request the export, **Then** the system rejects the request with an appropriate error.

---

### User Story 3 - View and Manage Export History (Priority: P3)

A user wants to see a list of their past and ongoing exports, check statuses, and delete old or unwanted exports.

**Why this priority**: Management and visibility of exports enhances user control but is secondary to the core export-and-download flow.

**Independent Test**: Can be tested by creating multiple exports and verifying the list endpoint returns them with correct statuses, and that deleting an export removes both the record and the stored file.

**Acceptance Scenarios**:

1. **Given** a user has created multiple exports over time, **When** they request their export list, **Then** they receive all their exports ordered by creation date (newest first) with current statuses.
2. **Given** a user has a completed export, **When** they delete it, **Then** both the database record and the stored PDF file are removed.
3. **Given** a user has a pending export, **When** they delete it, **Then** the export is cancelled and the record is removed.
4. **Given** a user requests details for a specific export, **When** the export exists and belongs to them, **Then** they receive the full status and metadata for that export.

---

### User Story 4 - Automatic Cleanup of Expired Exports (Priority: P4)

Exported PDFs consume storage space. The system automatically removes exports and their files after 24 hours to prevent unbounded storage growth.

**Why this priority**: This is an operational concern that prevents storage costs from growing indefinitely. It runs autonomously and does not directly affect the user-facing export flow.

**Independent Test**: Can be tested by creating export records with past expiration times and verifying the cleanup process removes them along with their stored files.

**Acceptance Scenarios**:

1. **Given** an export has been in Ready status for more than 24 hours, **When** the daily cleanup process runs, **Then** the export record and its stored file are both deleted.
2. **Given** an export is still within its 24-hour window, **When** the cleanup process runs, **Then** the export is not affected.

---

### User Story 5 - PDF Rendering with Accurate Layout (Priority: P1)

The generated PDF must faithfully represent the notebook's content and visual structure: a cover page with the notebook's color and title, an index of lessons, and lesson pages where modules appear at their exact grid positions with correct styling and building block rendering.

**Why this priority**: The quality of the rendered PDF is essential to the feature's value. A poorly rendered PDF undermines the entire export feature.

**Independent Test**: Can be tested by creating notebooks with known module placements and content types, exporting them, and visually/programmatically verifying the output PDF matches expected layout.

**Acceptance Scenarios**:

1. **Given** a notebook with a cover color, title, instrument, and owner, **When** the PDF is generated, **Then** the first page shows a solid color background matching the cover color with the title, instrument name, owner name, and creation date centered on the page.
2. **Given** a notebook with multiple lessons, **When** the PDF is generated, **Then** an index page appears after the cover listing all lesson titles with their corresponding page numbers, displayed on a dotted paper background.
3. **Given** a lesson page with modules placed at specific grid coordinates, **When** the PDF is generated, **Then** each module appears at its exact grid position with correct width and height, styled according to its notebook module style (background color, border, header).
4. **Given** a module containing chord tablature building blocks, **When** the PDF is generated, **Then** fretboard diagrams are rendered as vector graphics.
5. **Given** a module containing musical notes building blocks, **When** the PDF is generated, **Then** notes appear as circular badges.
6. **Given** a module containing chord progression building blocks, **When** the PDF is generated, **Then** chord names appear as horizontal pill-shaped badges with beat counts.
7. **Given** a multi-page PDF, **When** the PDF is generated, **Then** page numbers appear in the bottom corner, sequentially numbered starting from 1 (the index page).

---

### Edge Cases

- What happens when a user requests an export while another export for the same notebook is already Pending or Processing? The system rejects the request with a 409 conflict.
- What happens when a user tries to download an export that has expired (past 24 hours)? The system returns a 404 indicating the export is no longer available.
- What happens when a user tries to download an export that is still Pending or Processing? The system returns a 404 indicating the export is not yet ready.
- What happens when PDF rendering fails (e.g., corrupted content data)? The export status transitions to Failed and the user receives a real-time failure notification with the export ID.
- What happens when a user tries to access another user's export? The system returns a 403 forbidden response.
- What happens when a notebook has no lessons? The PDF contains only a cover page and an empty index page.
- What happens when the real-time connection is unavailable? The user can fall back to polling the export status endpoint every 3 seconds.
- What happens when a user deletes a notebook that has an active export? The export is cascade-deleted along with the notebook.
- What happens when the background processor crashes while an export is Processing? On restart, the processor detects stale Processing exports and resets them to Pending for automatic re-processing.
- What happens when a user deletes a Pending export? The record is removed. If the background service picks up the ID, it finds no record and skips silently.
- What happens when a user deletes a Processing export? The record is removed. The background service detects the missing record before uploading and aborts gracefully.
- What happens when module content exceeds the module's grid bounds? Content is clipped to the module boundaries.
- What happens when a module has empty ContentJson (`[]`)? The styled module box (background, border, header) is rendered with an empty body.
- What happens when a building block has empty data (empty list, empty text)? The block is skipped during rendering.
- What happens when a chord ID in a building block is not found in the database? A placeholder is rendered using the chord's DisplayName without a fretboard diagram.
- What happens when the notebook title exceeds 200 characters? The PDF filename is truncated to 200 characters; the cover page renders the full title.
- What happens when the export queue is full (50 slots)? If the enqueue does not succeed within 5 seconds, the API returns 503.
- What happens when the notebook is deleted while an export is Processing? The background service handles missing data gracefully and marks the export as Failed.
- What happens when the index page has too many lessons to fit on one page? The index continues onto subsequent pages, all numbered sequentially.

## Clarifications

### Session 2026-03-29

- Q: What happens to exports stuck in Processing status after a server crash/restart? → A: The background processor checks for stale Processing exports on startup and resets them to Pending for re-processing.
- Q: Should users receive a real-time notification when an export fails? → A: Yes, notify on failure too — send a real-time event with the export ID and a failure indicator.
- Q: Should the cleanup service also remove Failed exports, or only expired Ready exports? → A: Cleanup service also deletes Failed exports older than 24 hours (based on creation time).

## Requirements *(mandatory)*

### Functional Requirements

#### Export Lifecycle

- **FR-001**: System MUST allow authenticated users to queue a PDF export for a notebook they own by providing the notebook ID and an optional list of lesson IDs.
- **FR-002**: System MUST reject an export request with a 409 conflict (error code `ACTIVE_EXPORT_EXISTS`) if a Pending or Processing export already exists for the same notebook.
- **FR-003**: System MUST return a 202 Accepted response with the export ID and Pending status when an export is successfully queued.
- **FR-004**: System MUST process queued exports asynchronously in the background, transitioning status from Pending to Processing to Ready (or Failed).
- **FR-011**: System MUST upload the rendered PDF to cloud file storage at a path scoped to the user and export ID.
- **FR-012**: System MUST consider exports expired 24 hours after completion (`CompletedAt + 24h`). No explicit expiration timestamp is stored; expiry is computed at access time and during cleanup.
- **FR-020**: System MUST enforce that only one active export (Pending or Processing) exists per notebook at any given time. Concurrent requests MUST be protected by a database-level unique constraint as a safeguard; if the service-level check passes but the constraint is violated, the system MUST return 409.
- **FR-022**: When lesson IDs are provided, the system MUST export only those lessons. When lesson IDs are null or empty, the system MUST export all lessons in the notebook. Duplicate lesson IDs MUST be silently deduplicated.
- **FR-023**: System MUST validate that all provided lesson IDs belong to the specified notebook and return an error (code `INVALID_LESSON_IDS`, 400) if any do not.

#### PDF Rendering

- **FR-005**: System MUST render the PDF with a cover page containing: solid color background filling the entire page (matching notebook cover color), notebook title, instrument name, owner display name (FirstName LastName), and creation date — all centered horizontally and vertically. The creation date MUST be formatted according to the user's language setting (en or hu). The cover page is unnumbered.
- **FR-006**: System MUST render an index page with a dotted paper background (light gray #CCCCCC dots, 0.5mm diameter, 5mm spacing) containing a localized heading ("Table of Contents" for en, localized equivalent for hu) and a table of contents listing lesson titles with their global page numbers. The index page is numbered as page 1. If the index exceeds one page, it MUST continue onto subsequent pages, all numbered sequentially.
- **FR-007**: System MUST render lesson pages with a dotted paper background (same dot styling as index), with each module positioned at its exact grid coordinates converted to millimeters (1 grid unit = 5mm). Modules on the same page MUST be rendered in ZIndex ascending order (lower ZIndex drawn first).
- **FR-007a**: The PDF physical page size MUST match the notebook's configured PageSize (A4, A5, A6, B5, B6) using the dimensions defined in PageSizeDimensions. No additional margins are applied — the grid maps directly to the page dimensions.
- **FR-008**: System MUST apply notebook module styles (background color, border color/width/radius, header background and text color, body text color, font family) to each rendered module.
- **FR-009**: System MUST render all 10 building block types within modules. PDF rendering MUST achieve visual parity with the web UI as documented in the frontend specification. Music-specific rendering: MusicalNotes as circular badges, ChordProgression as horizontal chord name pills with beat counts, and ChordTablatureGroup as vector fretboard diagrams. Module body text MUST use the FontFamily from the notebook's module style. Cover page and index MUST use a default sans-serif font.
- **FR-009a**: Module content that exceeds the module's grid bounds MUST be clipped. Empty building blocks (empty text, empty lists) MUST be skipped during rendering. Modules with empty ContentJson (`[]`) MUST render as a styled box with an empty body.
- **FR-009b**: If a chord ID referenced in a ChordTablatureGroup or ChordProgression building block is not found in the database, the system MUST render a placeholder using the chord's DisplayName (stored in the building block data) without a fretboard diagram.
- **FR-010**: System MUST number all PDF pages sequentially starting from 1 (the index page), displayed in the bottom-right corner. The cover page is excluded from numbering.

#### Notifications

- **FR-013**: System MUST notify the user in real-time upon successful export completion, providing the export ID and file name.
- **FR-013a**: System MUST notify the user in real-time when an export fails, providing the export ID and an error code indicating the failure type (e.g., `RENDER_FAILED`, `UPLOAD_FAILED`).

#### Access & Download

- **FR-014**: System MUST allow authenticated users to list all their exports, ordered by creation date descending, with status and metadata.
- **FR-015**: System MUST allow authenticated users to retrieve the status and metadata of a specific export they own.
- **FR-016**: System MUST allow authenticated users to download a completed export as a streamed PDF file with a Content-Disposition attachment header. The filename MUST be the notebook title with `.pdf` extension, with characters not in `[a-zA-Z0-9 _-.()]` replaced by `_` and truncated to 200 characters. The download endpoint MUST NOT expose the underlying storage URL.
- **FR-017**: System MUST return a 404 with error code `EXPORT_NOT_READY` if a user attempts to download an export that is not in Ready status. System MUST return a 404 with error code `EXPORT_EXPIRED` if the export's 24-hour window has passed.
- **FR-018**: System MUST allow authenticated users to delete an export they own in any status (Pending, Processing, Ready, or Failed), removing both the database record and the stored file (if any).
- **FR-019**: System MUST return a 403 if a user attempts to access, download, or delete an export belonging to another user.

#### Background Processing & Cleanup

- **FR-024**: System MUST set the export status to Failed if any error occurs during PDF rendering or file upload, without crashing the background processor.
- **FR-025**: On startup, the background processor MUST detect any exports still in Processing status (stale from a previous crash) and reset them to Pending so they are re-processed.
- **FR-026**: If the export queue is full and a new export cannot be enqueued within 5 seconds, the system MUST return a 503 Service Unavailable response.
- **FR-027**: If an export record is deleted while the background service is processing it, the service MUST detect the missing record and abort gracefully without uploading or notifying.
- **FR-028**: If the notebook or its data is deleted while an export is being processed, the background service MUST handle the missing data gracefully and set the export status to Failed.
- **FR-021**: System MUST automatically delete expired Ready exports (older than 24 hours from completion) and their stored files on a daily schedule.
- **FR-021a**: System MUST automatically delete Failed exports older than 24 hours (from creation time) on the same daily schedule.
- **FR-029**: On graceful shutdown, the background processor MUST complete the currently in-progress export before stopping. It MUST NOT pick up new jobs after receiving the shutdown signal.

#### Observability

- **FR-030**: System MUST log export lifecycle events (queued, processing started, completed, failed) with export ID, user ID, and processing duration.

### Key Entities

- **PdfExport**: Represents a single export job. Key attributes: unique identifier, associated notebook, requesting user, status (Pending/Processing/Ready/Failed), optional list of specific lesson IDs, file storage reference, creation timestamp, completion timestamp, expiration timestamp. Belongs to one notebook and one user. Only one active (Pending or Processing) export may exist per notebook at a time.

## Assumptions

- Lessons within the PDF appear in the same order as in the notebook (ordered by creation date ascending).
- When exporting selected lessons, the index page reflects only the selected lessons.
- Failed exports do not have a stored file but their database records are cleaned up after 24 hours (from creation time) by the same daily cleanup service.
- The daily cleanup service runs once every 24 hours and targets exports where the expiration timestamp has passed.
- Bold text spans within building blocks are rendered as bold in the PDF.
- The export pipeline targets notebooks up to 200 pages. Larger notebooks are processed on a best-effort basis with no hard rejection.
- No retry is attempted on file storage upload failure. The export is marked as Failed and the user can re-trigger.
- Index page and cover page text labels are localized according to the user's language setting (en or hu).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can initiate a PDF export and receive a downloadable file within 30 seconds for a notebook with up to 50 pages of content (one LessonPage with up to 6 modules containing mixed building block types per page).
- **SC-002**: 100% of generated PDFs accurately reflect module positions within 1mm tolerance of the calculated grid position (gridUnit * 5mm).
- **SC-003**: Users receive a real-time notification within 2 seconds of export completion when connected to the real-time channel.
- **SC-004**: The system correctly prevents duplicate active exports, rejecting 100% of concurrent export requests for the same notebook.
- **SC-005**: Expired exports are cleaned up within 48 hours of their expiration time (24 hours of creation + up to 24 hours until the next cleanup cycle).
- **SC-006**: All 10 building block types render without missing content, overlapping elements, or incorrectly clipped text within module bounds (content overflow clipping per FR-009a excluded).
- **SC-007**: Users can download their completed exports 100% of the time within the 24-hour availability window.
- **SC-008**: The export pipeline processes jobs without blocking or crashing, recovering gracefully from individual job failures to continue processing subsequent jobs.
