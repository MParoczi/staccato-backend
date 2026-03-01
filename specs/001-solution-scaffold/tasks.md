# Tasks: Solution Scaffold — 9-Project ASP.NET Core Backend

**Input**: Design documents from `/specs/001-solution-scaffold/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅

**Tests**: No test tasks generated — test subdirectory scaffold only (no test logic at this stage).

**Organization**: Tasks grouped by user story. US1 (clean build) is the MVP gate. US2 (running API) depends on US1. US3 (test runner) depends on US1. US4 (config validation) depends on US2.

---

## Phase 1: Setup — Delete All Placeholder Files

**Purpose**: Remove all generated placeholder files before any project work begins.

- [ ] T001 [P] Delete `Api/Class1.cs`
- [ ] T002 [P] Delete `ApiModels/Class1.cs`
- [ ] T003 [P] Delete `Domain/Class1.cs`
- [ ] T004 [P] Delete `DomainModels/Class1.cs`
- [ ] T005 [P] Delete `EntityModels/Class1.cs`
- [ ] T006 [P] Delete `Persistence/Class1.cs`
- [ ] T007 [P] Delete `Repository/Class1.cs`
- [ ] T008 [P] Delete `Tests/UnitTest1.cs`

---

## Phase 2: Foundational — Fix Project References and Add NuGet Packages

**Purpose**: Correct all `.csproj` dependency violations and install all required packages. MUST complete before any user story can build.

**⚠️ CRITICAL**: Three pre-existing `.csproj` reference violations must be fixed before adding any new code.

### Fix Pre-Existing Reference Violations

- [ ] T009 Fix `Domain/Domain.csproj`: remove `<ProjectReference>` entries for `ApiModels` and `Repository`; confirm only `DomainModels` reference remains
- [ ] T010 Fix `Repository/Repository.csproj`: remove `<ProjectReference>` for `DomainModels`; add `<ProjectReference>` for `Domain`
- [ ] T011 Fix `Api/Api.csproj`: add missing `<ProjectReference>` for `DomainModels`

### Remove Deprecated Package

- [ ] T012 Remove `<PackageReference Include="Microsoft.AspNetCore.OpenApi" .../>` from `Application/Application.csproj`

### Add NuGet Packages

- [ ] T013 [P] Add to `Application/Application.csproj`: `Microsoft.AspNetCore.Authentication.JwtBearer`, `Azure.Storage.Blobs`, `QuestPDF`, `Microsoft.AspNetCore.SignalR.Core`, `FluentValidation.AspNetCore`
- [ ] T014 [P] Add to `Api/Api.csproj`: `AutoMapper.Extensions.Microsoft.DependencyInjection`
- [ ] T015 [P] Add to `Domain/Domain.csproj`: `FluentValidation`
- [ ] T016 [P] Add to `Repository/Repository.csproj`: `Microsoft.EntityFrameworkCore`
- [ ] T017 [P] Add to `Persistence/Persistence.csproj`: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`
- [ ] T018 [P] Add to `ApiModels/ApiModels.csproj`: `FluentValidation`
- [ ] T019 [P] Add to `Tests/Tests.csproj`: `Moq`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory`; add `<ProjectReference>` entries for all 8 other projects

**Checkpoint**: Run `dotnet build Staccato.sln` — must compile with zero errors before proceeding.

---

## Phase 3: User Story 1 — Developer Builds the Solution from Scratch (Priority: P1) 🎯 MVP

**Goal**: Every project compiles clean; dependency graph exactly matches the spec; no placeholder files remain; test subdirectory scaffold in place.

**Independent Test**: `dotnet build Staccato.sln` — zero errors, zero warnings.

- [ ] T020 [P] [US1] Create `Tests/Unit/.gitkeep` (empty file) to establish the Unit test subdirectory
- [ ] T021 [P] [US1] Create `Tests/Integration/.gitkeep` (empty file) to establish the Integration test subdirectory
- [ ] T022 [US1] Run `dotnet build Staccato.sln` and confirm zero errors and zero warnings across all 9 projects

**Checkpoint**: US1 complete — solution builds clean. MVP deliverable validated.

---

## Phase 4: User Story 2 — Developer Runs the Application and Gets a Live API (Priority: P2)

**Goal**: `dotnet run --project Application/Application.csproj` starts without error; all middleware (CORS, rate limiter, auth, business exception handler, SignalR, FluentValidation) is active in the correct pipeline order.

**Independent Test**: Start the API; verify CORS preflight succeeds from allowed origin; send 11 requests to `/auth/*` and confirm 429 on the 11th with `Retry-After` header; confirm unauthenticated `/hubs/notifications` negotiate returns 401.

### Options Classes

- [ ] T023 [P] [US2] Create `Application/Options/JwtOptions.cs` — POCO with `[Required]` data annotations on: `Issuer` (string), `Audience` (string), `SecretKey` (string, `[MinLength(32)]`), `AccessTokenExpiryMinutes` (int), `RefreshTokenExpiryDays` (int), `RememberMeExpiryDays` (int); add `[Range]` validation ensuring `RememberMeExpiryDays >= RefreshTokenExpiryDays`; file-scoped namespace `Application.Options`
- [ ] T024 [P] [US2] Create `Application/Options/AzureBlobOptions.cs` — POCO with `[Required]` on: `ConnectionString` (string), `ContainerName` (string); file-scoped namespace `Application.Options`
- [ ] T025 [P] [US2] Create `Application/Options/CorsOptions.cs` — POCO with `[Required]` on: `AllowedOrigins` (string[]); edge-case note in XML doc: empty array → CORS rejects all origins (no startup failure); null → startup fails; file-scoped namespace `Application.Options`
- [ ] T026 [P] [US2] Create `Application/Options/RateLimitOptions.cs` — POCO with `[Required]` and `[Range(1, int.MaxValue)]` on: `AuthWindowSeconds` (int), `AuthMaxRequests` (int); file-scoped namespace `Application.Options`

### Assembly Anchor and Domain Exception

- [ ] T027 [P] [US2] Create `ApiModels/ApiModelsAssemblyMarker.cs` — empty `internal static class ApiModelsAssemblyMarker` used as stable assembly anchor for FluentValidation scanner; file-scoped namespace `ApiModels`
- [ ] T028 [P] [US2] Create `Domain/Exceptions/BusinessException.cs` — `public abstract class BusinessException : Exception` with properties `string Code`, `int StatusCode { get; protected init; } = 422`, `object? Details`; protected constructor `(string code, string message, object? details = null)`; file-scoped namespace `Domain.Exceptions`

### SignalR Hub

- [ ] T029 [P] [US2] Create `Application/Hubs/NotificationHub.cs` — `public interface INotificationClient { Task PdfReady(string exportId, string fileName); }` and `[Authorize] public class NotificationHub : Hub<INotificationClient> { }` in same file; file-scoped namespace `Application.Hubs`

### Stub Background Services

- [ ] T030 [P] [US2] Create `Application/BackgroundServices/PdfExportBackgroundService.cs` — stub `IHostedService` implementing `StartAsync(CancellationToken)` and `StopAsync(CancellationToken)` with empty bodies (returns `Task.CompletedTask`); file-scoped namespace `Application.BackgroundServices`
- [ ] T031 [P] [US2] Create `Application/BackgroundServices/ExportCleanupService.cs` — stub `IHostedService` with empty `StartAsync`/`StopAsync` bodies; file-scoped namespace `Application.BackgroundServices`
- [ ] T032 [P] [US2] Create `Application/BackgroundServices/AccountDeletionCleanupService.cs` — stub `IHostedService` with empty `StartAsync`/`StopAsync` bodies; file-scoped namespace `Application.BackgroundServices`

### Business Exception Middleware

- [ ] T033 [US2] Create `Application/Middleware/BusinessExceptionMiddleware.cs` — catches `BusinessException`; writes `{ "code": ex.Code, "message": ex.Message, "details": ex.Details }` as `application/json` with `ex.StatusCode`; all other exceptions call `await _next(context)` to fall through to the Problem Details handler; use primary constructor; file-scoped namespace `Application.Middleware`

### Service Collection Extensions

- [ ] T034 [US2] Create `Application/Extensions/ServiceCollectionExtensions.cs` — static class with the following extension methods on `IServiceCollection` (file-scoped namespace `Application.Extensions`):
  - `AddAuth(IConfiguration)` — JWT Bearer with `JwtOptions`; symmetric key HS256; `AddAuthorization()`
  - `AddCorsPolicy(CorsOptions)` — named policy `"StaccatoPolicy"` with `AllowCredentials()`, `AllowAnyHeader()`, `AllowAnyMethod()`, specific origins from `CorsOptions.AllowedOrigins` (no wildcards)
  - `AddRateLimiting(RateLimitOptions)` — `AddRateLimiter` with `GlobalLimiter` using `PartitionedRateLimiter<HttpContext>`; partition key = IP; applies fixed-window limit only when path starts with `/auth/`; sets `OnRejected` to return 429 with `Retry-After` header
  - `AddAzureBlob(IConfiguration)` — registers `BlobServiceClient` as singleton using `AzureBlobOptions.ConnectionString`
  - `AddAutoMapper()` — scans `Application` and `Api` assemblies for AutoMapper profiles
  - `AddFluentValidationPipeline()` — `AddFluentValidationAutoValidation()` + `AddValidatorsFromAssembly(typeof(ApiModelsAssemblyMarker).Assembly)`
  - `AddSignalRHub()` — `services.AddSignalR()`
  - `AddBackgroundWorkers()` — registers `PdfExportBackgroundService`, `ExportCleanupService`, `AccountDeletionCleanupService` as `IHostedService`
  - `AddDatabase(IConfiguration)` — stub, empty body, comment: "populated in a future feature"
  - `AddRepositories()` — stub, empty body, comment: "populated in a future feature"
  - `AddDomainServices()` — stub, empty body, comment: "populated in a future feature"

### appsettings.json

- [ ] T035 [US2] Rewrite `Application/appsettings.json` with the following top-level sections (all values are safe placeholders):
  ```json
  {
    "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
    "AllowedHosts": "*",
    "ConnectionStrings": {
      "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=Staccato;Trusted_Connection=True;"
    },
    "Jwt": {
      "Issuer": "https://staccato.local",
      "Audience": "staccato-client",
      "SecretKey": "CHANGE-ME-USE-USER-SECRETS-IN-DEV-ENV",
      "AccessTokenExpiryMinutes": 15,
      "RefreshTokenExpiryDays": 7,
      "RememberMeExpiryDays": 30
    },
    "AzureBlob": {
      "ConnectionString": "UseDevelopmentStorage=true",
      "ContainerName": "staccato-exports"
    },
    "Cors": {
      "AllowedOrigins": [ "http://localhost:5173" ]
    },
    "RateLimit": {
      "AuthWindowSeconds": 60,
      "AuthMaxRequests": 10
    }
  }
  ```

### Program.cs

- [ ] T036 [US2] Rewrite `Application/Program.cs` with the full pipeline (depends on T023–T035):
  1. `QuestPDF.Settings.License = LicenseType.Community;` — before `WebApplication.CreateBuilder`
  2. Register options with `.ValidateDataAnnotations().ValidateOnStart()` for all four: `JwtOptions` (section `"Jwt"`), `AzureBlobOptions` (section `"AzureBlob"`), `CorsOptions` (section `"Cors"`), `RateLimitOptions` (section `"RateLimit"`)
  3. Read bound `CorsOptions` and `RateLimitOptions` from the service provider to pass to extension methods
  4. Call extension methods: `AddAuth`, `AddCorsPolicy`, `AddRateLimiting`, `AddAzureBlob`, `AddAutoMapper`, `AddFluentValidationPipeline`, `AddSignalRHub`, `AddBackgroundWorkers`, `AddDatabase`, `AddRepositories`, `AddDomainServices`
  5. `AddControllers()`, `AddProblemDetails()`, `AddHttpContextAccessor()`
  6. `var app = builder.Build()`
  7. Middleware pipeline in exact order:
     - `app.UseMiddleware<BusinessExceptionMiddleware>()`
     - `app.UseExceptionHandler()`
     - `app.UseHttpsRedirection()`
     - `app.UseCors("StaccatoPolicy")`
     - `app.UseRateLimiter()`
     - `app.UseAuthentication()`
     - `app.UseAuthorization()`
     - `app.MapHub<NotificationHub>("/hubs/notifications")`
     - `app.MapControllers()`
  8. `app.Run()`

**Checkpoint**: `dotnet run --project Application/Application.csproj` starts without errors. US2 scenarios testable manually.

---

## Phase 5: User Story 3 — Developer Runs the Test Suite (Priority: P3)

**Goal**: `dotnet test Staccato.sln` discovers tests; unit and integration filter commands each execute cleanly.

**Independent Test**: Run `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Unit"` and `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Integration"` — both complete without configuration errors (pass or skip is acceptable; fail is not).

- [ ] T037 [US3] Verify `Tests/Tests.csproj` has `<ProjectReference>` entries for all 8 other projects (added in T019); confirm `.gitkeep` files exist in `Tests/Unit/` and `Tests/Integration/` (created in T020–T021); confirm `UnitTest1.cs` is absent (deleted in T008)
- [ ] T038 [US3] Run `dotnet test Staccato.sln` and confirm the test runner starts, discovers zero test cases, and exits without configuration errors

**Checkpoint**: US3 complete — test infrastructure scaffold verified.

---

## Phase 6: User Story 4 — Developer Configures the Application via appsettings.json (Priority: P4)

**Goal**: All four options classes are bound, validated on startup, and fail fast with a clear error on misconfiguration.

**Independent Test**: Temporarily set `Jwt:SecretKey` to a value shorter than 32 characters and restart; verify startup fails with a validation error that names the failing property. Restore the placeholder value.

- [ ] T039 [US4] Confirm `JwtOptions`, `AzureBlobOptions`, `CorsOptions`, `RateLimitOptions` all have `[Required]` (and `[MinLength]`/`[Range]` where applicable) data annotations and are registered with `.ValidateDataAnnotations().ValidateOnStart()` in `Program.cs` (cross-check T023–T026 and T036)
- [ ] T040 [US4] Confirm `appsettings.json` contains only safe placeholder values — no real connection strings, JWT secrets, or API keys that would be hazardous to commit

**Checkpoint**: US4 complete — misconfiguration caught at startup with descriptive error.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final verification of coding standards applied consistently across all new files.

- [ ] T041 [P] Verify all 9 `.csproj` files contain `<Nullable>enable</Nullable>` (FR-026)
- [ ] T042 [P] Verify every new `.cs` file uses file-scoped namespace syntax (`namespace Foo.Bar;` not `namespace Foo.Bar { }`) (FR-027)
- [ ] T043 Run `dotnet build Staccato.sln` one final time and confirm zero errors, zero warnings across the full solution

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately. All T001–T008 are parallel.
- **Phase 2 (Foundational)**: Depends on Phase 1 completion. T009–T011 (ref fixes) must precede T013–T019 (package adds) within this phase. T013–T019 are otherwise parallel once T009–T011 are done.
- **Phase 3 (US1)**: Depends on Phase 2. T020–T021 are parallel; T022 (build verification) follows both.
- **Phase 4 (US2)**: Depends on Phase 3. T023–T032 are parallel (different files). T033 (middleware) must precede T034 (extensions) because extensions reference middleware types. T035 (appsettings) is parallel to T033–T034. T036 (Program.cs) depends on T023–T035 all being complete.
- **Phase 5 (US3)**: Depends on Phase 3. T037 (verification) depends on T019, T020, T021, T008. T038 follows T037.
- **Phase 6 (US4)**: Depends on Phase 4. T039–T040 are verification tasks.
- **Phase 7 (Polish)**: Depends on Phases 3–6 all complete.

### User Story Dependencies

| Story | Depends On | Can Parallel With |
|---|---|---|
| US1 (P1) | Phase 2 complete | — |
| US2 (P2) | US1 complete | US3 (different files) |
| US3 (P3) | US1 complete | US2 (different files) |
| US4 (P4) | US2 complete | — |

### Parallel Opportunities Within Phase 4 (US2)

```
# Launch all options classes together:
T023 JwtOptions.cs
T024 AzureBlobOptions.cs
T025 CorsOptions.cs
T026 RateLimitOptions.cs
T027 ApiModelsAssemblyMarker.cs
T028 BusinessException.cs
T029 NotificationHub.cs
T030 PdfExportBackgroundService.cs
T031 ExportCleanupService.cs
T032 AccountDeletionCleanupService.cs

# Then (T033 first, T034 and T035 in parallel once T033 is done):
T033 BusinessExceptionMiddleware.cs
T034 ServiceCollectionExtensions.cs  ← parallel with T035
T035 appsettings.json                ← parallel with T034

# Then (T036 depends on all above):
T036 Program.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Delete placeholder files
2. Complete Phase 2: Fix references + add packages
3. Complete Phase 3: US1 (clean build + test subdirs)
4. **STOP and VALIDATE**: `dotnet build Staccato.sln` — zero errors, zero warnings
5. MVP delivered: clean, correctly structured 9-project solution

### Incremental Delivery

1. Phase 1 + 2 → Foundation: correct structure, all packages
2. Phase 3 (US1) → Clean build verified ← **MVP**
3. Phase 4 (US2) → Running API with full middleware pipeline
4. Phase 5 (US3) → Test suite scaffold verified
5. Phase 6 (US4) → Configuration validation confirmed
6. Phase 7 → Polish and final build clean

### Total Task Count: 43 tasks

| Phase | Tasks | Story |
|---|---|---|
| Phase 1: Setup | T001–T008 (8 tasks) | — |
| Phase 2: Foundational | T009–T019 (11 tasks) | — |
| Phase 3: US1 | T020–T022 (3 tasks) | US1 |
| Phase 4: US2 | T023–T036 (14 tasks) | US2 |
| Phase 5: US3 | T037–T038 (2 tasks) | US3 |
| Phase 6: US4 | T039–T040 (2 tasks) | US4 |
| Phase 7: Polish | T041–T043 (3 tasks) | — |
| **Total** | **43 tasks** | |

---

## Notes

- `[P]` = parallelizable (different files, no dependency on incomplete task)
- `[US1]`–`[US4]` label maps each task to the user story it directly advances
- Tasks T020–T021 (`.gitkeep` files) are the only US1-labelled content tasks — the bulk of Phase 2 (Foundational) is shared infrastructure that enables US1 to be provable
- FR-021 (auth middleware wiring in `Program.cs`) is captured in T036 step 7 — authentication before authorization in the pipeline
- The three stub background services (T030–T032) have no-op bodies; future features add only the body content without touching `Program.cs` or `AddBackgroundWorkers()`
- Real secrets must never appear in `appsettings.json` — placeholder values only (T040 verifies this)
- After T036, any pre-existing `app.MapOpenApi()` call must also be removed if present (part of the OpenAPI removal initiated in T012)
