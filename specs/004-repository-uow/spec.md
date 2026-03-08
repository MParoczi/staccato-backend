# Feature Specification: Repository Pattern and Unit of Work

**Feature Branch**: `004-repository-uow`
**Created**: 2026-03-08
**Status**: Draft
**Input**: User description: "Implement the repository pattern and unit of work in the Repository project for the Staccato application."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Generic Data Access Contract (Priority: P1)

A service that needs to retrieve or persist an entity (e.g., a Notebook, a User, a Module) does so through a typed repository interface. The service has no knowledge of how data is stored — it only calls methods like `GetByIdAsync`, `AddAsync`, `Remove`, or `Update`. This abstraction allows the service to be tested with a mock repository, with no database required.

**Why this priority**: Every other story depends on the generic `IRepository<T>` base contract existing first. Without it, none of the specific repository interfaces can be defined.

**Independent Test**: Can be fully tested by instantiating a service with a Moq-backed `IRepository<T>` and verifying that calls are forwarded to the mock without hitting any database.

**Acceptance Scenarios**:

1. **Given** a service that depends on `IRepository<Notebook>`, **When** `GetByIdAsync(id)` is called, **Then** the service returns the domain model returned by the repository without any additional transformation.
2. **Given** a service that depends on `IRepository<User>`, **When** `AddAsync(user)` is called, **Then** the entity is staged for insertion; no data is persisted until `IUnitOfWork.CommitAsync` is called.
3. **Given** a service that depends on `IRepository<Lesson>`, **When** `Remove(lesson)` is called, **Then** the entity is staged for deletion; no data is persisted until commit.

---

### User Story 2 — Atomic Persistence via Unit of Work (Priority: P1)

A service that creates a Notebook also creates 12 `NotebookModuleStyle` records in the same operation. The notebook is staged via `INotebookRepository` and each style is staged via `INotebookModuleStyleRepository`. A single call to `IUnitOfWork.CommitAsync` persists all staged changes as one atomic database transaction, guaranteeing that partial-creation states never exist.

**Why this priority**: Data integrity across the application depends on transactional commits. Services like notebook creation, lesson creation, and PDF export status updates each require atomic multi-step persistence.

**Independent Test**: Can be fully tested by seeding a service with Moq-backed repositories and a Moq-backed `IUnitOfWork`, then asserting that `CommitAsync` is called exactly once per operation regardless of how many Add/Update/Remove calls were made.

**Acceptance Scenarios**:

1. **Given** a service that calls `AddAsync` on two different repositories, **When** `CommitAsync` is called once, **Then** both staged records are written to the database in a single transaction.
2. **Given** a service that throws an exception before calling `CommitAsync`, **When** the exception propagates, **Then** no data is written to the database.
3. **Given** `CommitAsync` is called with a `CancellationToken` that is already cancelled, **When** the call is made, **Then** an `OperationCanceledException` is thrown and no data is written.

---

### User Story 3 — Entity-Specific Query Operations (Priority: P2)

Services need to execute queries that are meaningful in the domain context — for example, finding all notebooks belonging to a user, checking whether a module placement overlaps an existing module, or searching chords by root note and quality. These operations are exposed as named methods on specific repository interfaces, keeping query intent explicit and keeping domain logic out of the data access layer.

**Why this priority**: Without entity-specific query methods, services would be forced to retrieve entire collections and filter in memory — which is both inefficient and incorrect for overlap detection and search scenarios.

**Independent Test**: Can be tested independently by calling each specific repository method through a Moq-backed interface and verifying the correct filters are applied, or via integration tests against an in-memory database.

**Acceptance Scenarios**:

1. **Given** a page with three existing modules, **When** `IModuleRepository.CheckOverlapAsync` is called with coordinates that would overlap one of them, **Then** the method returns `true`.
2. **Given** the same page with three existing modules, **When** `CheckOverlapAsync` is called with the ID of the overlapping module as `excludeModuleId`, **Then** the method returns `false` (the module does not overlap with itself).
3. **Given** a chord database with chords for multiple instruments, **When** `IChordRepository.SearchAsync(instrumentId, root: "A", quality: null)` is called, **Then** only chords with root "A" for that instrument are returned, regardless of quality.
4. **Given** a notebook with multiple lessons created at different times, **When** `ILessonRepository.GetByNotebookIdOrderedByCreatedAtAsync` is called, **Then** lessons are returned in ascending creation-time order.

---

### User Story 4 — PDF Export and Refresh Token Lifecycle Queries (Priority: P3)

The PDF export background service needs to query whether an active export already exists for a notebook, retrieve all exports that have expired (older than 24 hours), and look up exports by user. The authentication service needs to look up refresh tokens and revoke them during rotation. These are lifecycle queries with precise time-boundary and state-based semantics.

**Why this priority**: These queries are narrowly scoped to specific background service and authentication flows. The rest of the application does not depend on them.

**Independent Test**: Can be tested via integration tests with seeded data using in-memory EF Core.

**Acceptance Scenarios**:

1. **Given** a notebook with an export in `Processing` status, **When** `IPdfExportRepository.GetActiveExportForNotebookAsync` is called, **Then** that export is returned.
2. **Given** exports where one was created 25 hours ago and one was created 1 hour ago, **When** `GetExpiredExportsAsync` is called with a 24-hour threshold, **Then** only the 25-hour-old export is returned.
3. **Given** a user with two refresh tokens (one revoked, one active), **When** `IRefreshTokenRepository` is queried for active tokens for that user, **Then** only the active token is returned.

---

### Edge Cases

- `CheckOverlapAsync` must return `false` when the page has no modules at all.
- `CheckOverlapAsync` must return `false` when a module is moved to its current position (same bounds, same `excludeModuleId`).
- `GetByIdAsync` must return `null` (not throw) when no record matches the given `Guid`.
- `IChordRepository.SearchAsync` must accept `null` for both `root` and `quality` and return all chords for the given instrument.
- `IPdfExportRepository.GetExpiredExportsAsync` must use a caller-supplied UTC cutoff time — it must not call `DateTime.UtcNow` internally, as that would make the method non-deterministic in tests.
- `IUserSavedPresetRepository` must return an empty list (not throw) when a user has no saved presets.
- Concurrent module placement on the same page by simultaneous requests is a known accepted risk — no repository-level locking is required. The overlap check is not atomic with the subsequent insert; this low-probability race condition is accepted and may be revisited in a future iteration.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST define a generic `IRepository<T>` base interface with four operations: retrieve by ID, stage an addition, stage a removal, and mark an entity as updated.
- **FR-002**: The system MUST define `IUnitOfWork` with a single `CommitAsync(CancellationToken)` method that persists all staged changes atomically.
- **FR-003**: The system MUST define eleven specific repository interfaces — one for each domain entity — each extending `IRepository<T>` with query methods relevant to that entity.
- **FR-004**: Every specific repository interface MUST be placed in `Domain/Interfaces/Repositories/`; `IUnitOfWork` MUST be placed in `Domain/Interfaces/`.
- **FR-005**: The system MUST provide a concrete implementation for each of the eleven repository interfaces, using the EF Core database context as the data source.
- **FR-006**: Repository implementations MUST return domain model types, not entity model types; mapping between the two is performed inside the repository using AutoMapper.
- **FR-007**: Repository implementations MUST NOT call `SaveChanges` or `SaveChangesAsync` — persistence is the exclusive responsibility of `IUnitOfWork.CommitAsync`.
- **FR-008**: `IModuleRepository` MUST expose an overlap-check operation that accepts the page ID, the proposed grid rectangle, and an optional module ID to exclude from the check (for update scenarios).
- **FR-009**: `IChordRepository` MUST expose a search operation that accepts an instrument ID and optional root note and quality filters; passing `null` for optional filters must return all matching records without filtering on that dimension.
- **FR-010**: `IPdfExportRepository` MUST expose operations to: find an active export for a specific notebook, find all exports older than a caller-supplied UTC cutoff datetime, and list exports by user.
- **FR-011**: `IUserRepository` MUST expose operations to look up a user by email address, look up a user by Google OAuth subject identifier, and retrieve a user together with their active (non-revoked) refresh tokens.
- **FR-012**: `ILessonRepository` MUST return lessons ordered by creation time (ascending) when querying by notebook ID.
- **FR-013**: All repository methods and the `CommitAsync` method MUST accept a `CancellationToken` parameter and pass it through to all async database calls.
- **FR-014**: Concrete repository implementations and `UnitOfWork` MUST be placed in `Repository/Repositories/` and `Repository/` respectively, and MUST receive the EF Core database context through constructor injection.
- **FR-015**: `IRefreshTokenRepository` MUST expose `RevokeAllForUserAsync(userId, CancellationToken)` as a single-operation bulk revocation method, used by logout-all-devices and account deletion flows. Single-token revocation (rotation) uses the base `Update` + `IUnitOfWork.CommitAsync` pattern.

### Key Entities

- **IRepository\<T\>**: Generic data access contract. Provides: retrieve-by-ID (async), stage-addition (async), stage-removal, mark-updated. The type parameter `T` is a domain model type.
- **IUnitOfWork**: Persistence coordinator. Single responsibility: flush all staged repository changes to the database as one atomic transaction.
- **IUserRepository**: Extends IRepository\<User\>. Additional queries: by email, by Google subject ID, with active refresh tokens eagerly loaded.
- **INotebookRepository**: Extends IRepository\<Notebook\>. Additional queries: all notebooks by owner user ID, single notebook with its module styles eagerly loaded.
- **ILessonRepository**: Extends IRepository\<Lesson\>. Additional queries: all lessons for a notebook ordered by creation time, single lesson with its pages eagerly loaded.
- **ILessonPageRepository**: Extends IRepository\<LessonPage\>. Additional queries: all pages for a lesson ordered by page number, single page with its modules eagerly loaded.
- **IModuleRepository**: Extends IRepository\<Module\>. Additional queries: all modules for a page, rectangle-overlap detection with optional exclusion.
- **IChordRepository**: Extends IRepository\<Chord\>. Additional queries: filtered search by instrument/root/quality.
- **IInstrumentRepository**: Extends IRepository\<Instrument\>. Additional queries: retrieve all instruments (`GetAllAsync`) to support the notebook creation instrument picker. Instruments are read-only after seeding — no add/update/remove operations will be called in practice.
- **IPdfExportRepository**: Extends IRepository\<PdfExport\>. Additional queries: active export for a notebook, expired exports by UTC cutoff, exports by user.
- **IRefreshTokenRepository**: Extends IRepository\<RefreshToken\>. Additional queries: look up by raw token value, retrieve all active tokens for a user. Bulk revocation operation: `RevokeAllForUserAsync(userId)` — executes as a single database operation for logout-all-devices and account deletion flows. Single-token rotation uses the base `Update` method followed by `IUnitOfWork.CommitAsync`.
- **INotebookModuleStyleRepository**: Extends IRepository\<NotebookModuleStyle\>. Additional queries: all styles for a notebook (`GetByNotebookIdAsync`), a single style by notebook ID and module type (`GetByNotebookIdAndTypeAsync`).
- **IUserSavedPresetRepository**: Extends IRepository\<UserSavedPreset\>. Additional queries: all presets for a user.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every service in the `Domain` project can be instantiated in a unit test with all repository and unit-of-work dependencies replaced by test doubles, with no database, HTTP, or file-system setup required.
- **SC-002**: A multi-step domain operation (e.g., creating a notebook with 12 styles) results in exactly one call to `CommitAsync`, and all records appear together or not at all.
- **SC-003**: All eleven repository interfaces resolve from the dependency injection container at application startup without error.
- **SC-004**: Overlap detection returns correct results for all placement cases: no modules on page, non-overlapping modules, overlapping modules, and self-exclusion via `excludeModuleId`.
- **SC-005**: Chord search returns only the records matching the provided filters; providing no filters returns the full set for the given instrument.
- **SC-006**: Expired-export queries return only records whose creation timestamp is strictly older than the provided cutoff, and never depend on system clock calls inside the repository.
- **SC-007**: All repository and unit-of-work methods respect cancellation — passing an already-cancelled token causes the call to exit promptly without writing data.

---

## Assumptions

- Domain model types for all eleven entities already exist in `DomainModels/` and entity model types exist in `EntityModels/`. AutoMapper profiles between them will be defined in or alongside the repository implementations.
- `AppDbContext` already exposes `DbSet<T>` properties for all eleven entity types (covered by the EF Core persistence feature).
- The `RefreshToken` entity has an `IsRevoked` boolean property and a `UserId` foreign key, enabling the active-token query on `IUserRepository` and `IRefreshTokenRepository`.
- `PdfExport` has a `Status` property (enum with at least `Processing`/`Ready`/`Failed` values), a `NotebookId` foreign key, a `UserId` foreign key, and a `CreatedAt` UTC datetime, enabling the lifecycle queries on `IPdfExportRepository`.
- The `UserSavedPreset` entity has a `UserId` foreign key.
- "Active" refresh token means `IsRevoked == false` and the token has not passed its expiry.
- "Active" PDF export means status is `Pending`, `Processing`, or `Ready` — anything except `Failed`. (`Pending` is included because an export queued but not yet picked up by the background service must block a new export request. There is no `Expired` status value in the enum.)
- Dependency injection wiring (registering all repositories and `UnitOfWork` in the DI container) is handled in `Application/Program.cs` and is outside the scope of this specification — it is assumed to be a straightforward extension method call.

---

## Clarifications

### Session 2026-03-08

- Q: How should NotebookModuleStyle entities be persisted and updated — through a dedicated repository or an existing one? → A: Add `INotebookModuleStyleRepository` as an eleventh interface with `GetByNotebookIdAsync` and `GetByNotebookIdAndTypeAsync`; it is a first-class entity with its own CRUD lifecycle separate from INotebookRepository.
- Q: Should an instrument-listing operation be added, and if so where? → A: Add `GetAllAsync()` to `IInstrumentRepository` only; instruments are small, immutable, and fully enumerable — scope the method narrowly rather than widening the base interface.
- Q: Should token revocation be a dedicated repository method or handled via Update + CommitAsync? → A: Add `RevokeAllForUserAsync(userId)` as a dedicated bulk method (logout-all-devices, account deletion); single-token rotation continues to use `Update` + `CommitAsync`.
- Q: Should the spec require protection against concurrent module placement conflicts? → A: No — accept the low-probability race condition as a known limitation; no repository-level locking required at this stage.
