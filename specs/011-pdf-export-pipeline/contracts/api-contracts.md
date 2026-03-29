# API Contracts: PDF Export Pipeline

**Branch**: `011-pdf-export-pipeline` | **Date**: 2026-03-29

## Endpoints

All endpoints require `[Authorize]`. User ID extracted from JWT claims.

---

### POST /exports

Queue a new PDF export.

**Request body**:
```json
{
  "notebookId": "guid",
  "lessonIds": ["guid", "guid"]  // optional — null or omitted = all lessons
}
```

**Responses**:

| Status | Condition | Body |
|--------|-----------|------|
| 202 | Export queued | `{ "exportId": "guid", "status": "Pending" }` |
| 400 | Validation error (missing notebookId) | `{ "errors": { "notebookId": ["..."] } }` |
| 400 | Invalid lessonIds (non-existent or wrong notebook) | `{ "code": "INVALID_LESSON_IDS", "message": "..." }` |
| 403 | Notebook belongs to another user | `{ "code": "FORBIDDEN", "message": "..." }` |
| 404 | Notebook not found | `{ "code": "NOT_FOUND", "message": "..." }` |
| 409 | Active export exists for this notebook | `{ "code": "ACTIVE_EXPORT_EXISTS", "message": "..." }` |

---

### GET /exports

List current user's exports.

**Response 200**:
```json
[
  {
    "id": "guid",
    "notebookId": "guid",
    "notebookTitle": "My Notebook",
    "status": "Ready",
    "createdAt": "2026-03-29T10:00:00Z",
    "completedAt": "2026-03-29T10:00:15Z",
    "lessonIds": null
  }
]
```

Ordered by `createdAt` descending.

---

### GET /exports/{id}

Get a single export's status and metadata.

**Response 200**:
```json
{
  "id": "guid",
  "notebookId": "guid",
  "notebookTitle": "My Notebook",
  "status": "Processing",
  "createdAt": "2026-03-29T10:00:00Z",
  "completedAt": null,
  "lessonIds": ["guid1", "guid2"]
}
```

| Status | Condition | Body |
|--------|-----------|------|
| 200 | Found and owned by user | Export object |
| 403 | Export belongs to another user | Error envelope |
| 404 | Export not found | Error envelope |

---

### GET /exports/{id}/download

Stream the PDF file.

**Response 200**:
- Content-Type: `application/pdf`
- Content-Disposition: `attachment; filename="Notebook Title.pdf"`
- Body: raw PDF byte stream

| Status | Condition | Body |
|--------|-----------|------|
| 200 | Ready and not expired | PDF stream |
| 403 | Export belongs to another user | Error envelope |
| 404 | Export not found, not Ready, or expired | Error envelope |

---

### DELETE /exports/{id}

Cancel or delete an export. Removes DB record and blob (if any).

| Status | Condition | Body |
|--------|-----------|------|
| 204 | Deleted successfully | (empty) |
| 403 | Export belongs to another user | Error envelope |
| 404 | Export not found | Error envelope |

---

## SignalR Events

Hub: `/hubs/notifications` (existing `NotificationHub`)

### PdfReady (existing)

Sent when export completes successfully.

```
PdfReady(exportId: string, fileName: string)
```

### PdfFailed (new)

Sent when export fails.

```
PdfFailed(exportId: string, errorCode: string)
```

Error codes: `RENDER_FAILED`, `UPLOAD_FAILED`

---

## Request/Response DTOs

### CreatePdfExportRequest
```
NotebookId: Guid (required)
LessonIds: List<Guid>? (optional)
```

**Validation** (FluentValidation):
- NotebookId must not be empty
- LessonIds, if provided, must not be an empty list
- Each lessonId must not be Guid.Empty

### PdfExportResponse
```
Id: Guid
NotebookId: Guid
NotebookTitle: string
Status: string (enum name)
CreatedAt: string (ISO 8601)
CompletedAt: string? (ISO 8601)
LessonIds: List<Guid>?
```

### CreatePdfExportResponse
```
ExportId: Guid
Status: string
```

---

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| ACTIVE_EXPORT_EXISTS | 409 | POST /exports when Pending/Processing export exists for notebook |
| INVALID_LESSON_IDS | 400 | POST /exports when lessonIds don't belong to notebook |
| EXPORT_NOT_READY | 404 | GET /exports/{id}/download when status != Ready |
| EXPORT_EXPIRED | 404 | GET /exports/{id}/download when 24h window passed |
