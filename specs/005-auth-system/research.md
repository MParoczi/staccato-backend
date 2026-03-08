# Research: Authentication System (005-auth-system)

## Key Architectural Decisions

### Decision 1: Where do Google.Apis.Auth and BCrypt.Net-Next live?

**Decision**: Both NuGet packages go in **Application**, not Domain.

**Rationale**: Domain must remain a pure business-logic layer. Infrastructure concerns (external service wrappers, cryptography libraries) belong in the layer that wires them to Domain interfaces via dependency injection. Interfaces (`IGoogleTokenValidator`, `IPasswordHasher`) are defined in Domain; their implementations (`GoogleTokenValidator`, `BcryptPasswordHasher`) live in Application alongside the other infrastructure wiring.

This diverges from the user's hint ("Add BCrypt.Net-Next to Domain") but aligns with the architecture's clean separation of concerns and avoids polluting Domain with external library dependencies.

**Alternatives considered**:
- Direct BCrypt in Domain via NuGet (allowed by constitution since it's not a project reference) — rejected because it unnecessarily entangles the pure business layer with a crypto library.
- Separate Infrastructure project — rejected as the constitution fixes the solution at 9 projects.

---

### Decision 2: JwtService lives in Application (interface in Domain)

**Decision**: `IJwtService` is defined in Domain/Services/. `JwtService` is implemented in Application/Services/, injecting `IOptions<JwtOptions>` (which already exists in Application/Options/).

**Rationale**: The JWT-signing key and parameters (`JwtOptions`: SecretKey, Issuer, Audience, AccessTokenExpiryMinutes) must remain in Application per the secret-injection rule. `JwtOptions` is already wired in Program.cs and lives in Application. Having `JwtService` consume it in Application avoids exposing Application-level option types to Domain.

`AuthService` (Domain) calls `_jwtService.GenerateAccessToken(user)` and `_jwtService.GenerateRefreshToken()` via the interface — it never touches signing keys or JwtOptions directly.

**Microsoft.IdentityModel.Tokens** is already transitively available in Application via `Microsoft.AspNetCore.Authentication.JwtBearer`.

---

### Decision 3: Localization strategy — two-layer approach

**Decision**: Localization is applied at two points:
1. **FluentValidation messages** (field-level, 400): Validators in ApiModels inject `IStringLocalizer<ValidationMessages>` where `ValidationMessages` is a marker class in ApiModels. Resource files `ValidationMessages.en.resx` and `ValidationMessages.hu.resx` live in `ApiModels/Resources/`. ApiModels adds `Microsoft.Extensions.Localization.Abstractions` NuGet (interfaces only, lightweight). Application calls `builder.Services.AddLocalization()`.
2. **Business-rule messages** (business exceptions, 401/409/503): `BusinessExceptionMiddleware` is extended to accept `IStringLocalizer<BusinessErrors>` via constructor injection. Resource files `BusinessErrors.en.resx` and `BusinessErrors.hu.resx` live in `Application/Resources/`. The exception `Code` (e.g., `"EMAIL_ALREADY_REGISTERED"`) is the resource key; if no localized string is found, the exception's `Message` property is used as the fallback.

The `Accept-Language` header is resolved by `IStringLocalizerFactory` via the standard `RequestLocalizationMiddleware` added to the pipeline.

**Alternatives considered**:
- Hardcode English in all business exceptions, localize only validation — rejected per spec requirement (FR-013) that all client-facing error text must be localized.
- Put all resources in a single assembly — rejected to keep concerns co-located (validator resources with validators, business-error resources with the middleware that handles them).

---

### Decision 4: Theft detection uses RevokeAllForUserAsync

**Decision**: When `POST /auth/refresh` receives a token that exists in the DB but is already revoked (`IsRevoked = true`), AuthService calls `_refreshTokenRepository.RevokeAllForUserAsync(userId)` (which commits immediately via `ExecuteUpdateAsync`) and then throws `UnauthorizedException("INVALID_TOKEN", ...)`.

**Rationale**: `RevokeAllForUserAsync` was designed for exactly this "logout all devices" scenario and already bypasses UoW (per its documented contract). Since the theft path terminates immediately with an exception (no further UoW usage), the direct commit causes no double-commit issue.

---

### Decision 5: New exception types required

Two new `BusinessException` subclasses are needed:

- **`UnauthorizedException`** — StatusCode=401, code=`"UNAUTHORIZED"`: for invalid/expired credentials and token validation failures.
- **`ServiceUnavailableException`** — StatusCode=503, code=`"SERVICE_UNAVAILABLE"`: for Google validation service outages. Caught in `GoogleTokenValidator.ValidateAsync` and re-thrown when `Google.Apis.Auth` throws a network/HTTP error.

---

### Decision 6: AuthTokens result record

**Decision**: AuthService methods that issue credentials return `AuthTokens` (a new record in `DomainModels/Models/`):

```
AuthTokens
  string AccessToken       — signed JWT (returned in response body via AuthResponse)
  int    ExpiresIn         — access token lifetime in seconds (for AuthResponse)
  string RefreshToken      — raw token value (controller puts in HttpOnly cookie)
  DateTime RefreshTokenExpiry — used by controller to set cookie Expires / Max-Age
```

The `AuthResponse` DTO in ApiModels contains only `accessToken` and `expiresIn`. The controller reads `RefreshToken` and `RefreshTokenExpiry` from `AuthTokens` to set the cookie.

---

### Decision 7: IGoogleTokenValidator returns GoogleUserInfo

**Decision**: `IGoogleTokenValidator.ValidateAsync` returns `GoogleUserInfo` (a new record in `DomainModels/Models/`):

```
GoogleUserInfo
  string  GoogleId
  string  Email
  string? Name        — may be null for accounts with no display name
  string? PictureUrl  — Google profile picture URL
```

---

### Decision 8: Display name splitting

**Decision**: AuthService splits `displayName` at the first space character.
- `"Jane Doe"` → FirstName=`"Jane"`, LastName=`"Doe"`
- `"Madonna"` → FirstName=`"Madonna"`, LastName=`""`
- `"Mary Ann Smith"` → FirstName=`"Mary"`, LastName=`"Ann Smith"` (remainder after first space)

For Google sign-in where `GoogleUserInfo.Name` is null or empty: FirstName=`""`, LastName=`""`.

---

### Decision 9: Password minimum length = 8 characters (validator-only enforcement)

**Decision**: Password strength is enforced only via FluentValidation in `RegisterRequestValidator`. No runtime check in AuthService (the validator runs before the controller delegates to the service). Minimum length: 8 characters.

---

## Existing Infrastructure (no changes needed)

| Concern | Already exists | Location |
|---|---|---|
| JWT Bearer authentication wiring | ✅ | `AddAuth()` in ServiceCollectionExtensions |
| Rate limiting on `/auth/*` | ✅ | `AddRateLimiting()` in ServiceCollectionExtensions |
| `JwtOptions` (Issuer, Audience, SecretKey, expiry fields) | ✅ | `Application/Options/JwtOptions.cs` |
| `BusinessExceptionMiddleware` | ✅ (needs localization enhancement) | `Application/Middleware/` |
| `IUserRepository` (GetByEmailAsync, GetByGoogleIdAsync) | ✅ | `Domain/Interfaces/Repositories/` |
| `IRefreshTokenRepository` (GetByTokenAsync, RevokeAllForUserAsync) | ✅ | `Domain/Interfaces/Repositories/` |
| `IUnitOfWork` | ✅ | `Domain/Interfaces/` |
| `AddDomainServices()` stub | ✅ (will be populated) | `ServiceCollectionExtensions` |
