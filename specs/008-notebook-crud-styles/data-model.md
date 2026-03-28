# Data Model: Notebook CRUD and Style Management

**Branch**: `008-notebook-crud-styles` | **Date**: 2026-03-28

---

## Entities Touched (Existing — Modified)

### NotebookEntity *(EntityModels/Entities/NotebookEntity.cs)*

**Change**: Add `CoverColor` property.

```
NotebookEntity
├── Id              : Guid          (PK, app-generated)
├── UserId          : Guid          (FK → UserEntity, cascade delete)
├── Title           : string        (required, nvarchar(max))
├── InstrumentId    : Guid          (FK → InstrumentEntity, restrict delete)
├── PageSize        : PageSize enum (required)
├── CoverColor      : string        (NEW — required, nvarchar(7), e.g. "#8B4513")
├── CreatedAt       : DateTime      (UTC)
├── UpdatedAt       : DateTime      (UTC)
--- navigation ---
├── User            : UserEntity
├── Instrument      : InstrumentEntity
├── Lessons         : ICollection<LessonEntity>
├── ModuleStyles    : ICollection<NotebookModuleStyleEntity>
└── PdfExports      : ICollection<PdfExportEntity>
```

**Migration required**: `AddNotebookCoverColor` — adds `CoverColor nvarchar(7) NOT NULL DEFAULT '#000000'`

---

### SystemStylePresetEntity *(EntityModels/Entities/SystemStylePresetEntity.cs)*

**Change**: Add `: IEntity` to the class declaration. No structural change — the `Id` property already satisfies the interface.

---

## Entities Touched (Existing — Read-Only During This Feature)

### NotebookModuleStyleEntity *(EntityModels/Entities/NotebookModuleStyleEntity.cs)* — no changes

```
NotebookModuleStyleEntity
├── Id          : Guid          (PK, app-generated)
├── NotebookId  : Guid          (FK → NotebookEntity, cascade delete)
├── ModuleType  : ModuleType    (enum, required)
├── StylesJson  : string        (nvarchar(max), required — JSON object of style properties)
└── Notebook    : NotebookEntity (navigation)

Unique index: (NotebookId, ModuleType)
```

**StylesJson schema** (per-record, single module type):
```json
{
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
```

---

### SystemStylePresetEntity *(read)*

```
SystemStylePresetEntity
├── Id           : Guid    (PK, app-generated at seed time)
├── Name         : string  (required, nvarchar(200))
├── DisplayOrder : int     (required — ordering for GET /presets)
├── IsDefault    : bool    (required — exactly one preset has IsDefault=true → Colorful)
└── StylesJson   : string  (nvarchar(max) — JSON array of 12 style objects, each with moduleType + properties)
```

**StylesJson schema** (array of 12):
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

**Seeder fix**: Change Classic `IsDefault` from `true` → `false`; change Colorful `IsDefault` from `false` → `true`.

---

### UserSavedPresetEntity *(read)*

```
UserSavedPresetEntity
├── Id          : Guid    (PK)
├── UserId      : Guid    (FK → UserEntity)
├── Name        : string
└── StylesJson  : string  (same array format as SystemStylePresetEntity.StylesJson)
```

---

## New Domain Models *(DomainModels/Models/)*

### Notebook *(existing — add CoverColor)*

```
Notebook
├── Id           : Guid
├── UserId       : Guid
├── Title        : string
├── InstrumentId : Guid    (init-only — immutable after creation)
├── PageSize     : PageSize (init-only — immutable after creation)
├── CoverColor   : string  (NEW)
├── CreatedAt    : DateTime
└── UpdatedAt    : DateTime
```

### NotebookSummary *(NEW)*

Lightweight projection for list view. No navigation properties.

```
NotebookSummary
├── Id             : Guid
├── UserId         : Guid
├── Title          : string
├── InstrumentName : string
├── PageSize       : PageSize
├── CoverColor     : string
├── LessonCount    : int
├── CreatedAt      : DateTime
└── UpdatedAt      : DateTime
```

---

## New API Models *(ApiModels/Notebooks/)*

### CreateNotebookRequest

```
CreateNotebookRequest
├── Title        : string                   (required, max 200 chars)
├── InstrumentId : Guid                     (required)
├── PageSize     : string                   (required, must parse to PageSize enum)
├── CoverColor   : string                   (required, valid hex #RRGGBB or #RGB)
└── Styles       : List<ModuleStyleRequest>? (optional; if provided, must be exactly 12, one per ModuleType)
```

**Validator rules:**
- `Title`: `NotEmpty`, `MaximumLength(200)`
- `InstrumentId`: `NotEmpty`
- `PageSize`: `NotEmpty`, `Must(v => Enum.TryParse<PageSize>(v, out _))`
- `CoverColor`: `NotEmpty`, `Matches(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$")`
- `Styles`: if not null: `Must(s => s.Count == 12)`, `Must(s => s.Select(x => x.ModuleType).Distinct().Count() == 12)` (all 12 ModuleType values present)

### UpdateNotebookRequest

```
UpdateNotebookRequest
├── Title        : string  (required, max 200 chars)
├── CoverColor   : string  (required, valid hex)
├── InstrumentId : Guid?   (must be null — present only to trigger rejection)
└── PageSize     : string? (must be null — present only to trigger rejection)
```

**Validator rules:**
- `Title`: `NotEmpty`, `MaximumLength(200)`
- `CoverColor`: `NotEmpty`, `Matches(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$")`
- `InstrumentId`: `Must(v => v == null).WithErrorCode("NOTEBOOK_INSTRUMENT_IMMUTABLE")`
- `PageSize`: `Must(v => v == null).WithErrorCode("NOTEBOOK_PAGE_SIZE_IMMUTABLE")`

### ModuleStyleRequest

```
ModuleStyleRequest
├── ModuleType      : string (required, must parse to ModuleType enum)
├── BackgroundColor : string (required, valid hex)
├── BorderColor     : string (required, valid hex)
├── BorderStyle     : string (required, must parse to BorderStyle enum)
├── BorderWidth     : int    (required, >= 0, <= 20)
├── BorderRadius    : int    (required, >= 0, <= 50)
├── HeaderBgColor   : string (required, valid hex)
├── HeaderTextColor : string (required, valid hex)
├── BodyTextColor   : string (required, valid hex)
└── FontFamily      : string (required, must parse to FontFamily enum)
```

### ModuleStyleResponse

```
ModuleStyleResponse
├── Id              : Guid
├── NotebookId      : Guid
├── ModuleType      : string
├── BackgroundColor : string
├── BorderColor     : string
├── BorderStyle     : string
├── BorderWidth     : int
├── BorderRadius    : int
├── HeaderBgColor   : string
├── HeaderTextColor : string
├── BodyTextColor   : string
└── FontFamily      : string
```

### NotebookSummaryResponse

```
NotebookSummaryResponse
├── Id             : Guid
├── Title          : string
├── InstrumentName : string
├── PageSize       : string
├── CoverColor     : string
├── LessonCount    : int
├── CreatedAt      : string (ISO 8601 UTC)
└── UpdatedAt      : string (ISO 8601 UTC)
```

### NotebookDetailResponse

```
NotebookDetailResponse
├── Id             : Guid
├── Title          : string
├── InstrumentId   : Guid
├── InstrumentName : string
├── PageSize       : string
├── CoverColor     : string
├── LessonCount    : int
├── CreatedAt      : string (ISO 8601 UTC)
├── UpdatedAt      : string (ISO 8601 UTC)
└── Styles         : List<ModuleStyleResponse>  (always 12 items)
```

### SystemStylePresetResponse *(to be added in ApiModels/Presets/ or reused in existing area)*

```
SystemStylePresetResponse
├── Id           : Guid
├── Name         : string
├── DisplayOrder : int
├── IsDefault    : bool
└── Styles       : List<ModuleStyleResponse>  (12 items, deserialized from StylesJson)
```

> **Note**: `ModuleStyleResponse` is reused for preset styles. Since preset entries have no notebook-specific identity, `Id` and `NotebookId` are set to `Guid.Empty` in the AutoMapper converter for `SystemStylePreset → SystemStylePresetResponse`. Frontend consumers must treat `Guid.Empty` as a sentinel meaning "not applicable".

---

## Repository Interface Changes

### INotebookRepository *(Domain/Interfaces/Repositories/)*

**Change**: Update `GetByUserIdAsync` return type from `IReadOnlyList<Notebook>` → `IReadOnlyList<NotebookSummary>`. Add `CountByUserIdAsync` for ownership validation.

```csharp
Task<IReadOnlyList<NotebookSummary>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
Task<(Notebook Notebook, IReadOnlyList<NotebookModuleStyle> Styles)?> GetWithStylesAsync(Guid notebookId, CancellationToken ct = default);
// GetByIdAsync inherited from IRepository<Notebook>
```

### ISystemStylePresetRepository *(NEW — Domain/Interfaces/Repositories/)*

```csharp
public interface ISystemStylePresetRepository : IRepository<SystemStylePreset>
{
    Task<IReadOnlyList<SystemStylePreset>> GetAllAsync(CancellationToken ct = default);
    Task<SystemStylePreset?> GetDefaultAsync(CancellationToken ct = default);
}
```

> `GetDefaultAsync` returns the preset where `IsDefault = true`. Used by `NotebookService.CreateAsync` when no explicit styles are provided — avoids loading all 5 presets when only the default is needed.

---

## Validation Rules Summary

| Field | Rule |
|---|---|
| Notebook.Title | Required, max 200 characters |
| Notebook.CoverColor | Required, matches `^#([0-9A-Fa-f]{3}\|[0-9A-Fa-f]{6})$` |
| Notebook.InstrumentId | Required, must reference a seeded instrument (validated in service, 422 INSTRUMENT_NOT_FOUND) |
| Notebook.PageSize | Required, must be one of: A4, A5, A6, B5, B6 |
| Styles (on creation) | If provided: exactly 12 items, one per ModuleType, no duplicates |
| ModuleStyleRequest.ModuleType | Required, must parse to ModuleType enum |
| ModuleStyleRequest color fields | Required, valid hex |
| ModuleStyleRequest enum fields | Required, valid BorderStyle / FontFamily values |
| ModuleStyleRequest.BorderWidth | Required, integer, 0–20 (inclusive) |
| ModuleStyleRequest.BorderRadius | Required, integer, 0–50 (inclusive) |
| Bulk styles (PUT) | Exactly 12 items, one per ModuleType, no duplicates |

---

## State Transitions

```
[Notebook created] → styles auto-applied from Colorful preset (or explicit styles)
[PUT /notebooks/{id}] → only Title and CoverColor change; UpdatedAt refreshed
[PUT /notebooks/{id}/styles] → all 12 style records replaced atomically; Notebook.UpdatedAt refreshed
[POST /notebooks/{id}/styles/apply-preset/{presetId}] → all 12 style records updated atomically; Notebook.UpdatedAt refreshed
[DELETE /notebooks/{id}] → notebook + lessons + pages + modules + styles hard-deleted (EF cascade)
```
