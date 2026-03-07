# Data Model: EF Core Entity Models and Database Persistence

**Feature**: `003-ef-core-persistence`
**Date**: 2026-03-07

---

## Entity Relationship Overview

```
InstrumentEntity (seeded, immutable)
    └── ChordEntity[] (seeded, immutable, Restrict delete)

SystemStylePresetEntity (seeded, no FKs)

UserEntity
    ├── RefreshTokenEntity[] (cascade)
    ├── UserSavedPresetEntity[] (cascade)
    ├── NotebookEntity[] (cascade)
    │   ├── NotebookModuleStyleEntity × 12 (cascade)
    │   ├── LessonEntity[] (cascade)
    │   │   └── LessonPageEntity[] (cascade)
    │   │       └── ModuleEntity[] (cascade)
    │   └── PdfExportEntity[] (cascade)
    └── PdfExportEntity[] (client-cascade via UserId — also FK to Notebook with cascade)
```

---

## Entities

### UserEntity
**Table**: `Users`
**Project**: `EntityModels/Entities/UserEntity.cs`
**Namespace**: `EntityModels.Entities`

| Property | Type | Column | Constraints |
|---|---|---|---|
| Id | Guid | Id | PK |
| Email | string | Email | NOT NULL, max 256, unique index |
| PasswordHash | string? | PasswordHash | NULL |
| GoogleId | string? | GoogleId | NULL, filtered unique index (non-null only) |
| FirstName | string | FirstName | NOT NULL, max 100 |
| LastName | string | LastName | NOT NULL, max 100 |
| AvatarUrl | string? | AvatarUrl | NULL, nvarchar(max) |
| CreatedAt | DateTime | CreatedAt | NOT NULL |
| ScheduledDeletionAt | DateTime? | ScheduledDeletionAt | NULL |
| Language | Language (int) | Language | NOT NULL |

**Indexes**:
- `IX_Users_Email` — unique
- `IX_Users_GoogleId` — unique, filtered `WHERE [GoogleId] IS NOT NULL`

**Navigation**:
- `ICollection<NotebookEntity> Notebooks`
- `ICollection<RefreshTokenEntity> RefreshTokens`
- `ICollection<UserSavedPresetEntity> UserSavedPresets`
- `ICollection<PdfExportEntity> PdfExports`

---

### RefreshTokenEntity
**Table**: `RefreshTokens`
**Project**: `EntityModels/Entities/RefreshTokenEntity.cs`

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| Token | string | NOT NULL, nvarchar(max), unique index |
| UserId | Guid | NOT NULL, FK → Users (cascade) |
| ExpiresAt | DateTime | NOT NULL |
| CreatedAt | DateTime | NOT NULL |
| IsRevoked | bool | NOT NULL |

**Navigation**: `UserEntity User`

---

### UserSavedPresetEntity
**Table**: `UserSavedPresets`
**Project**: `EntityModels/Entities/UserSavedPresetEntity.cs`

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| UserId | Guid | NOT NULL, FK → Users (cascade) |
| Name | string | NOT NULL, max 200 |
| StylesJson | string | NOT NULL, nvarchar(max) — JSON array of 12 module-type style objects |

**Navigation**: `UserEntity User`

---

### SystemStylePresetEntity
**Table**: `SystemStylePresets`
**Project**: `EntityModels/Entities/SystemStylePresetEntity.cs`

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| Name | string | NOT NULL, max 200 |
| DisplayOrder | int | NOT NULL |
| IsDefault | bool | NOT NULL |
| StylesJson | string | NOT NULL, nvarchar(max) — JSON array of 12 module-type style objects |

No FKs. No navigation properties.

---

### InstrumentEntity
**Table**: `Instruments`
**Project**: `EntityModels/Entities/InstrumentEntity.cs`

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| Key | InstrumentKey (int) | NOT NULL, unique index |
| DisplayName | string | NOT NULL, max 200 |
| StringCount | int | NOT NULL |

**Indexes**: `IX_Instruments_Key` — unique (prevents duplicate instrument seeding)

**Navigation**: `ICollection<ChordEntity> Chords`

---

### ChordEntity
**Table**: `Chords`
**Project**: `EntityModels/Entities/ChordEntity.cs`

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| InstrumentId | Guid | NOT NULL, FK → Instruments (Restrict) |
| Name | string | NOT NULL, max 200 |
| Suffix | string | NOT NULL, max 200 |
| PositionsJson | string | NOT NULL, nvarchar(max) — JSON array of ChordPosition objects |

**Navigation**: `InstrumentEntity Instrument`

---

### NotebookEntity
**Table**: `Notebooks`
**Project**: `EntityModels/Entities/NotebookEntity.cs`

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| UserId | Guid | NOT NULL, FK → Users (cascade) |
| Title | string | NOT NULL, nvarchar(max) |
| InstrumentId | Guid | NOT NULL, FK → Instruments (no delete action — instrument is immutable) |
| PageSize | PageSize (int) | NOT NULL |
| CreatedAt | DateTime | NOT NULL |
| UpdatedAt | DateTime | NOT NULL |

**Navigation**:
- `UserEntity User`
- `InstrumentEntity Instrument`
- `ICollection<LessonEntity> Lessons`
- `ICollection<NotebookModuleStyleEntity> ModuleStyles`
- `ICollection<PdfExportEntity> PdfExports`

---

### NotebookModuleStyleEntity
**Table**: `NotebookModuleStyles`
**Project**: `EntityModels/Entities/NotebookModuleStyleEntity.cs`

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| NotebookId | Guid | NOT NULL, FK → Notebooks (cascade) |
| ModuleType | ModuleType (int) | NOT NULL |
| StylesJson | string | NOT NULL, nvarchar(max) — JSON object with 9 style fields |

**Indexes**: `IX_NotebookModuleStyles_NotebookId_ModuleType` — unique composite

**Navigation**: `NotebookEntity Notebook`

---

### LessonEntity
**Table**: `Lessons`
**Project**: `EntityModels/Entities/LessonEntity.cs`

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| NotebookId | Guid | NOT NULL, FK → Notebooks (cascade) |
| Title | string | NOT NULL, nvarchar(max) |
| CreatedAt | DateTime | NOT NULL |
| UpdatedAt | DateTime | NOT NULL |

**Navigation**:
- `NotebookEntity Notebook`
- `ICollection<LessonPageEntity> LessonPages`

---

### LessonPageEntity
**Table**: `LessonPages`
**Project**: `EntityModels/Entities/LessonPageEntity.cs`

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| LessonId | Guid | NOT NULL, FK → Lessons (cascade) |
| PageNumber | int | NOT NULL |

**Navigation**:
- `LessonEntity Lesson`
- `ICollection<ModuleEntity> Modules`

---

### ModuleEntity
**Table**: `Modules`
**Project**: `EntityModels/Entities/ModuleEntity.cs`

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| LessonPageId | Guid | NOT NULL, FK → LessonPages (cascade) |
| ModuleType | ModuleType (int) | NOT NULL |
| GridX | int | NOT NULL |
| GridY | int | NOT NULL |
| GridWidth | int | NOT NULL |
| GridHeight | int | NOT NULL |
| ContentJson | string | NOT NULL, nvarchar(max) — JSON array of BuildingBlock objects |

**Navigation**: `LessonPageEntity LessonPage`

---

### PdfExportEntity
**Table**: `PdfExports`
**Project**: `EntityModels/Entities/PdfExportEntity.cs`

| Property | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| NotebookId | Guid | NOT NULL, FK → Notebooks (cascade) |
| UserId | Guid | NOT NULL, FK → Users (client-cascade — see FR-042) |
| Status | ExportStatus (int) | NOT NULL |
| CreatedAt | DateTime | NOT NULL |
| CompletedAt | DateTime? | NULL |
| BlobReference | string? | NULL, nvarchar(max) |
| LessonIdsJson | string? | NULL, nvarchar(max) — JSON array of Guid strings; NULL = whole notebook |

**Indexes**: `IX_PdfExports_NotebookId_Active` — unique, filtered `WHERE [Status] = 0 OR [Status] = 1` (Pending=0, Processing=1)

**Navigation**:
- `NotebookEntity Notebook`
- `UserEntity User`

---

## Domain Model Update Required

### PdfExport.cs (DomainModels/Models/PdfExport.cs)
Add `List<Guid>? LessonIds` property (null = export entire notebook):
```csharp
public List<Guid>? LessonIds { get; set; }
```

---

## JSON Column Schemas

### NotebookModuleStyleEntity.StylesJson / NotebookModuleStyle applied style
```json
{
  "backgroundColor": "#FFFFFF",
  "borderColor": "#E0E0E0",
  "borderStyle": "Solid",
  "borderWidth": 1,
  "borderRadius": 4,
  "headerBgColor": "#F5F5F5",
  "headerTextColor": "#212121",
  "bodyTextColor": "#212121",
  "fontFamily": "Default"
}
```

### SystemStylePresetEntity.StylesJson / UserSavedPresetEntity.StylesJson
```json
[
  {
    "moduleType": "Title",
    "backgroundColor": "#FFFFFF",
    "borderColor": "#E0E0E0",
    "borderStyle": "None",
    "borderWidth": 0,
    "borderRadius": 0,
    "headerBgColor": "#FFFFFF",
    "headerTextColor": "#212121",
    "bodyTextColor": "#212121",
    "fontFamily": "Default"
  }
  // ... 11 more entries (one per ModuleType)
]
```

### ChordEntity.PositionsJson
```json
[
  {
    "label": "Open position",
    "baseFret": 1,
    "barre": null,
    "strings": [
      { "string": 1, "state": "open",    "fret": null, "finger": null },
      { "string": 2, "state": "fretted", "fret": 2,    "finger": 1   },
      { "string": 3, "state": "fretted", "fret": 2,    "finger": 2   },
      { "string": 4, "state": "fretted", "fret": 2,    "finger": 3   },
      { "string": 5, "state": "open",    "fret": null, "finger": null },
      { "string": 6, "state": "open",    "fret": null, "finger": null }
    ]
  }
]
```

### PdfExportEntity.LessonIdsJson
```json
["3fa85f64-5717-4562-b3fc-2c963f66afa6", "..."]
```
SQL NULL = export covers entire notebook. Never store `"null"` or `"[]"` for the whole-notebook case.

---

## Seed Data Summary

### Instruments (7 rows)
| InstrumentKey | DisplayName | StringCount |
|---|---|---|
| Guitar6String | 6-String Guitar | 6 |
| Guitar7String | 7-String Guitar | 7 |
| Bass4String | 4-String Bass | 4 |
| Bass5String | 5-String Bass | 5 |
| Ukulele4String | Ukulele | 4 |
| Banjo4String | 4-String Banjo | 4 |
| Banjo5String | 5-String Banjo | 5 |

### Chords (loaded from guitar_chords.json)
- Instrument: Guitar6String only
- Coverage: 12 root notes × 17 qualities minimum = ~204 entries
- 2–3 voicings (positions) per chord entry

### System Style Presets (5 rows)
| Name | DisplayOrder | IsDefault |
|---|---|---|
| Classic | 1 | true |
| Colorful | 2 | false |
| Dark | 3 | false |
| Minimal | 4 | false |
| Pastel | 5 | false |

Color details documented in `research.md` Decision 4.
