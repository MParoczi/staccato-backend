# Research: Repository Pattern and Unit of Work

**Branch**: `004-repository-uow` | **Date**: 2026-03-08

---

## D1 — AutoMapper Bidirectional Mapping

**Decision**: Configure `ReverseMap()` in `EntityToDomainProfile` for every mapping pair, enabling both `Entity → Domain` (reads) and `Domain → Entity` (writes/adds).

**Rationale**: The base `IRepository<T>.AddAsync`, `Update`, and `Remove` methods receive domain model types. To stage them with EF Core, the repository must produce an `EntityModel` from the `DomainModel`. `ReverseMap()` avoids a parallel set of duplicate profile definitions. The mapping is property-name-flat for all 11 entity pairs (verified: every EntityModel property name matches its DomainModel counterpart; the only difference is navigation properties on EntityModels, which AutoMapper ignores when mapping Domain → Entity because DomainModels have no navigation properties).

**Alternatives considered**:
- Manual mapping inside each repository method — rejected: error-prone, duplicates N × M lines of property assignments.
- Separate explicit `CreateMap<Domain, Entity>()` calls alongside `CreateMap<Entity, Domain>()` — acceptable but verbose; `ReverseMap()` is equivalent and more concise.

---

## D2 — RevokeAllForUserAsync: Bulk vs Change-Tracking Approach

**Decision**: Use EF Core `ExecuteUpdateAsync` for a single-query bulk UPDATE. This method is an intentional, documented exception to the UoW change-tracking pattern.

**Rationale**: Loading all refresh tokens for a user, setting `IsRevoked = true` on each via change tracking, and then calling `SaveChangesAsync` through `IUnitOfWork` is O(N) change-tracking overhead and N-row writes. `ExecuteUpdateAsync` emits one `UPDATE … WHERE UserId = @userId AND IsRevoked = false`, which is O(1) regardless of token count. This is safe because revocation is always the terminal operation in logout and account-deletion flows — the caller does not need to include this write in a larger unit-of-work transaction.

**Note**: `ExecuteUpdateAsync` bypasses EF change tracking and commits immediately. It does **not** call `SaveChanges`. This is not a violation of the "repositories MUST NOT call SaveChanges" rule — it uses a different EF execution path. The trade-off (immediate commit, not participates in UoW) is documented in the interface contract.

**Alternatives considered**:
- Change-tracking approach via UoW — rejected: inefficient for large token counts; also breaks the caller's expectation that CommitAsync flushes all pending changes atomically (revocation would already be committed before CommitAsync is called).
- `ExecuteDeleteAsync` on old tokens — rejected: deleted tokens cannot be audited; soft-delete via IsRevoked is the correct pattern.

---

## D3 — Active PdfExport Status Definition

**Decision**: Active = `Pending | Processing | Ready` (anything except `Failed`).

**Rationale**: The `ExportStatus` enum has four values: `Pending`, `Processing`, `Ready`, `Failed`. The export flow is: POST /exports → `Pending` (queued but not yet picked up by background service) → `Processing` (background service is working) → `Ready` (uploaded) or `Failed` (error). A new export request while one is `Pending` should also be blocked — otherwise a burst of rapid requests could enqueue duplicate exports before the background service starts processing. The spec assumption ("Processing or Ready") omitted `Pending`; this research overrides that assumption.

**Alternatives considered**:
- Active = Processing only — rejected: allows duplicate exports when one is Pending.
- Active = Processing | Ready — rejected: same issue as above for Pending state.

---

## D4 — Repository Base Class

**Decision**: Define a generic abstract `RepositoryBase<TEntity, TDomain>` class in `Repository/Repositories/` implementing `IRepository<TDomain>`. All 11 concrete repositories extend it.

**Rationale**: `GetByIdAsync`, `AddAsync`, `Remove`, and `Update` have identical logic across all 11 repositories. Without a base class, these 4 methods are copy-pasted 11 × 4 = 44 times. The base class holds `AppDbContext` and `IMapper` as protected fields injected via primary constructor, accessible to all subclasses.

**Base class signature**:
```
RepositoryBase<TEntity, TDomain>(AppDbContext context, IMapper mapper)
  where TEntity : class
```

**Alternatives considered**:
- Pure interface delegation (no base class) — rejected: significant boilerplate duplication.
- Extension methods on `DbSet<T>` — rejected: not compatible with the scoped AppDbContext injection pattern.

---

## D5 — "GetWith…" Method Return Types

**Decision**: Methods that load an entity together with its child collection return a **named C# tuple**: `(TParent Entity, IReadOnlyList<TChild> Children)?`. Returns `null` if the parent entity does not exist.

**Rationale**: Domain models intentionally have no navigation properties (they are pure POCO models with zero dependencies). Returning both the parent and its children as a named tuple is the most type-safe approach that does not require modifying `DomainModels/`. The service layer receives both pieces from one round trip and can destructure the tuple.

**Affected methods**:
| Method | Return type |
|---|---|
| `IUserRepository.GetWithActiveTokensAsync` | `(User User, IReadOnlyList<RefreshToken> Tokens)?` |
| `INotebookRepository.GetWithStylesAsync` | `(Notebook Notebook, IReadOnlyList<NotebookModuleStyle> Styles)?` |
| `ILessonRepository.GetWithPagesAsync` | `(Lesson Lesson, IReadOnlyList<LessonPage> Pages)?` |
| `ILessonPageRepository.GetPageWithModulesAsync` | `(LessonPage Page, IReadOnlyList<Module> Modules)?` |

**Alternatives considered**:
- Return only the child collection (e.g., `IReadOnlyList<LessonPage>`) and force a separate `GetByIdAsync` for the parent — rejected: the naming convention "GetWith…" implies the parent is included; two round trips defeat the purpose.
- Add optional navigation properties to domain models — rejected: out of scope for this feature; would require a DomainModels change across the project.

---

## D6 — DI Registration Lifetime

**Decision**: All 11 repositories and `UnitOfWork` registered as **Scoped** in `Application`'s `AddRepositories()` extension method.

**Rationale**: `AppDbContext` is Scoped (per HTTP request). All repositories and `UnitOfWork` wrap the same `AppDbContext` instance. If repositories were Transient, different instances might receive different `AppDbContext` instances from DI (depending on DI container behaviour), breaking the shared change-tracking that makes `IUnitOfWork.CommitAsync` flush all staged changes.

**Alternatives considered**:
- Singleton — rejected: `AppDbContext` is not thread-safe.
- Transient — rejected: risks disjoint change trackers; also inconsistent with `UnitOfWork` scope.
