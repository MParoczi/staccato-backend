# Quickstart: PDF Export Pipeline

**Branch**: `011-pdf-export-pipeline` | **Date**: 2026-03-29

## Prerequisites

- .NET 10 SDK
- SQL Server (local or Docker)
- Azure Storage Emulator (Azurite) or Azure Blob Storage account
- Solution builds cleanly: `dotnet build Staccato.sln`
- All existing tests pass: `dotnet test Staccato.sln`

## Implementation Order

Follow this sequence to maintain a buildable solution at each step:

### Step 1: Domain Layer (no external dependencies)
1. Create `IPdfExportQueue` interface in `Domain/Interfaces/`
2. Create `IPdfExportService` interface in `Domain/Services/`
3. Implement `PdfExportService` in `Domain/Services/`
4. Update `IPdfExportRepository` — add `GetByStatusAsync` method
5. Add new domain exception constants for export error codes

### Step 2: Repository Layer
6. Update `PdfExportRepository` — implement `GetByStatusAsync`, modify `GetExpiredExportsAsync`

### Step 3: API Layer
7. Create request/response DTOs in `ApiModels/`
8. Create `CreatePdfExportRequestValidator` in `ApiModels/Validators/`
9. Create `ExportsController` in `Api/Controllers/`
10. Add export mappings to `DomainToResponseProfile`

### Step 4: Application Infrastructure
11. Create `PdfExportChannel` in `Application/Channels/`
12. Add `PdfFailed` method to `INotificationClient`
13. Register channel and service in `ServiceCollectionExtensions`

### Step 5: Background Services
14. Implement `PdfExportBackgroundService` (channel reader + stale recovery)
15. Implement `ExportCleanupService` (timer-based cleanup)

### Step 6: PDF Rendering
16. Create `PdfDataLoader` — data aggregation from repositories
17. Create `PdfRenderModels` — POCOs for rendering context
18. Create `DottedPaperBackground` — reusable QuestPDF component
19. Create `StaccatoPdfDocument` — IDocument implementation
20. Create page renderers (Cover, Index, LessonPage)
21. Create `ModuleRenderer` — module box with style
22. Create building block renderers (all 10 types)

### Step 7: Tests
23. Unit tests for `PdfExportService` (all methods + edge cases)
24. Integration tests for `ExportsController` (all endpoints)
25. Unit tests for `ExportCleanupService` logic

## Verification

```bash
# Build
dotnet build Staccato.sln

# Run all tests
dotnet test Staccato.sln

# Run only new tests
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~PdfExport"
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Export"

# Manual smoke test
dotnet run --project Application/Application.csproj
# POST /exports with a valid notebookId → 202
# GET /exports → list with Pending/Ready status
# GET /exports/{id}/download → PDF file
```

## Key Files to Reference

| Concern | Example File |
|---------|-------------|
| Service pattern | `Domain/Services/NotebookService.cs` |
| Controller pattern | `Api/Controllers/NotebooksController.cs` |
| Repository pattern | `Repository/Repositories/PdfExportRepository.cs` |
| Background service | `Application/BackgroundServices/AccountDeletionCleanupService.cs` |
| Entity→Domain mapping | `Repository/Mapping/EntityToDomainProfile.cs` |
| Domain→Response mapping | `Api/Mapping/DomainToResponseProfile.cs` |
| Validator pattern | `ApiModels/Validators/` (any existing validator) |
| SignalR hub | `Application/Hubs/NotificationHub.cs` |
| Azure Blob service | `Application/Services/AzureBlobService.cs` |
