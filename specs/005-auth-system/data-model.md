# Data Model: Authentication System (005-auth-system)

## Existing Entities (no structural changes)

### User (existing — `DomainModels/Models/User.cs`)

No new columns. Auth uses existing fields:

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK, app-generated |
| `Email` | `string` | Unique, max 256 chars |
| `FirstName` | `string` | Derived from registration `displayName` (pre-first-space) |
| `LastName` | `string` | Derived from registration `displayName` (post-first-space, may be empty) |
| `PasswordHash` | `string?` | Null for Google-only accounts |
| `GoogleId` | `string?` | Null for password-only accounts; unique |
| `AvatarUrl` | `string?` | Set from Google profile picture on Google sign-in |
| `CreatedAt` | `DateTime` | UTC, set at creation |
| `ScheduledDeletionAt` | `DateTime?` | Soft-delete field (not touched by auth) |
| `Language` | `Language` enum | Default `Language.English` on creation |

### RefreshToken (existing — `DomainModels/Models/RefreshToken.cs`)

No new columns.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK, app-generated |
| `Token` | `string` | 64 cryptographically random bytes as Base64Url, max 500 chars, unique index |
| `UserId` | `Guid` | FK → User, cascade delete |
| `ExpiresAt` | `DateTime` | UTC; `UtcNow + 7d` (standard) or `UtcNow + 30d` (rememberMe) |
| `CreatedAt` | `DateTime` | UTC |
| `IsRevoked` | `bool` | Starts false; set true on logout or theft detection |

---

## New DomainModels Types

### AuthTokens (`DomainModels/Models/AuthTokens.cs`)

Result type returned by all `IAuthService` methods that issue credentials. The controller uses it to build `AuthResponse` and set the refresh token cookie.

```csharp
namespace DomainModels.Models;

public sealed record AuthTokens(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    DateTime RefreshTokenExpiry
);
```

| Property | Description |
|---|---|
| `AccessToken` | Signed JWT string; returned in response body |
| `ExpiresIn` | Access token lifetime in seconds (e.g., 900 for 15 min) |
| `RefreshToken` | Raw token value; controller writes to HttpOnly cookie |
| `RefreshTokenExpiry` | Token expiry; controller sets as cookie `Expires` / `Max-Age` |

### GoogleUserInfo (`DomainModels/Models/GoogleUserInfo.cs`)

Return type of `IGoogleTokenValidator.ValidateAsync`. Carries validated claims from the Google ID token.

```csharp
namespace DomainModels.Models;

public sealed record GoogleUserInfo(
    string GoogleId,
    string Email,
    string? Name,
    string? PictureUrl
);
```

---

## New Domain Exceptions

### UnauthorizedException (`Domain/Exceptions/UnauthorizedException.cs`)

```
StatusCode = 401
Code       = "UNAUTHORIZED"
Default message = "Authentication failed."
```

Used for: invalid credentials, wrong password, no password set, invalid/expired refresh token.

### ServiceUnavailableException (`Domain/Exceptions/ServiceUnavailableException.cs`)

```
StatusCode = 503
Code       = "SERVICE_UNAVAILABLE"
Default message = "An external service is temporarily unavailable. Please try again later."
```

Used for: Google validation service unreachable or returning unexpected errors.

---

## New Domain Service Interfaces

### IJwtService (`Domain/Services/IJwtService.cs`)

```csharp
public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    int AccessTokenExpirySeconds { get; }
    int RefreshTokenExpiryDays   { get; }  // exposes JwtOptions.RefreshTokenExpiryDays to Domain
    int RememberMeExpiryDays     { get; }  // exposes JwtOptions.RememberMeExpiryDays to Domain
}
```

Implementation: `Application/Services/JwtService.cs`
- `GenerateAccessToken`: creates a signed HS256 JWT with claims `sub` (userId), `email`, `displayName` (FirstName + " " + LastName, trimmed). Reads `JwtOptions` for Issuer, Audience, SecretKey, AccessTokenExpiryMinutes.
- `GenerateRefreshToken`: fills 64 bytes from `RandomNumberGenerator`, returns as Base64Url string.
- `AccessTokenExpirySeconds`: returns `JwtOptions.AccessTokenExpiryMinutes * 60`.
- `RefreshTokenExpiryDays`: returns `JwtOptions.RefreshTokenExpiryDays`.
- `RememberMeExpiryDays`: returns `JwtOptions.RememberMeExpiryDays`.

> **Note**: These two properties exist solely to give `AuthService` (Domain) access to token expiry configuration without referencing `JwtOptions` (Application) directly — which would violate Constitution Principle I.

### IGoogleTokenValidator (`Domain/Services/IGoogleTokenValidator.cs`)

```csharp
public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken ct = default);
}
```

Implementation: `Application/Services/GoogleTokenValidator.cs`
- Calls `GoogleJsonWebSignature.ValidateAsync(idToken, settings)` where settings includes the configured Client ID from `GoogleOptions.ClientId`.
- On `InvalidJwtException` → re-throws as `UnauthorizedException("GOOGLE_AUTH_FAILED", ...)`.
- On `HttpRequestException` or any other unexpected exception → re-throws as `ServiceUnavailableException("GOOGLE_SERVICE_UNAVAILABLE", ...)`.

### IPasswordHasher (`Domain/Services/IPasswordHasher.cs`)

```csharp
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
```

Implementation: `Application/Services/BcryptPasswordHasher.cs`
- `Hash`: calls `BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12)`.
- `Verify`: calls `BCrypt.Net.BCrypt.Verify(password, hash)`.

### IAuthService (`Domain/Services/IAuthService.cs`)

```csharp
public interface IAuthService
{
    Task<AuthTokens> RegisterAsync(string email, string displayName, string password,
        CancellationToken ct = default);
    Task<AuthTokens> LoginAsync(string email, string password, bool rememberMe = false,
        CancellationToken ct = default);
    Task<AuthTokens> GoogleLoginAsync(string idToken, CancellationToken ct = default);
    Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
}
```

---

## New Application Options

### GoogleOptions (`Application/Options/GoogleOptions.cs`)

```csharp
public sealed class GoogleOptions
{
    [Required]
    public string ClientId { get; init; } = string.Empty;
}
```

Bound from `appsettings.json` section `"Google"`. Validated on startup.

---

## API DTOs (ApiModels)

### Request Models (`ApiModels/Auth/`)

**RegisterRequest**
- `Email: string` — required, valid email format, max 256 chars
- `DisplayName: string` — required, max 100 chars
- `Password: string` — required, min 8 chars

**LoginRequest**
- `Email: string` — required, valid email format
- `Password: string` — required, not empty
- `RememberMe: bool` — optional, default false

**GoogleAuthRequest**
- `IdToken: string` — required, not empty

### Response Models (`ApiModels/Auth/`)

**AuthResponse**
- `AccessToken: string`
- `ExpiresIn: int` — seconds

---

## AuthService Logic Contracts

### RegisterAsync

1. `GetByEmailAsync(email)` → if user exists → throw `ConflictException("EMAIL_ALREADY_REGISTERED", ...)`
2. Split `displayName` at first space → `firstName`, `lastName`
3. `_passwordHasher.Hash(password)` → `passwordHash`
4. Create `User` with `Id = Guid.NewGuid()`, set fields, `Language = Language.English`
5. `_userRepository.AddAsync(user)`
6. `_jwtService.GenerateRefreshToken()` → `tokenValue`
7. Create `RefreshToken` with `ExpiresAt = UtcNow + JwtOptions.RefreshTokenExpiryDays`
8. `_refreshTokenRepository.AddAsync(refreshToken)`
9. `_uow.CommitAsync()`
10. Return `new AuthTokens(_jwtService.GenerateAccessToken(user), _jwtService.AccessTokenExpirySeconds, tokenValue, refreshToken.ExpiresAt)`

### LoginAsync

1. `GetByEmailAsync(email)` → if null → throw `UnauthorizedException("INVALID_CREDENTIALS", ...)` (same message as wrong-password to prevent enumeration)
2. If `user.PasswordHash == null` → throw `UnauthorizedException("NO_PASSWORD_SET", ...)`
3. `!_passwordHasher.Verify(password, user.PasswordHash)` → throw `UnauthorizedException("INVALID_CREDENTIALS", ...)`
4. `_jwtService.GenerateRefreshToken()` → `tokenValue`
5. Create `RefreshToken` with `ExpiresAt = UtcNow + (rememberMe ? RememberMeExpiryDays : RefreshTokenExpiryDays)`
6. `_refreshTokenRepository.AddAsync(refreshToken)`
7. `_uow.CommitAsync()`
8. Return `AuthTokens`

### GoogleLoginAsync

1. `_googleTokenValidator.ValidateAsync(idToken)` → `GoogleUserInfo` (throws on failure)
2. `GetByGoogleIdAsync(googleId)` → if found → go to step 6
3. `GetByEmailAsync(email)` → if found (existing local account) → link: `user.GoogleId = googleId`, set `AvatarUrl` if currently null, `_userRepository.Update(user)`, go to step 6
4. Create new `User` from `GoogleUserInfo`: split `Name` for FirstName/LastName, `AvatarUrl = PictureUrl`
5. `_userRepository.AddAsync(user)`
6. Create `RefreshToken`, `_refreshTokenRepository.AddAsync(refreshToken)`
7. `_uow.CommitAsync()`
8. Return `AuthTokens`

### RefreshAsync

1. `GetByTokenAsync(tokenValue)` → if null → throw `UnauthorizedException("INVALID_TOKEN", ...)`
2. If `token.IsRevoked`:
   a. `_refreshTokenRepository.RevokeAllForUserAsync(token.UserId)` — commits immediately
   b. Throw `UnauthorizedException("INVALID_TOKEN", ...)`
3. If `token.ExpiresAt <= UtcNow` → throw `UnauthorizedException("TOKEN_EXPIRED", ...)`
4. `GetByIdAsync(token.UserId)` → if null → throw `NotFoundException()`
5. Create `revokedToken = token with { IsRevoked = true }`, `_refreshTokenRepository.Update(revokedToken)`
6. Create new `RefreshToken` with `ExpiresAt = token.ExpiresAt` (inherits the original token's fixed expiry — session ends at the original deadline regardless of rotation count), `_refreshTokenRepository.AddAsync(newToken)`
7. `_uow.CommitAsync()`
8. Return `AuthTokens` (with `RefreshTokenExpiry = token.ExpiresAt`)

### LogoutAsync

1. `GetByTokenAsync(tokenValue)` → if null → return (idempotent)
2. Create `revokedToken = token with { IsRevoked = true }`, `_refreshTokenRepository.Update(revokedToken)`
3. `_uow.CommitAsync()`

---

## Business Error Codes

| Code | Status | Trigger |
|---|---|---|
| `EMAIL_ALREADY_REGISTERED` | 409 | Email already exists at registration |
| `INVALID_CREDENTIALS` | 401 | Wrong password or unregistered email at login |
| `NO_PASSWORD_SET` | 401 | Google-only account attempts local login |
| `INVALID_TOKEN` | 401 | Refresh token not found or already revoked |
| `TOKEN_EXPIRED` | 401 | Refresh token found but past `ExpiresAt` |
| `GOOGLE_AUTH_FAILED` | 401 | Google ID token rejected by Google |
| `SERVICE_UNAVAILABLE` | 503 | Google validation service unreachable |

---

## Localization Resources

### `ApiModels/Resources/ValidationMessages.en.resx` (English)

Key → Value examples:
- `EmailRequired` → `"Email address is required."`
- `EmailInvalid` → `"Please enter a valid email address."`
- `DisplayNameRequired` → `"Display name is required."`
- `PasswordRequired` → `"Password is required."`
- `PasswordTooShort` → `"Password must be at least 8 characters."`
- `IdTokenRequired` → `"Google ID token is required."`

### `ApiModels/Resources/ValidationMessages.hu.resx` (Hungarian)

Key → Value examples:
- `EmailRequired` → `"Az e-mail cím megadása kötelező."`
- `EmailInvalid` → `"Kérem érvényes e-mail címet adjon meg."`
- `DisplayNameRequired` → `"A megjelenítendő név megadása kötelező."`
- `PasswordRequired` → `"A jelszó megadása kötelező."`
- `PasswordTooShort` → `"A jelszónak legalább 8 karakter hosszúnak kell lennie."`
- `IdTokenRequired` → `"A Google azonosító token megadása kötelező."`

### `Application/Resources/BusinessErrors.en.resx` (English)

Key = error code → Value = localized message:
- `EMAIL_ALREADY_REGISTERED` → `"An account with this email address already exists."`
- `INVALID_CREDENTIALS` → `"Invalid email address or password."`
- `NO_PASSWORD_SET` → `"This account uses Google Sign-In. Please log in with Google."`
- `INVALID_TOKEN` → `"Your session is no longer valid. Please log in again."`
- `TOKEN_EXPIRED` → `"Your session has expired. Please log in again."`
- `GOOGLE_AUTH_FAILED` → `"Google Sign-In failed. Please try again."`
- `SERVICE_UNAVAILABLE` → `"An external service is temporarily unavailable. Please try again later."`

### `Application/Resources/BusinessErrors.hu.resx` (Hungarian)

Key = error code → Value = localized message:
- `EMAIL_ALREADY_REGISTERED` → `"Ezzel az e-mail címmel már létezik fiók."`
- `INVALID_CREDENTIALS` → `"Érvénytelen e-mail cím vagy jelszó."`
- `NO_PASSWORD_SET` → `"Ez a fiók Google bejelentkezést használ. Kérem, lépjen be a Google-lal."`
- `INVALID_TOKEN` → `"A munkamenet már nem érvényes. Kérem, lépjen be újra."`
- `TOKEN_EXPIRED` → `"A munkamenet lejárt. Kérem, lépjen be újra."`
- `GOOGLE_AUTH_FAILED` → `"A Google bejelentkezés sikertelen volt. Kérem, próbálja újra."`
- `SERVICE_UNAVAILABLE` → `"Egy külső szolgáltatás átmenetileg nem érhető el. Kérem, próbálja újra."`
