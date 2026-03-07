# Feature Specification: EF Core Entity Models and Database Persistence

**Feature Branch**: `003-ef-core-persistence`
**Created**: 2026-03-07
**Status**: Draft
**Input**: User description: "Implement EF Core entity classes in EntityModels and full database configuration in Persistence for the Staccato application using SQL Server."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Persistent Data Storage Across All Entities (Priority: P1)

As a user of the Staccato application, all the data I create (notebooks, lessons, pages, modules, chord progressions, PDF exports) must survive application restarts. Every entity in the application must have a reliable database representation that stores and retrieves data correctly, enforces relationships, and cascades deletes appropriately.

**Why this priority**: Without a correctly wired persistence layer, no feature of the application works end-to-end. All other stories depend on this foundation.

**Independent Test**: Can be tested by seeding a database from scratch, writing a record for each entity type, restarting the application, and verifying all records are retrievable with correct values and relationships intact.

**Acceptance Scenarios**:

1. **Given** an empty database, **When** the application starts and a user registers, **Then** a User record is stored with a unique Guid PK, UTC timestamps, and all required fields persisted correctly.
2. **Given** a user with a notebook, **When** the notebook is deleted, **Then** all related lessons, lesson pages, modules, notebook module styles, and PDF exports are removed automatically via cascade delete.
3. **Given** a lesson page with several modules placed at specific grid positions, **When** those modules are retrieved, **Then** each module's grid coordinates, dimensions, module type, and JSON content are returned exactly as stored.
4. **Given** a chord entity linked to an instrument, **When** an attempt is made to delete the instrument, **Then** the deletion is rejected and no data is modified.
5. **Given** a notebook that has been created, **When** the notebook is loaded, **Then** exactly 12 NotebookModuleStyle records (one per ModuleType) are associated with it.

---

### User Story 2 - Automated Database Initialization and Seeding (Priority: P2)

As a developer deploying the application to a new environment, the database must be created, migrated, and populated with all required reference data (system style presets, instruments, and guitar chords) automatically on first startup, without any manual SQL scripts or intervention.

**Why this priority**: Without seed data, the application cannot function — instrument selection is impossible and chord lookups fail. Automating initialization eliminates deployment errors.

**Independent Test**: Can be tested by pointing the application at an empty SQL Server instance and verifying the database is created, migrations applied, and all reference data present after a single application start.

**Acceptance Scenarios**:

1. **Given** a fresh SQL Server instance with no Staccato database, **When** the application starts, **Then** the database is created, all migrations are applied, and the process completes without errors.
2. **Given** a newly initialized database, **When** the system style presets are queried, **Then** exactly 5 presets are present: Classic, Colorful, Dark, Minimal, and Pastel — each with real hex color values defined per module type.
3. **Given** a newly initialized database, **When** the instruments are queried, **Then** exactly 7 instruments are present, one for each value in the InstrumentKey enum (Guitar6String, Guitar7String, Bass4String, Bass5String, Ukulele4String, Banjo4String, Banjo5String).
4. **Given** a newly initialized database, **When** guitar chords are queried, **Then** a comprehensive set of 6-string guitar chords are present, loaded from the application's chord data file, each with correct name, suffix, instrument association, and fret-position JSON.
5. **Given** an already-initialized database with existing data, **When** the application restarts, **Then** migrations and seeding run idempotently without duplicating any reference data.
6. **Given** the `guitar_chords.json` file is missing or contains invalid JSON, **When** the application starts, **Then** startup is aborted with a clear exception identifying the missing or malformed file — the application does not start in a broken state.

---

### User Story 3 - Data Integrity Constraint Enforcement (Priority: P3)

As a developer maintaining the application, the database must enforce uniqueness, referential integrity, and business constraints at the storage level, so that application bugs cannot produce corrupted or ambiguous data states that are impossible to reconcile.

**Why this priority**: Constraints at the database layer are the last line of defence against data corruption. Without them, integrity can only be guaranteed by the application layer, making it fragile.

**Independent Test**: Can be tested by attempting direct database inserts that violate each constraint (duplicate email, duplicate active export, overlapping module styles, delete chord instrument) and verifying each attempt is rejected.

**Acceptance Scenarios**:

1. **Given** a user with a specific email, **When** a second user record with the same email is inserted, **Then** the database rejects the insert with a unique constraint violation.
2. **Given** a user with a Google ID, **When** a second user record with the same Google ID is inserted, **Then** the database rejects the insert; users with null Google IDs are not affected by this constraint.
3. **Given** a notebook with a pending or processing PDF export, **When** a second export for the same notebook with status Pending or Processing is inserted, **Then** the database rejects the insert via a partial unique constraint.
4. **Given** a notebook with a NotebookModuleStyle for a specific ModuleType, **When** a second NotebookModuleStyle for the same notebook and ModuleType is inserted, **Then** the database rejects the insert.
5. **Given** a refresh token stored with a specific token string, **When** a second refresh token with the same token string is inserted, **Then** the database rejects the insert.
6. **Given** a JSON column value (ContentJson, PositionsJson, StylesJson, LessonIdsJson), **When** the value is stored and retrieved, **Then** the full JSON string is preserved without truncation or encoding loss.

---

### Edge Cases

- If `guitar_chords.json` is missing or malformed at startup, the application throws a fatal exception and does not start (resolved — see Clarifications).
- How does the system handle a database that is partially migrated (some migrations applied, others pending)?
- What happens if seed data records already exist when the seeder runs (idempotency)?
- How does the partial unique index on PdfExport behave when a notebook has one Ready export and attempts a new export with status Pending?
- What happens when a LessonIdsJson field stores an empty array vs. a null value?
- How does the filtered (sparse) unique index on GoogleId handle multiple users with null GoogleId?

---

## Requirements *(mandatory)*

### Functional Requirements

**EntityModels Project**

- **FR-001**: The EntityModels project MUST contain one entity class for each of the 12 domain entities: UserEntity, RefreshTokenEntity, UserSavedPresetEntity, SystemStylePresetEntity, InstrumentEntity, ChordEntity, NotebookEntity, NotebookModuleStyleEntity, LessonEntity, LessonPageEntity, ModuleEntity, PdfExportEntity.
- **FR-002**: Every entity class MUST mirror its corresponding domain model in all scalar properties (same names, same types) and additionally include EF Core navigation properties for all relationships.
- **FR-003**: Every entity MUST have a `Guid Id` primary key property.
- **FR-004**: The following properties MUST store their values as NVARCHAR(MAX) JSON strings: `ChordEntity.PositionsJson`, `ModuleEntity.ContentJson`, `NotebookModuleStyleEntity.StylesJson`, `UserSavedPresetEntity.StylesJson`, `SystemStylePresetEntity.StylesJson`, `PdfExportEntity.LessonIdsJson`.
- **FR-005**: `PdfExportEntity` MUST include a `LessonIdsJson` property (nullable `string?`) that stores a JSON-serialised array of lesson Guids. A SQL NULL value means the export covers the entire notebook. An empty array `[]` is not a valid representation for whole-notebook exports.
- **FR-039**: The `PdfExport` domain model in `DomainModels/Models/PdfExport.cs` MUST be updated to add a `List<Guid>? LessonIds` property (null = entire notebook) as part of this feature, so that the domain model and entity model remain in sync.
- **FR-006**: Enum-typed properties (`ModuleType`, `PageSize`, `ExportStatus`, `InstrumentKey`, `Language`) MUST be stored as their underlying integer values in the database. `HasConversion<string>()` MUST NOT be applied to any enum property — string storage would break the `HasFilter` SQL expressions that reference integer enum values (e.g., `[Status] = 0 OR [Status] = 1`).

**Persistence Project — AppDbContext**

- **FR-007**: `AppDbContext` MUST define a `DbSet<T>` property for every entity listed in FR-001.
- **FR-008**: `AppDbContext` MUST apply every `IEntityTypeConfiguration<T>` implementation automatically via `modelBuilder.ApplyConfigurationsFromAssembly`.

**Persistence Project — EF Core Configurations**

- **FR-009**: Each entity MUST have its own `IEntityTypeConfiguration<T>` implementation in `Persistence/Configurations/`, named with the `Configuration` suffix (e.g., `UserConfiguration`).
- **FR-010**: Table names MUST be plural PascalCase with no `Entity` suffix (e.g., `Users`, `Notebooks`, `LessonPages`).
- **FR-011**: `UserEntity.Email` MUST have a unique index.
- **FR-012**: `UserEntity.GoogleId` MUST have a filtered (sparse) unique index that only applies to non-null values.
- **FR-013**: `NotebookEntity.UserId` MUST be a foreign key to `UserEntity` with cascade delete.
- **FR-014**: `LessonEntity.NotebookId` MUST be a foreign key to `NotebookEntity` with cascade delete.
- **FR-015**: `LessonPageEntity.LessonId` MUST be a foreign key to `LessonEntity` with cascade delete.
- **FR-016**: `ModuleEntity.LessonPageId` MUST be a foreign key to `LessonPageEntity` with cascade delete.
- **FR-017**: `NotebookModuleStyleEntity` MUST have a composite unique index on `(NotebookId, ModuleType)`.
- **FR-018**: `NotebookModuleStyleEntity.NotebookId` MUST be a foreign key to `NotebookEntity` with cascade delete.
- **FR-019**: `PdfExportEntity` MUST have a partial unique index on `NotebookId` filtered to rows where `Status` is `Pending` (0) or `Processing` (1), enforcing at most one active export per notebook at the database level.
- **FR-020**: `RefreshTokenEntity.Token` MUST have a unique index.
- **FR-021**: `RefreshTokenEntity.UserId` MUST be a foreign key to `UserEntity` with cascade delete.
- **FR-022**: `ChordEntity.InstrumentId` MUST be a foreign key to `InstrumentEntity` with `DeleteBehavior.Restrict`.
- **FR-040**: `NotebookEntity.InstrumentId` MUST be a foreign key to `InstrumentEntity` with `DeleteBehavior.Restrict`. Instruments are immutable post-seed and must never be deleted; this constraint reinforces that invariant at the database level.
- **FR-023**: `UserSavedPresetEntity.UserId` MUST be a foreign key to `UserEntity` with cascade delete.
- **FR-024**: `PdfExportEntity.NotebookId` MUST be a foreign key to `NotebookEntity` with cascade delete.
- **FR-042**: `PdfExportEntity.UserId` MUST be a foreign key to `UserEntity` with `DeleteBehavior.ClientCascade`. SQL Server prohibits multiple cascade paths to the same table; `PdfExportEntity` is already reachable from `UserEntity` via the `Notebooks.UserId → cascade → PdfExports.NotebookId` chain. Configuring `DeleteBehavior.Cascade` on both FKs would cause the migration to fail with "may cause cycles or multiple cascade paths". `ClientCascade` generates `ON DELETE NO ACTION` in the migration (no DB-level cascade via this FK) while instructing EF Core to delete related `PdfExportEntity` rows in memory before deleting the `UserEntity`, making it safe regardless of what the change tracker contains.
- **FR-025**: All JSON columns MUST be configured with `.HasColumnType("nvarchar(max)")`.
- **FR-041**: All values stored in JSON columns MUST use camelCase property names (e.g., `backgroundColor`, `baseFret`, `moduleType`). This applies to seed data written by seeders and to any JSON produced by repository or service code. PascalCase property names in stored JSON are prohibited.
- **FR-026**: String properties with bounded maximum lengths MUST have those lengths configured via Fluent API. Bounded properties and their limits: `User.Email` max 256, `User.FirstName` max 100, `User.LastName` max 100, `UserSavedPreset.Name` max 200, `SystemStylePreset.Name` max 200, `Instrument.DisplayName` max 200, `Chord.Name` max 200, `Chord.Suffix` max 200. All other string properties default to `nvarchar(max)`.

**Persistence Project — DbInitializer**

- **FR-027**: A `DbInitializer` class MUST exist in the Persistence project and MUST, when invoked: apply all pending migrations (via `Database.MigrateAsync`), then call each seeder in order (InstrumentSeeder → ChordSeeder → SystemStylePresetSeeder). `DbInitializer` MUST be called synchronously from `Application/Program.cs` before `app.Run()`, so the application does not accept any traffic until the database is fully initialized and seeded. When running under the InMemory EF Core provider (e.g., integration tests), `DbInitializer` MUST skip `MigrateAsync` — calling migrate on InMemory throws; tests configure the schema directly via `EnsureCreated` or equivalent.
- **FR-028**: `DbInitializer` MUST be idempotent — repeated calls on an already-initialized database MUST produce no duplicate data and no errors. Each seeder guards with a "skip if any rows exist" check (i.e., if the target table is non-empty, the seeder exits immediately without inserting).
- **FR-038**: `ChordSeeder` MUST validate `guitar_chords.json` before inserting data. The seeder MUST throw an `InvalidOperationException` identifying the file path and failure reason — aborting startup — in any of these cases: (a) the file is missing, (b) the file contains invalid JSON, (c) the deserialised array is null or empty, (d) any chord entry is missing a required field (`name`, `suffix`, or `positions`), (e) any chord entry's `positions` array is empty, (f) the file contains two or more entries with the same `name` + `suffix` combination (duplicate chord). The chord file is a developer-maintained asset; data authoring errors MUST be surfaced immediately rather than silently producing an incomplete chord library.

**Persistence Project — Seed Data**

- **FR-029**: A `SystemStylePresetSeeder` MUST insert exactly 5 presets if none exist: Classic, Colorful, Dark, Minimal, and Pastel.
- **FR-030**: Each system style preset's `StylesJson` MUST contain real hex color values for all 12 module types, with a visually distinct palette per preset theme. Exact hex values are implementation choices made by the developer; the spec constrains theme and mood, not specific color codes — except for the Colorful preset.
- **FR-031**: Classic MUST use neutral/professional tones (warm beige/brown, serif aesthetic). Colorful MUST exactly match the reference hex values from the frontend documentation (teal `#E0F7FA`/`#00838F` for Theory, orange `#FFF3E0`/`#E65100` for Practice, green `#E8F5E9`/`#2E7D32` for Example, yellow `#FFFDE7`/`#F57F17` for Important, blue `#E3F2FD`/`#1565C0` for Tip, purple `#F3E5F5`/`#6A1B9A` for Homework, pink `#FCE4EC`/`#880E4F` for Question, grey `#F5F5F5`/`#424242` for ChordTablature, white `#FFFFFF`/`#9E9E9E` for FreeText). For **Title**, **Subtitle**, and **Breadcrumb** module types in the Colorful preset, the frontend only renders `bodyTextColor` and `fontFamily` (no background, border, or header fields apply per the frontend docs); store `#FFFFFF` as `backgroundColor`/`headerBgColor` and `#212121` as `bodyTextColor`/`headerTextColor` — identical to the Minimal preset for these three types. Note: the frontend documentation's Colorful reference table includes a `Definition` entry that has no corresponding value in the domain `ModuleType` enum — this entry is a documentation artifact and MUST be ignored. Dark MUST use dark backgrounds (`#1E1E1E` range) with light text. Minimal MUST use white/near-white backgrounds with thin borders and no vivid header colors. Pastel MUST use soft muted backgrounds per module type.
- **FR-032**: One preset among the 5 MUST be designated as the default (Classic).
- **FR-033**: Presets MUST have an explicit `DisplayOrder` field (1–5) reflecting the intended UI ordering.
- **FR-034**: An `InstrumentSeeder` MUST insert exactly 7 instruments if none exist, one for each `InstrumentKey` enum value, with a human-readable display name and the correct string count per instrument type.
- **FR-035**: A `ChordSeeder` MUST load chord data from `Persistence/Data/guitar_chords.json` and insert all chords associated with the Guitar6String instrument if no chords exist. The `guitar_chords.json` file MUST be included in the `Persistence.csproj` as a content file with `CopyToOutputDirectory` set to `PreserveNewest` so it is available at the application base directory at runtime.
- **FR-036**: The `guitar_chords.json` file MUST exist in `Persistence/Data/` and MUST contain comprehensive 6-string guitar chord data in the application's chord JSON schema format, covering at minimum: major, minor, dominant 7th, major 7th, minor 7th, suspended, diminished, augmented chords across all 12 root notes.
- **FR-037**: Each chord record in the JSON file MUST include: chord name (root note), suffix (chord quality), and a `positions` array of fret-position objects matching the `ChordStringState` schema.

### Key Entities

- **UserEntity**: Represents an application user. Properties: Id (Guid PK), Email (unique), PasswordHash (nullable), GoogleId (nullable, filtered-unique), FirstName, LastName, AvatarUrl (nullable), CreatedAt (UTC), ScheduledDeletionAt (nullable UTC), Language (enum). Navigation: Notebooks, RefreshTokens, UserSavedPresets, PdfExports.
- **RefreshTokenEntity**: Represents a session refresh token. Properties: Id (Guid PK), Token (unique), UserId (FK), ExpiresAt (UTC), CreatedAt (UTC), IsRevoked. Navigation: User.
- **UserSavedPresetEntity**: Represents a user-created module style preset. Properties: Id (Guid PK), UserId (FK), Name, StylesJson (NVARCHAR(MAX) — JSON array of 12 module-type style objects). Navigation: User.
- **SystemStylePresetEntity**: Represents a built-in style preset. Properties: Id (Guid PK), Name, DisplayOrder, IsDefault, StylesJson (NVARCHAR(MAX) — JSON array of 12 module-type style objects). No FKs.
- **InstrumentEntity**: Represents an instrument type. Properties: Id (Guid PK), Key (enum, unique), DisplayName, StringCount. Navigation: Chords. Immutable after seeding.
- **ChordEntity**: Represents a chord for a specific instrument. Properties: Id (Guid PK), InstrumentId (FK Restrict), Name, Suffix, PositionsJson (NVARCHAR(MAX) — JSON array of position objects). Navigation: Instrument. Immutable after seeding.
- **NotebookEntity**: Represents a user's notebook. Properties: Id (Guid PK), UserId (FK cascade), Title, InstrumentId (FK), PageSize (enum, immutable), CreatedAt (UTC), UpdatedAt (UTC). Navigation: User, Instrument, Lessons, ModuleStyles, PdfExports.
- **NotebookModuleStyleEntity**: Represents per-notebook styling for a module type. Properties: Id (Guid PK), NotebookId (FK cascade), ModuleType (enum), StylesJson (NVARCHAR(MAX) — JSON object with 9 style fields for that one module type). Unique constraint on (NotebookId, ModuleType). Navigation: Notebook.
- **LessonEntity**: Represents a lesson within a notebook. Properties: Id (Guid PK), NotebookId (FK cascade), Title, CreatedAt (UTC), UpdatedAt (UTC). Navigation: Notebook, LessonPages.
- **LessonPageEntity**: Represents a page within a lesson. Properties: Id (Guid PK), LessonId (FK cascade), PageNumber. Navigation: Lesson, Modules.
- **ModuleEntity**: Represents a content module placed on a lesson page grid. Properties: Id (Guid PK), LessonPageId (FK cascade), ModuleType (enum), GridX, GridY, GridWidth, GridHeight, ContentJson (NVARCHAR(MAX)). Navigation: LessonPage.
- **PdfExportEntity**: Represents a PDF export job. Properties: Id (Guid PK), NotebookId (FK cascade), UserId (FK client-cascade — see FR-042), Status (enum), CreatedAt (UTC), CompletedAt (nullable UTC), BlobReference (nullable), LessonIdsJson (NVARCHAR(MAX), nullable — SQL NULL means export covers entire notebook; a non-null value is a JSON array of lesson Guid strings). Partial unique index on NotebookId where Status IN (Pending, Processing). Navigation: Notebook, User.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every entity type can be written to and read back from the database with 100% field fidelity — no data loss, truncation, or encoding errors on any property including JSON columns.
- **SC-002**: All 7 instruments are present after first-run startup. The chord count in the database equals the number of entries in `guitar_chords.json` — every record from the file is persisted, no more and no fewer.
- **SC-003**: All 5 system style presets with their full color configurations are present and queryable after first-run startup.
- **SC-004**: All 17 database constraints (5 unique indexes, 1 partial unique index, 11 FK behaviors) prevent invalid data states at the storage layer, independently of application-layer validation. Constraint inventory: unique on Users.Email; filtered unique on Users.GoogleId; unique on Instruments.Key; composite unique on NotebookModuleStyles(NotebookId, ModuleType); unique on RefreshTokens.Token; partial unique on PdfExports.NotebookId (active exports); cascade FK on Notebooks.UserId, Lessons.NotebookId, LessonPages.LessonId, Modules.LessonPageId, NotebookModuleStyles.NotebookId, RefreshTokens.UserId, UserSavedPresets.UserId, PdfExports.NotebookId; client-cascade FK on PdfExports.UserId (avoids SQL Server multiple-cascade-paths error; EF Core handles in-memory cleanup — see FR-042); restrict FK on Chords.InstrumentId and Notebooks.InstrumentId.
- **SC-005**: The database initializer completes without error on both a fresh database and an already-migrated database with existing seed data, with no duplicate records created on repeated runs.
- **SC-006**: Deleting a user cascades correctly to remove all 7 dependent entity types (notebooks, lessons, lesson pages, modules, refresh tokens, user saved presets, PDF exports) in a single operation without constraint violations.
- **SC-007**: Attempting to delete an instrument or chord is rejected at the database level with a referential integrity error.
- **SC-008**: When `guitar_chords.json` is absent, contains invalid JSON, or deserialises to an empty array, application startup throws an `InvalidOperationException` identifying the file path and failure reason, and the process exits before accepting any traffic.

---

## Assumptions

- **A-001**: *(Promoted to FR-039 — this assumption is now a normative requirement.)* `PdfExportEntity.LessonIdsJson` is an additional field not yet present in the `PdfExport` domain model. See FR-039 for the authoritative statement.
- **A-002**: The guitar chord data file (`guitar_chords.json`) covers only 6-string guitar chords as specified. Chords for other instrument types (7-string guitar, bass, ukulele, banjo) are out of scope and can be added in future iterations. All 7 instruments are seeded regardless; it is expected and valid for non-guitar instruments to have zero chord rows in the database after initial seeding.
- **A-003**: `StylesJson` stores different shapes depending on the entity. For `NotebookModuleStyleEntity`, it stores a single flat style object with 9 fields: `backgroundColor`, `borderColor`, `borderStyle`, `borderWidth`, `borderRadius`, `headerBgColor`, `headerTextColor`, `bodyTextColor`, `fontFamily`. For `SystemStylePresetEntity` and `UserSavedPresetEntity`, it stores a JSON array of 12 objects — one per module type — each containing the `moduleType` discriminator plus the same 9 style fields. All 12 module types (including Title, Subtitle, and Breadcrumb) MUST store all 9 fields — the frontend ignores inapplicable fields for Title/Subtitle but the stored shape is always uniform. The exact schema matches the `NotebookModuleStyle` frontend contract. All JSON columns (`StylesJson`, `PositionsJson`, `ContentJson`, `LessonIdsJson`) MUST use **camelCase** property names when serialised, matching the `System.Text.Json` default policy and the TypeScript frontend contract. PascalCase property names MUST NOT be used in stored JSON.
- **A-004**: The partial unique index on `PdfExportEntity` for active exports is implemented using `.HasIndex(e => e.NotebookId).IsUnique().HasFilter("[Status] = 0 OR [Status] = 1")`. EF Core 6+ natively supports filtered indexes via `HasFilter` — no raw SQL migrations are required.
- **A-005**: The filtered unique index on `UserEntity.GoogleId` is implemented using `.HasIndex(u => u.GoogleId).IsUnique().HasFilter("[GoogleId] IS NOT NULL")`. EF Core 6+ natively supports this via `HasFilter` — no raw SQL migrations are required.
- **A-006**: Enum values are stored as integers in the database (EF Core default). No string conversion is applied.
- **A-007**: The `InstrumentEntity.Key` property has its own unique index to prevent duplicate instrument entries during seeding.
- **A-008**: *(Promoted to FR-026 — string max lengths are now normative requirements, not assumptions.)*

---

## Clarifications

### Session 2026-03-07

- Q: What should happen when `guitar_chords.json` is missing or malformed at application startup? → A: Throw a fatal exception and abort startup (Option A — fail fast).
- Q: What are the null semantics for `LessonIdsJson` and should the `PdfExport` domain model be updated in this feature? → A: SQL NULL = whole-notebook export; `PdfExport.cs` domain model updated within this feature (Option A).
- Q: Where should `DbInitializer` be invoked in the application? → A: Synchronous call in `Program.cs` before `app.Run()` (Option A — blocks startup until DB is fully ready).
