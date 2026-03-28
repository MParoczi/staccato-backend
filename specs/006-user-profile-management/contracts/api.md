# API Contracts: User Profile Management

**Feature**: 006-user-profile-management
**Base path**: all endpoints require `Authorization: Bearer <access_token>`
**Error envelope** (business rules): `{ "code": "...", "message": "...", "details": {} }`

---

## Shared Shapes

### UserResponse

```jsonc
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "firstName": "Jane",
  "lastName": "Doe",
  "language": "en",                   // Language enum: "en" | "hu"
  "defaultPageSize": "A4",            // PageSize enum or null
  "defaultInstrumentId": "guid|null",
  "avatarUrl": "https://...blob.../avatars/guid|null",
  "scheduledDeletionAt": "2026-04-27T10:00:00Z|null"
}
```

### PresetResponse

```jsonc
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "My Dark Theme",
  "styles": [
    { "moduleType": "Title",    "stylesJson": "{...}" },
    { "moduleType": "Text",     "stylesJson": "{...}" },
    // ... 10 more entries — exactly 12 total
  ]
}
```

---

## Profile Endpoints

### GET /users/me

Returns the authenticated user's full profile.

**Response 200** → `UserResponse`

---

### PUT /users/me

Replaces all editable profile fields. All five body fields must be present; `defaultPageSize` and `defaultInstrumentId` may be null.

**Request body**:
```jsonc
{
  "firstName": "Jane",             // required, 1–100 chars
  "lastName": "Doe",               // required, 0–100 chars (empty string allowed)
  "language": "hu",                // required, "en" | "hu"
  "defaultPageSize": "B5",         // PageSize enum or null
  "defaultInstrumentId": "guid"    // existing instrument ID or null
}
```

**Response 200** → `UserResponse`
**Response 400** → validation errors (missing/invalid fields)
**Response 404** → `{ "code": "INSTRUMENT_NOT_FOUND", "message": "..." }` (unknown defaultInstrumentId)

---

## Account Deletion Endpoints

### DELETE /users/me

Schedules the account for hard deletion in 30 days. Does not immediately delete any data or invalidate tokens.

**Response 204** — no body
**Response 409** → `{ "code": "ACCOUNT_DELETION_ALREADY_SCHEDULED", "message": "..." }`

---

### POST /users/me/cancel-deletion

Cancels a pending deletion by clearing `ScheduledDeletionAt`.

**Response 204** — no body
**Response 400** → `{ "code": "ACCOUNT_DELETION_NOT_SCHEDULED", "message": "..." }`

---

## Avatar Endpoints

### PUT /users/me/avatar

Uploads or replaces the user's avatar. If an existing avatar is present, its blob is deleted before the new one is stored.

**Request**: `multipart/form-data` with one file field named `file`.

Constraints:
- Max size: 2 MB (2,097,152 bytes)
- Allowed MIME types: `image/jpeg`, `image/png`, `image/webp`
- Blob path: `avatars/{userId}` (overwrites on replace)

**Response 200** → `UserResponse` (with updated `avatarUrl`)
**Response 400** → validation errors (missing file / size exceeded / unsupported format)
**Response 500** → RFC 7807 Problem Details (blob storage unavailable; existing avatar unchanged)

---

### DELETE /users/me/avatar

Removes the user's avatar. Blob is deleted from storage and `avatarUrl` is set to null. Idempotent — calling when no avatar is set returns 204 without error.

**Response 204** — no body

---

## Preset Endpoints

### GET /users/me/presets

Returns all of the authenticated user's saved style presets.

**Response 200** → `PresetResponse[]` (empty array if none)

---

### POST /users/me/presets

Creates a new named style preset.

**Request body**:
```jsonc
{
  "name": "My Dark Theme",   // required, 1–100 chars, unique per user
  "styles": [               // required, exactly 12 elements
    { "moduleType": "Title",        "stylesJson": "{...}" },
    { "moduleType": "Breadcrumb",   "stylesJson": "{...}" },
    { "moduleType": "Text",         "stylesJson": "{...}" },
    { "moduleType": "BulletList",   "stylesJson": "{...}" },
    { "moduleType": "NumberedList", "stylesJson": "{...}" },
    { "moduleType": "CheckboxList", "stylesJson": "{...}" },
    { "moduleType": "Table",        "stylesJson": "{...}" },
    { "moduleType": "MusicalNotes", "stylesJson": "{...}" },
    { "moduleType": "ChordProgression",    "stylesJson": "{...}" },
    { "moduleType": "ChordTablatureGroup", "stylesJson": "{...}" },
    { "moduleType": "Date",         "stylesJson": "{...}" },
    { "moduleType": "SectionHeading", "stylesJson": "{...}" }
  ]
}
```

Validation:
- `name`: required, non-empty, max 100 chars, unique per user
- `styles`: exactly 12 elements; each `moduleType` must be a valid `ModuleType` enum value; no duplicate `moduleType` values

**Response 201** → `PresetResponse`
**Response 400** → validation errors (invalid count / unknown moduleType / duplicate moduleType / empty name)
**Response 409** → `{ "code": "DUPLICATE_PRESET_NAME", "message": "..." }`

---

### PUT /users/me/presets/{id}

Updates the name and/or styles of an existing preset. At least one field must be provided.

**Request body**:
```jsonc
{
  "name": "Updated Theme",   // optional, 1–100 chars, unique per user if changed
  "styles": [ ... ]          // optional, same shape and validation as POST
}
```

**Response 200** → `PresetResponse`
**Response 400** → validation errors
**Response 403** → preset belongs to another user
**Response 404** → preset not found
**Response 409** → `{ "code": "DUPLICATE_PRESET_NAME", "message": "..." }`

---

### DELETE /users/me/presets/{id}

Permanently deletes a user preset.

**Response 204** — no body
**Response 403** → preset belongs to another user
**Response 404** → preset not found

---

## Service Method Map

| Endpoint | IUserService method |
|---|---|
| GET /users/me | `GetProfileAsync(userId, ct)` |
| PUT /users/me | `UpdateProfileAsync(userId, firstName, lastName, language, defaultPageSize, defaultInstrumentId, ct)` |
| DELETE /users/me | `ScheduleDeletionAsync(userId, ct)` |
| POST /users/me/cancel-deletion | `CancelDeletionAsync(userId, ct)` |
| PUT /users/me/avatar | `UploadAvatarAsync(userId, stream, contentType, ct)` |
| DELETE /users/me/avatar | `DeleteAvatarAsync(userId, ct)` |
| GET /users/me/presets | `GetPresetsAsync(userId, ct)` |
| POST /users/me/presets | `CreatePresetAsync(userId, name, stylesJson, ct)` |
| PUT /users/me/presets/{id} | `UpdatePresetAsync(userId, presetId, name?, stylesJson?, ct)` |
| DELETE /users/me/presets/{id} | `DeletePresetAsync(userId, presetId, ct)` |
