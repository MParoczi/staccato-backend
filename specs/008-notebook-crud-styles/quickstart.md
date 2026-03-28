# Quickstart: Notebook CRUD and Style Management

**Branch**: `008-notebook-crud-styles` | **Date**: 2026-03-28

---

## Prerequisites

1. .NET 10 SDK installed.
2. SQL Server running and `DefaultConnection` in `appsettings.json` configured.
3. Run `dotnet restore Staccato.sln` from repo root.

---

## Apply the Migration

A new migration `AddNotebookCoverColor` adds `CoverColor` to the `Notebooks` table:

```bash
dotnet ef database update \
  --project Persistence/Persistence.csproj \
  --startup-project Application/Application.csproj
```

> **Note**: If running against an existing database that was seeded before this feature, the `SystemStylePresets` table may have `Classic` as `IsDefault = true`. The seeder is idempotent and will not re-run. Manually correct the data if needed:
> ```sql
> UPDATE SystemStylePresets SET IsDefault = 0 WHERE Name = 'Classic';
> UPDATE SystemStylePresets SET IsDefault = 1 WHERE Name = 'Colorful';
> ```

---

## Run the API

```bash
dotnet run --project Application/Application.csproj
```

---

## Quick Manual Test

### 1. Register and get a token:
```bash
curl -X POST http://localhost:5000/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","displayName":"Test","password":"Password1!"}'
```

Copy `accessToken` from the response.

### 2. Get available instruments:
```bash
curl http://localhost:5000/instruments
```
Copy an instrument `id`.

### 3. Create a notebook:
```bash
curl -X POST http://localhost:5000/notebooks \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"title":"My First Notebook","instrumentId":"<instrument-id>","pageSize":"A5","coverColor":"#4A235A","styles":null}'
```

Expect **201** with full `NotebookDetailResponse` including 12 styles from the Colorful preset.

### 4. View system presets (no auth):
```bash
curl http://localhost:5000/presets
```

Expect **200** with 5 presets. Colorful should have `"isDefault": true`.

### 5. Apply a preset:
```bash
curl -X POST "http://localhost:5000/notebooks/<notebook-id>/styles/apply-preset/<classic-preset-id>" \
  -H "Authorization: Bearer <token>"
```

Expect **200** with 12 styles updated to the Classic palette.

---

## Run Tests

```bash
# All tests
dotnet test Tests/Tests.csproj

# Unit tests only
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Unit"

# Integration tests only
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Integration"
```

---

## Key Files for This Feature

| File | Role |
|---|---|
| `EntityModels/Entities/NotebookEntity.cs` | Added `CoverColor` |
| `DomainModels/Models/Notebook.cs` | Added `CoverColor` |
| `DomainModels/Models/NotebookSummary.cs` | New — list-view projection |
| `Persistence/Configurations/NotebookConfiguration.cs` | Updated for `CoverColor` |
| `Persistence/Seed/SystemStylePresetSeeder.cs` | Fixed `IsDefault` (Colorful = true) |
| `Domain/Interfaces/Repositories/ISystemStylePresetRepository.cs` | New |
| `Repository/Repositories/SystemStylePresetRepository.cs` | New |
| `Domain/Interfaces/Repositories/INotebookRepository.cs` | Updated `GetByUserIdAsync` signature |
| `Repository/Repositories/NotebookRepository.cs` | Updated with ordering + `NotebookSummary` projection |
| `Domain/Services/INotebookService.cs` | New |
| `Domain/Services/NotebookService.cs` | New |
| `ApiModels/Notebooks/` | New folder — all request/response/validator files |
| `Api/Controllers/NotebooksController.cs` | New |
| `Api/Controllers/PresetsController.cs` | New |
| `Api/Mapping/DomainToResponseProfile.cs` | Updated — new mappings |
| `Application/Extensions/ServiceCollectionExtensions.cs` | Updated — new registrations |
| `Tests/Unit/Services/NotebookServiceTests.cs` | New |
| `Tests/Integration/Controllers/NotebooksControllerTests.cs` | New |
| `Tests/Integration/Controllers/PresetsControllerTests.cs` | New |
