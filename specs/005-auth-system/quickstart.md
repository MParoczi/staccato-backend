# Quickstart: Authentication System (005-auth-system)

## Prerequisites

- `appsettings.json` must contain `Jwt`, `Google`, `RateLimit`, `Cors`, and `AzureBlob` sections.
- SQL Server running and `DefaultConnection` set.
- `dotnet run --project Application/Application.csproj` starts the API.

## New appsettings.json sections required

```json
{
  "Jwt": {
    "Issuer": "staccato",
    "Audience": "staccato-client",
    "SecretKey": "<min-32-char-random-key>",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7,
    "RememberMeExpiryDays": 30
  },
  "Google": {
    "ClientId": "<google-oauth-client-id>"
  }
}
```

## Quick integration scenarios

### Register a new user

```http
POST /auth/register
Content-Type: application/json
Accept-Language: en

{
  "email": "jane@example.com",
  "displayName": "Jane Doe",
  "password": "mypassword1"
}
```

**Expected**: 201, body `{ "accessToken": "...", "expiresIn": 900 }`, cookie `staccato_refresh` set.

### Log in

```http
POST /auth/login
Content-Type: application/json

{
  "email": "jane@example.com",
  "password": "mypassword1",
  "rememberMe": false
}
```

**Expected**: 200, new tokens.

### Refresh the session

```http
POST /auth/refresh
Cookie: staccato_refresh=<token>
```

**Expected**: 200, new `accessToken`, new `staccato_refresh` cookie.

### Log out

```http
DELETE /auth/logout
Cookie: staccato_refresh=<token>
```

**Expected**: 204, cookie cleared.

## Verification Checklist

- [ ] No `SaveChanges` called directly in `AuthService` — all persistence via `_uow.CommitAsync()`, except `RevokeAllForUserAsync` which commits atomically.
- [ ] `staccato_refresh` cookie is `HttpOnly`, `SameSite=Strict`, `Secure=true` in production.
- [ ] Access token never appears in a `Set-Cookie` header.
- [ ] Registering with an existing email returns 409, not 500.
- [ ] Login with wrong password and login with unknown email return identical 401 `INVALID_CREDENTIALS` responses (no email enumeration).
- [ ] Theft detection: presenting a revoked token at `POST /auth/refresh` returns 401 AND revokes all user tokens.
- [ ] `POST /auth/google` with a Google credential whose email matches an existing account logs into the existing account (no duplicate user created).
- [ ] `POST /auth/google` when Google service is down returns 503 `SERVICE_UNAVAILABLE`.
- [ ] Sending 11 requests to any `/auth` endpoint within 60s from one IP returns 429 on the 11th request.
- [ ] Error messages from all auth endpoints respect `Accept-Language: hu` and return Hungarian text.
- [ ] All async methods in `AuthService`, `JwtService`, `GoogleTokenValidator` accept and thread through `CancellationToken`.
- [ ] `GoogleOptions.ClientId` is bound from `appsettings.json` — not hardcoded.
- [ ] Unit tests cover: duplicate email (register), wrong password (login), revoked token (refresh), stolen token (refresh), Google validation failure, Google service unavailable.
- [ ] Integration tests cover all 5 endpoints with WebApplicationFactory.
