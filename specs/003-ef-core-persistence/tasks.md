# Tasks: EF Core Entity Models and Database Persistence

**Input**: Design documents from `/specs/003-ef-core-persistence/`
**Branch**: `003-ef-core-persistence`
**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Data Model**: [data-model.md](data-model.md)

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Parallelizable — different files, no dependency on incomplete siblings
- **[US1]**: User Story 1 — Persistent Data Storage (P1)
- **[US2]**: User Story 2 — Automated Database Initialization and Seeding (P2)
- **[US3]**: User Story 3 — Data Integrity Constraint Enforcement (P3, delivered alongside US1 via EF configurations)

---

## Phase 1: Setup

**Purpose**: Pre-requisite changes outside the EntityModels/Persistence projects that must land before entity classes are written.

- [x] T001 Update `DomainModels/Models/PdfExport.cs` — add `public List<Guid>? LessonIds { get; set; }` property (null = entire notebook export) to keep domain model in sync with the forthcoming `PdfExportEntity.LessonIdsJson` field [FR-039]

---

## Phase 2: Foundational — Entity Classes and AppDbContext

**Purpose**: All 12 EF Core entity classes plus `AppDbContext`. These are shared prerequisites that every EF configuration and seeder depends on. All entity classes can be written in parallel.

**⚠️ CRITICAL**: No EF configuration, migration, or seeder can begin until all entity classes and `AppDbContext` are complete.

- [ ] T002 [P] Create `EntityModels/Entities/UserEntity.cs` — mirror `User` domain model scalar properties (`Id`, `Email`, `PasswordHash?`, `GoogleId?`, `FirstName`, `LastName`, `AvatarUrl?`, `CreatedAt`, `ScheduledDeletionAt?`, `Language`); add navigation collections `ICollection<NotebookEntity> Notebooks`, `ICollection<RefreshTokenEntity> RefreshTokens`, `ICollection<UserSavedPresetEntity> UserSavedPresets`, `ICollection<PdfExportEntity> PdfExports`; namespace `EntityModels.Entities` [FR-001, FR-002]

- [ ] T003 [P] Create `EntityModels/Entities/RefreshTokenEntity.cs` — mirror `RefreshToken` scalar properties (`Id`, `Token`, `UserId`, `ExpiresAt`, `CreatedAt`, `IsRevoked`); add navigation `UserEntity User` [FR-001, FR-002]

- [ ] T004 [P] Create `EntityModels/Entities/UserSavedPresetEntity.cs` — mirror `UserSavedPreset` scalar properties (`Id`, `UserId`, `Name`, `StylesJson`); add navigation `UserEntity User` [FR-001, FR-002]

- [ ] T005 [P] Create `EntityModels/Entities/SystemStylePresetEntity.cs` — mirror `SystemStylePreset` scalar properties (`Id`, `Name`, `DisplayOrder`, `IsDefault`, `StylesJson`); no navigation properties [FR-001, FR-002]

- [ ] T006 [P] Create `EntityModels/Entities/InstrumentEntity.cs` — mirror `Instrument` scalar properties (`Id`, `Key`, `DisplayName`, `StringCount`); add navigation `ICollection<ChordEntity> Chords` [FR-001, FR-002]

- [ ] T007 [P] Create `EntityModels/Entities/ChordEntity.cs` — mirror `Chord` scalar properties (`Id`, `InstrumentId`, `Name`, `Suffix`, `PositionsJson`); add navigation `InstrumentEntity Instrument` [FR-001, FR-002]

- [ ] T008 [P] Create `EntityModels/Entities/NotebookEntity.cs` — mirror `Notebook` scalar properties (`Id`, `UserId`, `Title`, `InstrumentId`, `PageSize`, `CreatedAt`, `UpdatedAt`); add navigations `UserEntity User`, `InstrumentEntity Instrument`, `ICollection<LessonEntity> Lessons`, `ICollection<NotebookModuleStyleEntity> ModuleStyles`, `ICollection<PdfExportEntity> PdfExports` [FR-001, FR-002]

- [ ] T009 [P] Create `EntityModels/Entities/NotebookModuleStyleEntity.cs` — mirror `NotebookModuleStyle` scalar properties (`Id`, `NotebookId`, `ModuleType`, `StylesJson`); add navigation `NotebookEntity Notebook` [FR-001, FR-002]

- [ ] T010 [P] Create `EntityModels/Entities/LessonEntity.cs` — mirror `Lesson` scalar properties (`Id`, `NotebookId`, `Title`, `CreatedAt`, `UpdatedAt`); add navigations `NotebookEntity Notebook`, `ICollection<LessonPageEntity> LessonPages` [FR-001, FR-002]

- [ ] T011 [P] Create `EntityModels/Entities/LessonPageEntity.cs` — mirror `LessonPage` scalar properties (`Id`, `LessonId`, `PageNumber`); add navigations `LessonEntity Lesson`, `ICollection<ModuleEntity> Modules` [FR-001, FR-002]

- [ ] T012 [P] Create `EntityModels/Entities/ModuleEntity.cs` — mirror `Module` scalar properties (`Id`, `LessonPageId`, `ModuleType`, `GridX`, `GridY`, `GridWidth`, `GridHeight`, `ContentJson`); add navigation `LessonPageEntity LessonPage` [FR-001, FR-002]

- [ ] T013 [P] Create `EntityModels/Entities/PdfExportEntity.cs` — mirror `PdfExport` scalar properties (`Id`, `NotebookId`, `UserId`, `Status`, `CreatedAt`, `CompletedAt?`, `BlobReference?`) plus additional `LessonIdsJson` (`string?`, nullable — SQL NULL = whole notebook); add navigations `NotebookEntity Notebook`, `UserEntity User` [FR-001, FR-002, FR-005]

- [ ] T014 Create `Persistence/Context/AppDbContext.cs` — inherit `DbContext`, primary constructor accepting `DbContextOptions<AppDbContext>`; add `DbSet<T>` expression-body property (`=> Set<T>()`) for all 12 entity types; override `OnModelCreating` to call `modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)` [FR-007, FR-008]

---

## Phase 3: User Story 1 — Persistent Data Storage (P1) + User Story 3 — Constraint Enforcement (P3)

**Goal**: Every entity persists correctly to SQL Server with proper column types, max lengths, FK relationships, cascade/restrict delete rules, unique indexes, filtered indexes, and the partial unique index for active exports. US3 constraint enforcement is delivered by these same configuration tasks.

**Independent Test**: Spin up a SQL Server instance, run `dotnet ef database update`, insert a row for each entity type, delete a `User` record and verify all 7 dependent types cascade, attempt duplicate email/GoogleId/active-export inserts and confirm each is rejected.

All 12 configuration files can be written in parallel (each is its own file with no sibling dependency).

- [ ] T015 [P] [US1] Create `Persistence/Configurations/UserConfiguration.cs` implementing `IEntityTypeConfiguration<UserEntity>` — table `Users`; PK `Id`; `Email` required max 256; `FirstName` required max 100; `LastName` required max 100; `AvatarUrl` optional `nvarchar(max)`; `GoogleId` optional `nvarchar(max)`; `PasswordHash` optional `nvarchar(max)`; unique index on `Email`; filtered unique index on `GoogleId` using `.HasFilter("[GoogleId] IS NOT NULL")` [FR-009, FR-010, FR-011, FR-012, FR-026, FR-041, US3]

- [ ] T016 [P] [US1] Create `Persistence/Configurations/RefreshTokenConfiguration.cs` implementing `IEntityTypeConfiguration<RefreshTokenEntity>` — table `RefreshTokens`; PK `Id`; `Token` required `nvarchar(max)`; unique index on `Token`; FK `UserId → Users` cascade delete [FR-009, FR-010, FR-020, FR-021, US3]

- [ ] T017 [P] [US1] Create `Persistence/Configurations/UserSavedPresetConfiguration.cs` implementing `IEntityTypeConfiguration<UserSavedPresetEntity>` — table `UserSavedPresets`; PK `Id`; `Name` required max 200; `StylesJson` required `nvarchar(max)` (camelCase JSON array of 12 style objects); FK `UserId → Users` cascade delete [FR-009, FR-010, FR-023, FR-025, FR-041]

- [ ] T018 [P] [US1] Create `Persistence/Configurations/SystemStylePresetConfiguration.cs` implementing `IEntityTypeConfiguration<SystemStylePresetEntity>` — table `SystemStylePresets`; PK `Id`; `Name` required max 200; `DisplayOrder` required; `IsDefault` required; `StylesJson` required `nvarchar(max)` (camelCase JSON array of 12 style objects); no FK [FR-009, FR-010, FR-025, FR-041]

- [ ] T019 [P] [US1] Create `Persistence/Configurations/InstrumentConfiguration.cs` implementing `IEntityTypeConfiguration<InstrumentEntity>` — table `Instruments`; PK `Id`; `Key` required (int enum); `DisplayName` required max 200; `StringCount` required; unique index on `Key` [FR-009, FR-010, FR-006, US3]

- [ ] T020 [P] [US1] Create `Persistence/Configurations/ChordConfiguration.cs` implementing `IEntityTypeConfiguration<ChordEntity>` — table `Chords`; PK `Id`; `Name` required max 200; `Suffix` required max 200; `PositionsJson` required `nvarchar(max)` (camelCase JSON array of position objects); FK `InstrumentId → Instruments` with `DeleteBehavior.Restrict` [FR-009, FR-010, FR-022, FR-025, FR-041, US3]

- [ ] T021 [P] [US1] Create `Persistence/Configurations/NotebookConfiguration.cs` implementing `IEntityTypeConfiguration<NotebookEntity>` — table `Notebooks`; PK `Id`; `Title` required `nvarchar(max)`; `PageSize` required (int enum); `CreatedAt` required; `UpdatedAt` required; FK `UserId → Users` cascade delete [FR-009]; FK `InstrumentId → Instruments` with `DeleteBehavior.Restrict` [FR-013, FR-040, US3]

- [ ] T022 [P] [US1] Create `Persistence/Configurations/NotebookModuleStyleConfiguration.cs` implementing `IEntityTypeConfiguration<NotebookModuleStyleEntity>` — table `NotebookModuleStyles`; PK `Id`; `ModuleType` required (int enum); `StylesJson` required `nvarchar(max)` (camelCase single-style JSON object); FK `NotebookId → Notebooks` cascade delete; composite unique index on `(NotebookId, ModuleType)` [FR-009, FR-010, FR-017, FR-018, FR-025, FR-041, US3]

- [ ] T023 [P] [US1] Create `Persistence/Configurations/LessonConfiguration.cs` implementing `IEntityTypeConfiguration<LessonEntity>` — table `Lessons`; PK `Id`; `Title` required `nvarchar(max)`; `CreatedAt` required; `UpdatedAt` required; FK `NotebookId → Notebooks` cascade delete [FR-009, FR-010, FR-014]

- [ ] T024 [P] [US1] Create `Persistence/Configurations/LessonPageConfiguration.cs` implementing `IEntityTypeConfiguration<LessonPageEntity>` — table `LessonPages`; PK `Id`; `PageNumber` required; FK `LessonId → Lessons` cascade delete [FR-009, FR-010, FR-015]

- [ ] T025 [P] [US1] Create `Persistence/Configurations/ModuleConfiguration.cs` implementing `IEntityTypeConfiguration<ModuleEntity>` — table `Modules`; PK `Id`; `ModuleType` required (int enum); `GridX`, `GridY`, `GridWidth`, `GridHeight` all required int; `ContentJson` required `nvarchar(max)`; FK `LessonPageId → LessonPages` cascade delete [FR-009, FR-010, FR-016, FR-025, FR-041]

- [ ] T026 [P] [US1] Create `Persistence/Configurations/PdfExportConfiguration.cs` implementing `IEntityTypeConfiguration<PdfExportEntity>` — table `PdfExports`; PK `Id`; `Status` required (int enum); `CreatedAt` required; `CompletedAt` optional; `BlobReference` optional `nvarchar(max)`; `LessonIdsJson` optional `nvarchar(max)`; FK `NotebookId → Notebooks` cascade delete; FK `UserId → Users` with `DeleteBehavior.ClientCascade` (SQL Server prohibits two cascade paths from Users to PdfExports; ClientCascade generates `ON DELETE NO ACTION` in migration but EF Core deletes PdfExports in memory before User — see FR-042); partial unique index on `NotebookId` using `.HasFilter("[Status] = 0 OR [Status] = 1")` [FR-009, FR-010, FR-019, FR-024, FR-025, FR-041, FR-042, US3]

- [ ] T027 Populate `AddDatabase()` extension method in `Application/Extensions/ServiceCollectionExtensions.cs` — call `services.AddDbContext<AppDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")))` and register `InstrumentSeeder`, `ChordSeeder`, `SystemStylePresetSeeder`, and `DbInitializer` as scoped services [FR-027]

- [ ] T028 Generate the initial EF Core migration by running: `dotnet ef migrations add InitialCreate --project Persistence/Persistence.csproj --startup-project Application/Application.csproj` — **requires T027 complete first** (EF tooling resolves `AppDbContext` from the Application startup project, which requires `AddDatabase()` to be populated); verify the generated `Migrations/[timestamp]_InitialCreate.cs` contains: all 12 `CreateTable` calls, correct `nvarchar(max)` for JSON columns, `UNIQUE` filtered index on `Users.GoogleId` with `WHERE [GoogleId] IS NOT NULL`, `UNIQUE` filtered index on `PdfExports.NotebookId` with `WHERE [Status] = 0 OR [Status] = 1`, composite unique on `NotebookModuleStyles`, `ReferentialAction.Restrict` on `Chords.InstrumentId` and `Notebooks.InstrumentId`, `NO ACTION` on `PdfExports.UserId` (generated by `ClientCascade`), `ReferentialAction.Cascade` on all other FKs [FR-006, FR-042, US3]

---

## Phase 4: User Story 2 — Automated Database Initialization and Seeding (P2)

**Goal**: On first startup against a fresh SQL Server instance, `DbInitializer` creates/migrates the database and populates it with 7 instruments, all guitar chords from `guitar_chords.json`, and 5 system style presets — all idempotently.

**Independent Test**: Delete the database, run `dotnet run --project Application/Application.csproj`, verify `SELECT COUNT(*) FROM Instruments` = 7, `SELECT COUNT(*) FROM Chords` = count of entries in `guitar_chords.json`, `SELECT COUNT(*) FROM SystemStylePresets` = 5, `SELECT Name, IsDefault FROM SystemStylePresets ORDER BY DisplayOrder` shows Classic first with `IsDefault = 1`. Run again — no duplicate rows.

- [ ] T029 Create `Persistence/Data/guitar_chords.json` — flat JSON array of chord objects, each with `name` (root note: A, Bb, B, C, C#, D, Eb, E, F, F#, G, Ab), `suffix` (quality string), and `positions` (array of ChordPosition objects with camelCase fields: `label`, `baseFret`, `barre`, `strings`). Each `strings` entry: `{ "string": N, "state": "open"|"fretted"|"muted", "fret": N|null, "finger": N|null }`. Cover at minimum: major, minor, 7, maj7, m7, dim, aug, sus2, sus4, m7b5, dim7, add9 — all 12 root notes — 2–3 positions each. No two entries may share the same `name`+`suffix`. All property names must be camelCase [FR-036, FR-037, FR-041]

- [ ] T030 Update `Persistence/Persistence.csproj` — add `<ItemGroup><Content Include="Data\guitar_chords.json"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content></ItemGroup>` so the file is available at the application base directory at runtime [FR-035]

- [ ] T031 [P] Create `Persistence/Seed/InstrumentSeeder.cs` — constructor injects `AppDbContext`; single `SeedAsync(CancellationToken ct = default)` method; guard: `if (await context.Instruments.AnyAsync(ct)) return;`; insert 7 `InstrumentEntity` rows using `Guid.NewGuid()` PKs — Guitar6String ("6-String Guitar", 6), Guitar7String ("7-String Guitar", 7), Bass4String ("4-String Bass", 4), Bass5String ("5-String Bass", 5), Ukulele4String ("Ukulele", 4), Banjo4String ("4-String Banjo", 4), Banjo5String ("5-String Banjo", 5); call `await context.SaveChangesAsync(ct)` [FR-034]

- [ ] T032 Create `Persistence/Seed/ChordSeeder.cs` — constructor injects `AppDbContext`; single `SeedAsync(CancellationToken ct = default)` method; guard: `if (await context.Chords.AnyAsync(ct)) return;`; resolve `guitar_chords.json` path via `Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "guitar_chords.json")`; throw `InvalidOperationException` with file path if file missing; deserialize using `System.Text.Json` with camelCase policy; throw if result is null or empty; validate each entry has non-empty `name`, `suffix`, and `positions`; throw on missing fields or duplicate `name`+`suffix`; look up Guitar6String instrument ID; insert one `ChordEntity` per record using `Guid.NewGuid()` PKs with `PositionsJson` re-serialised from the raw `positions` `JsonElement`; call `await context.SaveChangesAsync(ct)` [FR-035, FR-038]

- [ ] T033 [P] Create `Persistence/Seed/SystemStylePresetSeeder.cs` — constructor injects `AppDbContext`; single `SeedAsync(CancellationToken ct = default)` method; guard: `if (await context.SystemStylePresets.AnyAsync(ct)) return;`; insert 5 `SystemStylePresetEntity` rows using `Guid.NewGuid()` PKs with correct `DisplayOrder` (1–5), `IsDefault = true` for Classic only; each `StylesJson` is a camelCase JSON array of 12 objects (one per `ModuleType` enum value) each with all 9 style fields (`backgroundColor`, `borderColor`, `borderStyle`, `borderWidth`, `borderRadius`, `headerBgColor`, `headerTextColor`, `bodyTextColor`, `fontFamily`); Colorful preset MUST use the exact hex reference values from FR-031; Classic/Dark/Minimal/Pastel use theme-appropriate values per FR-031 descriptions; call `await context.SaveChangesAsync(ct)` [FR-029, FR-030, FR-031, FR-032, FR-033, FR-041]

- [ ] T034 Create `Persistence/DbInitializer.cs` — primary constructor injects `AppDbContext`, `InstrumentSeeder`, `ChordSeeder`, `SystemStylePresetSeeder`; single `InitializeAsync(CancellationToken ct = default)` method; skip `MigrateAsync` when provider is `InMemoryDatabase` (check `context.Database.ProviderName?.Contains("InMemory") == true`); otherwise call `await context.Database.MigrateAsync(ct)`; then `await instrumentSeeder.SeedAsync(ct)`, `await chordSeeder.SeedAsync(ct)`, `await presetSeeder.SeedAsync(ct)` in order [FR-027, FR-028]

- [ ] T035 Add `DbInitializer` invocation to `Application/Program.cs` — after `var app = builder.Build()` and before `app.Run()`, add: `using (var scope = app.Services.CreateScope()) { var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>(); await initializer.InitializeAsync(); }` [FR-027]

---

## Phase 5: Polish and Cross-Cutting Concerns

**Purpose**: Verification, documentation sync, and build validation.

- [ ] T036 Build the full solution and confirm zero errors and zero warnings: `dotnet build Staccato.sln` — fix any nullable reference type warnings in the new entity classes or configurations [Constitution §IX]

- [ ] T037 Update `specs/003-ef-core-persistence/data-model.md` — mark `PdfExport.cs` domain model as updated (T001 complete), confirm FK delete behavior table reflects FR-040 (`NotebookEntity.InstrumentId → Restrict`) and FR-042 (`PdfExports.UserId → NoAction`), confirm constraint inventory matches SC-004 (17 constraints)

---

## Phase 6: Unit and Integration Tests

**Purpose**: Satisfy Constitution §VIII and plan.md G11 — unit tests for seeder/initializer logic (happy path + all error paths); EF InMemory integration tests for entity persistence round-trips and cascade behaviour.

All test tasks in this phase are independent and can be written in parallel.

- [ ] T038 [P] Create `Tests/Unit/Persistence/DbInitializerTests.cs` — (a) happy path: verify `MigrateAsync` is called followed by all three seeders in order (InstrumentSeeder → ChordSeeder → SystemStylePresetSeeder); (b) InMemory skip: verify `MigrateAsync` is NOT called when `context.Database.ProviderName` contains "InMemory"; use Moq to mock seeder dependencies and a real InMemory `AppDbContext` for the provider-check branch [FR-027]

- [ ] T039 [P] Create `Tests/Unit/Persistence/InstrumentSeederTests.cs` — (a) happy path: given an empty Instruments table, `SeedAsync` inserts exactly 7 rows with correct `Key`, `DisplayName`, and `StringCount` values; (b) idempotency: given a non-empty Instruments table, `SeedAsync` exits without inserting any rows; use InMemory EF provider [FR-034]

- [ ] T040 [P] Create `Tests/Unit/Persistence/ChordSeederHappyPathTests.cs` — (a) happy path: given a valid `guitar_chords.json` fixture file and an empty Chords table, `SeedAsync` inserts one row per JSON entry with correct `InstrumentId`, `Name`, `Suffix`, and `PositionsJson`; (b) idempotency: given a non-empty Chords table, `SeedAsync` exits without inserting; use InMemory EF provider and a temp fixture file [FR-035]

- [ ] T041 [P] Create `Tests/Unit/Persistence/ChordSeederFailTests.cs` — six `[Fact]` tests, one per FR-038 failure case: (a) file missing → `InvalidOperationException` with file path in message; (b) invalid JSON content → exception with file path; (c) deserialised array is null or empty → exception; (d) chord entry missing `name`, `suffix`, or `positions` field → exception; (e) chord entry with empty `positions` array → exception; (f) two entries with identical `name`+`suffix` → exception; each test asserts the exception type is `InvalidOperationException` and the message contains the file path [FR-038]

- [ ] T042 [P] Create `Tests/Unit/Persistence/SystemStylePresetSeederTests.cs` — (a) happy path: given empty SystemStylePresets table, `SeedAsync` inserts exactly 5 rows; Classic row has `IsDefault = true`; all 5 rows have distinct `DisplayOrder` values 1–5; each row's `StylesJson` deserialises to an array of exactly 12 objects; (b) idempotency: non-empty table causes early exit; (c) Colorful row: deserialised `StylesJson` contains an entry for Theory with `backgroundColor` = `#E0F7FA`; use InMemory EF [FR-029, FR-030, FR-031, FR-032, FR-033]

- [ ] T043 [P] Create `Tests/Integration/Persistence/EntityPersistenceTests.cs` — for each of the 12 entity types: insert one row via InMemory `AppDbContext`, call `SaveChangesAsync`, detach the entity, query it back, assert all scalar properties match the inserted values exactly (no data loss, no truncation); this covers SC-001 and verifies JSON column round-trip fidelity [FR-001, FR-002, SC-001]

- [ ] T044 [P] Create `Tests/Integration/Persistence/CascadeDeleteTests.cs` — using InMemory `AppDbContext`: insert a User with one of each of the 7 dependent entity types (Notebook, Lesson, LessonPage, Module, RefreshToken, UserSavedPreset, PdfExport); delete the User and call `SaveChangesAsync`; assert the Notebooks, Lessons, LessonPages, Modules, NotebookModuleStyles, RefreshTokens, UserSavedPresets, and PdfExports tables are all empty afterwards [SC-006]

- [ ] T045 [P] Create `Tests/Integration/Persistence/MigrationInspectionTests.cs` — load the generated `InitialCreate` migration class in `Persistence/Migrations/` and assert the following via string inspection of the `.Up()` method source: (a) `WHERE [GoogleId] IS NOT NULL` filter exists; (b) `WHERE [Status] = 0 OR [Status] = 1` filter exists; (c) `onDelete: ReferentialAction.Restrict` appears exactly twice (for Chords.InstrumentId and Notebooks.InstrumentId); (d) `NO ACTION` appears for PdfExports.UserId; note — unique constraint violations cannot be tested against InMemory EF; schema constraints are verified through migration inspection [SC-004, FR-042]

---

## Dependencies

```
T001 (domain model)
  └─→ T013 (PdfExportEntity needs LessonIds on domain model for reference)

T002–T013 (entity classes, all parallel)
  └─→ T014 (AppDbContext needs all 12 entity types)
        └─→ T015–T026 (EF configurations, all parallel)
                └─→ T027 (AddDatabase registration)
                      └─→ T028 (migration — needs all configs + AppDbContext + DI registration)
                            └─→ T034 (DbInitializer calls MigrateAsync)

T029 (guitar_chords.json data file)
  └─→ T032 (ChordSeeder reads the file)
T030 (csproj CopyToOutputDirectory)
  └─→ T032 (ChordSeeder finds the file at runtime)

T027 (registers seeders in DI)
  └─→ T031, T032, T033 (seeders inject AppDbContext from DI)
        └─→ T034 (DbInitializer injects all 3 seeders)
              └─→ T035 (Program.cs resolves DbInitializer from DI)

T035 (all implementation complete)
  └─→ T036 (build validation)
  └─→ T037 (doc update)
```

## Parallel Execution

**Phase 2 — Entity classes (T002–T013)**: All 12 can be written simultaneously — each is an independent file in `EntityModels/Entities/`.

**Phase 3 — EF configurations (T015–T026)**: All 12 can be written simultaneously after T014 — each is an independent file in `Persistence/Configurations/`.

**Phase 4 — Seeders (T031 and T033)**: `InstrumentSeeder` and `SystemStylePresetSeeder` can be written in parallel. `ChordSeeder` (T032) depends on T029 (data file) and T030 (csproj) being done first, but is independent of T031/T033.

**T029 + T030**: Data file and csproj update can be done in parallel.

**Phase 6 — Tests (T038–T045)**: All 8 test tasks are independent of each other and can be written simultaneously.

## Implementation Strategy

**MVP = Phase 1 + Phase 2 + Phase 3** (T001–T028): Delivers User Story 1 — all entities persist correctly with all constraints. The database schema is fully correct and verifiable via migration inspection.

**Add seeding (Phase 4, T029–T035)**: Delivers User Story 2 — application self-initializes on first startup.

**US3 (constraint enforcement) is delivered automatically by Phase 3** — the EF configurations encode all 17 constraints; the migration is the artefact that proves they are implemented.

**Tests (Phase 6, T038–T045)**: Required by Constitution §VIII and plan.md G11. Can be written in parallel with Phase 4 once Phase 2 entity classes (T002–T013) are complete, since all tests depend only on entity types and `AppDbContext`.
