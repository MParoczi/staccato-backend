# Data Model: PDF Export Pipeline

**Branch**: `011-pdf-export-pipeline` | **Date**: 2026-03-29

## Existing Entities (No Schema Changes)

### PdfExport

Already exists with all required fields. No migration needed.

| Field | Type | Nullable | Notes |
|-------|------|----------|-------|
| Id | Guid | No | PK, app-generated |
| NotebookId | Guid | No | FK → Notebooks |
| UserId | Guid | No | FK → Users |
| Status | ExportStatus (int) | No | 0=Pending, 1=Processing, 2=Ready, 3=Failed |
| CreatedAt | DateTime | No | UTC |
| CompletedAt | DateTime? | Yes | UTC, set when Ready or Failed |
| BlobReference | string? | Yes | Azure Blob path, set when Ready |
| LessonIdsJson | string? | Yes | JSON-serialized List<Guid>, null = all lessons |

**Relationships**:
- PdfExport → NotebookEntity (many-to-one, cascade delete)
- PdfExport → UserEntity (many-to-one, client cascade)

**Constraints**:
- Unique filtered index: one row per NotebookId where Status IN (Pending, Processing) — enforced at database level
- Only one active export (Pending or Processing) per notebook

### ExportStatus Enum (Existing)

```
Pending = 0     → Created, queued for processing
Processing = 1  → Background worker actively rendering
Ready = 2       → PDF generated and uploaded, downloadable
Failed = 3      → Rendering or upload failed
```

**State transitions**:
```
[Queue]       → Pending
[Pickup]      → Pending → Processing
[Success]     → Processing → Ready
[Failure]     → Processing → Failed
[Recovery]    → Processing → Pending (on server restart)
[Cleanup]     → Ready (24h from CompletedAt) → deleted
[Cleanup]     → Failed (24h from CreatedAt) → deleted
```

## Entities Read for Rendering (No Changes)

These existing entities are loaded read-only by PdfDataLoader:

| Entity | Key Fields for PDF | Source |
|--------|--------------------|--------|
| Notebook | Title, CoverColor, PageSize, InstrumentId, CreatedAt | INotebookRepository |
| User | FirstName, LastName, Language | IUserRepository |
| Instrument | DisplayName, StringCount | IInstrumentRepository |
| Lesson | Title, CreatedAt | ILessonRepository |
| LessonPage | PageNumber | ILessonPageRepository |
| Module | ModuleType, GridX, GridY, GridWidth, GridHeight, ZIndex, ContentJson | IModuleRepository |
| NotebookModuleStyle | ModuleType, StylesJson | INotebookModuleStyleRepository |
| Chord | Name, Positions (ChordPosition[]) | IChordRepository |

## New Interface

### IPdfExportQueue (Domain/Interfaces/)

Abstract channel interface to maintain Domain purity.

```
EnqueueAsync(Guid exportId, CancellationToken ct) → ValueTask
```

## Repository Changes

### IPdfExportRepository — Modified Method

`GetExpiredExportsAsync(DateTime utcNow, CancellationToken ct)` — updated to return:
- Ready exports where CompletedAt is non-null and CompletedAt + 24h <= utcNow
- Failed exports where CreatedAt + 24h <= utcNow

Also needs new method:
`GetByStatusAsync(ExportStatus status, CancellationToken ct)` — for stale Processing recovery on startup.

## Rendering Data Transfer Object

### PdfExportData (Application/Pdf/)

Aggregated data structure passed to the PDF renderer. Not persisted.

```
PdfExportData
├── Notebook: { Title, CoverColor, PageSize, CreatedAt }
├── OwnerName: string (User.FirstName + " " + User.LastName)
├── Language: Language (User.Language — used for date formatting and index heading localization)
├── InstrumentName: string
├── InstrumentStringCount: int
├── Styles: Dictionary<ModuleType, ModuleStyleData>
├── Lessons: List<LessonRenderData>
│   ├── Title: string
│   └── Pages: List<PageRenderData>
│       ├── PageNumber: int
│       └── Modules: List<ModuleRenderData>
│           ├── ModuleType, GridX, GridY, GridWidth, GridHeight, ZIndex
│           └── BuildingBlocks: List<BuildingBlock>
└── Chords: Dictionary<Guid, Chord> (referenced chords by ID)
```
