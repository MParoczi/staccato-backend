# Research: Solution Scaffold

**Branch**: `001-solution-scaffold` | **Date**: 2026-03-01

---

## Decision 1: Current Dependency Violations (Pre-existing)

**Finding**: The template `.csproj` files shipped with incorrect project references that violate Principle I.

| Project | Current (Wrong) References | Required References |
|---|---|---|
| Domain | ApiModels ❌, DomainModels, Repository ❌ | DomainModels **only** |
| Repository | DomainModels ❌, EntityModels, Persistence | Domain, EntityModels, Persistence |
| Api | ApiModels, Domain | ApiModels, Domain, **DomainModels** |

**Decision**: Fix all three during this feature. Domain must have only `DomainModels`. Repository must swap `DomainModels` for `Domain`. Api must add `DomainModels`.

**Alternatives considered**: Leaving violations — rejected; violates NON-NEGOTIABLE Principle I.

---

## Decision 2: FluentValidation Auto-Validation Pipeline

**Finding**: In FluentValidation v11+, the auto-validation pipeline requires `FluentValidation.AspNetCore`, a separate but official companion package from the same author and repository. `AddFluentValidation()` (v10 and earlier) was removed; the replacement is `AddFluentValidationAutoValidation()`.

**Decision**:
- `FluentValidation` → Domain.csproj (per FR-006) and ApiModels.csproj (needed for `AbstractValidator<T>` when validators are written)
- `FluentValidation.AspNetCore` → Application.csproj (provides `AddFluentValidationAutoValidation()`)
- Registration: `builder.Services.AddFluentValidationAutoValidation()` + `builder.Services.AddValidatorsFromAssembly(typeof(SomeApiModelsType).Assembly)`
- At scaffold time, since no validators exist yet, `AddValidatorsFromAssembly` uses a placeholder type from ApiModels

**Rationale**: `FluentValidation.AspNetCore` is the official integration package, part of the FluentValidation ecosystem; no constitution amendment required. The constitution states "FluentValidation — all validators in ApiModels; auto-validation pipeline configured in Application," which implies this package.

**Alternatives considered**: `SharpGrip.FluentValidation.AutoValidation.Mvc` — rejected; third-party library outside approved stack.

---

## Decision 3: Rate Limiting — Path-Based Global Limiter

**Finding**: ASP.NET Core's built-in rate limiting (`Microsoft.AspNetCore.RateLimiting`, included in the framework since .NET 7, no extra package) supports `PartitionedRateLimiter` with path-based logic.

**Decision**: Configure a global `PartitionedRateLimiter<HttpContext>` that returns a fixed-window limiter only for paths starting with `/auth/`, and a no-op limiter for all other paths. Values (window seconds, max requests) are read from `RateLimitOptions` injected from `appsettings.json`.

```
GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    ctx.Request.Path.StartsWithSegments("/auth")
        ? RateLimitPartition.GetFixedWindowLimiter(ip, _ => new { PermitLimit=10, Window=60s })
        : RateLimitPartition.GetNoLimiter("open"))
```

**Rationale**: Centralised in Application; no rate-limiting concerns leak into Api controllers. Controllers need no `[EnableRateLimiting]` attributes.

**Alternatives considered**: Named policy + `[EnableRateLimiting]` on Auth controllers — rejected; spreads rate-limiting concern to the Api project.

---

## Decision 4: BusinessException HTTP Status Code Mapping

**Finding**: The constitution (Principle V) mandates: business rule violations use `{ code, message, details }` envelope with status codes 422, 409, 400, or 403 depending on the violation type.

**Decision**: `BusinessException` (abstract base class in `Domain/Exceptions/`) carries a `StatusCode` property (type `int`, default `422 Unprocessable Entity`). Specific subclasses override `StatusCode` appropriately:
- `ConflictException` → 409
- `ForbiddenException` → 403
- `ValidationException` → 400
- General rule violations → 422 (default)

The `BusinessExceptionMiddleware` reads `exception.StatusCode` to set the response status code.

**Rationale**: Avoids a brittle type-to-status-code dictionary in middleware; each exception type self-describes its HTTP semantics.

**Alternatives considered**: Dictionary mapping in middleware — rejected (fragile, requires updating middleware for each new exception type).

---

## Decision 5: SignalR Hub Design — Typed Hub

**Decision**: `NotificationHub : Hub<INotificationClient>` with typed client interface:
```csharp
public interface INotificationClient {
    Task PdfReady(string exportId, string fileName);
}
public class NotificationHub : Hub<INotificationClient> { }
```
Route: `/hubs/notifications`

The `PdfExportBackgroundService` (future feature) injects `IHubContext<NotificationHub, INotificationClient>` to push `PdfReady` events to connected clients.

**Rationale**: Typed hubs provide compile-time safety for server→client method calls; prevents method-name typos in background services.

**Alternatives considered**: Untyped `Hub` with `Clients.All.SendAsync("PdfReady", ...)` — rejected; no compile-time checking.

---

## Decision 6: QuestPDF License Setup

**Finding**: QuestPDF 2024+ requires an explicit license declaration before the first `Document.Create(...)` call; omitting it throws `QuestPDFException`.

**Decision**: Add to `Program.cs` before `builder.Build()`:
```csharp
QuestPDF.Settings.License = LicenseType.Community;
```

**Rationale**: Community license is free for projects under $1M revenue/funding. Required by the library; must be at application startup.

---

## Decision 7: OpenAPI Package Removal

**Finding**: `Application.csproj` currently references `Microsoft.AspNetCore.OpenApi` (added by the project template). This package is not in the approved technology stack.

**Decision**: Remove `Microsoft.AspNetCore.OpenApi` from `Application.csproj`. Also remove the `app.MapOpenApi()` call from `Program.cs`.

**Rationale**: Swagger/OpenAPI is not in the constitution's technology stack. No spec requirement covers it. Keeping it violates G13.

---

## Decision 8: Problem Details + BusinessException Middleware Ordering

**Decision**: Two-stage exception handling in the pipeline:
1. **Outer**: `BusinessExceptionMiddleware` (custom) — catches `BusinessException`, returns custom JSON envelope.
2. **Inner**: `app.UseExceptionHandler("/error")` with Problem Details — catches all remaining unhandled exceptions, returns RFC 7807 format.

`BusinessExceptionMiddleware` is registered first (outermost) so it catches domain errors before the general exception handler runs.

**Rationale**: Separates domain exception handling from infrastructure exception handling, keeping each handler focused and simple.

---

## Decision 9: CORS AllowCredentials Constraint

**Finding**: When `AllowCredentials()` is set on a CORS policy, the browser rejects wildcard `*` origins. All origins must be explicit strings.

**Decision**: CORS policy is built from `CorsOptions.AllowedOrigins` (always explicit strings). `AllowAnyHeader()` and `AllowAnyMethod()` are used alongside `AllowCredentials()`. If `AllowedOrigins` is empty, CORS middleware returns 403 for all cross-origin requests.

---

## Decision 10: RateLimitOptions — Fourth Options Class

**Finding**: The user plan (step 5) specifies a `RateLimit` section in `appsettings.json` with `AuthWindowSeconds` and `AuthMaxRequests`. A fourth strongly typed options class is needed.

**Decision**: Add `RateLimitOptions` to `Application/Options/` alongside the other three options classes, bound to the `"RateLimit"` configuration section.

**Rationale**: Keeps rate limiting values configurable without code changes (satisfies SC-007 and FR-019).
