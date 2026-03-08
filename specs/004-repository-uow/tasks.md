# Tasks: Repository Pattern and Unit of Work

**Input**: Design documents from `/specs/004-repository-uow/`
**Branch**: `004-repository-uow`
**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Contracts**: [contracts/domain-interfaces.md](contracts/domain-interfaces.md) | **Data model**: [data-model.md](data-model.md)

**Tests**: Integration tests included for the 4 high-risk repository methods (Phase 6 / US4).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US4)

---

## Phase 1: Setup

**Purpose**: Verify project references are correct before writing any code. No files created here.

- [ ] T001 Confirm `Domain/Domain.csproj` references only `DomainModels` — open the file and verify no reference to `Repository`, `Persistence`, or `EntityModels` exists
- [ ] T002 Confirm `Repository/Repository.csproj` references `Domain`, `EntityModels`, and `Persistence` — open the file and verify all three `<ProjectReference>` entries are present; add any that are missing

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infrastructure required by every user story. MUST be complete before Phase 3+.

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

- [ ] T003 [P] Create `Domain/Exceptions/NotFoundException.cs` — class `NotFoundException : BusinessException`, constructor sets `StatusCode = 404` in body (not `protected override`), error code `"NOT_FOUND"`, default message `"The requested resource was not found."`. See `plan.md §Exception Subclass Pattern` for the exact constructor shape compatible with `BusinessException`'s `protected init` StatusCode.
- [ ] T004 [P] Create `Domain/Exceptions/ConflictException.cs` — class `ConflictException : BusinessException`, `StatusCode = 409`, error code `"CONFLICT"`, default message `"A conflicting resource already exists."`
- [ ] T005 [P] Create `Domain/Exceptions/ForbiddenException.cs` — class `ForbiddenException : BusinessException`, `StatusCode = 403`, error code `"FORBIDDEN"`, default message `"You do not have access to this resource."`
- [ ] T006 [P] Create `Domain/Exceptions/ValidationException.cs` — class `ValidationException : BusinessException`, `StatusCode = 422`, error code `"VALIDATION_ERROR"`, default message `"A business rule was violated."`
- [ ] T009 [P] Create `Domain/Interfaces/Repositories/IRepository.cs` — generic interface `IRepository<T>` with four methods: `Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)`, `Task AddAsync(T entity, CancellationToken ct = default)`, `void Remove(T entity)`, `void Update(T entity)`. See `contracts/domain-interfaces.md §IRepository` for the exact signatures with XML docs. *(Moved from Phase 3: `RepositoryBase` in T008 implements this interface and cannot compile without it.)*
- [ ] T007 Create `Repository/Mapping/EntityToDomainProfile.cs` — AutoMapper profile with `CreateMap<TEntity, TDomain>().ReverseMap()` for all 11 entity pairs listed in `data-model.md §AutoMapper Profile Mappings`. For `PdfExportEntity ↔ PdfExport`: map `LessonIdsJson` (entity `string?`) to `LessonIds` (domain `List<Guid>?`) using a custom `ValueConverter` that serializes/deserializes with `System.Text.Json`. Navigation properties on EntityModels are automatically ignored by AutoMapper because DomainModels have no corresponding navigation properties.
- [ ] T008 Create `EntityModels/IEntity.cs` (if it does not already exist) — `public interface IEntity { Guid Id { get; } }` — then apply it to all 11 entity classes (`public class UserEntity : IEntity`, etc.). Then create `Repository/Repositories/RepositoryBase.cs` — abstract class `RepositoryBase<TEntity, TDomain>(AppDbContext context, IMapper mapper) where TEntity : class, IEntity` implementing `IRepository<TDomain>`. Protected fields `_context` and `_mapper`. Implement: `GetByIdAsync` via `_context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, ct)` then `_mapper.Map<TDomain?>`; `AddAsync` via `_mapper.Map<TEntity>(entity)` then `_context.Set<TEntity>().AddAsync`; `Remove` via `_mapper.Map<TEntity>(entity)` then `_context.Remove`; `Update` via `_mapper.Map<TEntity>(entity)` then `_context.Update`. The `IEntity` constraint makes the `e.Id == id` predicate compile without reflection. See `data-model.md §RepositoryBase` for the full specification.

**Checkpoint**: All exception types compile. `IRepository<T>` interface compiles. `IEntity` marker interface exists and is applied to all entity classes. AutoMapper profile compiles. RepositoryBase compiles. Run `dotnet build Staccato.sln` — zero errors expected.

---

## Phase 3: User Story 1 — Generic Data Access Contract (Priority: P1) 🎯 MVP

**Goal**: Define `IUnitOfWork` and its `UnitOfWork` implementation so any service can depend on both `IRepository<T>` (defined in Phase 2) and `IUnitOfWork` and be tested with mocks.

**Independent Test**: Instantiate a Moq-backed `IRepository<Notebook>` and `IUnitOfWork` in a test — verify the mock calls are forwarded correctly with no database involved.

- [ ] T010 [P] [US1] Create `Domain/Interfaces/IUnitOfWork.cs` — interface `IUnitOfWork` with one method: `Task<int> CommitAsync(CancellationToken ct = default)`. XML doc: "Flushes all staged repository changes to the database as one atomic transaction. Returns the number of state entries written." See `contracts/domain-interfaces.md §IUnitOfWork`.
- [ ] T011 [US1] Create `Repository/UnitOfWork.cs` — class `UnitOfWork(AppDbContext context) : IUnitOfWork`. Single method: `public Task<int> CommitAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct)`. File-scoped namespace `Repository`.

**Checkpoint**: `IRepository<T>`, `IUnitOfWork`, and `UnitOfWork` compile. `dotnet build` — zero errors.

---

## Phase 4: User Story 2 — Atomic Persistence via Unit of Work (Priority: P1)

**Goal**: All 11 concrete repository classes exist, extend `RepositoryBase`, and implement their stub interface — enabling multi-step staging (Add + Add + CommitAsync) across any combination of repositories.

**Independent Test**: In an integration test with in-memory EF, call `INotebookRepository.AddAsync` and `INotebookModuleStyleRepository.AddAsync`, then `IUnitOfWork.CommitAsync` — verify both records are persisted and neither record exists before `CommitAsync`.

All T012–T022 tasks are fully parallel (each touches a different pair of files).

- [ ] T012 [P] [US2] Create `Domain/Interfaces/Repositories/IUserRepository.cs` (stub — extends `IRepository<User>`, no extra methods yet) and `Repository/Repositories/UserRepository.cs` (class `UserRepository(AppDbContext context, IMapper mapper) : RepositoryBase<UserEntity, User>(context, mapper), IUserRepository` — no method body needed; RepositoryBase provides all four IRepository<T> methods). File-scoped namespace `Domain.Interfaces.Repositories` / `Repository.Repositories`.
- [ ] T013 [P] [US2] Create `Domain/Interfaces/Repositories/IRefreshTokenRepository.cs` (stub) and `Repository/Repositories/RefreshTokenRepository.cs` — same pattern as T012 with `RefreshTokenEntity` and `RefreshToken`.
- [ ] T014 [P] [US2] Create `Domain/Interfaces/Repositories/IUserSavedPresetRepository.cs` (stub) and `Repository/Repositories/UserSavedPresetRepository.cs` — `UserSavedPresetEntity` and `UserSavedPreset`.
- [ ] T015 [P] [US2] Create `Domain/Interfaces/Repositories/IInstrumentRepository.cs` (stub) and `Repository/Repositories/InstrumentRepository.cs` — `InstrumentEntity` and `Instrument`.
- [ ] T016 [P] [US2] Create `Domain/Interfaces/Repositories/IChordRepository.cs` (stub) and `Repository/Repositories/ChordRepository.cs` — `ChordEntity` and `Chord`.
- [ ] T017 [P] [US2] Create `Domain/Interfaces/Repositories/INotebookRepository.cs` (stub) and `Repository/Repositories/NotebookRepository.cs` — `NotebookEntity` and `Notebook`.
- [ ] T018 [P] [US2] Create `Domain/Interfaces/Repositories/INotebookModuleStyleRepository.cs` (stub) and `Repository/Repositories/NotebookModuleStyleRepository.cs` — `NotebookModuleStyleEntity` and `NotebookModuleStyle`.
- [ ] T019 [P] [US2] Create `Domain/Interfaces/Repositories/ILessonRepository.cs` (stub) and `Repository/Repositories/LessonRepository.cs` — `LessonEntity` and `Lesson`.
- [ ] T020 [P] [US2] Create `Domain/Interfaces/Repositories/ILessonPageRepository.cs` (stub) and `Repository/Repositories/LessonPageRepository.cs` — `LessonPageEntity` and `LessonPage`.
- [ ] T021 [P] [US2] Create `Domain/Interfaces/Repositories/IModuleRepository.cs` (stub) and `Repository/Repositories/ModuleRepository.cs` — `ModuleEntity` and `Module`.
- [ ] T022 [P] [US2] Create `Domain/Interfaces/Repositories/IPdfExportRepository.cs` (stub) and `Repository/Repositories/PdfExportRepository.cs` — `PdfExportEntity` and `PdfExport`.
- [ ] T023 [US2] Create `Application/Extensions/ServiceCollectionExtensions.cs` with `public static IServiceCollection AddRepositories(this IServiceCollection services)` — register all 11 repositories and `UnitOfWork` as `Scoped`: `services.AddScoped<IUserRepository, UserRepository>()` … (repeat for all 11) … `services.AddScoped<IUnitOfWork, UnitOfWork>()`. Return `services`.
- [ ] T024 [US2] Register `AddRepositories()` in `Application/Program.cs` — add `builder.Services.AddRepositories()`. Also ensure `builder.Services.AddAutoMapper(...)` includes the `Repository` assembly so `EntityToDomainProfile` is auto-discovered (e.g., `AddAutoMapper(typeof(Program).Assembly, typeof(EntityToDomainProfile).Assembly)`).

**Checkpoint**: All 11 stub interfaces and concrete repositories compile. DI wiring compiles. `dotnet build Staccato.sln` — zero errors.

---

## Phase 5: User Story 3 — Entity-Specific Query Operations (Priority: P2)

**Goal**: Every specific repository interface gains its domain-relevant query methods, implemented in the concrete class. Services can now call named, semantically meaningful queries rather than filtering in-memory.

**Independent Test**: Each repository method can be tested independently via a Moq-backed interface or an in-memory EF integration test. The overlap check and chord search are the highest-priority validation targets.

All T025–T035 tasks are fully parallel (each touches only its own interface file + concrete repository file).

- [ ] T025 [P] [US3] Extend `IUserRepository` in `Domain/Interfaces/Repositories/IUserRepository.cs` and implement in `Repository/Repositories/UserRepository.cs`: `GetByEmailAsync` (filter `Users` by `Email`), `GetByGoogleIdAsync` (filter by `GoogleId`), `GetWithActiveTokensAsync` (load user + eagerly load `RefreshTokens` filtered to `!IsRevoked && ExpiresAt > DateTime.UtcNow`, return named tuple `(User User, IReadOnlyList<RefreshToken> Tokens)?` or `null` if user not found). See `contracts/domain-interfaces.md §IUserRepository` and `data-model.md §IUserRepository` for full signatures and constraints.
- [ ] T026 [P] [US3] Extend `INotebookRepository` and implement in `NotebookRepository`: `GetByUserIdAsync` (filter `Notebooks` by `UserId`, return empty list if none), `GetWithStylesAsync` (load notebook + eagerly load `ModuleStyles`, return tuple `(Notebook, IReadOnlyList<NotebookModuleStyle>)?` or `null`). See `contracts/domain-interfaces.md §INotebookRepository`.
- [ ] T027 [P] [US3] Extend `ILessonRepository` and implement in `LessonRepository`: `GetByNotebookIdOrderedByCreatedAtAsync` (filter by `NotebookId`, order by `CreatedAt` ascending, return empty list if none), `GetWithPagesAsync` (load lesson + eagerly load `LessonPages` ordered by `PageNumber` ascending, return tuple or `null`). See `contracts/domain-interfaces.md §ILessonRepository`.
- [ ] T028 [P] [US3] Extend `ILessonPageRepository` and implement in `LessonPageRepository`: `GetByLessonIdOrderedAsync` (filter by `LessonId`, order by `PageNumber` ascending, return empty list if none), `GetPageWithModulesAsync` (load page + eagerly load `Modules` ordered by `GridY` ascending then `GridX` ascending, return tuple or `null`). See `contracts/domain-interfaces.md §ILessonPageRepository`.
- [ ] T029 [P] [US3] Extend `IModuleRepository` and implement in `ModuleRepository`: `GetByPageIdAsync` (filter by `LessonPageId`, order by `GridY` ascending then `GridX` ascending, return empty list if none), `CheckOverlapAsync` (filter modules by `pageId`, optionally exclude `excludeModuleId`, apply server-side LINQ rectangle intersection predicate — both axes must intersect — use `AnyAsync`; return `false` for empty page). Exact predicate from `data-model.md §IModuleRepository`. **Critical**: predicate must be EF-translatable to SQL — no `.ToList()` before filtering.
- [ ] T030 [P] [US3] Extend `IChordRepository` and implement in `ChordRepository`: `SearchAsync` (filter by `InstrumentId`, apply `root` filter only if non-null using exact string equality, apply `quality` filter only if non-null using exact string equality; all filters server-side LINQ; return empty list if no matches). See `data-model.md §IChordRepository` for the "no client-side evaluation" constraint.
- [ ] T031 [P] [US3] Extend `IInstrumentRepository` and implement in `InstrumentRepository`: `GetAllAsync` (return all instruments ordered by `Name` ascending, return empty list if none). See `contracts/domain-interfaces.md §IInstrumentRepository`.
- [ ] T032 [P] [US3] Extend `IPdfExportRepository` and implement in `PdfExportRepository`: `GetActiveExportForNotebookAsync` (filter by `NotebookId` and `Status != ExportStatus.Failed`, return first or `null`; active = Pending | Processing | Ready), `GetExpiredExportsAsync` (filter `Status != Failed && CreatedAt < utcCutoff`; **do NOT call `DateTime.UtcNow`** inside this method; return empty list if none), `GetByUserIdAsync` (filter by `UserId`, order by `CreatedAt` descending, return empty list if none). See `contracts/domain-interfaces.md §IPdfExportRepository` and `data-model.md §IPdfExportRepository`.
- [ ] T033 [P] [US3] Extend `IRefreshTokenRepository` and implement in `RefreshTokenRepository`: `GetByTokenAsync` (filter by raw `Token` string value), `GetActiveByUserIdAsync` (filter `UserId == userId && !IsRevoked && ExpiresAt > DateTime.UtcNow`, return empty list if none), `RevokeAllForUserAsync` — implement using `_context.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true), ct)`. **This method commits immediately; it does NOT go through UoW.** See `contracts/domain-interfaces.md §IRefreshTokenRepository` for the XML doc warning that must appear on the interface method.
- [ ] T034 [P] [US3] Extend `INotebookModuleStyleRepository` and implement in `NotebookModuleStyleRepository`: `GetByNotebookIdAsync` (filter by `NotebookId`, order by `ModuleType` ascending as integer, return empty list if none), `GetByNotebookIdAndTypeAsync` (filter by both `NotebookId` and `ModuleType`, return `null` if not found). See `contracts/domain-interfaces.md §INotebookModuleStyleRepository`.
- [ ] T035 [P] [US3] Extend `IUserSavedPresetRepository` and implement in `UserSavedPresetRepository`: `GetByUserIdAsync` (filter by `UserId`, return empty list if none). See `contracts/domain-interfaces.md §IUserSavedPresetRepository`.

**Checkpoint**: All 11 specific interfaces have their full method sets. All implementations compile with server-side LINQ predicates. `dotnet build Staccato.sln` — zero errors.

---

## Phase 6: User Story 4 — Lifecycle Queries & Integration Tests (Priority: P3)

**Goal**: Validate the four highest-risk repository methods with integration tests against in-memory EF Core. Each test class uses unique `Guid`-named databases for full isolation.

**Independent Test**: `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Integration.Repositories"` — all tests pass.

- [ ] T036 [P] [US4] Create `Tests/Integration/Repositories/ModuleRepositoryTests.cs` — xUnit test class using `UseInMemoryDatabase(Guid.NewGuid().ToString())`. Seed module data per test. Four test methods for `CheckOverlapAsync`: (1) empty page returns `false`, (2) non-overlapping modules return `false`, (3) overlapping module returns `true`, (4) self-exclusion via `excludeModuleId` returns `false`. See `plan.md §Test Coverage` for method names and scenarios.
- [ ] T037 [P] [US4] Create `Tests/Integration/Repositories/PdfExportRepositoryTests.cs` — three test methods for `GetExpiredExportsAsync`: (1) only older-than-cutoff records returned, (2) `Failed`-status records excluded even if older than cutoff, (3) all-recent returns empty list. Three test methods for `GetActiveExportForNotebookAsync`: (1) `Pending` status returns export, (2) `Failed` status returns `null`, (3) no export returns `null`. Pass explicit `utcCutoff` value — never rely on `DateTime.UtcNow` inside the test assertion.
- [ ] T038 [P] [US4] Create `Tests/Integration/Repositories/RefreshTokenRepositoryTests.cs` — two test methods for `RevokeAllForUserAsync`: (1) only active (non-revoked) tokens are updated, already-revoked tokens are not double-updated, (2) method commits immediately — query the DB directly after the call without calling `CommitAsync` and verify tokens are revoked. **Use SQLite in-memory** (`UseSqlite("DataSource=:memory:")` with `dbContext.Database.EnsureCreated()` in test setup) — the EF `InMemory` provider does not support `ExecuteUpdateAsync` and will throw `InvalidOperationException` at runtime. First verify `Tests/Tests.csproj` has a `<PackageReference>` for `Microsoft.EntityFrameworkCore.Sqlite`; add it if missing.
- [ ] T039 [US4] Run `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Integration.Repositories"` — verify all tests in the three new test classes pass. Fix any implementation defects found.

**Checkpoint**: All integration tests green. The four high-risk methods are validated against real EF semantics.

---

## Polish & Cross-Cutting Concerns

- [ ] T040 Run `dotnet build Staccato.sln` — confirm zero errors and zero nullable warnings across all modified projects (`Domain`, `Repository`, `Application`, `Tests`)
- [ ] T041 [P] Verify `quickstart.md §Verification Checklist` — work through each bullet point and mark complete: no `SaveChanges` calls in repositories (grep the `Repository` folder), all async methods have `CancellationToken`, `<Nullable>enable</Nullable>` in all modified `.csproj` files
- [ ] T042 [P] Verify `Domain` has no prohibited references — run `dotnet build Domain/Domain.csproj` in isolation and confirm it succeeds with only `DomainModels` as a project reference

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user story phases
- **Phase 3 (US1)**: Depends on Phase 2
- **Phase 4 (US2)**: Depends on Phase 2 (`IRepository<T>` must exist before stub interfaces can extend it) and Phase 3 (`IUnitOfWork` must exist)
- **Phase 5 (US3)**: Depends on Phase 4 (stub interfaces and concrete classes must exist before adding methods)
- **Phase 6 (US4)**: Depends on Phase 5 (all query methods must be implemented before integration tests)
- **Polish**: Depends on Phase 6

### User Story Dependencies

- **US1 (P1)**: After Phase 2 — no story dependencies
- **US2 (P1)**: After Phase 2 (`IRepository<T>` defined there) and after US1 (`IUnitOfWork` defined in Phase 3)
- **US3 (P2)**: After US2 (stub files must exist to add methods to)
- **US4 (P3)**: After US3 (all query methods must exist before testing them)

### Parallel Opportunities Within Each Phase

- **Phase 2**: T003–T006, T009 fully parallel (5 separate files — T007/T008 follow sequentially after T009)
- **Phase 4**: T012–T022 fully parallel (11 independent interface+class pairs, no shared files)
- **Phase 5**: T025–T035 fully parallel (11 independent interface+class pairs)
- **Phase 6**: T036–T038 fully parallel (3 independent test class files)

---

## Parallel Execution Examples

```
# Phase 2 — exception subclasses + IRepository<T> all in parallel:
T003: NotFoundException.cs
T004: ConflictException.cs
T005: ForbiddenException.cs
T006: ValidationException.cs
T009: IRepository.cs  ← moved from Phase 3; required by T008 (RepositoryBase)

# Phase 4 — all 11 stub interface+class pairs in parallel:
T012: IUserRepository + UserRepository
T013: IRefreshTokenRepository + RefreshTokenRepository
T014: IUserSavedPresetRepository + UserSavedPresetRepository
T015: IInstrumentRepository + InstrumentRepository
T016: IChordRepository + ChordRepository
T017: INotebookRepository + NotebookRepository
T018: INotebookModuleStyleRepository + NotebookModuleStyleRepository
T019: ILessonRepository + LessonRepository
T020: ILessonPageRepository + LessonPageRepository
T021: IModuleRepository + ModuleRepository
T022: IPdfExportRepository + PdfExportRepository

# Phase 5 — all 11 query method additions in parallel:
T025–T035 (one per repository)
```

---

## Implementation Strategy

### MVP (User Stories 1 + 2 only)

1. Complete Phase 1: Setup verification
2. Complete Phase 2: Foundational (exceptions, AutoMapper, RepositoryBase)
3. Complete Phase 3: US1 — `IRepository<T>`, `IUnitOfWork`, `UnitOfWork`
4. Complete Phase 4: US2 — 11 stub repos, DI registration
5. **STOP and VALIDATE**: `dotnet build` passes; services can be injected with repositories via DI
6. Ship MVP — atomic CRUD works end-to-end; service layer can use mocks in unit tests

### Incremental Delivery

1. MVP above → Foundation + basic CRUD abstraction ready
2. Add Phase 5 (US3) → Domain-specific queries available to all services
3. Add Phase 6 (US4) → High-risk methods validated with integration tests
4. Each phase adds value without breaking previous phases

---

## Notes

- `[P]` tasks = different files, no dependency on each other — safe to parallelise
- `[Story]` label maps each task to its user story for traceability
- All implementation details (method signatures, exact predicates, sort orders) are in `data-model.md` and `contracts/domain-interfaces.md` — reference those files during implementation
- The `RepositoryBase` generic `Id` constraint (T008) requires that `TEntity` has a `Guid Id` property — consider adding a marker interface `IEntity { Guid Id { get; } }` in `EntityModels/` if the base constraint doesn't compile cleanly
- `RevokeAllForUserAsync` (T033) must **not** be included in any UoW transaction — it commits immediately via `ExecuteUpdateAsync`
- Do not call `DateTime.UtcNow` inside `GetExpiredExportsAsync` (T032) — the caller always supplies the `utcCutoff` parameter
