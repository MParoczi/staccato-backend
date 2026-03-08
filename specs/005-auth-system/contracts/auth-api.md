# API Contracts: Authentication System (005-auth-system)

All routes are prefixed with `/auth`. Rate limit: 10 req/min/IP on every endpoint in this group (already configured via `AddRateLimiting`).

Cookie name: `staccato_refresh`. Settings: `HttpOnly=true`, `SameSite=Strict`, `Secure=true` (production).

---

## POST /auth/register

**Authorization**: None
**Description**: Creates a new user account and issues tokens.

### Request

```json
{
  "email": "jane@example.com",
  "displayName": "Jane Doe",
  "password": "s3cr3tP@ss"
}
```

| Field | Type | Validation |
|---|---|---|
| `email` | `string` | Required, valid email format, max 256 chars |
| `displayName` | `string` | Required, max 100 chars |
| `password` | `string` | Required, min 8 chars |

### Success Response — 201 Created

Sets cookie: `staccato_refresh=<token>; HttpOnly; SameSite=Strict; Secure; Expires=+7d`

```json
{
  "accessToken": "eyJ...",
  "expiresIn": 900
}
```

### Error Responses

| Status | Code | Condition |
|---|---|---|
| 400 | (FluentValidation format) | Validation failure |
| 409 | `EMAIL_ALREADY_REGISTERED` | Email already in use |

---

## POST /auth/login

**Authorization**: None
**Description**: Authenticates with email and password.

### Request

```json
{
  "email": "jane@example.com",
  "password": "s3cr3tP@ss",
  "rememberMe": false
}
```

| Field | Type | Validation |
|---|---|---|
| `email` | `string` | Required, valid email format |
| `password` | `string` | Required, not empty |
| `rememberMe` | `bool` | Optional, default `false` |

### Success Response — 200 OK

- `rememberMe=false` → cookie `Expires=+7d`
- `rememberMe=true` → cookie `Expires=+30d`

```json
{
  "accessToken": "eyJ...",
  "expiresIn": 900
}
```

### Error Responses

| Status | Code | Condition |
|---|---|---|
| 400 | (FluentValidation format) | Validation failure |
| 401 | `INVALID_CREDENTIALS` | Wrong email or password (same error for both to prevent enumeration) |
| 401 | `NO_PASSWORD_SET` | Account exists but uses Google Sign-In only |

---

## POST /auth/google

**Authorization**: None
**Description**: Authenticates or registers via Google Sign-In. The Google ID token is validated server-side.

### Request

```json
{
  "idToken": "eyJ..."
}
```

| Field | Type | Validation |
|---|---|---|
| `idToken` | `string` | Required, not empty |

### Success Response — 200 OK

Sets cookie: `staccato_refresh=<token>; HttpOnly; SameSite=Strict; Secure; Expires=+7d`

```json
{
  "accessToken": "eyJ...",
  "expiresIn": 900
}
```

**Behaviour**:
- If `GoogleId` is known → return existing user.
- If email matches existing account → link Google ID to existing account and update `AvatarUrl` **only if the existing value is currently null**; return existing user.
- Otherwise → create new account from Google profile.

### Error Responses

| Status | Code | Condition |
|---|---|---|
| 400 | (FluentValidation format) | Validation failure |
| 401 | `GOOGLE_AUTH_FAILED` | Google rejected the ID token (invalid, expired, wrong audience) |
| 503 | `SERVICE_UNAVAILABLE` | Google validation service unreachable |

---

## POST /auth/refresh

**Authorization**: None (reads `staccato_refresh` cookie)
**Description**: Exchanges the current refresh token for a new access token. Rotates the refresh token atomically.

### Request

No body. Reads refresh token from the `staccato_refresh` HttpOnly cookie.

### Success Response — 200 OK

Sets new cookie: `staccato_refresh=<newToken>; HttpOnly; SameSite=Strict; Secure; Expires=<original expiry>`
Clears old cookie.

```json
{
  "accessToken": "eyJ...",
  "expiresIn": 900
}
```

**Theft detection**: If the presented token is already revoked, all active tokens for the user are immediately revoked (full logout) before returning 401.

### Error Responses

| Status | Code | Condition |
|---|---|---|
| 401 | `INVALID_TOKEN` | Cookie absent, empty, whitespace, or token not found in DB |
| 401 | `INVALID_TOKEN` | Token found but already revoked (triggers full-revocation theft path) |
| 401 | `TOKEN_EXPIRED` | Token found but past expiry |

---

## DELETE /auth/logout

**Authorization**: None (reads `staccato_refresh` cookie)
**Description**: Revokes the current refresh token server-side and clears the cookie. Idempotent.

### Request

No body. Reads refresh token from the `staccato_refresh` HttpOnly cookie.

### Success Response — 204 No Content

Clears cookie: `staccato_refresh` (set to expired).

### Error Responses

No errors returned — logout is idempotent. If the cookie is missing or token not found, still returns 204.

---

## JWT Access Token Claims

| Claim | Source |
|---|---|
| `sub` | `user.Id` (Guid as string) |
| `email` | `user.Email` |
| `displayName` | `user.FirstName + " " + user.LastName` (trimmed) |
| `iss` | `JwtOptions.Issuer` |
| `aud` | `JwtOptions.Audience` |
| `exp` | `UtcNow + AccessTokenExpiryMinutes` |

Algorithm: HS256. Signing key: `JwtOptions.SecretKey` (UTF-8 encoded, minimum 32 characters).

---

## Localization

All error messages in `code`/`message` envelopes and FluentValidation responses are returned in the language specified by `Accept-Language: en` or `Accept-Language: hu`. Defaults to English if absent or unsupported.
