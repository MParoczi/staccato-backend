# Quickstart: Repository Pattern and Unit of Work

**Branch**: `004-repository-uow` | **Date**: 2026-03-08

---

## What Gets Built

This feature delivers two layers:

1. **Domain layer** — 11 repository interfaces + `IUnitOfWork` in `Domain/Interfaces/`
2. **Repository layer** — 11 concrete implementations + `UnitOfWork` + AutoMapper profile in `Repository/`

No new controllers, services, or database tables are introduced.

---

## Project Reference Requirements

Verify these references exist before starting:

| Project | Must reference |
|---|---|
| `Domain` | `DomainModels` only |
| `Repository` | `Domain`, `EntityModels`, `Persistence` |
| `Application` | `Repository` (already present) |
| `Tests` | All projects |

---

## Exception Subclasses to Add

`BusinessException` already exists at `Domain/Exceptions/BusinessException.cs`. Add these subclasses alongside it:

| Class | StatusCode | Usage |
|---|---|---|
| `NotFoundException` | 404 | Entity not found by ID (ownership check returns 403, not 404) |
| `ConflictException` | 409 | Duplicate resource (e.g., `ACTIVE_EXPORT_EXISTS`) |
| `ForbiddenException` | 403 | Resource belongs to another user |
| `ValidationException` | 422 | Business rule violation (e.g., `MODULE_OVERLAP`) |

---

## Step-by-Step Implementation Order

### Step 1 — Domain: Base interfaces

1. Create `Domain/Interfaces/Repositories/IRepository.cs`
2. Create `Domain/Interfaces/IUnitOfWork.cs`

### Step 2 — Domain: Specific repository interfaces

Create one file per interface in `Domain/Interfaces/Repositories/`. See `contracts/domain-interfaces.md` for full method signatures with XML docs.

Order (least → most dependent):
1. `IInstrumentRepository`
2. `IChordRepository`
3. `IUserRepository`
4. `IRefreshTokenRepository`
5. `IUserSavedPresetRepository`
6. `INotebookRepository`
7. `INotebookModuleStyleRepository`
8. `ILessonRepository`
9. `ILessonPageRepository`
10. `IModuleRepository`
11. `IPdfExportRepository`

### Step 3 — Domain: Exception subclasses

Add to `Domain/Exceptions/`:
- `NotFoundException.cs`
- `ConflictException.cs`
- `ForbiddenException.cs`
- `ValidationException.cs`

### Step 4 — Repository: AutoMapper profile

Create `Repository/Mapping/EntityToDomainProfile.cs`. Configure `CreateMap<TEntity, TDomain>().ReverseMap()` for all 11 entity pairs. See `data-model.md` for the full mapping table.

### Step 5 — Repository: Base class

Create `Repository/Repositories/RepositoryBase.cs`:
- Abstract class generic on `TEntity` (EF entity) and `TDomain` (domain model)
- Primary constructor: `AppDbContext context, IMapper mapper`
- Implements `IRepository<TDomain>`
- `GetByIdAsync`: `context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, ct)`, map result with `_mapper.Map<TDomain?>`
- `AddAsync`: map domain → entity with `_mapper.Map<TEntity>`, call `context.Set<TEntity>().AddAsync`
- `Remove`: map domain → entity, call `context.Remove`
- `Update`: map domain → entity, call `context.Update`

### Step 6 — Repository: Concrete implementations

One file per implementation in `Repository/Repositories/`. Each extends `RepositoryBase<TEntity, TDomain>`.

**Notable implementations**:

- **`ModuleRepository.CheckOverlapAsync`**: Use LINQ over `context.Modules` filtered by `pageId`, optionally excluding `excludeModuleId`, then apply the rectangle intersection predicate (see `data-model.md`). Use `AnyAsync`.

- **`PdfExportRepository.GetExpiredExportsAsync`**: Filter `Status != ExportStatus.Failed && CreatedAt < utcCutoff`. Do not call `DateTime.UtcNow` inside the repository.

- **`PdfExportRepository.GetActiveExportForNotebookAsync`**: Filter `NotebookId == notebookId && Status != ExportStatus.Failed`. Active statuses: `Pending`, `Processing`, `Ready`.

- **`RefreshTokenRepository.RevokeAllForUserAsync`**: Use `context.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true), ct)`. No call to `SaveChanges`. See research D2.

### Step 7 — Repository: UnitOfWork

Create `Repository/UnitOfWork.cs`:

```csharp
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
```

### Step 8 — Application: DI registration

Create or extend `Application/Extensions/ServiceCollectionExtensions.cs` with `AddRepositories(this IServiceCollection services)`. Register all 11 repositories and `UnitOfWork` as **Scoped**. Call this method from `Program.cs`.

`AddRepositories()` does **not** register AutoMapper profiles — that is the responsibility of `Program.cs` via `services.AddAutoMapper(...)` with assembly scanning. Ensure the `Repository` assembly is included in the scan so `EntityToDomainProfile` is discovered automatically.

### Step 9 — Tests: Repository integration tests

Create `Tests/Integration/Repositories/` folder. Add tests for the two highest-risk methods:
- `ModuleRepositoryTests` — `CheckOverlapAsync`: test empty page, non-overlapping, overlapping, self-exclusion
- `PdfExportRepositoryTests` — `GetExpiredExportsAsync`: test with cutoff boundaries, no-Failed filter

Use in-memory EF Core provider (`UseInMemoryDatabase`). Each test uses a unique `Guid`-named database to prevent state sharing.

---

## Verification Checklist

After implementation, verify:

- [ ] `dotnet build Staccato.sln` — zero errors
- [ ] All 11 interfaces resolve from the DI container (run the app)
- [ ] `dotnet test --filter "FullyQualifiedName~Integration.Repositories"` — all green
- [ ] `Domain` project has no reference to `Repository`, `Persistence`, or `EntityModels`
- [ ] No repository implementation calls `SaveChanges` or `SaveChangesAsync` (except `UnitOfWork` and `RevokeAllForUserAsync` via `ExecuteUpdateAsync`)
- [ ] All async methods accept and thread through `CancellationToken`
- [ ] `<Nullable>enable</Nullable>` in all modified `.csproj` files
