# Data Model: Module Management

**Feature**: 010-module-management
**Date**: 2026-03-29

## Entity Changes

### Module (DomainModels/Models/Module.cs) — Modified

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK, app-generated |
| LessonPageId | Guid | FK → LessonPage |
| ModuleType | ModuleType (enum) | Immutable after creation |
| GridX | int | >= 0 |
| GridY | int | >= 0 |
| GridWidth | int | >= MinWidth for type |
| GridHeight | int | >= MinHeight for type |
| **ZIndex** | **int** | **NEW — >= 0, visual stacking order** |
| ContentJson | string | JSON array of BuildingBlock objects, default "[]" |

### ModuleEntity (EntityModels/Entities/ModuleEntity.cs) — Modified

| Field | Type | EF Config |
|-------|------|-----------|
| (all existing fields) | — | unchanged |
| **ZIndex** | **int** | **NEW — required, default 0** |

### Database Migration

- Add column `ZIndex` (int, NOT NULL, DEFAULT 0) to `Modules` table.
- No other schema changes. All existing FK relationships and cascade rules remain.

## Relationships (Unchanged)

```
LessonPage (1) ──→ (N) Module    [cascade delete]
Lesson (1) ──→ (N) LessonPage   [cascade delete]
Notebook (1) ──→ (N) Lesson      [cascade delete]
User (1) ──→ (N) Notebook        [cascade delete]
```

## Validation Rules (Enforced in ModuleService)

| Rule | Check | Error Code | HTTP |
|------|-------|------------|------|
| Minimum size | gridWidth >= MinWidth AND gridHeight >= MinHeight | MODULE_TOO_SMALL | 422 |
| Page boundary | gridX >= 0 AND gridY >= 0 AND gridX + gridWidth <= pageGridWidth AND gridY + gridHeight <= pageGridHeight | MODULE_OUT_OF_BOUNDS | 422 |
| No overlap | AABB test against all other modules on page (exclude self on update) | MODULE_OVERLAP | 422 |
| Allowed blocks | Each content block type ∈ AllowedBlocks[moduleType] | INVALID_BUILDING_BLOCK | 422 |
| Breadcrumb empty | If moduleType == Breadcrumb, content must be [] | BREADCRUMB_CONTENT_NOT_EMPTY | 422 |
| Title unique | Max 1 Title module per lesson (across all pages) | DUPLICATE_TITLE_MODULE | 409 |
| ZIndex range | zIndex >= 0 | (FluentValidation) | 400 |
| Type immutable | PUT moduleType must match stored value | MODULE_TYPE_IMMUTABLE | 400 |

## Repository Methods

### IModuleRepository (existing + new)

| Method | Status | Description |
|--------|--------|-------------|
| `GetByIdAsync(Guid id, ct)` | Existing (base) | Get single module by ID |
| `AddAsync(Module, ct)` | Existing (base) | Insert module |
| `Update(Module)` | Existing (base) | Update module |
| `Remove(Module)` | Existing (base) | Delete module |
| `GetByPageIdAsync(Guid pageId, ct)` | Existing | All modules on page, ordered by GridY, GridX |
| `CheckOverlapAsync(pageId, x, y, w, h, excludeId?, ct)` | Existing | AABB overlap check |
| **`HasTitleModuleInLessonAsync(Guid lessonId, Guid? excludeModuleId, ct)`** | **NEW** | Check if any Title module exists in any page of the lesson |

## Service Methods

### IModuleService (new)

| Method | Returns | Description |
|--------|---------|-------------|
| `GetModulesByPageIdAsync(Guid pageId, Guid userId, ct)` | `IReadOnlyList<Module>` | Get all modules on a page |
| `CreateModuleAsync(Guid pageId, ModuleCreateParams, Guid userId, ct)` | `Module` | Create module with all validations |
| `UpdateModuleAsync(Guid moduleId, ModuleUpdateParams, Guid userId, ct)` | `Module` | Full update with all validations |
| `UpdateModuleLayoutAsync(Guid moduleId, ModuleLayoutParams, Guid userId, ct)` | `Module` | Layout-only update with grid validations |
| `DeleteModuleAsync(Guid moduleId, Guid userId, ct)` | `void` | Delete with ownership check |

### Internal Validation Methods (private in ModuleService)

| Method | Description |
|--------|-------------|
| `ValidateGridPlacementAsync(...)` | Checks minimum size, page boundary, overlap |
| `ValidateContentAsync(...)` | Checks building block types, breadcrumb empty rule |
| `VerifyPageOwnershipAsync(...)` | Page → Lesson → Notebook → UserId |
| `VerifyModuleOwnershipAsync(...)` | Module → Page → Lesson → Notebook → UserId |
