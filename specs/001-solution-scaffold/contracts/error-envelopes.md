# Contract: Error Response Envelopes

**Branch**: `001-solution-scaffold` | **Date**: 2026-03-01

---

## Business Rule Violation (4xx from BusinessExceptionMiddleware)

**Status codes**: 400, 403, 409, 422 (determined by the thrown `BusinessException` subclass)

**Body**:
```json
{
  "code": "SCREAMING_SNAKE_CASE_ERROR_CODE",
  "message": "Human-readable, localised message.",
  "details": { }
}
```

- `code` — always English, always SCREAMING_SNAKE_CASE. Defined as a constant in the Domain project.
- `message` — localised per `Accept-Language` header (`en` or `hu`). Provided by `IStringLocalizer` in the service layer.
- `details` — optional structured object with additional context (e.g., conflicting module IDs). Omitted or `null` when not applicable.

---

## Infrastructure / Unexpected Error (500 from Problem Details handler)

**Status code**: 500

**Body** (RFC 7807):
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "An unexpected error occurred.",
  "status": 500,
  "detail": "Internal server error detail (suppressed in production).",
  "traceId": "00-abc123..."
}
```

---

## Validation Error (400 from FluentValidation auto-pipeline)

**Status code**: 400

**Body** (ASP.NET Core model state format):
```json
{
  "errors": {
    "fieldName": ["Validation message 1.", "Validation message 2."],
    "anotherField": ["Message."]
  }
}
```

---

## Rate Limit Exceeded (429 from Rate Limiter)

**Status code**: 429

**Body**: Empty or minimal (default ASP.NET Core rate limiter response).

**Header**: `Retry-After` (seconds until the window resets).
