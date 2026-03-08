# Data Model: Repository Pattern and Unit of Work

**Branch**: `004-repository-uow` | **Date**: 2026-03-08

---

## Overview

This feature introduces no new database tables or entity model changes. All `DbSet<T>` properties required by the 11 repositories already exist in `AppDbContext`. This document defines the **repository interface contracts** — the method signatures, return types, and behavioural constraints that implementations must satisfy.

---

## Repository Interface Summary

| Interface | Extends | Location | Extra Methods |
|---|---|---|---|
| `IRepository<T>` | — | `Domain/Interfaces/Repositories/` | GetByIdAsync, AddAsync, Remove, Update |
| `IUnitOfWork` | — | `Domain/Interfaces/` | CommitAsync |
| `IUserRepository` | `IRepository<User>` | `Domain/Interfaces/Repositories/` | GetByEmailAsync, GetByGoogleIdAsync, GetWithActiveTokensAsync |
| `INotebookRepository` | `IRepository<Notebook>` | `Domain/Interfaces/Repositories/` | GetByUserIdAsync, GetWithStylesAsync |
| `ILessonRepository` | `IRepository<Lesson>` | `Domain/Interfaces/Repositories/` | GetByNotebookIdOrderedByCreatedAtAsync, GetWithPagesAsync |
| `ILessonPageRepository` | `IRepository<LessonPage>` | `Domain/Interfaces/Repositories/` | GetByLessonIdOrderedAsync, GetPageWithModulesAsync |
| `IModuleRepository` | `IRepository<Module>` | `Domain/Interfaces/Repositories/` | GetByPageIdAsync, CheckOverlapAsync |
| `IChordRepository` | `IRepository<Chord>` | `Domain/Interfaces/Repositories/` | SearchAsync |
| `IInstrumentRepository` | `IRepository<Instrument>` | `Domain/Interfaces/Repositories/` | GetAllAsync |
| `IPdfExportRepository` | `IRepository<PdfExport>` | `Domain/Interfaces/Repositories/` | GetActiveExportForNotebookAsync, GetExpiredExportsAsync, GetByUserIdAsync |
| `IRefreshTokenRepository` | `IRepository<RefreshToken>` | `Domain/Interfaces/Repositories/` | GetByTokenAsync, GetActiveByUserIdAsync, RevokeAllForUserAsync |
| `INotebookModuleStyleRepository` | `IRepository<NotebookModuleStyle>` | `Domain/Interfaces/Repositories/` | GetByNotebookIdAsync, GetByNotebookIdAndTypeAsync |
| `IUserSavedPresetRepository` | `IRepository<UserSavedPreset>` | `Domain/Interfaces/Repositories/` | GetByUserIdAsync |

---

## Method Signatures

### IRepository\<T\> (generic base)

```csharp
Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
Task AddAsync(T entity, CancellationToken ct = default);
void Remove(T entity);
void Update(T entity);
```

**Constraints**:
- `GetByIdAsync` returns `null` (never throws) when no record matches.
- `Remove` and `Update` are synchronous — they only stage changes in the EF change tracker.
- No method calls `SaveChanges` or `SaveChangesAsync`.

---

### IUnitOfWork

```csharp
Task<int> CommitAsync(CancellationToken ct = default);
```

**Constraints**:
- Wraps `AppDbContext.SaveChangesAsync(ct)`.
- Returns the number of state entries written.
- Passes `ct` through; an already-cancelled token causes `OperationCanceledException`.

---

### IUserRepository

```csharp
Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default);
Task<(User User, IReadOnlyList<RefreshToken> Tokens)?> GetWithActiveTokensAsync(Guid userId, CancellationToken ct = default);
```

**Constraints**:
- `GetWithActiveTokensAsync` returns `null` when no user with that ID exists.
- "Active token" filter: `IsRevoked == false && ExpiresAt > DateTime.UtcNow`.

---

### INotebookRepository

```csharp
Task<IReadOnlyList<Notebook>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
Task<(Notebook Notebook, IReadOnlyList<NotebookModuleStyle> Styles)?> GetWithStylesAsync(Guid notebookId, CancellationToken ct = default);
```

**Constraints**:
- `GetByUserIdAsync` returns an empty list (never null) when the user has no notebooks.
- `GetWithStylesAsync` returns `null` when no notebook with that ID exists.

---

### ILessonRepository

```csharp
Task<IReadOnlyList<Lesson>> GetByNotebookIdOrderedByCreatedAtAsync(Guid notebookId, CancellationToken ct = default);
Task<(Lesson Lesson, IReadOnlyList<LessonPage> Pages)?> GetWithPagesAsync(Guid lessonId, CancellationToken ct = default);
```

**Constraints**:
- `GetByNotebookIdOrderedByCreatedAtAsync` returns an empty list when the notebook has no lessons.
- Order: ascending by `CreatedAt`.
- `GetWithPagesAsync` returns `null` when no lesson with that ID exists. Pages ordered by `PageNumber` ascending.

---

### ILessonPageRepository

```csharp
Task<IReadOnlyList<LessonPage>> GetByLessonIdOrderedAsync(Guid lessonId, CancellationToken ct = default);
Task<(LessonPage Page, IReadOnlyList<Module> Modules)?> GetPageWithModulesAsync(Guid pageId, CancellationToken ct = default);
```

**Constraints**:
- `GetByLessonIdOrderedAsync` returns an empty list when the lesson has no pages.
- Order: ascending by `PageNumber`.
- `GetPageWithModulesAsync` returns `null` when no page with that ID exists. Modules in the tuple are ordered by `GridY` ascending, then `GridX` ascending.

---

### IModuleRepository

```csharp
Task<IReadOnlyList<Module>> GetByPageIdAsync(Guid pageId, CancellationToken ct = default);
Task<bool> CheckOverlapAsync(Guid pageId, int gridX, int gridY, int gridWidth, int gridHeight, Guid? excludeModuleId = null, CancellationToken ct = default);
```

**Constraints**:
- `GetByPageIdAsync` returns modules ordered by `GridY` ascending, then `GridX` ascending (top-to-bottom, left-to-right). Returns an empty list (never null) when the page has no modules.
- `CheckOverlapAsync` returns `false` when the page has no modules (or only the excluded module).
- Overlap condition: two rectangles overlap when their X and Y ranges both intersect:
  - `existing.GridX < gridX + gridWidth && existing.GridX + existing.GridWidth > gridX`
  - `existing.GridY < gridY + gridHeight && existing.GridY + existing.GridHeight > gridY`
- When `excludeModuleId` is provided, the module with that ID is excluded from the check.

---

### IChordRepository

```csharp
Task<IReadOnlyList<Chord>> SearchAsync(Guid instrumentId, string? root, string? quality, CancellationToken ct = default);
```

**Constraints**:
- `null` for `root` or `quality` means "no filter on that dimension".
- Filtering uses exact string equality (case-sensitive, as stored in seed data).
- All filters MUST be applied as server-side SQL predicates via EF Core LINQ translation — no `.ToList()` before filtering is permitted.
- Returns an empty list (never null) when no chords match the filters.

---

### IInstrumentRepository

```csharp
Task<IReadOnlyList<Instrument>> GetAllAsync(CancellationToken ct = default);
```

**Constraints**:
- Returns all instruments; result is ordered by `Name` ascending.
- Returns an empty list (never null) when no instruments are seeded.

---

### IPdfExportRepository

```csharp
Task<PdfExport?> GetActiveExportForNotebookAsync(Guid notebookId, CancellationToken ct = default);
Task<IReadOnlyList<PdfExport>> GetExpiredExportsAsync(DateTime utcCutoff, CancellationToken ct = default);
Task<IReadOnlyList<PdfExport>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
```

**Constraints**:
- "Active" status = `Pending | Processing | Ready` (not `Failed`). See research D3.
- `GetExpiredExportsAsync` filters: `Status != Failed && CreatedAt < utcCutoff`. The cutoff is caller-supplied; the repository does NOT call `DateTime.UtcNow` internally. Returns an empty list (never null) when no records match.
- `GetByUserIdAsync` returns all exports for the user (any status), ordered by `CreatedAt` descending. Returns an empty list (never null) when the user has no exports.

---

### IRefreshTokenRepository

```csharp
Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
```

**Constraints**:
- `GetByTokenAsync` looks up by the raw token string value.
- `GetActiveByUserIdAsync` filters: `IsRevoked == false && ExpiresAt > DateTime.UtcNow`. Returns an empty list (never null) when no active tokens exist.
- `RevokeAllForUserAsync` uses `ExecuteUpdateAsync` (single bulk SQL UPDATE). This is an intentional exception to the UoW pattern — the call commits immediately and does not participate in the current unit of work. See research D2.

---

### INotebookModuleStyleRepository

```csharp
Task<IReadOnlyList<NotebookModuleStyle>> GetByNotebookIdAsync(Guid notebookId, CancellationToken ct = default);
Task<NotebookModuleStyle?> GetByNotebookIdAndTypeAsync(Guid notebookId, ModuleType moduleType, CancellationToken ct = default);
```

**Constraints**:
- `GetByNotebookIdAsync` returns all 12 styles for a notebook ordered by `ModuleType` (enum integer value ascending). Returns an empty list (never null) when the notebook has no styles.
- `GetByNotebookIdAndTypeAsync` returns `null` when not found.

---

### IUserSavedPresetRepository

```csharp
Task<IReadOnlyList<UserSavedPreset>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
```

**Constraints**:
- Returns an empty list (never null) when the user has no saved presets.

---

## AutoMapper Profile Mappings

File: `Repository/Mapping/EntityToDomainProfile.cs`

All mappings use `ReverseMap()` to support both `Entity → Domain` (reads) and `Domain → Entity` (writes). Navigation properties on EntityModels are ignored automatically (DomainModels have no navigation properties).

| Source (Entity) | Destination (Domain) |
|---|---|
| `UserEntity` | `User` |
| `RefreshTokenEntity` | `RefreshToken` |
| `UserSavedPresetEntity` | `UserSavedPreset` |
| `InstrumentEntity` | `Instrument` |
| `ChordEntity` | `Chord` |
| `NotebookEntity` | `Notebook` |
| `NotebookModuleStyleEntity` | `NotebookModuleStyle` |
| `LessonEntity` | `Lesson` |
| `LessonPageEntity` | `LessonPage` |
| `ModuleEntity` | `Module` |
| `PdfExportEntity` | `PdfExport` |

`SystemStylePresetEntity → SystemStylePreset` is excluded from this feature (no repository required for read-only system presets).

---

## Concrete Implementation Structure

```
Repository/
├── Mapping/
│   └── EntityToDomainProfile.cs
├── Repositories/
│   ├── RepositoryBase.cs               ← abstract generic base
│   ├── UserRepository.cs
│   ├── RefreshTokenRepository.cs
│   ├── UserSavedPresetRepository.cs
│   ├── InstrumentRepository.cs
│   ├── ChordRepository.cs
│   ├── NotebookRepository.cs
│   ├── NotebookModuleStyleRepository.cs
│   ├── LessonRepository.cs
│   ├── LessonPageRepository.cs
│   ├── ModuleRepository.cs
│   └── PdfExportRepository.cs
└── UnitOfWork.cs
```

### RepositoryBase\<TEntity, TDomain\> (abstract)

```
RepositoryBase<TEntity, TDomain>(AppDbContext context, IMapper mapper)
  where TEntity : class

Protected fields: _context, _mapper
Implements: IRepository<TDomain>
Methods: GetByIdAsync, AddAsync, Remove, Update
```

**Method implementation rules**:
- `GetByIdAsync`: use `_context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, ct)` — always issues a SQL query, bypasses the change tracker for predictable behavior.
- `AddAsync`: map domain → entity with `_mapper.Map<TEntity>(entity)`, call `_context.Set<TEntity>().AddAsync(entityModel, ct)`.
- `Remove`: map domain → entity with `_mapper.Map<TEntity>(entity)`, call `_context.Remove(entityModel)`.
- `Update`: map domain → entity with `_mapper.Map<TEntity>(entity)`, call `_context.Update(entityModel)` — marks all scalar properties as modified.

All 11 concrete repositories extend `RepositoryBase<TEntity, TDomain>` and inject `AppDbContext` + `IMapper` via primary constructor, passing them to `base(context, mapper)`.
