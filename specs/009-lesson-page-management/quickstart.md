# Quickstart: Lesson & Lesson Page Management

**Feature**: 009-lesson-page-management
**Date**: 2026-03-29

## Prerequisites

- .NET 10 SDK
- SQL Server (local or container)
- Solution builds: `dotnet build Staccato.sln`
- Existing migrations applied: `dotnet ef database update --project Persistence/Persistence.csproj --startup-project Application/Application.csproj`

## What Already Exists

The data foundation is fully scaffolded from previous features:

| Layer | Files | Status |
|-------|-------|--------|
| Entity Models | `LessonEntity.cs`, `LessonPageEntity.cs` | Done |
| Domain Models | `Lesson.cs`, `LessonPage.cs` | Done |
| EF Configurations | `LessonConfiguration.cs`, `LessonPageConfiguration.cs` | Done |
| Repository Interfaces | `ILessonRepository.cs`, `ILessonPageRepository.cs` | Done (needs method additions) |
| Repository Implementations | `LessonRepository.cs`, `LessonPageRepository.cs` | Done (needs method additions) |
| AutoMapper Entity↔Domain | `EntityToDomainProfile.cs` | Done |
| DI Registrations | Repositories registered | Done (services need registration) |

## What This Feature Adds

1. **DomainModels**: `LessonSummary.cs`, `NotebookIndexEntry.cs`
2. **Repository extensions**: New query methods on existing interfaces/implementations
3. **Services**: `ILessonService` + `LessonService`, `ILessonPageService` + `LessonPageService`
4. **ApiModels**: Request/response DTOs + FluentValidation validators in `ApiModels/Lessons/`
5. **Controllers**: `LessonsController.cs`, `LessonPagesController.cs`
6. **AutoMapper**: Domain→Response mappings added to `DomainToResponseProfile.cs`
7. **DI**: Service registrations in `ServiceCollectionExtensions.cs`
8. **Tests**: Unit + integration test classes

## Implementation Order

```
1. DomainModels (LessonSummary, NotebookIndexEntry)
2. Repository interface extensions + implementations
3. Service interfaces + implementations
4. ApiModels (DTOs + validators)
5. AutoMapper response mappings
6. Controllers
7. DI registrations
8. Unit tests
9. Integration tests
```

## Build & Test

```bash
# Build
dotnet build Staccato.sln

# Run all tests
dotnet test Staccato.sln

# Run only unit tests for this feature
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Unit.LessonService"
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Unit.LessonPageService"

# Run only integration tests for this feature
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Integration.Lessons"

# Run the API
dotnet run --project Application/Application.csproj
```

## Key Patterns to Follow

- **Ownership**: Load notebook via `INotebookRepository.GetByIdAsync()`, check `UserId`, throw `ForbiddenException` on mismatch
- **Service returns**: Domain models or tuples; never IActionResult
- **Controller mapping**: `mapper.Map<ResponseType>(domainModel)` using AutoMapper
- **UoW commit**: Service calls `unitOfWork.CommitAsync(ct)` once after staging all changes
- **Response DTOs**: Use C# records with positional parameters
- **Validators**: FluentValidation `AbstractValidator<T>` in ApiModels
