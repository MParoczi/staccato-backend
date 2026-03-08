# Feature Specification: Authentication System

**Feature Branch**: `005-auth-system`
**Created**: 2026-03-08
**Status**: Draft
**Input**: User description: "Implement the full authentication system for the Staccato API including local registration/login and Google OAuth, using JWT access tokens and refresh tokens."

## Clarifications

### Session 2026-03-08

- Q: Is a "forgot password" / password reset flow in scope for this feature? → A: Out of scope — password reset is a separate future feature; no email delivery or reset-token entity required here.
- Q: When a revoked refresh token is presented again (potential theft), should the system silently reject, revoke all user tokens, or log only? → A: Revoke all active session tokens for the user — treat re-use of a revoked token as a theft signal and force re-login on all devices.
- Q: When the Google Sign-In validation service is temporarily unavailable, what should the system return? → A: 503 Service Unavailable — signals a temporary dependency failure so the client knows to retry rather than treating the credential as invalid.
- Q: Should there be an upper limit on concurrent active sessions per user? → A: No limit — all concurrent sessions allowed; rely on theft detection and natural token expiry to bound session accumulation.
- Q: Should localization apply only to field-level validation errors, or also to business-rule error messages (e.g., "email already in use")? → A: Both — all client-facing error text (field-level validation and business-rule messages) is localized using the Accept-Language header.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Local Account Registration (Priority: P1)

A new visitor creates an account using their email address, chosen display name, and a password. Upon successful registration they are immediately authenticated and can start using the application — no separate login step required.

**Why this priority**: Registration is the entry point for all users. Without it, no other feature is accessible. It must work correctly and securely before any other auth flow is built.

**Independent Test**: Register a new account via the API and confirm the response contains an access credential and a secure session cookie, then call a protected endpoint with that credential to verify it grants access.

**Acceptance Scenarios**:

1. **Given** a visitor with an email address not yet registered, **When** they submit their email, display name, and a valid password, **Then** they receive an access credential, a session cookie is set, and their account is persisted.
2. **Given** an email address already registered in the system, **When** a registration attempt is made with that email, **Then** the request is rejected with a clear error indicating the email is already in use.
3. **Given** invalid input (missing required fields, malformed email, password below minimum length), **When** registration is submitted, **Then** the request is rejected with field-level validation errors in the user's preferred language.

---

### User Story 2 - Local Email Login (Priority: P1)

A registered user logs in with their email and password. They can opt into an extended session ("remember me") so they remain authenticated for a longer period without re-entering credentials.

**Why this priority**: Login is the entry point for every returning user. Alongside registration it forms the mandatory foundation of all access control.

**Independent Test**: Create a user account, log in with correct credentials, and verify an access credential and session cookie are returned. Attempt login with the wrong password and verify rejection.

**Acceptance Scenarios**:

1. **Given** a registered user, **When** they submit correct credentials without "remember me", **Then** they receive an access credential and a standard-duration session cookie.
2. **Given** a registered user, **When** they submit correct credentials with "remember me" enabled, **Then** they receive an access credential and an extended-duration session cookie.
3. **Given** a registered user, **When** they submit an incorrect password, **Then** the request is rejected with an authentication error.
4. **Given** an unregistered email address, **When** a login is attempted, **Then** the request is rejected with an authentication error indistinguishable from the wrong-password case (prevents email enumeration).
5. **Given** a registered user, **When** they submit credentials without the `rememberMe` field in the request body, **Then** the system treats `rememberMe` as `false` and issues a standard-duration (7-day) session cookie — the absent field is never a validation error.

---

### User Story 3 - Session Renewal (Priority: P2)

A user whose short-lived access credential has expired can silently obtain a new one using their session cookie, without re-entering credentials. Each renewal rotates the session token so that no single token can be replayed.

**Why this priority**: Without silent token renewal, users are forced to re-authenticate every 15 minutes. This creates the seamless session experience the frontend requires.

**Independent Test**: Authenticate to get a session cookie, call the renewal endpoint with that cookie, verify a new access credential is returned and a new session cookie is issued, then verify the old session cookie no longer works.

**Acceptance Scenarios**:

1. **Given** a user with a valid, unexpired session cookie, **When** they request a new access credential, **Then** a new access credential is returned, a new session cookie is set, and the old cookie is invalidated.
2. **Given** a user with a session cookie that has been revoked (e.g., by a prior logout), **When** they request a new access credential, **Then** the request is rejected and the cookie is cleared.
3. **Given** a user with an expired session cookie, **When** they request a new access credential, **Then** the request is rejected.

---

### User Story 4 - Logout (Priority: P2)

A user explicitly ends their session. Their session token is immediately invalidated server-side and their cookie is cleared. Subsequent renewal attempts with the same cookie are rejected.

**Why this priority**: Secure logout is a baseline security requirement — critical for shared-device safety and privacy.

**Independent Test**: Authenticate, call the logout endpoint, then attempt session renewal with the same cookie and verify it is rejected.

**Acceptance Scenarios**:

1. **Given** an authenticated user with a valid session cookie, **When** they log out, **Then** their session token is revoked server-side, their cookie is cleared, and any subsequent renewal attempt with the old cookie is rejected.

---

### User Story 5 - Google Sign-In (Priority: P3)

A user can sign in or create an account using their Google identity. The application validates the Google credential server-side before trusting any claims. If the email matches an existing account the user is logged into that account; otherwise a new account is created from their Google profile.

**Why this priority**: Google Sign-In lowers the barrier to entry but is additive — the application is fully functional without it.

**Independent Test**: Submit a valid Google credential; verify an access credential and session cookie are returned. Submit the same credential again and verify the same account is returned (no duplicate). Submit a credential whose email matches an existing local account and verify the same account is returned.

**Acceptance Scenarios**:

1. **Given** a Google credential for an email not yet registered, **When** it is submitted, **Then** a new account is created using the Google profile name and picture, and the user is authenticated.
2. **Given** a Google credential for an email already registered locally, **When** it is submitted, **Then** the existing account is used and the user is authenticated (no new account created).
3. **Given** an invalid or tampered Google credential, **When** it is submitted, **Then** the request is rejected.

---

### Edge Cases

- What happens when a Google sign-in email matches an account already registered locally — does it link or reject? (Assumption: link by email; the Google identity is associated with the existing account.)
- What if the `Accept-Language` header is absent or specifies an unsupported language? (Assumption: fall back to English.)
- What if a previously rotated (revoked) session token is presented again? The system treats this as a theft signal: all active session tokens for that user are immediately revoked, forcing re-authentication on all devices. The response is the same 401 as a normal expiry — the full revocation happens silently server-side.
- What if the renewal endpoint is called simultaneously from two clients holding the same session cookie? The first rotation wins; the second client presents the now-revoked token, triggering the theft-detection path — all user tokens are revoked.
- What happens when a rate-limited IP continues sending auth requests? (Further requests in the current window are rejected with a rate-limit error until the window resets.)
- What if "remember me" is absent from a login request? (Assumption: defaults to false — standard session duration applies.)
- What if a Google-only account (no password) attempts local login? (Rejected with an appropriate error indicating no password is set for the account.)
- What if the Google Sign-In validation service is temporarily unavailable? The system returns 503 Service Unavailable with a user-facing error. The response must not imply the credential is invalid — the client should retry.
- What if `POST /auth/refresh` is called with no `staccato_refresh` cookie, or with an empty/whitespace cookie value? Both cases are treated as token-not-found and return 401 `INVALID_TOKEN`. No cookie is written in the response.
- What if `DELETE /auth/logout` is called with no cookie, an empty value, or an unknown token? The operation is idempotent — returns 204 regardless of whether a matching token exists.

## Requirements *(mandatory)*

### Explicitly Out of Scope

- **Password reset / forgot password**: No email delivery, reset-token entity, or reset endpoints are included. This is a separate future feature.
- **Multi-factor authentication (MFA)**: Not included in this feature.
- **Email verification on registration**: Explicitly excluded per project convention.
- **Explicit account-linking endpoint**: Linking a Google identity to an already-authenticated local account via a dedicated endpoint is not included; linking occurs implicitly during Google sign-in when the email matches.

### Functional Requirements

- **FR-001**: System MUST allow new users to register with a unique email address, a display name, and a password.
- **FR-002**: System MUST reject registration if the provided email address is already associated with an existing account.
- **FR-003**: System MUST allow registered users to authenticate using their email address and password.
- **FR-004**: System MUST allow users to request an extended session duration ("remember me") at login time.
- **FR-005**: System MUST issue a short-lived access credential and a longer-lived session credential upon successful authentication (registration, login, or Google sign-in).
- **FR-006**: System MUST allow users to authenticate via Google Sign-In, validating the Google credential on the server before accepting any identity claims.
- **FR-007**: System MUST create a new account for a first-time Google sign-in user, using their Google profile name and picture URL.
- **FR-008**: System MUST associate a Google sign-in with an existing account when the Google email matches a registered email, rather than creating a duplicate account.
- **FR-009**: System MUST allow users to exchange their session credential for a new access credential, atomically revoking the old session credential and issuing a new one.
- **FR-010**: System MUST allow users to log out, immediately revoking their session credential server-side and clearing the session cookie.
- **FR-011**: System MUST enforce a rate limit of 10 requests per minute per originating IP address on all five authentication endpoints: `POST /auth/register`, `POST /auth/login`, `POST /auth/google`, `POST /auth/refresh`, and `DELETE /auth/logout`.
- **FR-012**: System MUST validate all authentication request inputs and return field-level error messages for invalid input.
- **FR-013**: System MUST return all client-facing error text — both field-level validation errors and business-rule error messages (e.g., "email already in use", "invalid credentials") — in the language specified by the `Accept-Language` request header, supporting English (`en`) and Hungarian (`hu`), defaulting to English when the header is absent or specifies an unsupported language.
- **FR-014**: System MUST transmit the session credential exclusively via a browser cookie with the following flags set: `HttpOnly` (prevents JavaScript access), `SameSite=Strict` (blocks cross-site requests), and `Secure` (HTTPS-only transmission in production; may be relaxed to allow HTTP in development environments only). The access credential MUST be returned in the response body only and must never be persisted in a cookie, `localStorage`, or `sessionStorage`.
- **FR-015**: System MUST reject session renewal requests where the session credential is expired or revoked, and clear the associated cookie on rejection.
- **FR-016**: System MUST treat the presentation of an already-revoked session credential as a theft signal: all active session tokens for the associated user are immediately revoked, and the requesting client receives a standard 401 response.
- **FR-017**: System MUST return 503 Service Unavailable when the Google Sign-In validation service is unreachable or returns an unexpected error, clearly distinguishing this failure from an invalid credential (401).

### Key Entities

- **User**: Represents an application account. Attributes: unique email address, display name (first name, last name), optional password credential (absent for Google-only accounts), optional Google identity reference, optional profile picture URL, registration timestamp, optional scheduled-deletion date.
- **Session Token**: Represents an active authenticated session. Linked to a user. Attributes: unique token value, expiry timestamp, revocation flag, creation timestamp. A single user may hold any number of concurrent session tokens (multiple devices or browsers); no upper limit is enforced. Expired and revoked tokens are retained in storage (not deleted) to enable theft detection.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new user can register and obtain a usable access credential in a single request — no secondary confirmation step required.
- **SC-002**: A returning user can log in and obtain a usable access credential in a single request.
- **SC-003**: All authentication endpoints respond within 500 ms under normal single-user load, excluding intentional BCrypt hashing time (work factor 12, ~100–300 ms per operation). The 500 ms target applies to all non-hashing path overhead — middleware, database queries, token generation, and serialization. BCrypt cost is security-mandated and measured separately.
- **SC-004**: Any IP address submitting more than 10 authentication requests within a 60-second window receives a rate-limit rejection for all subsequent requests in that window.
- **SC-005**: Every session token exchange produces a new, distinct session token; the token used in the exchange is immediately unusable for a subsequent exchange. Verifiable by: performing a renewal, then presenting the original token at `POST /auth/refresh` again and confirming a 401 response.
- **SC-006**: After a user logs out, the revoked session token cannot be used to obtain a new access credential.
- **SC-007**: All client-facing error text — field-level validation errors and business-rule messages — is returned in the language matching the `Accept-Language` header (English or Hungarian) for every client-error response from auth endpoints.
- **SC-008**: A Google sign-in with an email already present in the system never creates a duplicate account.

## Assumptions

- **Display name storage**: The registration request accepts a single "display name" string. It is stored internally as `FirstName` and `LastName` by splitting at the first space (e.g. "Jane Doe" → firstName = "Jane", lastName = "Doe"). A single-word name is stored as `FirstName` with an empty `LastName`. The access credential payload exposes the combined value as `displayName`.
- **Password strength**: A minimum password length of 8 characters is enforced. No complexity rules beyond length are required.
- **Password-only accounts**: Users registered locally have a password but no Google identity reference. They can log in only via the local login endpoint.
- **Google-only accounts**: Users created via Google Sign-In have no password. Attempting local login for such an account returns `NO_PASSWORD_SET` (401) rather than the generic `INVALID_CREDENTIALS`. This constitutes a deliberate enumeration trade-off: the caller learns that the account exists and uses Google Sign-In only, but gains no credential access. The UX benefit — guiding the user to the correct sign-in method — outweighs the minor disclosure risk in this context.
- **Google email collision with a different Google ID**: If a Google sign-in presents an email already linked to a different Google ID (a rare edge case), the email is treated as the canonical identity anchor — the new Google ID is associated with the existing account.
- **rememberMe default**: Absent from the login request → defaults to `false` (standard 7-day session duration). Extended ("remember me") session duration is 30 days.
- **Access credential lifetime**: 15 minutes.
- **No email verification**: Account registration does not require email address confirmation — consistent with existing project decisions.
- **Cookie security**: The session cookie is `HttpOnly`, `SameSite=Strict`, and `Secure=true` in production (see FR-014). Development environments may omit `Secure` to allow non-HTTPS testing.
- **Rate limiting applies to all five auth endpoints**: `POST /auth/register`, `POST /auth/login`, `POST /auth/google`, `POST /auth/refresh`, and `DELETE /auth/logout`.
- **JWT signing**: The access credential is signed with the HS256 algorithm using a symmetric key of minimum 32 characters. The signing key is never hardcoded — it is loaded from configuration via `IOptions<T>` at startup.
- **Accept-Language region tags**: Region-qualified header values (e.g., `en-US`, `hu-HU`) are resolved by matching the primary language subtag. `en-US` resolves to English; `hu-HU` resolves to Hungarian. Unsupported primary subtags fall back to English.
- **Google sign-in profile picture**: When linking a Google identity to an existing account, the user's `AvatarUrl` is updated to the Google profile picture **only if the existing `AvatarUrl` is currently null**. A user who already has a profile picture retains it.
- **Default language for new accounts**: All newly created user accounts (both local registration and Google sign-in) are assigned English as the default display language. This applies to both local and Google-registered users.
- **Required configuration**: A working deployment requires `Jwt` (Issuer, Audience, SecretKey ≥32 chars, AccessTokenExpiryMinutes, RefreshTokenExpiryDays, RememberMeExpiryDays) and `Google` (ClientId) sections in `appsettings.json`. Missing or invalid values cause startup failure.
