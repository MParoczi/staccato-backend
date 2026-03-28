# API Contracts: Notebook CRUD and Style Management

**Branch**: `008-notebook-crud-styles` | **Date**: 2026-03-28

Route prefix: all routes mounted as `/notebooks` or `/presets` (no version prefix).
Auth: all `/notebooks/*` endpoints require `Authorization: Bearer <access-token>` unless marked **public**.

---

## Notebooks

### GET /notebooks

Returns all notebooks belonging to the authenticated user, ordered by `createdAt` ascending. No pagination.

**Auth**: Required.

**Response 200**:
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Guitar Journey 2025",
    "instrumentName": "6-String Guitar",
    "pageSize": "A5",
    "coverColor": "#8B4513",
    "lessonCount": 7,
    "createdAt": "2025-02-01T10:00:00Z",
    "updatedAt": "2025-03-15T14:22:00Z"
  }
]
```

---

### POST /notebooks

Creates a new notebook and atomically creates 12 `NotebookModuleStyle` records.

**Auth**: Required.

**Request body**:
```json
{
  "title": "Guitar Journey 2025",
  "instrumentId": "11111111-1111-1111-1111-111111111111",
  "pageSize": "A5",
  "coverColor": "#8B4513",
  "styles": null
}
```

`styles` is optional. When `null` or omitted, the Colorful system preset is applied. When provided, must contain exactly 12 `ModuleStyleRequest` objects — one per `ModuleType` with no duplicates.

**Validation errors 400**:
```json
{ "errors": { "title": ["'Title' must not be empty."], "coverColor": ["Invalid hex color."] } }
```

**Business error 422** (unknown instrument):
```json
{ "code": "INSTRUMENT_NOT_FOUND", "message": "The specified instrument does not exist.", "details": {} }
```

**Response 201**: `NotebookDetailResponse`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Guitar Journey 2025",
  "instrumentId": "11111111-1111-1111-1111-111111111111",
  "instrumentName": "6-String Guitar",
  "pageSize": "A5",
  "coverColor": "#8B4513",
  "lessonCount": 0,
  "createdAt": "2025-02-01T10:00:00Z",
  "updatedAt": "2025-02-01T10:00:00Z",
  "styles": [ /* 12 ModuleStyleResponse objects */ ]
}
```

---

### GET /notebooks/{id}

Returns full detail of one notebook, including all 12 styles.

**Auth**: Required.

**Response 200**: `NotebookDetailResponse` (same shape as POST 201)

**Error 403**: Notebook belongs to another user.
**Error 404**: Notebook not found.

---

### PUT /notebooks/{id}

Updates `title` and `coverColor`. Both fields are required. Including `instrumentId` or `pageSize` in the request body returns 400.

**Auth**: Required.

**Request body**:
```json
{
  "title": "New Notebook Title",
  "coverColor": "#3E2723"
}
```

**Response 200**: `NotebookDetailResponse` with updated values.

**Error 400**: `title` missing, `coverColor` missing, or disallowed fields present.
**Error 403**: Notebook belongs to another user.
**Error 404**: Notebook not found.

---

### DELETE /notebooks/{id}

Hard-deletes the notebook and all its lessons, pages, and modules (EF cascade).

**Auth**: Required.

**Response 204**: No body.

**Error 403**: Notebook belongs to another user.
**Error 404**: Notebook not found.

---

## Notebook Styles

### GET /notebooks/{id}/styles

Returns all 12 module type style configurations for the notebook.

**Auth**: Required.

**Response 200**: Array of 12 `ModuleStyleResponse` objects, ordered by `ModuleType` enum integer value ascending.

```json
[
  {
    "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "notebookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "moduleType": "Title",
    "backgroundColor": "#FFFFFF",
    "borderColor": "#E0E0E0",
    "borderStyle": "Solid",
    "borderWidth": 1,
    "borderRadius": 0,
    "headerBgColor": "#FFFFFF",
    "headerTextColor": "#212121",
    "bodyTextColor": "#212121",
    "fontFamily": "Default"
  }
  // ... 11 more
]
```

**Error 403**: Notebook belongs to another user.
**Error 404**: Notebook not found.

---

### PUT /notebooks/{id}/styles

Bulk-replaces all 12 module type styles in a single transaction.

**Auth**: Required.

**Request body**: Array of exactly 12 `ModuleStyleRequest` objects.
```json
[
  {
    "moduleType": "Theory",
    "backgroundColor": "#E0F7FA",
    "borderColor": "#00838F",
    "borderStyle": "Solid",
    "borderWidth": 1,
    "borderRadius": 4,
    "headerBgColor": "#00838F",
    "headerTextColor": "#FFFFFF",
    "bodyTextColor": "#212121",
    "fontFamily": "Default"
  }
  // ... 11 more
]
```

**Validation rules**: Array must contain exactly 12 items; all 12 `ModuleType` values must be present with no duplicates.

**Response 200**: Array of 12 updated `ModuleStyleResponse` objects.

**Error 400**: Array count ≠ 12, duplicate module types, or missing module types.
**Error 403**: Notebook belongs to another user.
**Error 404**: Notebook not found.

---

### POST /notebooks/{id}/styles/apply-preset/{presetId}

Applies a system preset or user-saved preset to the notebook, replacing all 12 styles.

**Auth**: Required.

Resolution order: system preset table first → user-saved preset table.

**Response 200**: Array of 12 updated `ModuleStyleResponse` objects.

**Error 403**: Notebook belongs to another user, OR `presetId` refers to a user-saved preset owned by a different user.
**Error 404**: `presetId` not found in either table.

---

## System Presets

### GET /presets

Returns all 5 system style presets, ordered by `displayOrder` ascending.

**Auth**: None required (public endpoint).

**Response 200**:
```json
[
  {
    "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    "name": "Classic",
    "displayOrder": 1,
    "isDefault": false,
    "styles": [ /* 12 ModuleStyleResponse objects */ ]
  },
  {
    "id": "cccccccc-cccc-cccc-cccc-cccccccccccc",
    "name": "Colorful",
    "displayOrder": 2,
    "isDefault": true,
    "styles": [ /* 12 ModuleStyleResponse objects */ ]
  }
  // ... 3 more
]
```

---

## Error Code Reference

| Code | Status | Trigger |
|---|---|---|
| `INSTRUMENT_NOT_FOUND` | 422 | `instrumentId` in POST /notebooks not found in seeded instruments |
| `NOTEBOOK_INSTRUMENT_IMMUTABLE` | 400 | `instrumentId` included in PUT /notebooks/{id} body |
| `NOTEBOOK_PAGE_SIZE_IMMUTABLE` | 400 | `pageSize` included in PUT /notebooks/{id} body |
| `FORBIDDEN` | 403 | Ownership violation on any notebook or user-saved preset |
| `NOT_FOUND` | 404 | Notebook or preset not found |
