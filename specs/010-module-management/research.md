# Research: Module Management

**Feature**: 010-module-management
**Date**: 2026-03-29

## R1: ZIndex Missing from Domain and Entity Models

**Decision**: Add `int ZIndex` property to both `Module` (DomainModels) and `ModuleEntity` (EntityModels), with a database migration.

**Rationale**: The frontend API contract requires `zIndex` on all module endpoints. The current Module domain model and ModuleEntity lack this field. The AutoMapper bidirectional mapping (`ModuleEntity ↔ Module`) will pick it up automatically since the property names match.

**Alternatives considered**:
- Store zIndex only in ContentJson → rejected: zIndex is a layout property, not content; PATCH layout endpoint needs it independently.

## R2: Title Uniqueness Check — Repository Method Needed

**Decision**: Add `Task<bool> HasTitleModuleInLessonAsync(Guid lessonId, Guid? excludeModuleId, CancellationToken ct)` to `IModuleRepository`.

**Rationale**: The existing repository has `GetByPageIdAsync` and `CheckOverlapAsync` but no method to check Title uniqueness across all pages of a lesson. The query needs to join through LessonPages to find any module with `ModuleType == Title` in any page of the given lesson. This is a cross-page query that doesn't fit the existing methods.

**Alternatives considered**:
- Load all modules for all pages in the service layer and filter → rejected: N+1 query pattern, inefficient.
- Add method to ILessonPageRepository → rejected: Module queries belong in IModuleRepository per separation of concerns.

## R3: Overlap Detection Algorithm

**Decision**: Use existing LINQ-based AABB (Axis-Aligned Bounding Box) overlap detection in `ModuleRepository.CheckOverlapAsync`.

**Rationale**: The repository already implements the correct rectangle overlap formula: `m.GridX < gridX + gridWidth && m.GridX + m.GridWidth > gridX && m.GridY < gridY + gridHeight && m.GridY + m.GridHeight > gridY`. This translates to efficient SQL via EF Core. No changes needed.

**Alternatives considered**:
- Raw SQL query → rejected: LINQ query is already efficient and type-safe.
- In-memory overlap check in service → rejected: would require loading all modules first; DB-side filtering is better.

## R4: Content Validation Depth

**Decision**: Validate building block `type` discriminator only. Do not validate internal structure of building blocks (date formats, chord IDs, note values, etc.).

**Rationale**: The spec explicitly states FR-009: "validate that all building blocks in a module's content are of types allowed for that module's type." Deep content validation (chord ID existence, date format, note values) is a separate concern and would significantly expand scope. The `type` field is deserialized from the JSON array and checked against `ModuleTypeConstraints.AllowedBlocks[moduleType]`.

**Alternatives considered**:
- Full building block schema validation → rejected: out of scope for this feature; would require per-block-type validators.

## R5: Controller Routing Pattern

**Decision**: Use a single `ModulesController` with two route bases: module-centric routes (`/modules/{id}`) and page-scoped routes (`/pages/{id}/modules`).

**Rationale**: The existing codebase uses `LessonPagesController` with route `[Route("lessons")]` and methods like `[HttpGet("{id:guid}/pages")]`. Following this pattern, the GET/POST endpoints under `/pages/{id}/modules` and PUT/DELETE/PATCH under `/modules/{id}` can coexist in one controller using explicit route templates per action. This avoids creating a second controller class for what is conceptually the same resource.

**Alternatives considered**:
- Two controllers (PagesModulesController + ModulesController) → rejected: splits module logic across two files unnecessarily; the existing pattern handles mixed routing.

## R6: Ownership Verification Chain for Modules

**Decision**: Navigate Module → LessonPage → Lesson → Notebook → UserId. Cache intermediate lookups where multiple validations need the same entities.

**Rationale**: Consistent with `LessonPageService.VerifyLessonOwnershipAsync` pattern (Lesson → Notebook → UserId check). For module operations, we need to go one level deeper: load the module, then its page, then verify lesson ownership. For POST (creating on a page), start from the page.

**Alternatives considered**:
- Store UserId directly on Module → rejected: violates data normalization; ownership is derived through the hierarchy.
- Single join query → considered but repository pattern favors individual lookups for clarity and reuse.

## R7: Module Type Immutability on PUT

**Decision**: PUT request body includes `moduleType`. Service validates that the provided moduleType matches the stored value; throws `BadRequestException("MODULE_TYPE_IMMUTABLE", ...)` if they differ.

**Rationale**: Clarified in spec session 2026-03-29. Including moduleType in the request makes the constraint explicit to API consumers and prevents silent bugs. A 400 status code signals a client error (sending wrong type), not a business rule violation.

**Alternatives considered**:
- Exclude moduleType from PUT body → rejected per clarification: makes the constraint implicit and harder to debug.
- Silently ignore → rejected per clarification: masks client-side bugs.
