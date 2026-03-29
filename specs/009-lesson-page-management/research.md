# Research: Lesson & Lesson Page Management

**Feature**: 009-lesson-page-management
**Date**: 2026-03-29

## Existing Infrastructure Audit

### Decision: No new data model or EF work needed

**Rationale**: The following already exist in the codebase:
- `LessonEntity` and `LessonPageEntity` in `EntityModels/Entities/`
- `Lesson` and `LessonPage` domain models in `DomainModels/Models/`
- `LessonConfiguration` and `LessonPageConfiguration` in `Persistence/Configurations/`
- `LessonRepository` and `LessonPageRepository` implementations in `Repository/Repositories/`
- `ILessonRepository` and `ILessonPageRepository` interfaces in `Domain/Interfaces/Repositories/`
- AutoMapper entity↔domain profiles (`LessonEntity ↔ Lesson`, `LessonPageEntity ↔ LessonPage`) in `Repository/Mapping/EntityToDomainProfile.cs`
- DI registrations for both repositories in `Application/Extensions/ServiceCollectionExtensions.cs`

**Alternatives considered**: Creating everything from scratch — rejected because entities, models, configurations, repositories, and profiles were already scaffolded in previous features (001–008).

## Lesson Summary Projection

### Decision: Create a `LessonSummary` domain model

**Rationale**: The existing `Lesson` domain model does not include `PageCount`. The `GET /notebooks/{id}/lessons` endpoint requires page count per lesson. Following the `NotebookSummary` precedent, create a `LessonSummary` domain model with Id, Title, CreatedAt, PageCount. The repository will project directly into this type using EF `.Select()`.

**Alternatives considered**: Computing page counts in the service layer by loading all pages separately — rejected because it requires N+1 queries.

## Notebook Index Computation

### Decision: Compute on-demand in the service layer using a single query

**Rationale**: The notebook index is a derived view (not persisted). The service fetches all lessons ordered by CreatedAt with their page counts in one query, then calculates cumulative `startPageNumber` values in memory with a running sum: `startPageNumber = 2 + sumOfPreviousPageCounts`.

**Alternatives considered**:
- Storing index in a separate table — rejected: would require synchronization on every lesson/page change.
- Computing in SQL with window functions — rejected: simpler to compute in memory since lesson count per notebook is bounded.

## Page Creation Response Envelope

### Decision: Use `LessonPageWithWarningResponse` record with `{ data, warning }` shape

**Rationale**: The constitution explicitly states that LessonPage creation is the sole exception to the "no envelope" rule and MUST use `{ data: T, warning: string | null }`. The frontend documentation confirms `warning` is `null` when under the limit. Both 201 (normal) and 200 (soft-limit) responses use this envelope. Status code differentiates the two cases.

**Alternatives considered**: Different response shapes for 201 vs 200 — rejected: the constitution mandates the envelope for all LessonPage creation responses.

## Soft Limit Trigger Point

### Decision: Warning triggers when lesson already has >= 10 pages BEFORE adding the new one

**Rationale**: The spec, frontend doc, and constitution all agree:
- Adding page 10 (when 9 exist): returns 201 with `warning: null`
- Adding page 11+ (when 10+ exist): returns 200 with warning string

The service returns a result tuple `(LessonPage page, bool isOverSoftLimit)`. The controller checks the flag to determine status code.

## Ownership Verification Pattern

### Decision: Ownership checked by loading the notebook via lesson/page chain and comparing UserId

**Rationale**: Following the existing `NotebookService` pattern where ownership is verified with `notebook.UserId != userId → throw ForbiddenException()`. For lesson endpoints, the service loads the lesson's notebook to check ownership. For page endpoints, the service loads the page's lesson's notebook.

To avoid N+1 lookups, the repository methods that return lessons and pages will include notebook ownership data (either by joining or by having the service call the notebook repository).

### Decision: Use `INotebookRepository` to verify ownership

The simplest and most consistent approach: services call `INotebookRepository.GetByIdAsync(notebookId)` to load the notebook, then check `UserId`. This reuses the existing ownership pattern.

## Last-Page Deletion Error

### Decision: Use `BadRequestException` with code `LAST_PAGE_DELETION`

**Rationale**: The error code `LAST_PAGE_DELETION` is already defined in the project's error code list (CLAUDE.md). The exception is a `BadRequestException` (400) because it represents a client-addressable business rule violation.

## Repository Extensions Needed

### Decision: Add focused query methods to existing repository interfaces

**ILessonRepository** additions:
- `GetSummariesByNotebookIdAsync(Guid notebookId, CancellationToken ct)` → returns `IReadOnlyList<LessonSummary>` with page counts via `.Select()` projection
- `GetLessonsWithPageCountsAsync(Guid notebookId, CancellationToken ct)` → returns lightweight data for index calculation (lesson id, title, createdAt, pageCount)

Since both queries need essentially the same data (lessons with page counts), a single method `GetSummariesByNotebookIdAsync` can serve both the listing endpoint and the index calculation.

**ILessonPageRepository** additions:
- `GetPageCountByLessonIdAsync(Guid lessonId, CancellationToken ct)` → returns `int` (for soft-limit check)
- `GetMaxPageNumberByLessonIdAsync(Guid lessonId, CancellationToken ct)` → returns `int` (for next page number assignment)
