# Data Model: Lesson & Lesson Page Management

**Feature**: 009-lesson-page-management
**Date**: 2026-03-29

## Existing Entities (no changes needed)

### LessonEntity (EntityModels/Entities/LessonEntity.cs)

| Field | Type | Constraints |
|-------|------|-------------|
| Id | Guid | PK, app-generated |
| NotebookId | Guid | FK → Notebooks, Cascade delete |
| Title | string | Required, nvarchar(max) |
| CreatedAt | DateTime | Required, UTC |
| UpdatedAt | DateTime | Required, UTC |

**Navigations**: `Notebook` (many-to-one), `LessonPages` (one-to-many)

### LessonPageEntity (EntityModels/Entities/LessonPageEntity.cs)

| Field | Type | Constraints |
|-------|------|-------------|
| Id | Guid | PK, app-generated |
| LessonId | Guid | FK → Lessons, Cascade delete |
| PageNumber | int | Required, 1-based within lesson |

**Navigations**: `Lesson` (many-to-one), `Modules` (one-to-many)

### Cascade Path

```
Notebook (delete) → Lesson (cascade) → LessonPage (cascade) → Module (cascade)
```

## Existing Domain Models (no changes needed)

### Lesson (DomainModels/Models/Lesson.cs)

| Property | Type |
|----------|------|
| Id | Guid |
| NotebookId | Guid |
| Title | string |
| CreatedAt | DateTime |
| UpdatedAt | DateTime |

### LessonPage (DomainModels/Models/LessonPage.cs)

| Property | Type | Notes |
|----------|------|-------|
| Id | Guid | |
| LessonId | Guid | |
| PageNumber | int | |
| ModuleCount | int | Derived via Count() in repository queries. Not stored in entity. |

## New Domain Models

### LessonSummary (DomainModels/Models/LessonSummary.cs) — NEW

Used as a projection for the lesson list endpoint and index calculation.

| Property | Type | Source |
|----------|------|--------|
| Id | Guid | Lesson.Id |
| NotebookId | Guid | Lesson.NotebookId |
| Title | string | Lesson.Title |
| CreatedAt | DateTime | Lesson.CreatedAt |
| PageCount | int | Count of LessonPages |

### NotebookIndexEntry (DomainModels/Models/NotebookIndexEntry.cs) — NEW

Derived (non-persisted) model for index endpoint. Computed in service layer.

| Property | Type | Source |
|----------|------|--------|
| LessonId | Guid | Lesson.Id |
| Title | string | Lesson.Title |
| CreatedAt | DateTime | Lesson.CreatedAt |
| StartPageNumber | int | 2 + sum(previous lessons' page counts) |

## Repository Interface Extensions

### ILessonRepository (additions)

```csharp
Task<IReadOnlyList<LessonSummary>> GetSummariesByNotebookIdAsync(
    Guid notebookId, CancellationToken ct = default);
```

Implementation: EF `.Select()` projection joining LessonPages count.

### ILessonPageRepository (additions)

```csharp
Task<int> GetPageCountByLessonIdAsync(Guid lessonId, CancellationToken ct = default);
Task<int> GetMaxPageNumberByLessonIdAsync(Guid lessonId, CancellationToken ct = default);
```

## Validation Rules

| Entity | Field | Rule |
|--------|-------|------|
| Lesson | Title | Required, max 200 chars, no whitespace-only |
| LessonPage | PageNumber | Auto-assigned: max existing + 1 |
| LessonPage | (delete) | Cannot delete last page (count must be >= 2) |
| LessonPage | (add) | Soft limit warning at >= 10 existing pages |
