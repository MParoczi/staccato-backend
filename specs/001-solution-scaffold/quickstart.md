# Quickstart: Solution Scaffold

**Branch**: `001-solution-scaffold` | **Date**: 2026-03-01

---

## Prerequisites

- .NET 10 SDK installed (`dotnet --version` should report `10.x.x`)
- SQL Server instance or LocalDB available
- Azure Storage emulator (Azurite) or real Azure account for blob storage

---

## 1. Build the Solution

```bash
cd /path/to/Staccato/Backend
dotnet build Staccato.sln
```

Expected: all 9 projects compile with zero errors.

---

## 2. Configure `appsettings.json`

Open `Application/appsettings.json` and populate:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=Staccato;Trusted_Connection=True;"
  },
  "Jwt": {
    "Issuer": "https://staccato.local",
    "Audience": "staccato-client",
    "SecretKey": "REPLACE-WITH-A-32-CHAR-MIN-SECRET-KEY",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7,
    "RememberMeExpiryDays": 30
  },
  "AzureBlob": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "staccato-exports"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  },
  "RateLimit": {
    "AuthWindowSeconds": 60,
    "AuthMaxRequests": 10
  }
}
```

> **Never commit real secrets.** Use `appsettings.Development.json` or user secrets (`dotnet user-secrets`) for local overrides.

---

## 3. Run the Application

```bash
dotnet run --project Application/Application.csproj
```

The API starts on `https://localhost:7xxx` (check console output for the port).

---

## 4. Verify Middleware

**CORS preflight** (from a browser on `http://localhost:5173`):
```
OPTIONS /api/health → 200 with Access-Control-Allow-Credentials: true
```

**Rate limiting** (send 11 rapid requests to any `/auth/*` endpoint):
```
Requests 1–10 → 200 (or 404 if no route yet)
Request 11     → 429 Too Many Requests
```

**Business error envelope** (once domain features are added):
```json
POST /notebooks → 422
{ "code": "MODULE_OVERLAP", "message": "...", "details": {} }
```

**SignalR hub connection**:
```
GET /hubs/notifications/negotiate → 200 (with valid JWT)
GET /hubs/notifications/negotiate → 401 (without JWT)
```

---

## 5. Run Tests

```bash
# All tests
dotnet test Staccato.sln

# Unit tests only
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Unit"

# Integration tests only
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Integration"
```

---

## 6. Add a Migration (when entity models are ready)

```bash
dotnet ef migrations add InitialCreate \
  --project Persistence/Persistence.csproj \
  --startup-project Application/Application.csproj

dotnet ef database update \
  --project Persistence/Persistence.csproj \
  --startup-project Application/Application.csproj
```
