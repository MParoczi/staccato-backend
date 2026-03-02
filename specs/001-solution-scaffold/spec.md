# Feature Specification: Solution Scaffold — 9-Project ASP.NET Core Backend

**Feature Branch**: `001-solution-scaffold`
**Created**: 2026-03-01
**Status**: Draft
**Input**: User description: "Set up a 9-project ASP.NET Core 10 solution named Staccato for a notebook application backend."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Developer Builds the Solution from Scratch (Priority: P1)

A new developer joins the team, clones the repository, and runs a single build command. Every project compiles successfully and the dependency graph is exactly as specified — no extra references, no missing packages, no placeholder files. The developer can immediately start adding features without needing to fix project structure or resolve package conflicts.

**Why this priority**: Without a clean, buildable solution, no other development work can begin. This is the foundation for all subsequent features.

**Independent Test**: Run the solution build command from the repository root. Every project must compile with zero errors and zero warnings.

**Acceptance Scenarios**:

1. **Given** a freshly cloned repository, **When** the developer runs the build command, **Then** all 9 projects compile successfully with no errors.
2. **Given** the compiled solution, **When** the dependency graph is inspected, **Then** each project only references the projects listed in its specification and no others.
3. **Given** any project in the solution, **When** its source files are listed, **Then** no `Class1.cs` or `UnitTest1.cs` placeholder file exists.

---

### User Story 2 - Developer Runs the Application and Gets a Live API (Priority: P2)

A developer runs the Application project and gets a live API server. The server starts without errors, reads all configuration from `appsettings.json`, and all middleware (CORS, rate limiting, authentication, error handling, SignalR) is active in the correct order. Endpoints under `/auth/*` are rate-limited automatically; all other endpoints are not.

**Why this priority**: Verifying the application actually runs and middleware is wired correctly is necessary before any API endpoint can be developed or tested manually.

**Independent Test**: Start the API and send requests to verify: (a) a cross-origin request from an allowed origin succeeds; (b) more than 10 rapid requests to an `/auth/*` endpoint triggers a 429 response; (c) a deliberate business-rule violation returns `{ "code": "...", "message": "..." }`; (d) an infrastructure error returns RFC 7807 Problem Details format.

**Acceptance Scenarios**:

1. **Given** valid configuration in `appsettings.json`, **When** the Application is started, **Then** it runs without startup errors.
2. **Given** a running API and a request from a configured allowed origin, **When** the request includes a CORS preflight, **Then** the server responds with appropriate CORS headers including `Access-Control-Allow-Credentials: true`.
3. **Given** a running API and an IP sending 11 requests within 60 seconds to `/auth/login`, **When** the 11th request arrives, **Then** the server responds with HTTP 429 and a `Retry-After` header.
4. **Given** a running API and an IP sending 11 requests within 60 seconds to any non-`/auth/*` endpoint, **When** the 11th request arrives, **Then** the request is not rate-limited (HTTP 404 or other non-429 status).
5. **Given** a running API and a request body that fails validation rules, **When** the request is submitted, **Then** the server responds with HTTP 400 and a body of `{ "errors": { "fieldName": ["message"] } }`.
6. **Given** a running API and a request that triggers a business rule violation, **When** the service throws a `BusinessException`, **Then** the middleware returns `{ "code": string, "message": string, "details": object? }` with the exception's HTTP status code.
7. **Given** a running API and an unexpected infrastructure error, **When** the error propagates, **Then** the response body conforms to RFC 7807 Problem Details format with HTTP 500.
8. **Given** a running API and a real-time client with a valid JWT, **When** the client connects to `/hubs/notifications`, **Then** the connection is accepted.
9. **Given** a running API and a real-time client without a JWT, **When** the client attempts to connect to `/hubs/notifications`, **Then** the connection is rejected with HTTP 401.

---

### User Story 3 - Developer Runs the Test Suite (Priority: P3)

A developer runs both the unit test suite and the integration test suite from a single command. Both pass, and the output clearly distinguishes unit tests from integration tests. Each suite can also be filtered and run independently.

**Why this priority**: A working test harness is required to validate all subsequent feature implementations.

**Independent Test**: Run unit tests with the unit filter and integration tests with the integration filter independently. Both subsets must execute and produce a result (pass or fail) without configuration errors.

**Acceptance Scenarios**:

1. **Given** the Tests project, **When** the full test command is run, **Then** the test runner discovers and executes all tests without configuration errors.
2. **Given** the Tests project, **When** the unit test filter is applied, **Then** only tests in the `Unit` namespace are executed.
3. **Given** the Tests project, **When** the integration test filter is applied, **Then** only tests in the `Integration` namespace are executed.

---

### User Story 4 - Developer Configures the Application via `appsettings.json` (Priority: P4)

All runtime configuration (database connection string, JWT settings, Azure Blob connection string, allowed CORS origins, rate limit parameters) lives in `appsettings.json`. A developer changes any value and restarts the application without modifying source code. Misconfiguration is caught at startup rather than silently at runtime. Committed `appsettings.json` contains only safe placeholder values — real secrets live outside source control.

**Why this priority**: Safe, centralised configuration is required before any environment-specific deployment can happen.

**Independent Test**: Change an allowed origin in `appsettings.json`, restart the application, and verify the new origin is accepted while a previously allowed (now removed) origin is rejected. Verify that a missing required config key causes a startup error with a clear message.

**Acceptance Scenarios**:

1. **Given** `appsettings.json` with an `AllowedOrigins` array, **When** the application starts, **Then** CORS allows exactly those origins and no others.
2. **Given** `appsettings.json` with JWT settings, **When** the application restarts, **Then** the JWT middleware is configured with those values without any code change.
3. **Given** `appsettings.json` with Azure Blob settings, **When** the application restarts, **Then** the blob storage client is initialised with those values.
4. **Given** a missing or empty required configuration section, **When** the application starts, **Then** startup fails immediately with a descriptive error identifying the missing key.

---

### Edge Cases

- What happens when `AllowedOrigins` in `appsettings.json` is an empty array? The API starts but rejects all cross-origin requests.
- What happens when `AllowedOrigins` is `null` (not present in `appsettings.json`)? Startup fails immediately — a null array is treated as a missing required configuration.
- What happens when a business-rule exception carries no `details` payload? The middleware omits the `details` field (or serialises it as `null`) without crashing.
- What happens when `BusinessExceptionMiddleware` itself throws while writing the response? The error propagates to the outer Problem Details handler, which returns HTTP 500.
- What happens when FluentValidation finds no validators registered for a given request type? The auto-pipeline passes the request through without error.
- What happens when two projects inadvertently reference each other (circular dependency)? The build fails with a clear error rather than silently allowing it.
- What happens when `RateLimitOptions.AuthWindowSeconds` or `AuthMaxRequests` is zero or negative? Startup fails with a descriptive validation error.
- What happens when `BusinessException.Code` is `null`? The middleware serialises `"code": null` — subclasses MUST guarantee a non-null `Code` value.
- What happens when an origin in `AllowedOrigins` is malformed (not a valid URI)? The CORS middleware rejects all cross-origin requests from that origin without a startup failure.

## Requirements *(mandatory)*

### Functional Requirements

**Project Structure**

- **FR-001**: The solution MUST contain exactly 9 projects: `Application`, `Api`, `Domain`, `Repository`, `Persistence`, `EntityModels`, `DomainModels`, `ApiModels`, and `Tests`.
- **FR-002**: Each project MUST reference only its specified dependencies — `Application` → Api, Domain, Repository, Persistence; `Api` → Domain, ApiModels, DomainModels; `Domain` → DomainModels only; `Repository` → Domain, EntityModels, Persistence; `Persistence` → EntityModels; `EntityModels`, `DomainModels`, `ApiModels` → none; `Tests` → all projects. Any violation causes a build error and MUST be corrected before merging.
- **FR-003**: Every `Class1.cs` and `UnitTest1.cs` placeholder file MUST be deleted from all projects during setup.

**Package Dependencies**

- **FR-004**: The `Application` project MUST include packages for JWT Bearer authentication, Azure Blob Storage, PDF generation, SignalR hosting, and the FluentValidation ASP.NET Core integration (required for the auto-validation pipeline).
- **FR-005**: The `Api` project MUST include the AutoMapper dependency injection integration package.
- **FR-006**: The `Domain` project MUST include the FluentValidation core package.
- **FR-007**: The `Repository` project MUST include Entity Framework Core.
- **FR-008**: The `Persistence` project MUST include the SQL Server EF Core provider and EF Core migration tools.
- **FR-009**: The `Tests` project MUST include xUnit, Moq, ASP.NET Core MVC testing utilities, InMemory EF Core, the .NET test SDK, and code coverage tooling. It MUST also reference all 8 other projects.
- **FR-022**: The `ApiModels` project MUST include the FluentValidation core package so that validators defined in that project can subclass `AbstractValidator<T>` without referencing any other project.

**Application Startup**

- **FR-010**: The application MUST configure a named CORS policy with: (a) allowed origins loaded exclusively from `appsettings.json` — origins MUST NOT be hardcoded; (b) `AllowCredentials()` so HttpOnly cookies are transmitted cross-origin; (c) `AllowAnyHeader()` and `AllowAnyMethod()`. Because `AllowCredentials()` is set, the `AllowedOrigins` array MUST contain only specific origin strings — wildcards are prohibited.
- **FR-011**: The application MUST apply a fixed-window rate limit of 10 requests per minute per IP address exclusively to routes beginning with `/auth/` and MUST NOT rate-limit any other route. Clients exceeding the limit receive HTTP 429 with a `Retry-After` header indicating seconds until the window resets.
- **FR-012**: The application MUST register FluentValidation as an automatic validation pipeline so that validation executes before any controller action body runs. Validation failures return HTTP 400 with a body of `{ "errors": { "fieldName": ["message"] } }`.
- **FR-013**: The application MUST configure Problem Details for infrastructure errors, returning RFC 7807-compliant response bodies for HTTP 500 responses.
- **FR-014**: The application MUST include `BusinessExceptionMiddleware` as the outermost middleware. It catches any exception derived from `BusinessException` (an abstract base class in `Domain/Exceptions/`) and returns `{ "code": string, "message": string, "details": object? }` using the exception's `StatusCode` property. Status codes follow these rules: ownership violations → 403; duplicate/conflict → 409; input rule violations → 400; all other business rule violations → 422. All non-`BusinessException` types propagate to the Problem Details handler (FR-013).
- **FR-015**: The application MUST configure SignalR and expose `NotificationHub` at the route `/hubs/notifications`. The hub MUST require authentication — unauthenticated connection attempts MUST be rejected with HTTP 401 on the negotiate handshake.
- **FR-021**: The application MUST wire `UseAuthentication()` and `UseAuthorization()` into the middleware pipeline so that JWT Bearer tokens are validated and `[Authorize]` attributes are enforced on all subsequent controllers and hubs.
- **FR-016**: The application MUST scan and register all AutoMapper profiles from the `Application`, `Api`, and `Repository` project assemblies. `Repository` must be included because `EntityModel → DomainModel` profiles (per constitution §Technology Stack) will live there and must be discoverable at startup.
- **FR-017**: The application MUST register stub `IHostedService` classes for the three background workers (`PdfExportBackgroundService`, `ExportCleanupService`, `AccountDeletionCleanupService`). These stubs contain no implementation logic; their full bodies are delivered in later feature specs.
- **FR-023**: The application MUST set `QuestPDF.Settings.License = LicenseType.Community` before any PDF generation occurs. This call MUST appear in `Program.cs` before `builder.Build()`.
- **FR-024**: The middleware pipeline MUST follow this exact order to ensure correct runtime behaviour: (1) `BusinessExceptionMiddleware`, (2) `UseExceptionHandler` (Problem Details), (3) `UseHttpsRedirection`, (4) `UseCors`, (5) `UseRateLimiter`, (6) `UseAuthentication`, (7) `UseAuthorization`, (8) `MapHub<NotificationHub>`, (9) `MapControllers`. Deviating from this order can cause silent CORS pre-flight failures, rate-limit bypass, or authentication gaps.

**Configuration**

- **FR-018**: Database connection string, JWT settings (secret, issuer, audience, token lifetimes), Azure Blob connection string, CORS allowed origins, and rate limit parameters MUST all reside in `appsettings.json`.
- **FR-019**: Each configuration group MUST be bound to a dedicated strongly typed options class (`JwtOptions`, `AzureBlobOptions`, `CorsConfiguration`, `RateLimitOptions`) via the options pattern, with `.ValidateDataAnnotations()` and `.ValidateOnStart()` so that any misconfiguration causes an immediate startup failure with a descriptive error rather than a silent runtime defect.
- **FR-020**: No secret or configuration value MUST be hardcoded in source code. Additionally, the committed `appsettings.json` MUST contain only safe placeholder values (e.g., `"CHANGE-ME"`); real secrets MUST be supplied via .NET user-secrets, environment variables, or a secrets manager and MUST NOT be committed to source control.

**Test Project Structure**

- **FR-025**: The `Tests` project MUST contain `Tests/Unit/` and `Tests/Integration/` subdirectories (with `.gitkeep` placeholder files) to enforce the namespace-based filter convention from day one.

**Code Quality**

- **FR-026**: Nullable reference types (`<Nullable>enable</Nullable>`) MUST be enabled in every project's `.csproj` file.
- **FR-027**: All new C# source files MUST use file-scoped namespace declarations.

### Key Entities

- **BusinessException**: Abstract base class defined in `Domain/Exceptions/`. Carries a non-null `Code` string (machine-readable, SCREAMING_SNAKE_CASE), an `int StatusCode` property (default `422`; overridden by subclasses), and an optional `Details` object. `Message` (inherited from `Exception`) carries the human-readable, localisable message. All specific domain rule exceptions inherit from this class. Subclasses are delivered in later feature specs; only the base class is created in this feature.
- **JwtOptions**: Encapsulates JWT configuration — `SecretKey` (string, minimum 32 characters for HS256 validity), `Issuer`, `Audience`, `AccessTokenExpiryMinutes` (> 0), `RefreshTokenExpiryDays` (> 0), `RememberMeExpiryDays` (must be ≥ `RefreshTokenExpiryDays`). Missing or invalid values prevent the application from starting.
- **AzureBlobOptions**: Encapsulates `ConnectionString` and `ContainerName` — both must be non-null and non-empty. Missing values prevent the application from starting.
- **CorsConfiguration**: Encapsulates `AllowedOrigins` (`string[]`). An empty array is valid (all cross-origin requests rejected); a null value prevents the application from starting. Values must be specific origin strings — wildcards are incompatible with `AllowCredentials()`. Named `CorsConfiguration` (not `CorsOptions`) to avoid an ambiguous-reference conflict with `Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions` in startup code.
- **RateLimitOptions**: Encapsulates `AuthWindowSeconds` (int, > 0) and `AuthMaxRequests` (int, > 0). Both values must be positive; zero or negative values prevent the application from starting.
- **ApiModelsAssemblyMarker**: A dedicated empty static class created in `ApiModels/` whose sole purpose is to serve as a stable type reference for `typeof(ApiModelsAssemblyMarker).Assembly` when registering FluentValidation validators at startup. It MUST NOT carry any other responsibility.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The entire solution builds to completion with zero errors and zero warnings (of any category).
- **SC-002**: All 9 projects are present with exactly the prescribed inter-project references, verifiable by inspecting each `.csproj` file — no missing and no extra `<ProjectReference>` entries.
- **SC-003**: The application starts and accepts its first request without error under typical development conditions.
- **SC-004**: A stream of 11 identical requests to an `/auth/*` route within 60 seconds from the same IP results in exactly 10 non-429 responses (including HTTP 404 if no route is mapped yet) and 1 HTTP 429 response.
- **SC-005**: A stream of 11 identical requests to any non-`/auth/*` route within 60 seconds from the same IP results in 11 non-429 responses (no rate limiting applied).
- **SC-006**: Both unit and integration test suites can be discovered and run independently under typical development conditions.
- **SC-007**: Changing any value in `appsettings.json` and restarting the application takes effect immediately with no source code changes required.
- **SC-008**: All 27 functional requirements (FR-001 through FR-027, excluding the renumbering gap at FR-021) are verifiably satisfied.

## Clarifications

### Session 2026-03-01

- Q: Should the CORS policy allow credentials (cookies/auth headers cross-origin)? → A: Yes — CORS policy calls `AllowCredentials()` for all configured origins (FR-010 updated).
- Q: Should `UseAuthentication()` and `UseAuthorization()` be wired into the middleware pipeline as part of this scaffold? → A: Yes — both are wired in the pipeline now (FR-021 added).
- Q: What type should the custom error middleware catch to identify domain exceptions? → A: Abstract `BusinessException` base class in `Domain` — middleware catches `BusinessException` and reads `Code`, `StatusCode`, and `Details` (FR-014 updated; `BusinessException` added to Key Entities).

### Session 2026-03-01 (Architecture Review)

- Q: `DomainException` (spec) vs `BusinessException` (plan/constitution) — which is canonical? → A: `BusinessException` — aligns with constitution Principles III, G4, and G7. All occurrences of `DomainException` in this spec replaced.
- Q: How should "standard development machine" in SC-003 and SC-006 be defined? → A: Remove hardware qualifier; restate as "under typical development conditions."
- Q: Should FR-017 say "MUST register" functional implementations or stubs? → A: Stubs — FR-017 updated to say stub classes with no logic; Assumptions updated accordingly.
- Q: What type from `ApiModels` should anchor the validator assembly scan? → A: A dedicated `ApiModelsAssemblyMarker` empty static class — added to Key Entities and FR-022.
- Q: Does FR-020 extend to prohibiting real secrets in committed `appsettings.json`? → A: Yes — FR-020 extended to require placeholder values in committed `appsettings.json`.

## Assumptions

- The solution already has a git repository initialised at the root; this spec branch is created from `main`.
- The target runtime is .NET 10 (ASP.NET Core 10); no downgrade to an earlier version is acceptable.
- SQL Server is the only supported database provider; SQLite and PostgreSQL are out of scope.
- Three pre-existing dependency violations in the template `.csproj` files are resolved by this feature: (a) `Domain.csproj` illegally references `ApiModels` and `Repository` — both removed; (b) `Repository.csproj` references `DomainModels` instead of `Domain` — corrected; (c) `Api.csproj` is missing a `DomainModels` reference — added.
- Adding `FluentValidation` to `Domain` makes it transitively available to projects that reference `Domain` (e.g., `Repository`, `Application`). This is intentional — `Domain` is the core package carrier.
- `appsettings.Development.json` may be used for local developer overrides and is not committed; it is not part of this scaffold specification.
- The `ApiModelsAssemblyMarker` class is a permanent scaffolding type in `ApiModels` and must not be removed even after real validators are added, as it is the stable DI assembly anchor.
- The three `ServiceCollectionExtensions` methods `AddDatabase`, `AddRepositories`, and `AddDomainServices` are stub methods with empty bodies. Their existence ensures `Program.cs` compiles and that future features only need to add method bodies, not restructure the startup file.
- The three background worker stub classes (`PdfExportBackgroundService`, `ExportCleanupService`, `AccountDeletionCleanupService`) each implement `IHostedService` with no-op `StartAsync`/`StopAsync` methods. Full implementations are delivered in later feature specs.
- Code quality rules from Constitution §IX apply throughout: `.Result`/`.Wait()` are prohibited; every public async method accepts `CancellationToken ct = default`.
