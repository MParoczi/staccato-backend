# Staccato Backend

ASP.NET Core 10 WebAPI for **Staccato**, a digital instrument learning notebook application. Users create dotted-paper notebooks, place structured content modules on a 2D grid canvas, style them per module type, and export lessons to PDF.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Solution Structure](#solution-structure)
- [Architecture](#architecture)
- [Data Model](#data-model)
- [API Reference](#api-reference)
- [Authentication](#authentication)
- [Content Model](#content-model)
- [PDF Export Pipeline](#pdf-export-pipeline)
- [Background Services](#background-services)
- [Localization](#localization)
- [Testing](#testing)
- [Configuration](#configuration)
- [Technology Stack](#technology-stack)

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local or remote)
- Azure Blob Storage account (for PDF exports and avatar uploads)
- (Optional) A Google OAuth client ID for Google sign-in

## Getting Started

```bash
# Clone and navigate to the backend
cd Backend

# Restore dependencies and build
dotnet build Staccato.sln

# Apply EF Core migrations
dotnet ef database update \
  --project Persistence/Persistence.csproj \
  --startup-project Application/Application.csproj

# Run the API
dotnet run --project Application/Application.csproj
```

The API starts on the configured port with Swagger UI available at `/swagger` in Development mode.

### Adding a Migration

```bash
dotnet ef migrations add <MigrationName> \
  --project Persistence/Persistence.csproj \
  --startup-project Application/Application.csproj
```

---

## Solution Structure

The solution is organized into 9 projects following clean architecture principles:

```
Staccato.sln
├── Application/     Startup host — Program.cs, DI registration, middleware, SignalR hub, background services
├── Api/             Controllers, action filters, AutoMapper response mapping (no business logic)
├── Domain/          Service interfaces + implementations, repository interfaces, IUnitOfWork, domain exceptions
├── Repository/      Repository implementations, UnitOfWork — uses EF Core via AppDbContext
├── Persistence/     AppDbContext, EF configurations, migrations, seed data
├── EntityModels/    EF Core entity classes with navigation properties
├── DomainModels/    Pure C# models and enums — zero dependencies
├── ApiModels/       Request/Response DTOs and FluentValidation validators — zero dependencies
└── Tests/           xUnit unit tests and integration tests
```

---

## Architecture

### Dependency Map

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

The `Domain` project is the clean core of the application and must never reference `Api`, `ApiModels`, `Repository`, `Persistence`, or `EntityModels`.

### Core Patterns

| Pattern | Description |
|---|---|
| **Service** | `IXxxService` + `XxxService` in `Domain/Services/`. Controllers call service interfaces only. All business logic lives here. |
| **Repository** | `IXxxRepository` in `Domain/Interfaces/Repositories/`, implemented in `Repository/Repositories/`. Data access only, returns domain models. |
| **Unit of Work** | `IUnitOfWork.CommitAsync()` in `Domain/Interfaces/`, wraps `SaveChangesAsync`. Services call it to persist; repositories never call `SaveChanges`. |

### Middleware Pipeline

The request pipeline is configured in the following order:

1. Swagger UI (Development only)
2. Request localization
3. Global exception handler
4. Business exception middleware (maps domain exceptions to structured error responses)
5. HTTPS redirection
6. CORS
7. Rate limiting (`/auth/*` endpoints — 10 req/min/IP)
8. Response caching
9. Authentication & Authorization
10. SignalR hub (`/hubs/notifications`)
11. Controller endpoints

---

## Data Model

```
User
└── Notebook  (PageSize and InstrumentId are immutable after creation)
    ├── NotebookModuleStyle × 12  (one per ModuleType — global styling per notebook)
    └── Lesson  (ordered by CreatedAt ascending)
        └── LessonPage  (PageNumber 1 auto-created with each Lesson)
            └── Module  (GridX, GridY, GridWidth, GridHeight in 5mm grid units)
                └── ContentJson  (JSON array of BuildingBlock objects)
```

**Key invariants:**
- All primary keys are application-generated `Guid` values
- All datetimes are stored in UTC
- Every notebook has exactly 12 `NotebookModuleStyle` records (one per module type), created atomically
- A lesson always has at least one page (auto-created)
- Chord and Instrument data is immutable after seeding

### Grid System

Pages use a 5mm dot-spacing grid. Supported page sizes (width x height in grid units):

| Page Size | Grid Dimensions |
|---|---|
| A4 | 42 x 59 |
| A5 | 29 x 42 |
| A6 | 21 x 29 |
| B5 | 35 x 50 |
| B6 | 25 x 35 |

Module placement is validated server-side against 6 rules: minimum width/height per module type, non-negative coordinates, within page bounds, and no overlap with other modules.

---

## API Reference

All routes are lowercase kebab-case with no trailing slash or version prefix.

### Authentication — `auth/`

| Method | Route | Description | Auth |
|---|---|---|---|
| POST | `auth/register` | Register a new user | No |
| POST | `auth/login` | Login with email/password | No |
| POST | `auth/refresh` | Refresh access token via HttpOnly cookie | No |
| POST | `auth/google` | Login/register with Google ID token | No |
| DELETE | `auth/logout` | Logout (clears refresh cookie) | No |

### Users — `users/`

| Method | Route | Description |
|---|---|---|
| GET | `users/me` | Get current user profile |
| PUT | `users/me` | Update profile |
| DELETE | `users/me` | Schedule account deletion (30-day grace period) |
| POST | `users/me/cancel-deletion` | Cancel scheduled account deletion |
| PUT | `users/me/avatar` | Upload avatar (multipart/form-data) |
| DELETE | `users/me/avatar` | Delete avatar |
| GET | `users/me/presets` | Get user's saved style presets |
| POST | `users/me/presets` | Create a new style preset |
| PUT | `users/me/presets/{id}` | Update a style preset |
| DELETE | `users/me/presets/{id}` | Delete a style preset |

### Notebooks — `notebooks/`

| Method | Route | Description |
|---|---|---|
| GET | `notebooks` | List user's notebooks |
| POST | `notebooks` | Create a notebook |
| GET | `notebooks/{id}` | Get notebook detail |
| PUT | `notebooks/{id}` | Update notebook |
| DELETE | `notebooks/{id}` | Delete notebook |
| GET | `notebooks/{id}/styles` | Get all 12 module styles |
| PUT | `notebooks/{id}/styles` | Bulk update module styles |
| POST | `notebooks/{id}/styles/apply-preset/{presetId}` | Apply a style preset |
| GET | `notebooks/{id}/index` | Get notebook index (table of contents) |

### Lessons

| Method | Route | Description |
|---|---|---|
| GET | `notebooks/{id}/lessons` | List lessons in a notebook |
| POST | `notebooks/{id}/lessons` | Create a lesson |
| GET | `lessons/{id}` | Get lesson detail |
| PUT | `lessons/{id}` | Update lesson |
| DELETE | `lessons/{id}` | Delete lesson |

### Lesson Pages — `lessons/`

| Method | Route | Description |
|---|---|---|
| GET | `lessons/{id}/pages` | List pages in a lesson |
| POST | `lessons/{id}/pages` | Add a page |
| DELETE | `lessons/{lessonId}/pages/{pageId}` | Delete a page |

### Modules

| Method | Route | Description |
|---|---|---|
| GET | `pages/{pageId}/modules` | List modules on a page |
| POST | `pages/{pageId}/modules` | Create a module |
| PUT | `modules/{moduleId}` | Update module (content + layout) |
| PATCH | `modules/{moduleId}/layout` | Update module layout only |
| DELETE | `modules/{moduleId}` | Delete a module |

### Chords — `chords/`

| Method | Route | Description | Auth |
|---|---|---|---|
| GET | `chords` | Search chords (by instrument, root, quality) | No |
| GET | `chords/{id}` | Get chord by ID | No |

### Instruments — `instruments/`

| Method | Route | Description | Auth |
|---|---|---|---|
| GET | `instruments` | List all instruments | No |

### System Presets — `presets/`

| Method | Route | Description | Auth |
|---|---|---|---|
| GET | `presets` | List all system style presets | No |

### PDF Exports — `exports/`

| Method | Route | Description |
|---|---|---|
| POST | `exports` | Queue a PDF export (returns 202) |
| GET | `exports` | List user's exports |
| GET | `exports/{id}` | Get export status/detail |
| GET | `exports/{id}/download` | Download the exported PDF file |
| DELETE | `exports/{id}` | Delete an export |

> All endpoints under Users, Notebooks, Lessons, Lesson Pages, Modules, and Exports require JWT authentication. Chord, Instrument, and Preset endpoints are public (cached for 5 minutes where applicable).

### HTTP Status Codes

| Code | Usage |
|---|---|
| 200 | GET and PUT success |
| 201 | POST resource creation |
| 202 | Async operation queued (PDF export) |
| 204 | DELETE or action with no response body |
| 400 | Validation error |
| 403 | Accessing another user's resource (never 404) |
| 404 | Resource not found |
| 409/422 | Business rule violation |
| 500 | Unexpected server error (RFC 7807 format) |

### Error Response Formats

```jsonc
// Business rule violation (400, 403, 409, 422)
{ "code": "MODULE_OVERLAP", "message": "Localized message.", "details": {} }

// Validation error (400)
{ "errors": { "fieldName": ["Validation message."] } }

// Server error (500) — RFC 7807
{ "type": "...", "title": "...", "status": 500, "detail": "..." }
```

---

## Authentication

Staccato supports two authentication methods:

| Method | Description |
|---|---|
| **Local registration** | Email + password. Password hashed with BCrypt (work factor 12). No email verification. |
| **Google OAuth** | Google ID token validated server-side via `Google.Apis.Auth`. |

### Token Strategy

| Token | Storage | Lifetime |
|---|---|---|
| Access token | Client memory only | 15 minutes |
| Refresh token | HttpOnly cookie (`staccato_refresh`) | 7 days sliding / 30 days with Remember Me |

- Access tokens are JWT signed with HS256, containing `userId`, `email`, and `displayName`
- Refresh tokens are rotated on every `POST /auth/refresh` call
- Rate limiting is applied to all `/auth/*` endpoints (10 requests/min/IP)

---

## Content Model

Module content is stored as a JSON array in `Module.ContentJson`. Each element has a `"type"` discriminator matching a `BuildingBlockType` enum value.

### Module Types (12)

Theory, Practice, Homework, Warmup, Exercise, Song, Technique, Repertoire, Goals, Notes, Reference, Subtitle (plus the special Breadcrumb type).

### Building Block Types

`SectionHeading`, `Date`, `Text`, `BulletList`, `NumberedList`, `CheckboxList`, `Table`, `MusicalNotes`, `ChordProgression`, `ChordTablatureGroup`

Each module type has a defined set of allowed building block types and minimum grid dimensions, enforced by `ModuleTypeConstraints` in `DomainModels/Constants/`.

### Text Formatting

All user text throughout the application is plain text with optional **bold** only. No italic, underline, color, or font-size formatting is supported. Text is stored as:

```json
{ "text": "string", "bold": false }
```

---

## PDF Export Pipeline

PDF generation is asynchronous and powered by QuestPDF:

```
POST /exports  →  Enqueue (Channel<Guid>)  →  Background service renders PDF
    →  Upload to Azure Blob  →  SignalR "PdfReady" notification
    →  GET /exports/{id}/download  (streams blob — URL never exposed directly)
```

- Returns `202 Accepted` immediately upon queuing
- Clients receive real-time status updates via the SignalR hub at `/hubs/notifications`
- Exported files are automatically cleaned up after 24 hours

---

## Background Services

Three `IHostedService` implementations run within the application host:

| Service | Trigger | Purpose |
|---|---|---|
| `PdfExportBackgroundService` | `Channel<Guid>` consumer | Renders PDF with QuestPDF, uploads to Azure Blob, sends SignalR notification |
| `ExportCleanupService` | Daily timer | Deletes expired exports (older than 24 hours) from database and Azure Blob |
| `AccountDeletionCleanupService` | Daily timer | Hard-deletes user accounts past their 30-day grace period, removes avatar blobs |

---

## Localization

The API supports English (`en`) and Hungarian (`hu`) localization via `IStringLocalizer` and `.resx` resource files. The active language is determined by the `Accept-Language` request header.

Error messages, validation messages, and business rule violation descriptions are all localized.

---

## Testing

```bash
# Run all tests
dotnet test Staccato.sln

# Run only unit tests
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Unit"

# Run only integration tests
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Integration"
```

### Unit Tests (`Tests/Unit/`)

- One test class per service
- All dependencies mocked with Moq
- Covers happy paths and all exception/edge-case paths
- Parameterized tests use `[Theory]` + `[InlineData]`

### Integration Tests (`Tests/Integration/`)

- One test class per controller
- Uses `WebApplicationFactory<Program>` with InMemory EF Core
- JWT authentication injected via `AuthHelper`
- Each test uses unique `Guid` values — no shared mutable state

---

## Configuration

Application settings are managed through `appsettings.json` and bound to strongly-typed options via `IOptions<T>`. The following configuration sections are validated at startup:

| Section | Purpose |
|---|---|
| `Jwt` | Secret key, issuer, audience, token lifetimes |
| `AzureBlob` | Connection string and container names |
| `Cors` | Allowed origins |
| `RateLimit` | Rate limiting rules |
| `Google` | Google OAuth client ID |

All secrets should be provided via environment variables or a secrets manager in production — never hardcoded in source.

---

## Technology Stack

| Concern | Technology |
|---|---|
| Framework | ASP.NET Core 10 (.NET 10) |
| ORM | Entity Framework Core 10 + SQL Server |
| DTO Mapping | AutoMapper |
| Validation | FluentValidation |
| Authentication | JWT Bearer (HS256) + HttpOnly refresh cookie |
| Google OAuth | Google.Apis.Auth |
| Password Hashing | BCrypt.Net-Next (work factor 12) |
| PDF Generation | QuestPDF |
| File Storage | Azure Blob Storage |
| Real-time | ASP.NET Core SignalR |
| Background Jobs | IHostedService + Channel\<T> |
| API Documentation | Swashbuckle (Swagger) |
| Testing | xUnit + Moq + WebApplicationFactory + InMemory EF Core |
| Localization | IStringLocalizer + .resx (en, hu) |
