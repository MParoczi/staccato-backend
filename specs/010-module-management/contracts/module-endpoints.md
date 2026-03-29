# API Contracts: Module Endpoints

**Feature**: 010-module-management
**Date**: 2026-03-29

All endpoints require JWT Bearer authentication (`[Authorize]`).

---

## GET /pages/{pageId}/modules

**Description**: Retrieve all modules on a lesson page.

**Path Parameters**:
- `pageId` (Guid) — Lesson page ID

**Response 200**:
```json
[
  {
    "id": "guid",
    "lessonPageId": "guid",
    "moduleType": "Theory",
    "gridX": 2,
    "gridY": 5,
    "gridWidth": 18,
    "gridHeight": 10,
    "zIndex": 0,
    "content": []
  }
]
```

**Error Responses**:
- `404` — Page not found
- `403` — Page belongs to another user

---

## POST /pages/{pageId}/modules

**Description**: Create a new module on a lesson page.

**Path Parameters**:
- `pageId` (Guid) — Lesson page ID

**Request Body**:
```json
{
  "moduleType": "Theory",
  "gridX": 2,
  "gridY": 5,
  "gridWidth": 18,
  "gridHeight": 10,
  "zIndex": 0,
  "content": []
}
```

**Validation (FluentValidation)**:
- `moduleType`: required, must be valid ModuleType enum value
- `gridX`: required, >= 0
- `gridY`: required, >= 0
- `gridWidth`: required, >= 1
- `gridHeight`: required, >= 1
- `zIndex`: required, >= 0
- `content`: required, must be empty array `[]`

**Response 201**: Created module (same shape as GET response item).

**Error Responses**:
- `400` — FluentValidation errors (field-level)
- `404` — Page not found
- `403` — Page belongs to another user
- `409` — `DUPLICATE_TITLE_MODULE` (Title already exists in lesson)
- `422` — `MODULE_TOO_SMALL`, `MODULE_OUT_OF_BOUNDS`, `MODULE_OVERLAP`, `BREADCRUMB_CONTENT_NOT_EMPTY`

---

## PUT /modules/{moduleId}

**Description**: Full update of a module including content.

**Path Parameters**:
- `moduleId` (Guid) — Module ID

**Request Body**:
```json
{
  "moduleType": "Theory",
  "gridX": 2,
  "gridY": 5,
  "gridWidth": 18,
  "gridHeight": 12,
  "zIndex": 0,
  "content": [
    {
      "type": "SectionHeading",
      "spans": [{ "text": "Section Title", "bold": false }]
    },
    {
      "type": "Text",
      "spans": [{ "text": "Paragraph text.", "bold": false }]
    }
  ]
}
```

**Validation (FluentValidation)**:
- `moduleType`: required, must be valid ModuleType enum value
- `gridX`: required, >= 0
- `gridY`: required, >= 0
- `gridWidth`: required, >= 1
- `gridHeight`: required, >= 1
- `zIndex`: required, >= 0
- `content`: required (array, can be empty or populated)

**Response 200**: Updated module.

**Error Responses**:
- `400` — FluentValidation errors OR `MODULE_TYPE_IMMUTABLE` (moduleType mismatch) OR `MALFORMED_CONTENT_JSON` (unparseable content)
- `404` — Module not found
- `403` — Module belongs to another user
- `422` — `MODULE_TOO_SMALL`, `MODULE_OUT_OF_BOUNDS`, `MODULE_OVERLAP`, `INVALID_BUILDING_BLOCK`, `BREADCRUMB_CONTENT_NOT_EMPTY`

---

## DELETE /modules/{moduleId}

**Description**: Permanently delete a module.

**Path Parameters**:
- `moduleId` (Guid) — Module ID

**Response 204**: No content.

**Error Responses**:
- `404` — Module not found
- `403` — Module belongs to another user

---

## PATCH /modules/{moduleId}/layout

**Description**: Update only layout properties (position, size, z-index). Used by frontend for debounced drag/resize auto-save.

**Path Parameters**:
- `moduleId` (Guid) — Module ID

**Request Body**:
```json
{
  "gridX": 4,
  "gridY": 7,
  "gridWidth": 20,
  "gridHeight": 12,
  "zIndex": 0
}
```

**Validation (FluentValidation)**:
- `gridX`: required, >= 0
- `gridY`: required, >= 0
- `gridWidth`: required, >= 1
- `gridHeight`: required, >= 1
- `zIndex`: required, >= 0

**Response 200**: Updated module (full module shape including content).

**Error Responses**:
- `400` — FluentValidation errors
- `404` — Module not found
- `403` — Module belongs to another user
- `422` — `MODULE_TOO_SMALL`, `MODULE_OUT_OF_BOUNDS`, `MODULE_OVERLAP`

---

## Response Model: ModuleResponse

```json
{
  "id": "Guid",
  "lessonPageId": "Guid",
  "moduleType": "string (enum name)",
  "gridX": "int",
  "gridY": "int",
  "gridWidth": "int",
  "gridHeight": "int",
  "zIndex": "int",
  "content": "array (deserialized BuildingBlock[])"
}
```

The `content` field is the deserialized JSON array from `ContentJson`, returned as a structured array (not a raw JSON string). The `moduleType` field is returned as the string name of the enum value (e.g., "Theory", "Practice").

---

## Error Envelope (Business Rules)

```json
{
  "code": "MODULE_OVERLAP",
  "message": "The module overlaps with an existing module on this page.",
  "details": {}
}
```

Error codes specific to this feature:
- `MODULE_TOO_SMALL` (422)
- `MODULE_OUT_OF_BOUNDS` (422)
- `MODULE_OVERLAP` (422)
- `INVALID_BUILDING_BLOCK` (422)
- `BREADCRUMB_CONTENT_NOT_EMPTY` (422)
- `DUPLICATE_TITLE_MODULE` (409)
- `MODULE_TYPE_IMMUTABLE` (400)
- `MALFORMED_CONTENT_JSON` (400)
