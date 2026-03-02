# Data Model: Solution Scaffold

**Branch**: `001-solution-scaffold` | **Date**: 2026-03-01

This feature introduces no database entities. It establishes the configuration
object model (Options classes), the domain exception hierarchy, and the SignalR
client contract — all of which are pure C# types with no EF Core involvement.

---

## Options Classes (Application/Options/)

### JwtOptions

Bound to `appsettings.json` → `"Jwt"` section.

| Property | Type | Description |
|---|---|---|
| `Issuer` | `string` | JWT issuer claim (`iss`) |
| `Audience` | `string` | JWT audience claim (`aud`) |
| `SecretKey` | `string` | HMAC-SHA256 signing key (min 32 chars) |
| `AccessTokenExpiryMinutes` | `int` | Lifetime of access token (e.g., 15) |
| `RefreshTokenExpiryDays` | `int` | Standard refresh token lifetime (e.g., 7) |
| `RememberMeExpiryDays` | `int` | Extended refresh token lifetime (e.g., 30) |

**Validation rules** (checked at startup via `ValidateDataAnnotations` or `ValidateOnStart`):
- `SecretKey` must not be null or empty
- `AccessTokenExpiryMinutes` must be > 0
- `RefreshTokenExpiryDays` must be > 0
- `RememberMeExpiryDays` must be ≥ `RefreshTokenExpiryDays`

---

### AzureBlobOptions

Bound to `appsettings.json` → `"AzureBlob"` section.

| Property | Type | Description |
|---|---|---|
| `ConnectionString` | `string` | Azure Storage connection string or emulator URI |
| `ContainerName` | `string` | Blob container name for PDF exports and avatars |

**Validation rules**: Both properties must not be null or empty.

---

### CorsConfiguration

Bound to `appsettings.json` → `"Cors"` section. Named `CorsConfiguration` to avoid ambiguous-reference conflict with `Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions`.

| Property | Type | Description |
|---|---|---|
| `AllowedOrigins` | `string[]` | Explicit list of allowed cross-origin origins |

**Validation rules**: Array must not be null (empty array is valid — rejects all cross-origin requests).

---

### RateLimitOptions

Bound to `appsettings.json` → `"RateLimit"` section.

| Property | Type | Description |
|---|---|---|
| `AuthWindowSeconds` | `int` | Fixed window duration in seconds (e.g., 60) |
| `AuthMaxRequests` | `int` | Max requests per window per IP (e.g., 10) |

**Validation rules**: Both values must be > 0.

---

## Domain Exception Hierarchy (Domain/Exceptions/)

### BusinessException (abstract base)

The single catch type used by `BusinessExceptionMiddleware`. All domain rule
violations are thrown as subclasses of this type.

| Property | Type | Description |
|---|---|---|
| `Code` | `string` | Machine-readable error code (English, SCREAMING_SNAKE_CASE) |
| `StatusCode` | `int` | HTTP status code to return (default: 422) |
| `Details` | `object?` | Optional structured detail payload |

`Message` (inherited from `Exception`) carries the human-readable, localisable message.

### Planned Subclasses (implemented in later features as needed)

| Class | StatusCode | Typical Use |
|---|---|---|
| `NotFoundException` | 404 | Resource not found |
| `ForbiddenException` | 403 | Ownership violation |
| `ConflictException` | 409 | Duplicate/conflict |
| `ValidationException` | 400 | Input rule violation |
| `UnprocessableException` | 422 | Business rule violation (default) |

> **Note**: Only `BusinessException` base class is created in this feature.
> Subclasses are introduced when their corresponding domain features are built.

---

## SignalR Client Interface (Application/Hubs/)

### INotificationClient

Defines the methods the server can invoke on connected SignalR clients.

| Method | Parameters | Description |
|---|---|---|
| `PdfReady` | `string exportId, string fileName` | Notifies a client that their PDF export is ready |

### NotificationHub

`Hub<INotificationClient>` — stateless typed hub. Hub route: `/hubs/notifications`.

The hub itself contains no server-callable methods at scaffold time. The background
service (`PdfExportBackgroundService`, future feature) sends notifications via
`IHubContext<NotificationHub, INotificationClient>`.
