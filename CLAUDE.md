# Staccato Backend — Claude Code Instructions

Staccato is an ASP.NET Core 10 WebAPI for an instrument learning notebook
application. Users create digital dotted-paper notebooks, place structured
content modules on a 2D grid, and export lessons to PDF.

## Key Reference Files

| File | Purpose |
|---|---|
| `STACCATO_FRONTEND_DOCUMENTATION.md` | Complete API contracts, data models, business rules, and rendering specs |
| `.specify/memory/constitution.md` | Authoritative architecture principles — read before every implementation task |
| `.specify/memory/architecture.md` | Dependency map, endpoint list, grid dimensions |
| `.specify/memory/domain-models.md` | All enums, building block schemas, chord JSON schema |

---

## Common Commands

```bash
# Build the entire solution
dotnet build Staccato.sln

# Run all tests
dotnet test Staccato.sln

# Run only unit tests
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Unit"

# Run only integration tests
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Integration"

# Run the API (from Application project)
dotnet run --project Application/Application.csproj

# Add a migration (always run from solution root, targeting Application)
dotnet ef migrations add <MigrationName> \
  --project Persistence/Persistence.csproj \
  --startup-project Application/Application.csproj

# Apply migrations
dotnet ef database update \
  --project Persistence/Persistence.csproj \
  --startup-project Application/Application.csproj
```

---

## Solution Structure (9 Projects)

```
Staccato.sln
├── Application/     Startup host (Program.cs, DI, middleware, SignalR hub, background services)
├── Api/             Controllers, action filters, AutoMapper response mapping — NO business logic
├── Domain/          Service interfaces + implementations, repository interfaces, IUnitOfWork,
│                    domain exceptions, all business rule logic — the clean core
├── Repository/      Repository implementations, UnitOfWork — uses EF Core via AppDbContext
├── Persistence/     AppDbContext, EF configurations, migrations, seed data (Data/ folder)
├── EntityModels/    EF Core entity classes with navigation properties only
├── DomainModels/    Pure C# models and all enums — zero dependencies
├── ApiModels/       Request/Response DTOs and FluentValidation validators — zero dependencies
└── Tests/           xUnit unit tests (Tests/Unit/) and integration tests (Tests/Integration/)
```

---

## Dependency Map — NEVER VIOLATE

```
Application  →  Api, Domain, Repository, Persistence
Api          →  Domain, ApiModels, DomainModels
Domain       →  DomainModels ONLY
Repository   →  Domain, EntityModels, Persistence
Persistence  →  EntityModels
ApiModels    →  (none)
DomainModels →  (none)
EntityModels →  (none)
Tests        →  all projects
```

**The most critical rule:** `Domain` must never reference `Api`, `ApiModels`,
`Repository`, `Persistence`, or `EntityModels`. Any such reference inverts
the architecture and must be removed immediately.

---

## Architecture Patterns (All Three Are Mandatory)

### Service Pattern
- Define `IXxxService` in `Domain/Services/`
- Implement `XxxService` in `Domain/Services/` alongside the interface
- Controllers call service interfaces only — never repositories directly
- Services contain all business logic; controllers contain none

### Repository Pattern
- Define `IXxxRepository : IRepository<T>` in `Domain/Interfaces/Repositories/`
- Implement in `Repository/Repositories/`
- Repositories perform data access only — no business logic
- Return `DomainModel` types (not `EntityModel`); map with AutoMapper

### Unit of Work
- `IUnitOfWork` defined in `Domain/Interfaces/` — single method: `CommitAsync(CancellationToken)`
- Implemented in `Repository/` wrapping `AppDbContext.SaveChangesAsync`
- Services call `IUnitOfWork.CommitAsync` to persist — repositories never call `SaveChanges`

---

## Naming Conventions

| Artefact | Convention | Example |
|---|---|---|
| EF Core entity | suffix `Entity` | `NotebookEntity` |
| Domain model | no suffix | `Notebook` |
| Repository interface | `I` prefix + `Repository` suffix | `INotebookRepository` |
| Repository impl | `Repository` suffix | `NotebookRepository` |
| Service interface | `I` prefix + `Service` suffix | `INotebookService` |
| Service impl | `Service` suffix | `NotebookService` |
| Request DTO | `Request` suffix | `CreateNotebookRequest` |
| Response DTO | `Response` suffix | `NotebookDetailResponse` |
| Validator | `Validator` suffix | `CreateNotebookRequestValidator` |
| EF configuration | `Configuration` suffix | `NotebookConfiguration` |
| AutoMapper profile | `Profile` suffix | `EntityToDomainProfile` |
| Background service | `Service` suffix | `PdfExportBackgroundService` |
| Exception | `Exception` suffix | `NotFoundException` |

Namespaces follow folder structure: `ProjectName.FolderName.SubfolderName`

Database: tables are plural PascalCase without `Entity` suffix (`Notebooks`, `Lessons`).
PKs are always `Guid` named `Id`. FKs are `{EntityName}Id` (`NotebookId`, `UserId`).

API routes: lowercase kebab-case, no trailing slash, no version prefix.
Collections are plural nouns (`/notebooks`), sub-resources nest (`/notebooks/{id}/lessons`).

---

## Data Model Hierarchy

```
User
└── Notebook  (PageSize and InstrumentId are immutable after creation)
    ├── NotebookModuleStyle × 12  (one per ModuleType — global styling per notebook)
    └── Lesson  (ordered by CreatedAt ascending)
        └── LessonPage  (PageNumber 1 auto-created with each Lesson)
            └── Module  (GridX, GridY, GridWidth, GridHeight in 5mm grid units)
                └── ContentJson  (JSON array of BuildingBlock objects)
```

**Invariants:**
- All PKs are `Guid` generated by the app (`Guid.NewGuid()`), never by the DB
- All datetimes are UTC (`DateTime.UtcNow`)
- Every Notebook has exactly 12 `NotebookModuleStyle` records — created atomically on notebook creation
- A Lesson always has at least one LessonPage; first page is auto-created
- Chord and Instrument entities are immutable after seeding — `DeleteBehavior.Restrict`

---

## API Conventions

**HTTP status codes:**
- `200` — GET, PUT success
- `201` — POST creates a resource
- `202` — async operation queued (PDF export)
- `204` — DELETE or action with no body

**Error formats:**
```jsonc
// Business rule violation (422, 409, 400, 403)
{ "code": "MODULE_OVERLAP", "message": "Localized message.", "details": {} }

// Infrastructure/unexpected error (500)
{ "type": "...", "title": "...", "status": 500, "detail": "..." }  // RFC 7807

// Validation error (400)
{ "errors": { "fieldName": ["message"] } }
```

**Key error codes:** `MODULE_OVERLAP`, `MODULE_OUT_OF_BOUNDS`, `MODULE_TOO_SMALL`,
`INVALID_BUILDING_BLOCK`, `BREADCRUMB_CONTENT_NOT_EMPTY`, `DUPLICATE_TITLE_MODULE`,
`NOTEBOOK_PAGE_SIZE_IMMUTABLE`, `NOTEBOOK_INSTRUMENT_IMMUTABLE`, `LAST_PAGE_DELETION`,
`ACTIVE_EXPORT_EXISTS`, `ACCOUNT_DELETION_ALREADY_SCHEDULED`,
`ACCOUNT_DELETION_NOT_SCHEDULED`, `DUPLICATE_PRESET_NAME`, `INSTRUMENT_NOT_FOUND`

**Ownership:** resources belonging to another user return `403`, never `404`.
**Rate limiting:** `/auth/*` endpoints only — 10 req/min/IP.
**Localization:** `Accept-Language: en` or `hu` — controls error message language.

---

## Content Model

Module content is stored as a JSON array in `Module.ContentJson`. Each element
has a `"type"` discriminator matching `BuildingBlockType`.

**Text rule (applies everywhere in the app):** all user text is plain text
with optional bold only. No italic, underline, colour, or font size ever.
Stored as: `{ "text": "string", "bold": false }`

**Building block types:** `SectionHeading`, `Date`, `Text`, `BulletList`,
`NumberedList`, `CheckboxList`, `Table`, `MusicalNotes`, `ChordProgression`,
`ChordTablatureGroup`

**Single sources of truth:**
- `ModuleTypeConstraints` (in `DomainModels/Constants/`) — allowed block types
  per module type, minimum grid dimensions per module type
- `PageSizeDimensions` (in `DomainModels/Constants/`) — grid width/height per
  `PageSize` enum (A4=42×59, A5=29×42, A6=21×29, B5=35×50, B6=25×35 dots)

**Module placement validation (server-side in ModuleService — all 6 rules):**
1. `gridWidth >= MinWidth` for the module type
2. `gridHeight >= MinHeight` for the module type
3. `gridX >= 0` and `gridY >= 0`
4. `gridX + gridWidth <= pageGridWidth`
5. `gridY + gridHeight <= pageGridHeight`
6. No rectangle overlap with existing modules on the same page

---

## Technology Stack

| Concern | Library |
|---|---|
| Web framework | ASP.NET Core 10 |
| ORM | Entity Framework Core 10 + SQL Server |
| DTO mapping | AutoMapper |
| Validation | FluentValidation (auto-pipeline in Application) |
| Auth | JWT Bearer HS256 + HttpOnly refresh cookie (`staccato_refresh`) |
| Google OAuth | `Google.Apis.Auth` (server-side validation only) |
| Password hashing | `BCrypt.Net-Next` (work factor 12) |
| PDF generation | QuestPDF |
| File storage | Azure Blob Storage (`Azure.Storage.Blobs`) |
| Real-time | ASP.NET Core SignalR |
| Background jobs | `IHostedService` + `Channel<T>` (no Hangfire) |
| Testing | xUnit + Moq + `WebApplicationFactory` + InMemory EF |
| Localization | `IStringLocalizer` + `.resx` files (`en`, `hu`) |

Do not introduce any library outside this list without updating
`.specify/memory/constitution.md`.

---

## Background Services

Two `IHostedService` implementations are registered in `Application`:

| Service | Trigger | Purpose |
|---|---|---|
| `PdfExportBackgroundService` | Reads from `Channel<Guid>` (singleton) | Renders PDF with QuestPDF, uploads to Azure Blob, notifies via SignalR |
| `ExportCleanupService` | Daily timer | Deletes expired exports (>24h) from DB and Azure Blob |
| `AccountDeletionCleanupService` | Daily timer | Hard-deletes accounts past their 30-day grace period, removes avatar blobs |

PDF export flow: `POST /exports` → enqueue → process → upload → `SignalR PdfReady` event → `GET /exports/{id}/download` streams blob (URL never exposed).

---

## Security Rules

- Access tokens in **memory only** — never `localStorage`/`sessionStorage`
- Refresh tokens in **HttpOnly cookie** only (`staccato_refresh`)
- Every `POST /auth/refresh` **rotates** the refresh token atomically
- All secrets in `appsettings.json` injected via `IOptions<T>` — no hardcoded secrets
- Google ID tokens validated server-side — never trust frontend-decoded claims
- User account deletion is **soft** (30-day grace, `ScheduledDeletionAt`); all other deletes are hard cascade

---

## Testing Rules

**Unit tests** (`Tests/Unit/`):
- Isolate services with Moq for all dependencies
- One test class per service
- Cover happy path **and** all exception paths
- Use `[Theory]` + `[InlineData]` for parameterised cases

**Integration tests** (`Tests/Integration/`):
- Use `WebApplicationFactory<Program>` with InMemory EF Core
- One test class per controller
- Inject test JWT via `AuthHelper`
- Use unique `Guid`s per test — no shared mutable state

---

## Absolute Prohibitions

- `Domain` referencing anything other than `DomainModels`
- Business logic in controllers
- Repository methods calling `SaveChanges` directly
- `IActionResult` or HTTP status codes returned from services
- Secrets hardcoded in source code
- `.Result` or `.Wait()` on async calls
- `IConfiguration` injected directly into services or repositories
- `HttpContext` used outside controllers and middleware
- `localStorage`/`sessionStorage` for tokens (frontend concern, but do not suggest it)
- Any new major library without a constitution amendment
- Deleting or modifying Chord or Instrument entities after seeding
- Changing `Notebook.PageSize` or `Notebook.InstrumentId` after creation
