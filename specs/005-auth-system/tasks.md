# Tasks: Authentication System

**Input**: Design documents from `/specs/005-auth-system/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | data-model.md ✅ | contracts/auth-api.md ✅ | research.md ✅

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no shared dependencies)
- **[Story]**: User story this task belongs to (US1–US5)
- Exact file paths are included in every task description

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Cross-cutting packages, resource scaffolding, startup wiring — must be complete before any user story work.

- [x] T001 Add NuGet packages: `Google.Apis.Auth` and `BCrypt.Net-Next` to `Application/Application.csproj`; `Microsoft.Extensions.Localization.Abstractions` to `ApiModels/ApiModels.csproj`

- [x] T002 [P] Create `Application/Resources/BusinessErrors.cs` — empty marker class `public sealed class BusinessErrors {}` in namespace `Application.Resources`, used as the type parameter for `IStringLocalizer<BusinessErrors>`

- [x] T003 [P] Create `ApiModels/Resources/ValidationMessages.cs` — empty marker class `public sealed class ValidationMessages {}` in namespace `ApiModels.Resources`, used as the type parameter for `IStringLocalizer<ValidationMessages>`

- [x] T004 [P] Create `Application/Resources/BusinessErrors.en.resx` — XML .resx with all 7 business error code keys (string data entries):
  - `EMAIL_ALREADY_REGISTERED` → `"An account with this email address already exists."`
  - `INVALID_CREDENTIALS` → `"Invalid email address or password."`
  - `NO_PASSWORD_SET` → `"This account uses Google Sign-In. Please log in with Google."`
  - `INVALID_TOKEN` → `"Your session is no longer valid. Please log in again."`
  - `TOKEN_EXPIRED` → `"Your session has expired. Please log in again."`
  - `GOOGLE_AUTH_FAILED` → `"Google Sign-In failed. Please try again."`
  - `SERVICE_UNAVAILABLE` → `"An external service is temporarily unavailable. Please try again later."`

- [x] T005 [P] Create `Application/Resources/BusinessErrors.hu.resx` — same 7 keys in Hungarian:
  - `EMAIL_ALREADY_REGISTERED` → `"Ezzel az e-mail címmel már létezik fiók."`
  - `INVALID_CREDENTIALS` → `"Érvénytelen e-mail cím vagy jelszó."`
  - `NO_PASSWORD_SET` → `"Ez a fiók Google bejelentkezést használ. Kérem, lépjen be a Google-lal."`
  - `INVALID_TOKEN` → `"A munkamenet már nem érvényes. Kérem, lépjen be újra."`
  - `TOKEN_EXPIRED` → `"A munkamenet lejárt. Kérem, lépjen be újra."`
  - `GOOGLE_AUTH_FAILED` → `"A Google bejelentkezés sikertelen volt. Kérem, próbálja újra."`
  - `SERVICE_UNAVAILABLE` → `"Egy külső szolgáltatás átmenetileg nem érhető el. Kérem, próbálja újra."`

- [x] T006 [P] Create `ApiModels/Resources/ValidationMessages.en.resx` — English validation messages:
  - `EmailRequired` → `"Email address is required."`
  - `EmailInvalid` → `"Please enter a valid email address."`
  - `EmailTooLong` → `"Email address must not exceed 256 characters."`
  - `DisplayNameRequired` → `"Display name is required."`
  - `DisplayNameTooLong` → `"Display name must not exceed 100 characters."`
  - `PasswordRequired` → `"Password is required."`
  - `PasswordTooShort` → `"Password must be at least 8 characters."`
  - `IdTokenRequired` → `"Google ID token is required."`

- [x] T007 [P] Create `ApiModels/Resources/ValidationMessages.hu.resx` — Hungarian validation messages:
  - `EmailRequired` → `"Az e-mail cím megadása kötelező."`
  - `EmailInvalid` → `"Kérem érvényes e-mail címet adjon meg."`
  - `EmailTooLong` → `"Az e-mail cím legfeljebb 256 karakter lehet."`
  - `DisplayNameRequired` → `"A megjelenítendő név megadása kötelező."`
  - `DisplayNameTooLong` → `"A megjelenítendő név legfeljebb 100 karakter lehet."`
  - `PasswordRequired` → `"A jelszó megadása kötelező."`
  - `PasswordTooShort` → `"A jelszónak legalább 8 karakter hosszúnak kell lennie."`
  - `IdTokenRequired` → `"A Google azonosító token megadása kötelező."`

- [x] T008 Create `Application/Options/GoogleOptions.cs` — sealed class with `[Required] public string ClientId { get; init; } = string.Empty;`, namespace `Application.Options`

- [x] T009 Modify `Application/Extensions/ServiceCollectionExtensions.cs` — in the existing extension method class, add:
  1. `AddLocalization(options => options.ResourcesPath = "Resources")` call
  2. `AddRequestLocalization()` configured with supported cultures `"en"` and `"hu"`, default culture `"en"`, with `AcceptLanguageHeaderRequestCultureProvider` as the only provider; match primary subtag only (strip region suffix)

- [x] T010 Modify `Application/Program.cs` — add two startup changes:
  1. Before `app.UseAuthentication()`: call `app.UseRequestLocalization()`
  2. In the services section: bind and validate `GoogleOptions` from `appsettings.json` section `"Google"` using `services.AddOptions<GoogleOptions>().BindConfiguration("Google").ValidateDataAnnotations().ValidateOnStart()`

**Checkpoint**: Packages installed, all resource files present, localization middleware wired. No compilation required yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared types and infrastructure that every user story depends on. Must be complete before Phase 3+.

- [x] T011 [P] Create `DomainModels/Models/AuthTokens.cs` — sealed record: `public sealed record AuthTokens(string AccessToken, int ExpiresIn, string RefreshToken, DateTime RefreshTokenExpiry);`, namespace `DomainModels.Models`

- [x] T012 [P] Create `DomainModels/Models/GoogleUserInfo.cs` — sealed record: `public sealed record GoogleUserInfo(string GoogleId, string Email, string? Name, string? PictureUrl);`, namespace `DomainModels.Models`

- [x] T013 [P] Create `Domain/Exceptions/UnauthorizedException.cs` — extends `BusinessException` with two constructors (constitution IX: no magic strings, all callers pass an explicit code):
  ```csharp
  public class UnauthorizedException : BusinessException
  {
      public UnauthorizedException(string code, string message, object? details = null)
          : base(code, message, details) { StatusCode = 401; }
      public UnauthorizedException(string message, object? details = null)
          : base("UNAUTHORIZED", message, details) { StatusCode = 401; }
      public UnauthorizedException() : this("UNAUTHORIZED", "Authentication failed.") { }
  }
  ```
  Also add a **code-accepting constructor overload** to the existing `Domain/Exceptions/ConflictException.cs`:
  ```csharp
  public ConflictException(string code, string message, object? details = null)
      : base(code, message, details) { StatusCode = 409; }
  ```
  This overload is required so `RegisterAsync` can emit `code = "EMAIL_ALREADY_REGISTERED"` instead of the hardcoded `"CONFLICT"`.
  Also create `Domain/Constants/AuthErrorCodes.cs` — static class with `const string` fields for all 7 auth error codes (constitution IX: no magic strings):
  ```csharp
  public static class AuthErrorCodes
  {
      public const string EmailAlreadyRegistered = "EMAIL_ALREADY_REGISTERED";
      public const string InvalidCredentials      = "INVALID_CREDENTIALS";
      public const string NoPasswordSet           = "NO_PASSWORD_SET";
      public const string InvalidToken            = "INVALID_TOKEN";
      public const string TokenExpired            = "TOKEN_EXPIRED";
      public const string GoogleAuthFailed        = "GOOGLE_AUTH_FAILED";
      public const string ServiceUnavailable      = "SERVICE_UNAVAILABLE";
  }
  ```
  All subsequent tasks that reference error code string literals MUST use these constants instead.

- [x] T014 [P] Create `Domain/Exceptions/ServiceUnavailableException.cs` — extends `BusinessException`: constructor sets `Code = "SERVICE_UNAVAILABLE"`, `StatusCode = 503`. Follow same pattern as T013

- [x] T015 [P] Create `Domain/Services/IPasswordHasher.cs`:
  ```csharp
  public interface IPasswordHasher
  {
      string Hash(string password);
      bool Verify(string password, string hash);
  }
  ```

- [x] T016 [P] Create `Domain/Services/IJwtService.cs`:
  ```csharp
  public interface IJwtService
  {
      string GenerateAccessToken(User user);
      string GenerateRefreshToken();
      int AccessTokenExpirySeconds { get; }
      int RefreshTokenExpiryDays   { get; }  // exposes JwtOptions value for AuthService (Domain cannot reference Application.Options)
      int RememberMeExpiryDays     { get; }  // exposes JwtOptions value for AuthService
  }
  ```

- [x] T017 [P] Create `Domain/Services/IAuthService.cs`:
  ```csharp
  public interface IAuthService
  {
      Task<AuthTokens> RegisterAsync(string email, string displayName, string password, CancellationToken ct = default);
      Task<AuthTokens> LoginAsync(string email, string password, bool rememberMe = false, CancellationToken ct = default);
      Task<AuthTokens> GoogleLoginAsync(string idToken, CancellationToken ct = default);
      Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct = default);
      Task LogoutAsync(string refreshToken, CancellationToken ct = default);
  }
  ```

- [x] T018 Modify `Application/Middleware/BusinessExceptionMiddleware.cs` — inject `IStringLocalizer<BusinessErrors>` alongside the existing `RequestDelegate next`. In the catch block, before writing the JSON payload, look up `localizer[ex.Code]`; if `ResourceNotFound` is false use the localized string, otherwise fall back to `ex.Message`. Import `Application.Resources` namespace

- [x] T019 Create `Application/Services/BcryptPasswordHasher.cs` — implements `IPasswordHasher`. `Hash`: `BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12)`. `Verify`: `BCrypt.Net.BCrypt.Verify(password, hash)`. Namespace `Application.Services`

- [x] T020 Create `Application/Services/JwtService.cs` — implements `IJwtService`, injects `IOptions<JwtOptions>`. Implement:
  - `GenerateAccessToken`: builds `SecurityTokenDescriptor` with claims `sub = user.Id.ToString()`, `email = user.Email`, `displayName = (user.FirstName + " " + user.LastName).Trim()`, issuer/audience from `JwtOptions`, expiry `UtcNow + AccessTokenExpiryMinutes`, signs with `HmacSha256` using `Encoding.UTF8.GetBytes(SecretKey)`
  - `GenerateRefreshToken`: `RandomNumberGenerator.GetBytes(64)` returned as `Convert.ToBase64String`
  - `AccessTokenExpirySeconds`: `_options.Value.AccessTokenExpiryMinutes * 60`
  - `RefreshTokenExpiryDays`: `_options.Value.RefreshTokenExpiryDays` ← exposes JwtOptions to Domain via interface
  - `RememberMeExpiryDays`: `_options.Value.RememberMeExpiryDays` ← exposes JwtOptions to Domain via interface

- [x] T021 Create `Domain/Services/AuthService.cs` — implements `IAuthService`. Constructor injects: `IUserRepository`, `IRefreshTokenRepository`, `IUnitOfWork`, `IPasswordHasher`, `IJwtService` (no `IOptions<JwtOptions>` — Domain cannot reference Application.Options; token expiry values are read via `_jwtService.RefreshTokenExpiryDays` and `_jwtService.RememberMeExpiryDays`). Stub all 5 interface methods with `throw new NotImplementedException()` — implementations follow per user story. Namespace `Domain.Services`

- [x] T022 Create `Api/Controllers/AuthController.cs` — `[ApiController]`, `[Route("auth")]`. Constructor injects `IAuthService`. Add two private helpers:
  - `SetRefreshCookie(string token, DateTime expiry)`: appends cookie `"staccato_refresh"` with `HttpOnly=true`, `SameSite=Strict`, `Secure = !env.IsDevelopment()`, `Expires = expiry`
  - `ClearRefreshCookie()`: calls `Response.Cookies.Delete("staccato_refresh")`
  No action methods yet — added per user story. `IWebHostEnvironment` must also be injected for the `Secure` flag

- [x] T023 Modify `Application/Extensions/ServiceCollectionExtensions.cs` — in `AddDomainServices()` (or equivalent registration method), register:
  - `IAuthService` → `AuthService` (scoped)
  - `IJwtService` → `JwtService` (singleton)
  - `IPasswordHasher` → `BcryptPasswordHasher` (singleton)
  (IGoogleTokenValidator registered later in T043)

**Checkpoint**: Solution compiles. All interfaces exist, AuthService stubs all methods, controller shell is present. No endpoints are functional yet.

---

## Phase 3: User Story 1 — Local Account Registration (Priority: P1) 🎯 MVP

**Goal**: `POST /auth/register` creates a new user, returns an access token, and sets the refresh cookie.

**Independent Test**: `POST /auth/register` with a new email returns 201 with `{ accessToken, expiresIn }` body and `staccato_refresh` cookie. Repeating with the same email returns 409 `EMAIL_ALREADY_REGISTERED`. Submitting missing fields returns 400 with field-level errors.

- [x] T024 [P] [US1] Create `ApiModels/Auth/RegisterRequest.cs` — record or class with `string Email`, `string DisplayName`, `string Password`. Namespace `ApiModels.Auth`

- [x] T025 [P] [US1] Create `ApiModels/Auth/AuthResponse.cs` — record with `string AccessToken`, `int ExpiresIn`. Used as the response body for all auth endpoints that issue tokens. Namespace `ApiModels.Auth`

- [x] T026 [P] [US1] Create `ApiModels/Auth/RegisterRequestValidator.cs` — `AbstractValidator<RegisterRequest>`, injects `IStringLocalizer<ValidationMessages>`. Rules:
  - `Email`: `NotEmpty` (key `EmailRequired`), `EmailAddress` (key `EmailInvalid`), `MaximumLength(256)` (key `EmailTooLong`)
  - `DisplayName`: `NotEmpty` (key `DisplayNameRequired`), `MaximumLength(100)` (key `DisplayNameTooLong`)
  - `Password`: `NotEmpty` (key `PasswordRequired`), `MinimumLength(8)` (key `PasswordTooShort`)

- [x] T027 [US1] Implement `AuthService.RegisterAsync` — replace `NotImplementedException`:
  1. `await _userRepository.GetByEmailAsync(email, ct)` → if not null → throw `new ConflictException(AuthErrorCodes.EmailAlreadyRegistered, "An account with this email address already exists.")`
  2. Split `displayName` at first space: `firstName = parts[0]`, `lastName = parts.Length > 1 ? string.Join(" ", parts[1..]) : ""`
  3. `passwordHash = _passwordHasher.Hash(password)`
  4. Create `User { Id = Guid.NewGuid(), Email = email, FirstName = firstName, LastName = lastName, PasswordHash = passwordHash, CreatedAt = DateTime.UtcNow, Language = Language.English }`
  5. `await _userRepository.AddAsync(user, ct)`
  6. `tokenValue = _jwtService.GenerateRefreshToken()`; create `RefreshToken { Id = Guid.NewGuid(), Token = tokenValue, UserId = user.Id, ExpiresAt = DateTime.UtcNow.AddDays(_jwtService.RefreshTokenExpiryDays), CreatedAt = DateTime.UtcNow, IsRevoked = false }`
  7. `await _refreshTokenRepository.AddAsync(refreshToken, ct)`
  8. `await _uow.CommitAsync(ct)`
  9. Return `new AuthTokens(_jwtService.GenerateAccessToken(user), _jwtService.AccessTokenExpirySeconds, tokenValue, refreshToken.ExpiresAt)`

- [x] T028 [US1] Add `POST /auth/register` action to `AuthController` — `[HttpPost("register")]`, accepts `RegisterRequest`, calls `_authService.RegisterAsync(...)`, calls `SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiry)`, returns `CreatedAtAction` (or `StatusCode(201)`) with `new AuthResponse(result.AccessToken, result.ExpiresIn)`

**Checkpoint**: `POST /auth/register` is fully functional. New user can register and receive tokens.

---

## Phase 4: User Story 2 — Local Email Login (Priority: P1)

**Goal**: `POST /auth/login` authenticates an existing user with email + password, supporting extended sessions via `rememberMe`.

**Independent Test**: Register a user, then `POST /auth/login` with correct credentials returns 200 with tokens and cookie. Wrong password returns 401 `INVALID_CREDENTIALS`. With `rememberMe: true` the cookie `Expires` is ~30 days out.

- [x] T029 [P] [US2] Create `ApiModels/Auth/LoginRequest.cs` — record with `string Email`, `string Password`, `bool RememberMe`. `RememberMe` defaults to `false`. Namespace `ApiModels.Auth`

- [x] T030 [P] [US2] Create `ApiModels/Auth/LoginRequestValidator.cs` — `AbstractValidator<LoginRequest>`, injects `IStringLocalizer<ValidationMessages>`. Rules:
  - `Email`: `NotEmpty` (key `EmailRequired`), `EmailAddress` (key `EmailInvalid`)
  - `Password`: `NotEmpty` (key `PasswordRequired`)
  - No rule on `RememberMe` — absent field treated as `false` (not a validation error)

- [x] T031 [US2] Implement `AuthService.LoginAsync` — replace `NotImplementedException`:
  1. `await _userRepository.GetByEmailAsync(email, ct)` → if null → throw `new UnauthorizedException(AuthErrorCodes.InvalidCredentials, "Invalid email address or password.")`
  2. If `user.PasswordHash == null` → throw `new UnauthorizedException(AuthErrorCodes.NoPasswordSet, "This account uses Google Sign-In.")`
  3. If `!_passwordHasher.Verify(password, user.PasswordHash)` → throw `new UnauthorizedException(AuthErrorCodes.InvalidCredentials, "Invalid email address or password.")`
  4. `tokenValue = _jwtService.GenerateRefreshToken()`; `expiryDays = rememberMe ? _jwtService.RememberMeExpiryDays : _jwtService.RefreshTokenExpiryDays`; create `RefreshToken` with `ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)`
  5. `await _refreshTokenRepository.AddAsync(refreshToken, ct)`
  6. `await _uow.CommitAsync(ct)`
  7. Return `new AuthTokens(_jwtService.GenerateAccessToken(user), _jwtService.AccessTokenExpirySeconds, tokenValue, refreshToken.ExpiresAt)`

- [x] T032 [US2] Add `POST /auth/login` action to `AuthController` — `[HttpPost("login")]`, accepts `LoginRequest`, calls `_authService.LoginAsync(request.Email, request.Password, request.RememberMe, ct)`, calls `SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiry)`, returns `Ok(new AuthResponse(result.AccessToken, result.ExpiresIn))`

**Checkpoint**: `POST /auth/login` is fully functional. Both standard and rememberMe login flows work.

---

## Phase 5: User Story 3 — Session Renewal (Priority: P2)

**Goal**: `POST /auth/refresh` silently exchanges the refresh cookie for new tokens, rotating the session token.

**Independent Test**: Register, extract cookie, call `POST /auth/refresh` → 200 with new access token and new cookie. Present the old cookie again → 401 `INVALID_TOKEN` (theft detection fires, all user tokens revoked).

- [x] T033 [US3] Implement `AuthService.RefreshAsync` — replace `NotImplementedException`:
  1. `await _refreshTokenRepository.GetByTokenAsync(tokenValue, ct)` → if null → throw `new UnauthorizedException(AuthErrorCodes.InvalidToken, "Your session is no longer valid.")`
  2. If `token.IsRevoked`: call `await _refreshTokenRepository.RevokeAllForUserAsync(token.UserId, ct)` (commits immediately — do NOT call `CommitAsync` after this), then throw `new UnauthorizedException(AuthErrorCodes.InvalidToken, "Your session is no longer valid.")`
  3. If `token.ExpiresAt <= DateTime.UtcNow` → throw `new UnauthorizedException(AuthErrorCodes.TokenExpired, "Your session has expired.")`
  4. `await _userRepository.GetByIdAsync(token.UserId, ct)` → if null → throw `NotFoundException()`
  5. Mark old token revoked: `_refreshTokenRepository.Update(token with { IsRevoked = true })`
  6. `newTokenValue = _jwtService.GenerateRefreshToken()`; create `newRefreshToken` with `ExpiresAt = token.ExpiresAt` (inherits original expiry)
  7. `await _refreshTokenRepository.AddAsync(newRefreshToken, ct)`
  8. `await _uow.CommitAsync(ct)`
  9. Return `new AuthTokens(_jwtService.GenerateAccessToken(user), _jwtService.AccessTokenExpirySeconds, newTokenValue, token.ExpiresAt)`

- [x] T034 [US3] Add `POST /auth/refresh` action to `AuthController` — `[HttpPost("refresh")]`. Read `Request.Cookies["staccato_refresh"]`; if null, empty, or whitespace → throw `new UnauthorizedException(AuthErrorCodes.InvalidToken, "Your session is no longer valid.")` (MUST throw — never `return Unauthorized(...)` directly, as that bypasses `BusinessExceptionMiddleware` and breaks the `{ code, message, details }` contract). Call `_authService.RefreshAsync(tokenValue, ct)`. Call `SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiry)`. Return `Ok(new AuthResponse(result.AccessToken, result.ExpiresIn))`

**Checkpoint**: Token rotation works. Theft detection revokes all sessions on stale-token replay.

---

## Phase 6: User Story 4 — Logout (Priority: P2)

**Goal**: `DELETE /auth/logout` revokes the refresh token server-side and clears the cookie. Fully idempotent.

**Independent Test**: Register, then `DELETE /auth/logout` returns 204 and cookie is cleared. Subsequent `POST /auth/refresh` with the old token returns 401. Calling logout with no cookie still returns 204.

- [x] T035 [US4] Implement `AuthService.LogoutAsync` — replace `NotImplementedException`:
  1. `await _refreshTokenRepository.GetByTokenAsync(tokenValue, ct)` → if null → return (idempotent, no exception)
  2. `_refreshTokenRepository.Update(token with { IsRevoked = true })`
  3. `await _uow.CommitAsync(ct)`

- [x] T036 [US4] Add `DELETE /auth/logout` action to `AuthController` — `[HttpDelete("logout")]`. Read `Request.Cookies["staccato_refresh"]`. If cookie is present and non-empty, call `await _authService.LogoutAsync(tokenValue, ct)`. Always call `ClearRefreshCookie()`. Always return `NoContent()` (204) — never return an error, including for missing or unknown tokens

**Checkpoint**: Logout is idempotent. Session is fully revoked server-side.

---

## Phase 7: User Story 5 — Google Sign-In (Priority: P3)

**Goal**: `POST /auth/google` validates a Google ID token server-side and authenticates or creates a user.

**Independent Test**: Submit a valid Google ID token → 200 with tokens and cookie. Submit the same token again → same user returned (no duplicate). Submit a token whose email matches an existing local account → existing account returned. Submit an invalid token → 401 `GOOGLE_AUTH_FAILED`. Simulate Google service down → 503 `SERVICE_UNAVAILABLE`.

- [x] T037 [P] [US5] Create `Domain/Services/IGoogleTokenValidator.cs`:
  ```csharp
  public interface IGoogleTokenValidator
  {
      Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken ct = default);
  }
  ```

- [x] T038 [P] [US5] Create `ApiModels/Auth/GoogleAuthRequest.cs` — record with `string IdToken`. Namespace `ApiModels.Auth`

- [x] T039 [P] [US5] Create `ApiModels/Auth/GoogleAuthRequestValidator.cs` — `AbstractValidator<GoogleAuthRequest>`, injects `IStringLocalizer<ValidationMessages>`. Rules: `IdToken`: `NotEmpty` (key `IdTokenRequired`)

- [x] T040 [US5] Create `Application/Services/GoogleTokenValidator.cs` — implements `IGoogleTokenValidator`, injects `IOptions<GoogleOptions>`. In `ValidateAsync`:
  - Build `GoogleJsonWebSignature.ValidationSettings { Audience = new[] { _googleOptions.ClientId } }`
  - Call `await GoogleJsonWebSignature.ValidateAsync(idToken, settings)`
  - Map payload to `new GoogleUserInfo(payload.Subject, payload.Email, payload.Name, payload.Picture)`
  - Catch `InvalidJwtException` → throw `new UnauthorizedException(AuthErrorCodes.GoogleAuthFailed, "Google Sign-In failed.")`
  - Catch `HttpRequestException` or any other unexpected exception → throw `new ServiceUnavailableException()`
  Namespace `Application.Services`

- [x] T041 [US5] Implement `AuthService.GoogleLoginAsync` — replace `NotImplementedException`. Constructor must also inject `IGoogleTokenValidator`:
  1. `googleUserInfo = await _googleTokenValidator.ValidateAsync(idToken, ct)` (let exceptions propagate)
  2. `user = await _userRepository.GetByGoogleIdAsync(googleUserInfo.GoogleId, ct)` → if found → skip to step 5
  3. `user = await _userRepository.GetByEmailAsync(googleUserInfo.Email, ct)` → if found → link: `user.GoogleId = googleUserInfo.GoogleId`; if `user.AvatarUrl == null` → `user.AvatarUrl = googleUserInfo.PictureUrl`; `_userRepository.Update(user)`; skip to step 5
  4. Create `new User { Id = Guid.NewGuid(), Email = googleUserInfo.Email, FirstName, LastName (split googleUserInfo.Name at first space; both empty if Name is null), GoogleId = googleUserInfo.GoogleId, AvatarUrl = googleUserInfo.PictureUrl, CreatedAt = DateTime.UtcNow, Language = Language.English }`; `await _userRepository.AddAsync(user, ct)`
  5. `tokenValue = _jwtService.GenerateRefreshToken()`; create `RefreshToken` with `ExpiresAt = DateTime.UtcNow.AddDays(_jwtService.RefreshTokenExpiryDays)`
  6. `await _refreshTokenRepository.AddAsync(refreshToken, ct)`
  7. `await _uow.CommitAsync(ct)`
  8. Return `AuthTokens`

- [x] T042 [US5] Add `POST /auth/google` action to `AuthController` — `[HttpPost("google")]`, accepts `GoogleAuthRequest`, calls `_authService.GoogleLoginAsync(request.IdToken, ct)`, calls `SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiry)`, returns `Ok(new AuthResponse(result.AccessToken, result.ExpiresIn))`

- [x] T043 [US5] Modify `Application/Extensions/ServiceCollectionExtensions.cs` — add `IGoogleTokenValidator` → `GoogleTokenValidator` (singleton) to the `AddDomainServices()` registrations added in T023

**Checkpoint**: All 5 auth endpoints are functional. Google sign-in creates, links, or returns existing accounts correctly.

---

## Phase 8: Polish & Tests

**Purpose**: Full test coverage, compilation validation, cross-cutting verification.

- [x] T044 [P] Write `Tests/Unit/AuthServiceTests.cs` — xUnit test class, Moq for all `AuthService` dependencies. Cover all 18 cases from plan.md:
  - `RegisterAsync_NewUser_ReturnsAuthTokens` (happy path)
  - `RegisterAsync_DuplicateEmail_ThrowsConflict`
  - `LoginAsync_ValidCredentials_ReturnsAuthTokens`
  - `LoginAsync_WrongPassword_ThrowsUnauthorized` (code = `INVALID_CREDENTIALS`)
  - `LoginAsync_UnknownEmail_ThrowsUnauthorized` (same code — no enumeration)
  - `LoginAsync_NoPasswordSet_ThrowsUnauthorized` (code = `NO_PASSWORD_SET`)
  - `LoginAsync_RememberMe_SetsLongerExpiry` (30d vs 7d on `RefreshToken.ExpiresAt`)
  - `GoogleLoginAsync_NewUser_CreatesAccountAndReturnsTokens`
  - `GoogleLoginAsync_ExistingEmail_LinksGoogleToAccount` (no duplicate created)
  - `GoogleLoginAsync_ExistingGoogleId_LogsIn`
  - `GoogleLoginAsync_InvalidToken_ThrowsUnauthorized` (code = `GOOGLE_AUTH_FAILED`)
  - `GoogleLoginAsync_ServiceDown_ThrowsServiceUnavailable`
  - `RefreshAsync_ValidToken_RotatesAndReturnsNew` (old token revoked, new token issued with same `ExpiresAt`)
  - `RefreshAsync_RevokedToken_RevokesAllAndThrows` (`RevokeAllForUserAsync` called, `INVALID_TOKEN` thrown)
  - `RefreshAsync_ExpiredToken_ThrowsUnauthorized` (code = `TOKEN_EXPIRED`)
  - `RefreshAsync_UnknownToken_ThrowsUnauthorized` (code = `INVALID_TOKEN`)
  - `LogoutAsync_ValidToken_RevokesToken`
  - `LogoutAsync_UnknownToken_IsIdempotent` (no exception thrown)

- [x] T045 [P] Write `Tests/Integration/Controllers/AuthControllerTests.cs` — xUnit test class, `WebApplicationFactory<Program>` with InMemory EF. Cover all 13 cases from plan.md:
  - `Register_ValidRequest_Returns201WithCookie` (body has `accessToken`, `expiresIn`; cookie `staccato_refresh` is set)
  - `Register_DuplicateEmail_Returns409` (code = `EMAIL_ALREADY_REGISTERED`)
  - `Register_InvalidInput_Returns400` (FluentValidation format — `errors` object)
  - `Login_ValidCredentials_Returns200WithCookie`
  - `Login_WrongPassword_Returns401` (code = `INVALID_CREDENTIALS`)
  - `Login_RememberMe_SetsLongerCookie` (cookie `Expires` ~30 days from now)
  - `Refresh_ValidCookie_Returns200WithNewCookie` (new cookie set, old cookie value different)
  - `Refresh_MissingCookie_Returns401` (code = `INVALID_TOKEN`)
  - `Refresh_RevokedToken_Returns401` (code = `INVALID_TOKEN`; all user refresh tokens revoked)
  - `Logout_ValidCookie_Returns204ClearedCookie` (204, cookie cleared)
  - `Logout_MissingCookie_Returns204` (204, idempotent)
  - `RateLimit_ExceedsLimit_Returns429` (send 11 requests to `/auth/login`; 11th returns 429)
  - `Localization_HungarianHeader_ReturnsHuMessage` (`Accept-Language: hu` on duplicate-email register → message is Hungarian)

- [x] T046 Build and verify — run `dotnet build Staccato.sln` and confirm zero errors and zero warnings related to new code. Fix any namespace, reference, or using-directive issues

- [x] T047 Run full test suite — `dotnet test Staccato.sln` and confirm all existing tests still pass alongside the new unit and integration tests

---

## Dependencies

```
Phase 1 (T001–T010)        → must complete before Phase 2
Phase 2 (T011–T023)        → must complete before Phase 3+
Phase 3 (T024–T028) [US1]  → can start once Phase 2 is done
Phase 4 (T029–T032) [US2]  → can start once Phase 2 is done; independent of Phase 3
Phase 5 (T033–T034) [US3]  → requires IRefreshTokenRepository.RevokeAllForUserAsync (existing)
Phase 6 (T035–T036) [US4]  → independent of Phase 5; only needs Phase 2
Phase 7 (T037–T043) [US5]  → requires IGoogleTokenValidator (T037); otherwise independent
Phase 8 (T044–T047)        → requires all prior phases complete
```

**Parallel opportunities within phases:**
- Phase 1: T002–T008 all parallelizable (different files)
- Phase 2: T011–T017 all parallelizable; T018–T022 sequential (each builds on prior)
- Phase 3: T024–T026 parallelizable; T027 depends on T024+T026; T028 depends on T027
- Phase 4: T029–T030 parallelizable; T031 depends on T029; T032 depends on T031
- Phase 7: T037–T039 parallelizable; T040–T042 sequential
- Phase 8: T044 and T045 parallelizable

## Implementation Strategy

**MVP (minimum shippable increment)**: Phases 1–4 only (US1 + US2). Registration and login with token issuance. Rate limiting already wired. No Google Sign-In, no refresh, no logout yet — but the feature branch is functional.

**Increment 2**: Add Phases 5–6 (US3 + US4). Session renewal and logout. Full local auth lifecycle complete.

**Increment 3**: Add Phase 7 (US5). Google Sign-In. Feature complete.

**Increment 4**: Phase 8. Tests and build verification. Feature ready for PR.
