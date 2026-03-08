# Requirements Quality Checklist: Authentication System

**Purpose**: Comprehensive author self-check across all requirement domains — security, API contracts, localization, edge cases, and acceptance criteria quality.
**Created**: 2026-03-08
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md) | [contracts/auth-api.md](../contracts/auth-api.md) | [data-model.md](../data-model.md)

> 🔒 = Hard gate: item must be resolved before proceeding to `/speckit.tasks`.

---

## Security Requirements Quality

- [x] CHK001 🔒 Are the required cookie flags (`HttpOnly`, `SameSite=Strict`, `Secure`) explicitly specified for the `staccato_refresh` cookie, and is the production-vs-development `Secure` relaxation formally documented as an assumption rather than left implicit? [Clarity, Spec §Assumptions]
- [x] CHK002 🔒 Are the exact conditions that trigger the theft-detection path (re-presentation of an already-revoked token at `POST /auth/refresh`) stated unambiguously — including that the outcome is revocation of **all** active tokens for the user, not just rejection of the current request? [Clarity, Spec §FR-016, §Edge Cases]
- [x] CHK003 🔒 Is the requirement that the access token must **never** appear in a cookie or browser storage written as an explicit prohibition (FR-014), or only implied by describing where it does appear? [Completeness, Spec §FR-014]
- [x] CHK004 🔒 Are the JWT signing algorithm (HS256), minimum key length (32 chars), and key storage mechanism (`IOptions<JwtOptions>`, no hardcoding) specified as explicit requirements rather than only in research notes? [Completeness, research.md §Decision 2, Spec §G10]
- [x] CHK005 🔒 Are the conditions that produce `GOOGLE_AUTH_FAILED` (invalid/tampered/expired Google token) versus `SERVICE_UNAVAILABLE` (network or infrastructure failure) sufficiently distinct in requirements that an implementor cannot reasonably conflate the two? [Clarity, Spec §FR-017, contracts/auth-api.md]
- [x] CHK006 Is the concurrent-refresh race condition (two clients simultaneously presenting the same cookie) documented as a specified scenario with an unambiguous outcome (first rotation wins; second triggers theft path)? [Coverage, Spec §Edge Cases]
- [x] CHK007 Is the rate limiting requirement quantified (10 req/min per IP, 60-second window) and are all five `/auth/*` endpoints explicitly named as in-scope, leaving no ambiguity about whether `DELETE /auth/logout` is included? [Clarity, Spec §FR-011]
- [x] CHK008 Is the requirement to **retain** revoked and expired tokens in storage (rather than delete them) explicitly stated, along with the rationale (theft detection requires querying revoked tokens)? [Completeness, data-model.md §Session Token]

---

## API Contract Completeness

- [x] CHK009 Does the contracts file specify the exact HTTP method, route, request body schema, success HTTP status, and cookie `Set-Cookie` behavior for each of the five endpoints? [Completeness, contracts/auth-api.md]
- [x] CHK010 Is the `AuthResponse` body schema (field names `accessToken: string`, `expiresIn: int`) explicitly defined, and is it clear that `DELETE /auth/logout` returns no body (204)? [Clarity, contracts/auth-api.md]
- [x] CHK011 Are all seven business error codes mapped to their triggering endpoint, HTTP status, and condition in a single authoritative location? [Completeness, data-model.md §Business Error Codes]
- [x] CHK012 Is the cookie `Expires` / `Max-Age` behavior specified per authentication path: register → 7d, login without rememberMe → 7d, login with rememberMe → 30d, refresh → inherits original token expiry? [Clarity, contracts/auth-api.md]
- [x] CHK013 Does `POST /auth/refresh` with a completely absent cookie (no `Cookie` header at all) have a specified outcome — and is it clear whether this maps to `INVALID_TOKEN` (token not found) or a distinct error? [Coverage, contracts/auth-api.md, Gap]
- [x] CHK014 Is the `DELETE /auth/logout` idempotency requirement — absent or unknown token returns 204, no error — stated explicitly so that it cannot be interpreted as requiring a 401 for a missing cookie? [Clarity, Spec §FR-010, contracts/auth-api.md]
- [x] CHK015 Does the JWT claims specification document exact claim names (`sub`, `email`, `displayName`), their source fields, and how `displayName` is constructed from `FirstName` and `LastName`? [Completeness, contracts/auth-api.md]

---

## Localization Requirements Quality

- [x] CHK016 🔒 Are localization requirements explicitly stated to cover **both** FluentValidation 400 responses **and** business-rule 401/409/503 responses — with no category left implicitly excluded? [Completeness, Spec §FR-013, §Clarifications]
- [x] CHK017 🔒 Is the Accept-Language fallback behavior (absent header → English; unsupported language → English) unambiguously stated in requirements, not just in assumptions? [Clarity, Spec §FR-013]
- [x] CHK018 🔒 Are localized messages specified for all seven business error codes in both English and Hungarian, with no codes left to implementor discretion? [Completeness, data-model.md §Localization Resources]
- [x] CHK019 🔒 Are localized messages specified for every validator rule (email required, email format, displayName required, password required, password min length, idToken required) in both English and Hungarian? [Completeness, data-model.md §Localization Resources]
- [x] CHK020 Is the Accept-Language parsing behavior specified for region-qualified tags (e.g., `en-US`, `hu-HU`, `hu-HU,hu;q=0.9`) — or are only bare `en` and `hu` covered, leaving region-tagged headers' behavior undefined? [Clarity, Gap]

---

## Edge Case & Exception Flow Coverage

- [x] CHK021 Is the display-name splitting rule (split at first space; single-word → empty `LastName`; Google null-name → both fields empty) documented precisely enough to produce deterministic behavior without implementor interpretation? [Clarity, Spec §Assumptions, data-model.md §AuthService Logic]
- [x] CHK022 Does the Google sign-in linking requirement specify which existing user fields are updated (`GoogleId`, and `AvatarUrl` only if currently null) when an email match is found — or could implementors overwrite an existing `AvatarUrl`? [Completeness, data-model.md §GoogleLoginAsync]
- [x] CHK023 Does the spec acknowledge that returning `NO_PASSWORD_SET` (vs. `INVALID_CREDENTIALS`) for Google-only accounts constitutes a user enumeration vector — confirming account existence and auth method — and is this trade-off intentional and documented? [Clarity, Conflict, Spec §Assumptions] — **Decision: intentional trade-off, documented in spec Assumptions.**
- [x] CHK024 Is the behavior for `POST /auth/refresh` when the cookie is present but contains an empty string or whitespace specified? Or is empty string treated the same as token-not-found? [Coverage, Gap]
- [x] CHK025 Is it specified what the new refresh token's expiry should be after a rotation — does it inherit the original token's remaining lifetime, reset to 7d, or match some other rule? [Completeness, data-model.md §RefreshAsync, Gap] — **Decision: inherits original token's `ExpiresAt` (fixed expiry, no sliding window).**

---

## Acceptance Criteria Quality

- [x] CHK026 Can SC-005 ("every session token exchange produces a new, distinct token; the token used is immediately unusable") be objectively verified — or does "immediately unusable" require a time-bound or concurrency caveat to be testable? [Measurability, Spec §SC-005]
- [x] CHK027 Is SC-003 (all auth endpoints respond within 500 ms) realistic given BCrypt work factor 12, which typically adds 100–300 ms to registration and login — and should the success criterion be adjusted or scoped to exclude BCrypt time? [Measurability, Spec §SC-003, Gap] — **Decision: keep 500 ms; explicitly exclude BCrypt hashing time from measurement.**
- [x] CHK028 Do the acceptance scenarios for US2 (login) cover the case where `rememberMe` is absent from the request body — confirming it defaults to `false` rather than being treated as a validation error? [Coverage, Spec §US2]

---

## Dependencies & Assumptions

- [x] CHK029 Is the `Language.English` default assigned to newly-created users (both local and Google-registered) documented as an explicit assumption rather than a silent implementation decision? [Assumption, data-model.md §RegisterAsync]
- [x] CHK030 Are the required `appsettings.json` sections (`Jwt`, `Google`) and their mandatory fields documented as configuration requirements (not just in quickstart), so that a new environment setup is unambiguous? [Completeness, quickstart.md]
- [x] CHK031 Is the `RevokeAllForUserAsync` UoW-bypass behavior (immediate commit, callers MUST NOT follow with `CommitAsync`) documented in a location that implementors of `AuthService` will read — not only in repository interface comments? [Completeness, data-model.md §RefreshAsync, Spec §G3]

---

## Notes

- 🔒 Hard-gate items: **CHK001–CHK005, CHK016–CHK020** — all resolved ✅
- All 31 items resolved. Safe to proceed to `/speckit.tasks`.
