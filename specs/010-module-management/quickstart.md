# Quickstart: Module Management

**Feature**: 010-module-management
**Date**: 2026-03-29

## Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB or full instance)
- Solution builds: `dotnet build Staccato.sln`
- Tests pass: `dotnet test Staccato.sln`

## Implementation Order

Execute in this order to maintain a green build at each step:

### Step 1: Schema Changes (DomainModels + EntityModels + Persistence)

1. Add `public int ZIndex { get; set; }` to `DomainModels/Models/Module.cs`
2. Add `public int ZIndex { get; set; }` to `EntityModels/Entities/ModuleEntity.cs`
3. Add `.Property(m => m.ZIndex).IsRequired()` to `Persistence/Configurations/ModuleConfiguration.cs`
4. Generate migration: `dotnet ef migrations add AddModuleZIndex --project Persistence/Persistence.csproj --startup-project Application/Application.csproj`
5. Apply: `dotnet ef database update --project Persistence/Persistence.csproj --startup-project Application/Application.csproj`
6. Verify: `dotnet build Staccato.sln` — AutoMapper profiles pick up ZIndex automatically.

### Step 2: Repository Enhancement (Domain + Repository)

1. Add `HasTitleModuleInLessonAsync` to `Domain/Interfaces/Repositories/IModuleRepository.cs`
2. Implement in `Repository/Repositories/ModuleRepository.cs` — join Modules through LessonPages where LessonId matches
3. Verify: `dotnet build Staccato.sln`

### Step 3: API Models (ApiModels)

1. Create `ApiModels/Modules/` directory
2. Create `CreateModuleRequest.cs` + `CreateModuleRequestValidator.cs`
3. Create `UpdateModuleRequest.cs` + `UpdateModuleRequestValidator.cs`
4. Create `PatchModuleLayoutRequest.cs` + `PatchModuleLayoutRequestValidator.cs`
5. Create `ModuleResponse.cs` (record with all fields)
6. Verify: `dotnet build Staccato.sln`

### Step 4: Service Layer (Domain)

1. Create `Domain/Services/IModuleService.cs` with 5 method signatures
2. Create `Domain/Services/ModuleService.cs` with:
   - Constructor: `IModuleRepository`, `ILessonPageRepository`, `ILessonRepository`, `INotebookRepository`, `IUnitOfWork`
   - Private: `ValidateGridPlacementAsync`, `ValidateContentAsync`, `VerifyPageOwnershipAsync`, `VerifyModuleOwnershipAsync`
   - All 6 validation rules in appropriate methods
3. Register `IModuleService → ModuleService` in `Application/Extensions/ServiceCollectionExtensions.cs`
4. Verify: `dotnet build Staccato.sln`

### Step 5: Controller + Mapping (Api)

1. Create `Api/Controllers/ModulesController.cs` with all 5 endpoints
2. Add `Module → ModuleResponse` mapping in `Api/Mapping/DomainToResponseProfile.cs`
3. Verify: `dotnet build Staccato.sln`

### Step 6: Tests

1. Create `Tests/Unit/ModuleServiceTests.cs` — all validation rules + CRUD paths
2. Create `Tests/Integration/Controllers/ModulesControllerTests.cs` — all 5 endpoints
3. Verify: `dotnet test Staccato.sln`

## Verification

```bash
# Full build
dotnet build Staccato.sln

# All tests
dotnet test Staccato.sln

# Quick smoke test (run API and test with curl)
dotnet run --project Application/Application.csproj
# Then: POST to /pages/{pageId}/modules with auth token
```

## Key Files to Reference

| What | Where |
|------|-------|
| Existing ModuleRepository | `Repository/Repositories/ModuleRepository.cs` |
| Overlap check (AABB) | `ModuleRepository.CheckOverlapAsync` |
| Type constraints | `DomainModels/Constants/ModuleTypeConstraints.cs` |
| Page dimensions | `DomainModels/Constants/PageSizeDimensions.cs` |
| Service pattern | `Domain/Services/LessonPageService.cs` |
| Controller pattern | `Api/Controllers/LessonPagesController.cs` |
| Integration test pattern | `Tests/Integration/Controllers/LessonPagesControllerTests.cs` |
| DI registration | `Application/Extensions/ServiceCollectionExtensions.cs` |
| Exception hierarchy | `Domain/Exceptions/` |
