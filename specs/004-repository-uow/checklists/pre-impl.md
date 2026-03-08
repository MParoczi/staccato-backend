# Pre-Implementation Checklist: Repository Pattern and Unit of Work

**Purpose**: Author self-review — validate the quality, completeness, and clarity of interface contract and implementation specifications before writing any code
**Created**: 2026-03-08
**Reviewed**: 2026-03-08
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md) | [contracts/domain-interfaces.md](../contracts/domain-interfaces.md) | [data-model.md](../data-model.md)
**Scope**: Full feature — Domain interfaces + Repository implementation spec + Integration test requirements
**Audience**: Author (pre-implementation) | **Depth**: Standard

> **How to use**: Work through each gate category top-down before writing code. Items marked 🚨 are mandatory — do not proceed if any are unchecked. Items marked ℹ️ are informational.

---

## 🚨 Mandatory Gate A — Architectural Boundary Requirements

*Block implementation if any item fails. These violations are the hardest to reverse.*

- [x] CHK001 - Are all 11 repository interfaces and `IUnitOfWork` specified to reside exclusively in the `Domain` project (not `Repository` or any other project)? [Completeness, Plan §Project Structure]
- [x] CHK002 - Do all Domain interface method signatures use exclusively `DomainModels` types in parameters and return types, with zero `EntityModel` references anywhere in `Domain/Interfaces/`? [Clarity, contracts/domain-interfaces.md]
- [x] CHK003 - Is the prohibition on repository implementations calling `SaveChanges` or `SaveChangesAsync` explicitly and unambiguously stated in the spec? [Clarity, Spec §FR-007]
- [x] CHK004 - Is the "Domain → DomainModels ONLY" dependency rule verified and marked ✅ in the Constitution Check gate G1? [Consistency, Plan §Constitution Check G1]
- [x] CHK005 - Are the specific project references required for the `Repository` project documented as `Repository → Domain + EntityModels + Persistence`? [Completeness, Plan §Technical Context]
- [x] CHK006 - Is the requirement that concrete repository implementations reside in `Repository/Repositories/` (not `Domain`) explicitly stated? [Clarity, Spec §FR-014]

---

## 🚨 Mandatory Gate B — Query Semantic Requirements

*Block implementation if any item fails. Incorrect filter semantics silently return wrong data.*

- [x] CHK007 - Is the overlap detection predicate formally specified with exact integer arithmetic for both X and Y axes, covering both edge-touching and interior-overlapping cases? [Clarity, data-model.md §IModuleRepository]
- [x] CHK008 - Is "active PdfExport" unambiguously defined as `Pending | Processing | Ready` — specifically including `Pending` and explicitly correcting the spec's initial assumption of "Processing or Ready only"? [Clarity, research.md §D3]
- [x] CHK009 - Is the active refresh token filter condition specified with both required predicates: `IsRevoked == false` AND `ExpiresAt > DateTime.UtcNow`? [Clarity, data-model.md §IUserRepository]
- [x] CHK010 - Is the exclusion of `Failed`-status exports from `GetExpiredExportsAsync` (i.e., only non-Failed exports expire) explicitly stated in the interface contract? [Clarity, contracts §IPdfExportRepository]
- [x] CHK011 - Is the null-filter behavior of `IChordRepository.SearchAsync` documented for each filter dimension individually — `null root` = no root filter; `null quality` = no quality filter — and not just as a combined statement? [Clarity, contracts §IChordRepository]
- [x] CHK012 - Are sort orders (specific field + ascending/descending direction) documented for every method that returns an ordered collection? [Completeness, data-model.md §all interfaces] — updated: modules ordered by GridY asc, GridX asc
- [x] CHK013 - Are `null` vs empty-list return semantics unambiguously and consistently documented for every method across all 11 interfaces — specifically: collection methods return empty list; single-entity and tuple methods return `null` when the entity doesn't exist? [Consistency, Clarity, data-model.md] — updated: added missing constraints to IChordRepository, IPdfExportRepository, IRefreshTokenRepository, INotebookModuleStyleRepository
- [x] CHK014 - Is `IRepository<T>.GetByIdAsync`'s null-on-missing behavior (return `null`, never throw) explicitly stated in the base interface contract documentation? [Clarity, data-model.md §IRepository]
- [x] CHK015 - Is the prohibition on `GetExpiredExportsAsync` calling `DateTime.UtcNow` internally stated at the interface level (not only in research)? [Clarity, contracts §IPdfExportRepository]

---

## Requirement Completeness

- [x] CHK016 - Are all 11 specific repository interfaces enumerated in a single summary table that includes their base type and extra methods? [Completeness, data-model.md §Repository Interface Summary]
- [x] CHK017 - Is `IRepository<T>`'s complete method set (all 4: `GetByIdAsync`, `AddAsync`, `Remove`, `Update`) specified with exact return types and parameter signatures? [Completeness, contracts §IRepository]
- [x] CHK018 - Is `IUnitOfWork.CommitAsync`'s return type specified as `Task<int>` (not `Task`) and the meaning of the integer return value documented? [Completeness, contracts §IUnitOfWork]
- [x] CHK019 - Is the `LessonIds` ↔ `LessonIdsJson` AutoMapper mapping exception documented — specifically that `PdfExport.LessonIds` (domain `List<Guid>?`) maps to `PdfExportEntity.LessonIdsJson` (entity `string?`) and requires a custom value converter? [Completeness, Plan §Phase 1]
- [x] CHK020 - Is the `IModuleRepository.GetByPageIdAsync` empty-list return behavior (when no modules exist on the page) explicitly documented? [Completeness, Gap] — updated in data-model.md §IModuleRepository
- [x] CHK021 - Is the `INotebookModuleStyleRepository`'s role in the atomic notebook-creation flow (staging all 12 style records via `AddAsync` before `IUnitOfWork.CommitAsync`) explicitly specified? [Completeness, Spec §User Story 2]

---

## Requirement Clarity

- [x] CHK022 - Is `RepositoryBase<TEntity, TDomain>` specified with its generic type parameter roles documented — `TEntity` = EF entity type, `TDomain` = domain model type — and the `where TEntity : class` constraint? [Clarity, data-model.md §RepositoryBase]
- [x] CHK023 - Is it specified which EF Core query method `RepositoryBase.GetByIdAsync` uses? [Gap] — resolved: `FirstOrDefaultAsync` (always queries DB, bypasses change tracker), documented in data-model.md §RepositoryBase
- [x] CHK024 - Is it specified how `RepositoryBase.Update` signals EF Core to track the entity? [Gap] — resolved: `context.Update(entityModel)` (marks all scalar properties modified), documented in data-model.md §RepositoryBase
- [x] CHK025 - Is the primary constructor (`C# 12+`) requirement for repository implementations stated, and is it clear that `AppDbContext` and `IMapper` are the injected parameters? [Clarity, quickstart.md §Step 5]
- [x] CHK026 - Is it specified whether `AddRepositories()` is responsible for registering the AutoMapper `EntityToDomainProfile` in DI? [Gap] — resolved: AutoMapper registered separately in `Program.cs` via assembly scanning; `AddRepositories()` does not register profiles. Documented in quickstart.md §Step 8.

---

## Requirement Consistency

- [x] CHK027 - Are `CancellationToken` parameters consistently named `ct` (not `cancellationToken`) and consistently defaulted to `= default` across all 11 interface method signatures? [Consistency, contracts §all]
- [x] CHK028 - Are all collection-returning methods consistently typed as `IReadOnlyList<T>` (not `List<T>`, `IEnumerable<T>`, or `ICollection<T>`) across all 11 interfaces? [Consistency, contracts §all]
- [x] CHK029 - Are the four exception subclass status codes (`NotFoundException` = 404, `ConflictException` = 409, `ForbiddenException` = 403, `ValidationException` = 422) consistent with the API Contract Discipline HTTP code requirements? [Consistency, Plan §Constitution Check G7]
- [x] CHK030 - Do all "GetWith…" methods use a consistent tuple return type pattern — `(TParent Entity, IReadOnlyList<TChild> Children)?` — with no mix of tuple vs separate collection returns? [Consistency, data-model.md §D5]

---

## Unit of Work Exception Contract

- [x] CHK031 - Is the `UoW`-bypass exception for `RevokeAllForUserAsync` documented in both (a) the interface XML doc and (b) the research rationale? [Completeness, contracts §IRefreshTokenRepository + research.md §D2]
- [x] CHK032 - Is the caller obligation stated explicitly — callers MUST NOT call `IUnitOfWork.CommitAsync` after `RevokeAllForUserAsync`? [Clarity, contracts §IRefreshTokenRepository] — strengthened: explicit MUST NOT added to XML doc
- [x] CHK033 - Are the two exclusive use cases for `RevokeAllForUserAsync` (logout-all-devices, account deletion) enumerated in the contract? [Completeness, Spec §FR-015]
- [x] CHK034 - Is the implementation mechanism specified with enough precision — `ExecuteUpdateAsync` with the exact predicate? [Clarity, quickstart.md §Step 6]

---

## Implementation Specification

- [x] CHK035 - Are all 11 `EntityModel → DomainModel` AutoMapper mapping pairs explicitly enumerated in a table, with no entity omitted? [Completeness, data-model.md §AutoMapper Profile Mappings]
- [x] CHK036 - Is the DI extension method name (`AddRepositories`), its file path (`Application/Extensions/ServiceCollectionExtensions.cs`), and the call site (`Program.cs`) all specified? [Completeness, quickstart.md §Step 8]
- [x] CHK037 - Is the `IChordRepository.SearchAsync` filter implementation approach specified as server-side SQL (no client-side evaluation)? [Gap] — updated: explicit prohibition on `.ToList()` before filtering added to data-model.md §IChordRepository
- [x] CHK038 - Is the `CheckOverlapAsync` overlap predicate confirmed to be EF LINQ translatable for server-side execution? [Clarity, Plan §Phase 1]

---

## ℹ️ Informational — Test Coverage Requirements

- [x] CHK039 - Are all four `CheckOverlapAsync` test scenarios (empty page, non-overlapping, overlapping, self-exclusion via `excludeModuleId`) listed in the plan? [Completeness, plan.md §Test Coverage]
- [x] CHK040 - Are all three `GetExpiredExportsAsync` scenarios (cutoff boundary, `Failed`-status exclusion, all-recent returns empty) listed in the plan? [Completeness, plan.md §Test Coverage]
- [x] CHK041 - Are test scenarios for `GetActiveExportForNotebookAsync` specified? [Gap] — added: Pending returns export, Failed returns null, no-export returns null. plan.md §Test Coverage
- [x] CHK042 - Are integration test scenarios for `RevokeAllForUserAsync` specified? [Gap] — added: revokes only active tokens, commits immediately without CommitAsync. plan.md §Test Coverage
- [x] CHK043 - Is the in-memory database isolation strategy (unique `Guid`-named database per test, no shared state) documented for the integration test classes? [Completeness, quickstart.md §Step 9]

---

## Dependencies and Assumptions

- [x] CHK044 - Is the assumption that all 12 `AppDbContext` `DbSet<T>` properties already exist explicitly validated against the actual source? [Assumption, Spec §Assumptions] — confirmed by reading AppDbContext.cs
- [x] CHK045 - Is the research D3 override (active PdfExport status includes `Pending`) propagated to the spec's Assumptions section? [Consistency, research.md §D3] — updated in spec.md §Assumptions; "Expired" status removed (not a real enum value)
- [x] CHK046 - Is the `Domain/Exceptions/BusinessException.cs` base class compatible with the four new subclasses? [Dependency] — identified incompatibility: `StatusCode` is `protected init`, not overridable. Example corrected in plan.md §Exception Subclass Pattern and quickstart.md §Step 5 to use constructor-body assignment (`StatusCode = 404`) instead of `protected override`.

---

## Notes

- All 46 items resolved. No blockers remain.
- Docs updated during review: data-model.md, contracts/domain-interfaces.md, quickstart.md, plan.md, spec.md
- Decisions made during review: modules sorted GridY/GridX asc (CHK012), FirstOrDefaultAsync for GetByIdAsync (CHK023), context.Update() for Update (CHK024), AutoMapper registered separately in Program.cs (CHK026).
