# Research: Notebook CRUD and Style Management

**Branch**: `008-notebook-crud-styles` | **Date**: 2026-03-28

---

## Decision 1: CoverColor is Missing from NotebookEntity and Notebook Domain Model

**Finding**: `NotebookEntity` (`EntityModels/Entities/NotebookEntity.cs`) and `DomainModels/Models/Notebook.cs` have no `CoverColor` property. The initial migration `20260308124514_InitialCreate.cs` confirms `CoverColor` was never added to the `Notebooks` table.

**Decision**: Add `CoverColor` as `string` to both files, update `NotebookConfiguration` with `.HasMaxLength(7)` for a `#RRGGBB` string, and add a new EF Core migration `AddNotebookCoverColor`.

**Rationale**: `CoverColor` is a required field in `POST /notebooks`, `GET /notebooks` (summary), and `GET /notebooks/{id}` (detail). Without it the feature cannot be implemented.

**Alternatives considered**: Storing colour as an integer — rejected; the frontend expects a hex string and the existing domain model uses strings throughout.

---

## Decision 2: SystemStylePresetEntity Does Not Implement IEntity

**Finding**: `SystemStylePresetEntity` is defined as `public class SystemStylePresetEntity` with a `Guid Id { get; set; }` property but without `: IEntity`. `RepositoryBase<TEntity, TDomain>` has a `where TEntity : class, IEntity` constraint.

**Decision**: Add `: IEntity` to `SystemStylePresetEntity`. Since `IEntity` only requires `Guid Id { get; }` and the entity already has `public Guid Id { get; set; }`, this is a safe, zero-behaviour-change addition that makes the entity compatible with `RepositoryBase`.

**Rationale**: Keeps the `ISystemStylePresetRepository` consistent with every other repository in the codebase. Avoids a one-off custom query pattern.

---

## Decision 3: ISystemStylePresetRepository Doesn't Exist

**Finding**: No `ISystemStylePresetRepository` interface or `SystemStylePresetRepository` implementation exists anywhere in the codebase. `AppDbContext.SystemStylePresets` is available, but no abstraction wraps it.

**Decision**: Create `Domain/Interfaces/Repositories/ISystemStylePresetRepository.cs` with:
- `Task<IReadOnlyList<SystemStylePreset>> GetAllAsync(CancellationToken ct = default)` (ordered by `DisplayOrder` ascending)
- `Task<SystemStylePreset?> GetByIdAsync(Guid id, CancellationToken ct = default)` (inherited from `IRepository<SystemStylePreset>`)

Create `Repository/Repositories/SystemStylePresetRepository.cs` extending `RepositoryBase<SystemStylePresetEntity, SystemStylePreset>`.

Register in `ServiceCollectionExtensions.AddRepositories()`.

**Rationale**: `GET /presets` and `POST /notebooks/{id}/styles/apply-preset/{presetId}` both need to read from `SystemStylePresets`. Accessing `AppDbContext` directly from a service would violate the clean architecture boundary.

---

## Decision 4: SystemStylePresetSeeder Sets Classic as IsDefault, Not Colorful

**Finding**: In `Persistence/Seed/SystemStylePresetSeeder.cs`:
```csharp
BuildPreset("Classic", 1, true, BuildClassicStyles()),   // IsDefault = true
BuildPreset("Colorful", 2, false, BuildColorfulStyles()), // IsDefault = false
```

Classic is marked as the default, but the feature spec (FR-003) and user description both state: *"if styles are not provided, the Colorful system preset is applied."* The spec assumption states: *"'Colorful' is identified programmatically as the system preset with `IsDefault = true`."*

**Decision**: Change the seeder to set Classic's `IsDefault = false` and Colorful's `IsDefault = true`. Because the seeder has an idempotency guard (`if (await context.SystemStylePresets.AnyAsync(ct)) return;`), no data migration is needed for fresh databases. For existing databases with incorrectly seeded presets, a note is added to the quickstart.

**Rationale**: The spec is the single source of truth; the seeder must match it.

---

## Decision 5: NotebookRepository.GetByUserIdAsync Lacks Ordering

**Finding**: `NotebookRepository.GetByUserIdAsync` queries notebooks with no `OrderBy` clause. The clarified spec (FR-001, Clarification Q1) requires results ordered by `createdAt` ascending.

**Decision**: Add `.OrderBy(n => n.CreatedAt)` to `GetByUserIdAsync` in `NotebookRepository.cs`.

**Rationale**: Ordering consistency. Lessons use `CreatedAt` ascending; notebooks follow the same pattern.

---

## Decision 6: Style Serialization — Typed DTOs vs Raw JSON Passthrough

**Finding**: Existing user-saved preset APIs use raw `StylesJson` passthrough (`StyleEntryDto(string ModuleType, string StylesJson)`). The notebook module style API requires typed DTOs (backgroundColor, borderColor, etc.) both for incoming validation and outgoing response, per the frontend TypeScript interface and FR-009.

**Decision**: Adopt a **controller-serializes, AutoMapper-deserializes** pattern:
- `ModuleStyleRequest` contains typed properties (`ModuleType`, `BackgroundColor`, `BorderColor`, `BorderStyle`, `BorderWidth`, `BorderRadius`, `HeaderBgColor`, `HeaderTextColor`, `BodyTextColor`, `FontFamily`). The controller serializes the style properties into `StylesJson` before passing `NotebookModuleStyle` domain models to the service.
- `ModuleStyleResponse` contains the same typed properties plus `Id` and `NotebookId`. A custom AutoMapper `ITypeConverter<NotebookModuleStyle, ModuleStyleResponse>` in `DomainToResponseProfile` deserializes `StylesJson` back into individual properties.
- A private `StyleProperties` record defined alongside the converter handles deserialization; it is not a public domain model.

**Rationale**: Keeps the service layer clean (it works with `NotebookModuleStyle` domain models using `StylesJson`). Keeps the controller thin (it only serializes input). AutoMapper handles response mapping consistently.

**Alternatives considered**:
- Adding typed fields to `NotebookModuleStyle` domain model — rejected; the domain model would need to know about serialization, mixing concerns.
- Returning raw `StylesJson` in the response — rejected; the frontend needs typed individual fields.

---

## Decision 7: ApplyPreset — Preset Resolution and Style Transfer

**Finding**: When applying a preset to a notebook, the preset's `StylesJson` on `SystemStylePresetEntity` is an **array** of 12 objects, each with a `moduleType` field plus style properties. The notebook's `NotebookModuleStyleEntity.StylesJson` stores a **single flat object** (no `moduleType`; that's already in the `ModuleType` column).

**Decision**: The `NotebookService.ApplyPresetAsync` method:
1. Looks up the preset ID in `ISystemStylePresetRepository.GetByIdAsync` first; if not found, tries `IUserSavedPresetRepository.GetByIdAsync`.
2. Deserializes the preset's `StylesJson` array into a list of `PresetStyleEntry` (private record with `moduleType` + style properties).
3. Fetches all 12 existing `NotebookModuleStyle` records for the notebook via `INotebookModuleStyleRepository.GetByNotebookIdAsync`.
4. For each existing record, finds the matching entry from the preset by `ModuleType`, re-serializes just the style properties (minus `moduleType`) into the record's `StylesJson`, and calls `_styleRepo.Update(record)`.
5. Commits via `IUnitOfWork.CommitAsync`.

**Rationale**: Reuses existing `INotebookModuleStyleRepository.Update` pattern (via `RepositoryBase.Update`). Avoids deleting and re-creating style records, which would change their `Id` values unexpectedly.

---

## Decision 8: No Migration Needed for NotebookModuleStyleConfiguration

**Finding**: `NotebookModuleStyleConfiguration` already has a unique index on `(NotebookId, ModuleType)` and cascade delete from `Notebook`. No changes needed.

---

## Decision 9: INotebookService — Method Signatures

Based on the spec requirements and codebase patterns, the service interface will expose:

```
GetAllByUserAsync(Guid userId, CancellationToken ct) → IReadOnlyList<NotebookSummary>
GetByIdAsync(Guid notebookId, Guid userId, CancellationToken ct) → (Notebook, IReadOnlyList<NotebookModuleStyle>)
CreateAsync(Guid userId, string title, Guid instrumentId, PageSize pageSize, string coverColor, IReadOnlyList<NotebookModuleStyle>? styles, CancellationToken ct) → (Notebook, IReadOnlyList<NotebookModuleStyle>)
UpdateAsync(Guid notebookId, Guid userId, string title, string coverColor, CancellationToken ct) → (Notebook, IReadOnlyList<NotebookModuleStyle>)
DeleteAsync(Guid notebookId, Guid userId, CancellationToken ct) → void
GetStylesAsync(Guid notebookId, Guid userId, CancellationToken ct) → IReadOnlyList<NotebookModuleStyle>
BulkUpdateStylesAsync(Guid notebookId, Guid userId, IReadOnlyList<NotebookModuleStyle> styles, CancellationToken ct) → IReadOnlyList<NotebookModuleStyle>
ApplyPresetAsync(Guid notebookId, Guid userId, Guid presetId, CancellationToken ct) → IReadOnlyList<NotebookModuleStyle>
```

A `NotebookSummary` domain model is needed in `DomainModels/Models/` with: `Id`, `UserId`, `Title`, `InstrumentName`, `PageSize`, `CoverColor`, `LessonCount`, `CreatedAt`, `UpdatedAt`.

**Rationale**: `GetAllByUserAsync` returns `NotebookSummary` (not full `Notebook`) to avoid the N+1 problem. The repository joins with `Instrument.DisplayName` and counts lessons in a single query. All ownership checks are performed inside the service using `ForbiddenException`.

---

## Decision 10: NotebookSummary Domain Model and Repository Query

**Finding**: The `INotebookRepository.GetByUserIdAsync` returns `IReadOnlyList<Notebook>` which has no `InstrumentName` or `LessonCount`. The summary response requires both.

**Decision**: Update `INotebookRepository.GetByUserIdAsync` to return `IReadOnlyList<NotebookSummary>` (a new domain model in `DomainModels/Models/`). The query will use `.Include(n => n.Instrument)` and `.Select()` or `.Include(n => n.Lessons)` to project `LessonCount`. Update the interface signature accordingly and update the `NotebookRepository` implementation.

**Rationale**: Keeps the data access efficient (single query with join). Avoids separate `GetInstrumentById` and `CountLessons` calls from the service layer.
