# API Contracts: Lesson & Lesson Page Management

**Feature**: 009-lesson-page-management
**Date**: 2026-03-29

All endpoints require `Authorization: Bearer <accessToken>`.

---

## Lesson Endpoints

### GET /notebooks/{id}/lessons

Returns all lessons for a notebook ordered by CreatedAt ascending.

**Response 200**:
```json
[
  {
    "id": "guid",
    "title": "string",
    "createdAt": "2026-03-29T10:00:00.0000000Z",
    "pageCount": 3
  }
]
```

**Errors**: 404 (notebook not found), 403 (not owner)

---

### POST /notebooks/{id}/lessons

Creates a new lesson with auto-created first page.

**Request**:
```json
{
  "title": "1. Guitar Basics"
}
```

**Validation**: `title` required, max 200 chars.

**Response 201**:
```json
{
  "id": "guid",
  "notebookId": "guid",
  "title": "1. Guitar Basics",
  "createdAt": "2026-03-29T10:00:00.0000000Z",
  "pages": [
    {
      "id": "guid",
      "lessonId": "guid",
      "pageNumber": 1,
      "moduleCount": 0
    }
  ]
}
```

**Errors**: 400 (validation), 404 (notebook not found), 403 (not owner)

---

### GET /lessons/{id}

Returns lesson detail with page list.

**Response 200**:
```json
{
  "id": "guid",
  "notebookId": "guid",
  "title": "1. Guitar Basics",
  "createdAt": "2026-03-29T10:00:00.0000000Z",
  "pages": [
    {
      "id": "guid",
      "lessonId": "guid",
      "pageNumber": 1,
      "moduleCount": 0
    }
  ]
}
```

**Errors**: 404 (lesson not found), 403 (not owner)

---

### PUT /lessons/{id}

Updates lesson title.

**Request**:
```json
{
  "title": "Updated Title"
}
```

**Validation**: `title` required, max 200 chars.

**Response 200**: Same shape as GET /lessons/{id}.

**Errors**: 400 (validation), 404 (lesson not found), 403 (not owner)

---

### DELETE /lessons/{id}

Hard-deletes lesson, cascading to all pages and modules.

**Response 204**: No content.

**Errors**: 404 (lesson not found), 403 (not owner)

---

## Lesson Page Endpoints

### GET /lessons/{id}/pages

Returns all pages for a lesson ordered by PageNumber ascending.

**Response 200**:
```json
[
  {
    "id": "guid",
    "lessonId": "guid",
    "pageNumber": 1,
    "moduleCount": 2
  }
]
```

**Errors**: 404 (lesson not found), 403 (not owner)

---

### POST /lessons/{id}/pages

Adds a new page. Auto-assigns PageNumber = max existing + 1.

**Request**: No body required.

**Response 201** (under 10 pages):
```json
{
  "data": {
    "id": "guid",
    "lessonId": "guid",
    "pageNumber": 2,
    "moduleCount": 0
  },
  "warning": null
}
```

**Response 200** (10+ pages already exist):
```json
{
  "data": {
    "id": "guid",
    "lessonId": "guid",
    "pageNumber": 11,
    "moduleCount": 0
  },
  "warning": "This lesson has reached the recommended maximum of 10 pages."
}
```

**Errors**: 404 (lesson not found), 403 (not owner)

---

### DELETE /lessons/{lessonId}/pages/{pageId}

Hard-deletes page and cascading modules.

**Response 204**: No content.

**Errors**: 404 (lesson/page not found), 403 (not owner), 400 `LAST_PAGE_DELETION` (last remaining page)

---

## Notebook Index Endpoint

### GET /notebooks/{id}/index

Returns auto-generated table of contents.

**Response 200**:
```json
{
  "entries": [
    {
      "lessonId": "guid",
      "title": "1. Guitar Basics",
      "createdAt": "2026-03-29T10:00:00.0000000Z",
      "startPageNumber": 2
    },
    {
      "lessonId": "guid",
      "title": "2. Chord Structures",
      "createdAt": "2026-03-29T11:00:00.0000000Z",
      "startPageNumber": 5
    }
  ]
}
```

**Calculation**: `startPageNumber = 2 + sum(pageCounts of all preceding lessons)`

**Errors**: 404 (notebook not found), 403 (not owner)

---

## Error Response Formats

**Business rule (400)**:
```json
{
  "code": "LAST_PAGE_DELETION",
  "message": "Cannot delete the last remaining page of a lesson.",
  "details": null
}
```

**Forbidden (403)**:
```json
{
  "code": "FORBIDDEN",
  "message": "You do not have access to this resource.",
  "details": null
}
```

**Not found (404)**:
```json
{
  "code": "NOT_FOUND",
  "message": "The requested resource was not found.",
  "details": null
}
```

**Validation (400)**:
```json
{
  "errors": {
    "title": ["Title is required."]
  }
}
```
