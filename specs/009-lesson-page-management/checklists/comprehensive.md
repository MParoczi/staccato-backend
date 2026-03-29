# Comprehensive Checklist: Lesson & Lesson Page Management

**Purpose**: Validate requirement quality across API contracts, business rules, data model, and security/ownership for the lesson & lesson page management feature
**Created**: 2026-03-29
**Feature**: [spec.md](../spec.md) | [contracts/endpoints.md](../contracts/endpoints.md) | [data-model.md](../data-model.md)
**Depth**: Thorough | **Audience**: Reviewer (PR)
**Status**: All 50 items reviewed and resolved (2026-03-29)

## Requirement Completeness

- [x] CHK001 - Are all 9 endpoints documented with request/response shapes, status codes, and error cases? [Completeness, Contracts §Lesson Endpoints, §Lesson Page Endpoints, §Notebook Index Endpoint]
  - **Resolved**: Yes, all 9 endpoints fully documented in contracts/endpoints.md.
- [x] CHK002 - Is the `UpdatedAt` field behavior specified for lesson updates (PUT /lessons/{id})? The entity has `UpdatedAt` but neither spec nor contracts mention whether it is returned or how it is set on update. [Gap, Data-Model §LessonEntity]
  - **Resolved**: Frontend docs intentionally omit `updatedAt` from `LessonDetail` and `LessonSummary` interfaces. Match frontend docs — no gap. UpdatedAt is set internally (follows NotebookService pattern) but not exposed in responses.
- [x] CHK003 - Are localization requirements for the soft-limit warning message specified? The constitution mandates `IStringLocalizer` and `.resx` files for `en` and `hu`, but the spec only shows an English warning string. [Gap, Spec §FR-009]
  - **Resolved**: Localization is a cross-cutting concern handled by `BusinessExceptionMiddleware` via `IStringLocalizer<BusinessErrors>`. Warning string will use a resource key at implementation time. Not a spec-level gap.
- [x] CHK004 - Is the `UpdatedAt` field included in the `LessonDetailResponse` contract? The entity has it, the `NotebookDetailResponse` includes it, but the lesson detail contract omits it. [Gap, Contracts §GET /lessons/{id}]
  - **Resolved**: Same as CHK002. Frontend docs omit it intentionally.
- [x] CHK005 - Are localization requirements for the `LAST_PAGE_DELETION` error message specified for both `en` and `hu`? [Gap, Contracts §Error Response Formats]
  - **Resolved**: Same as CHK003. Middleware handles localization via resource keys.
- [x] CHK006 - Is the `LessonSummary` response missing the `updatedAt` field that exists on the entity? The notebook summary includes both `createdAt` and `updatedAt`, but the lesson summary only specifies `createdAt`. [Completeness, Spec §FR-003]
  - **Resolved**: Same as CHK002. Frontend docs' `LessonSummary` omits `updatedAt`.
- [x] CHK007 - Are requirements specified for what happens when `POST /lessons/{id}/pages` is called on a lesson whose pages were all deleted via cascade (notebook deletion race)? [Gap, Edge Case]
  - **Resolved**: If notebook is deleted mid-request, lesson lookup returns null → NotFoundException (404). Standard behavior; no special handling needed.

## Requirement Clarity

- [x] CHK008 - Is the soft-limit trigger threshold unambiguous? Spec §FR-009 says "10 or more pages" while §FR-010 says "fewer than 10 pages" — are the boundary semantics clear that "10 existing pages" means the 11th page triggers the warning? [Clarity, Spec §FR-009/FR-010]
  - **Resolved**: Yes, unambiguous. research.md confirms: 9 existing → add 10th → 201. 10+ existing → add next → 200 with warning. FR-009 and FR-010 are complementary, not conflicting.
- [x] CHK009 - Is "max 200 characters" defined precisely — does it mean 200 Unicode characters, 200 UTF-16 code units, or 200 bytes? [Clarity, Spec §FR-001]
  - **Resolved**: C# `MaximumLength(200)` uses `string.Length` which counts UTF-16 code units. This is the standard FluentValidation behavior used across the codebase.
- [x] CHK010 - Is "whitespace-only" title rejection explicitly specified in the spec requirements section? It appears in Edge Cases but not in FR-018 which only says "required and no longer than 200 characters." [Clarity, Spec §FR-018 vs Edge Cases]
  - **Resolved**: FluentValidation's `NotEmpty()` already rejects whitespace-only strings by design. The existing `CreateNotebookRequestValidator` uses the same pattern. Edge Case section documents the expected behavior; FR-018's "required" implicitly covers this.
- [x] CHK011 - Is the page number assignment formula clear for the case when all pages have been deleted except one, and that page has a non-1 number (e.g., page 5 is the sole remaining page)? The new page would be 6, not 2. [Clarity, Spec §FR-008]
  - **Resolved**: Yes, spec §FR-008 says "max existing + 1" and Assumptions section says "gaps are acceptable." Edge Case section explicitly covers this: "The new page is numbered max + 1 based on existing page numbers, so it would be 4."
- [x] CHK012 - Is the phrase "hard delete" sufficiently defined? Does it mean EF cascade, explicit deletion in service code, or relying on DB-level cascade? The spec uses it but the implementation strategy is split across research.md decisions. [Clarity, Spec §FR-006/FR-012]
  - **Resolved**: EF cascade is already configured in `LessonConfiguration` and `LessonPageConfiguration` (DeleteBehavior.Cascade). "Hard delete" means permanent removal (vs soft delete). Implementation uses EF cascade.

## Requirement Consistency

- [x] CHK013 - Does the `LessonDetailResponse` contract in endpoints.md match the frontend documentation's `LessonDetail` interface? The frontend doc includes `notebookId` and `pages[]` — is this consistent with the contract? [Consistency, Contracts §GET /lessons/{id} vs Frontend Doc §7]
  - **Resolved**: Yes, both include `id, notebookId, title, createdAt, pages[]`. Consistent.
- [x] CHK014 - Is the response envelope for page creation consistent between spec, contracts, and constitution? The constitution says "MUST use `{ data: T, warning: string | null }`" for ALL page creation, but Spec §FR-010 describes 201 separately from the envelope in §FR-009. [Consistency, Spec §FR-009/FR-010 vs Constitution §V]
  - **Resolved**: research.md settles this: both 201 and 200 use the `{ data, warning }` envelope. 201 has `warning: null`, 200 has the warning string. Consistent with constitution.
- [x] CHK015 - Are the `LessonPageResponse` fields consistent between the page listing endpoint and the page creation envelope's `data` field? Both should return the same `LessonPage` shape. [Consistency, Contracts §GET /lessons/{id}/pages vs §POST /lessons/{id}/pages]
  - **Resolved**: Both use `{ id, lessonId, pageNumber, moduleCount }`. Consistent.
- [x] CHK016 - Is the notebook index `startPageNumber` formula consistent between spec (FR-015), contracts (endpoints.md §Calculation), and frontend documentation (§10)? All three should match: `2 + sum(previous page counts)`. [Consistency, Spec §FR-015 vs Contracts vs Frontend Doc §10]
  - **Resolved**: All three match: `startPageNumber = 2 + sum(pageCounts of all preceding lessons)`.
- [x] CHK017 - Are the error status codes for `LAST_PAGE_DELETION` consistent? Spec §FR-011 says "returning an error" without specifying 400, while contracts specify 400. Is the status code explicitly required in the spec? [Consistency, Spec §FR-011 vs Contracts §DELETE page]
  - **Resolved**: research.md specifies `BadRequestException` (400) with code `LAST_PAGE_DELETION`. Contracts correctly show 400. Spec is deliberately status-code-agnostic (business requirement language).
- [x] CHK018 - Does the plan's `LessonSummary` domain model include `NotebookId` (data-model.md shows it) while the spec's LessonSummary response only shows `id, title, createdAt, pageCount`? Is the extra field intentional for internal use? [Consistency, Data-Model §LessonSummary vs Spec §FR-003]
  - **Resolved**: Intentional. `NotebookId` is on the domain model for internal ownership verification in the service layer. The API response DTO excludes it.

## Acceptance Criteria Quality

- [x] CHK019 - Are the notebook index acceptance scenarios (User Story 3) testable with specific numeric values? Scenario 1 provides exact numbers (2, 5, 7) — are these correct given the formula? Lesson A: 2, Lesson B: 2+3=5, Lesson C: 2+3+2=7. [Measurability, Spec §User Story 3]
  - **Resolved**: Math verified correct. A(3 pages): start=2. B(2 pages): start=2+3=5. C(1 page): start=2+3+2=7.
- [x] CHK020 - Is SC-004 ("rejected 100% of the time") a testable metric, or should it be rephrased as specific test scenarios covering all endpoint/method combinations? [Measurability, Spec §SC-004]
  - **Resolved**: Testable via integration tests that systematically cover all 9 endpoints with cross-user access. "100%" is verified by covering every endpoint.
- [x] CHK021 - Is SC-003 ("recalculating start page numbers dynamically after any lesson or page change") testable without defining specific before/after scenarios? [Measurability, Spec §SC-003]
  - **Resolved**: Testable by: (1) creating lessons, (2) verifying index, (3) adding/deleting pages, (4) verifying index again. Acceptance Scenario 3 in User Story 3 covers the "after deletion" case.
- [x] CHK022 - Are acceptance scenarios defined for the notebook index endpoint when the notebook has only one lesson? This is a boundary case not covered in User Story 3. [Coverage, Spec §User Story 3]
  - **Resolved**: Single lesson → `startPageNumber = 2 + 0 = 2`. Trivial boundary case implicitly covered by the formula. Not a gap requiring a dedicated scenario.

## Scenario Coverage

- [x] CHK023 - Are requirements defined for what the `GET /notebooks/{id}/lessons` endpoint returns when the notebook exists but has zero lessons? The spec covers empty index but not empty lesson list explicitly. [Coverage, Spec §FR-003]
  - **Resolved**: Standard REST convention: returns empty array `[]`. Consistent with existing patterns (`GetByNotebookIdOrderedByCreatedAtAsync` returns empty list).
- [x] CHK024 - Are requirements defined for `PUT /lessons/{id}` when the new title is identical to the current title? Should it still update `UpdatedAt` and return 200? [Coverage, Gap]
  - **Resolved**: Yes, follows existing `NotebookService.UpdateAsync` pattern which unconditionally sets `UpdatedAt = DateTime.UtcNow`. Returns 200 regardless.
- [x] CHK025 - Are requirements defined for what `GET /lessons/{id}/pages` returns for the page's `moduleCount` field? Is it a live count from the database or a cached value? [Coverage, Contracts §GET /lessons/{id}/pages]
  - **Resolved**: Live count, derived via `Count()` in repository queries. Not a cached or stored value.
- [x] CHK026 - Is the interaction between lesson deletion and the notebook's `LessonCount` field specified? `NotebookSummary` includes `LessonCount` — does deleting a lesson automatically update this derived count? [Coverage, Gap]
  - **Resolved**: `LessonCount` is derived via `Count()` query (not stored on `NotebookEntity`). Deletion automatically reflects in subsequent queries.
- [x] CHK027 - Are requirements defined for `DELETE /lessons/{lessonId}/pages/{pageId}` when the pageId belongs to a different lesson than lessonId? Should this return 404 or 400? [Coverage, Edge Case]
  - **Decision**: Return 404. Service verifies `page.LessonId == lessonId`; mismatch treated as "page not found in this lesson context."

## Edge Case Coverage

- [x] CHK028 - Is the behavior specified for `POST /lessons/{id}/pages` when called concurrently? Two simultaneous requests could both read `maxPageNumber = 5` and create two pages numbered 6. [Edge Case, Gap]
  - **Decision**: Ignore for now. Low risk given single-user-per-notebook model. No concurrency guard needed at this stage.
- [x] CHK029 - Is the behavior specified for `DELETE /lessons/{lessonId}/pages/{pageId}` when the page has already been deleted (idempotency)? Should it return 204 or 404? [Edge Case, Gap]
  - **Decision**: Return 404. Resource no longer exists. Consistent with existing delete patterns in the codebase.
- [x] CHK030 - Are requirements defined for lesson title containing special characters (emoji, Unicode, HTML entities, SQL injection attempts)? The spec mentions max length but not character set restrictions. [Edge Case, Spec §FR-001]
  - **Decision**: Allow all Unicode. No character set restrictions. Max length (200 UTF-16 code units) is the only constraint. EF Core parameterization prevents SQL injection.
- [x] CHK031 - Is the behavior specified for `POST /notebooks/{id}/lessons` when the notebook has been soft-deleted (user account scheduled for deletion)? Should lesson creation be blocked? [Edge Case, Gap]
  - **Decision**: Allow. Account remains fully active during the 30-day grace period. Users can cancel deletion and continue working.
- [x] CHK032 - Are requirements defined for the maximum number of lessons per notebook? The spec defines a 10-page soft limit per lesson but no lesson count limit per notebook. Is this intentionally unlimited? [Edge Case, Spec §Assumptions]
  - **Decision**: No limit. Neither spec nor frontend docs mention one. Intentionally unlimited.
- [x] CHK033 - Is the behavior specified for `GetMaxPageNumberByLessonIdAsync` when the lesson has zero pages (should never happen given invariant, but what if data is corrupt)? [Edge Case, Data-Model §ILessonPageRepository]
  - **Resolved**: Invariant guarantees >= 1 page. If data were corrupt, `MAX()` on empty set returns 0, so new page would be numbered 1. Safe default behavior.

## Security & Ownership Requirements

- [x] CHK034 - Is the ownership verification chain fully specified for page endpoints? Page operations require traversing Page → Lesson → Notebook → UserId. Is this multi-hop ownership check documented as a requirement? [Completeness, Spec §FR-017]
  - **Resolved**: Documented in research.md §Ownership Verification Pattern. Service uses `INotebookRepository.GetByIdAsync(notebookId)` to verify ownership.
- [x] CHK035 - Is the 403 vs 404 ordering requirement clear? When a user requests a lesson that exists but belongs to another user, the spec says return 403 — but is the order of checks specified (check existence first, then ownership, or ownership first)? [Clarity, Spec §FR-017]
  - **Resolved**: Existing `NotebookService.GetByIdAsync` pattern: check existence first (→404), then ownership (→403). Same order will be followed.
- [x] CHK036 - Are ownership requirements specified for the `DELETE /lessons/{lessonId}/pages/{pageId}` endpoint when the lesson belongs to the user but the page belongs to a different lesson? [Coverage, Spec §FR-017]
  - **Resolved**: Covered by CHK027 decision. Service verifies page belongs to the specified lesson; mismatch → 404.
- [x] CHK037 - Is the requirement specified that unauthenticated requests (no JWT / expired JWT) return 401, not 403? The spec covers 403 for wrong-user but doesn't mention 401 for missing auth. [Gap, Spec §FR-016]
  - **Resolved**: 401 is handled automatically by ASP.NET Core JWT Bearer middleware. Not a feature-level requirement.
- [x] CHK038 - Are rate limiting requirements explicitly excluded for these endpoints? The constitution says rate limiting applies to `/auth/*` only — is this exclusion documented in the feature spec? [Clarity, Spec §FR-016]
  - **Resolved**: Rate limiting is a project-wide concern documented in the constitution, not per-feature. No spec gap.

## Data Model Requirements

- [x] CHK039 - Is the `LessonSummary` domain model's relationship to the `Lesson` entity clearly specified? Is it a projection-only type (never persisted) or could it be confused with an entity? [Clarity, Data-Model §LessonSummary]
  - **Resolved**: Clearly a projection. Follows `NotebookSummary` precedent (also projection-only, used in repository Select() queries).
- [x] CHK040 - Is the `NotebookIndexEntry` model's non-persisted nature explicitly documented in the spec, or only in research.md/data-model.md? [Completeness, Spec §Key Entities]
  - **Resolved**: Spec §Key Entities says "derived (non-persisted) data structure." Documented in both spec and data-model.md.
- [x] CHK041 - Are the repository extension method signatures (GetSummariesByNotebookIdAsync, GetPageCountByLessonIdAsync, GetMaxPageNumberByLessonIdAsync) consistent between research.md and data-model.md? Research.md mentions `GetLessonsWithPageCountsAsync` as a separate method but then says a single method can serve both. [Consistency, Research §Repository Extensions vs Data-Model §ILessonRepository]
  - **Resolved**: research.md concludes single method `GetSummariesByNotebookIdAsync` suffices. Plan §Project Structure has a stale reference to `GetLessonsWithPageCountsAsync` — needs cleanup. data-model.md is authoritative.
- [x] CHK042 - Is the `LessonPage` domain model missing a `ModuleCount` property? The API returns `moduleCount` in page responses, but the domain model only has `Id, LessonId, PageNumber`. How is module count derived? [Gap, Data-Model §LessonPage vs Contracts §LessonPage shape]
  - **Decision**: Add `ModuleCount` property to the `LessonPage` domain model, populated during repository queries via `Count()`. Follows the `Notebook.LessonCount` pattern.

## Dependencies & Assumptions

- [x] CHK043 - Is the assumption "lesson titles do not need to be unique within a notebook" validated against the frontend documentation? Could duplicate titles cause confusion in the notebook index? [Assumption, Spec §Assumptions]
  - **Resolved**: Spec explicitly assumes allowed. Frontend docs impose no uniqueness constraint. Index entries also include `lessonId` and `createdAt` for disambiguation.
- [x] CHK044 - Is the assumption "page number gaps after deletion are acceptable" validated against the PDF export pipeline? Do gap-numbered pages render correctly in the PDF? [Assumption, Spec §Assumptions]
  - **Decision**: No impact. PDF renders pages in PageNumber order. Page numbers are internal identifiers, not displayed labels in the PDF. Gaps are irrelevant.
- [x] CHK045 - Is the dependency on `INotebookRepository` for ownership checks documented in the plan's service layer design? The services will depend on a repository from a different domain area. [Dependency, Research §Ownership Verification Pattern]
  - **Resolved**: Documented in research.md §Ownership Verification Pattern.
- [x] CHK046 - Is the assumption that "creating a page requires no request body" consistent with the existing `POST` endpoint conventions in the project? Other POST endpoints (notebooks, lessons) require a body. [Assumption, Spec §Assumptions vs Contracts §POST page]
  - **Resolved**: Valid REST pattern for "add next item" operations where all properties are auto-assigned. Frontend docs confirm no body.

## Ambiguities & Conflicts

- [x] CHK047 - Does the plan list both `GetSummariesByNotebookIdAsync` and `GetLessonsWithPageCountsAsync` as repository additions, while research.md concludes a single method suffices? Which is authoritative? [Conflict, Plan §Project Structure vs Research §Repository Extensions]
  - **Resolved**: Same as CHK041. research.md and data-model.md are authoritative. Plan needs cleanup to remove `GetLessonsWithPageCountsAsync`.
- [x] CHK048 - Is there a conflict between the spec's description of the page creation response (Acceptance Scenario 2.1: "returns it with a 201 status") and the constitution's mandate to always use the `{ data, warning }` envelope? The spec text could imply raw page data for 201. [Ambiguity, Spec §User Story 2 Scenario 1 vs Constitution §V]
  - **Resolved**: research.md settles: both 201 and 200 use the envelope. "Returns it" is shorthand; the envelope with `warning: null` is implied.
- [x] CHK049 - Is the `details` field in the `LAST_PAGE_DELETION` error response specified as `{}` (empty object) in contracts but `null` in other error responses (FORBIDDEN, NOT_FOUND)? Is this inconsistency intentional? [Ambiguity, Contracts §Error Response Formats]
  - **Resolved**: Middleware uses `ex.Details` directly. `BadRequestException` accepts `details: null` by default. Contracts should show `null` consistently. Will fix in contracts.
- [x] CHK050 - Is the warning message string "This lesson has reached the recommended maximum of 10 pages." specified as exact text or as a localizable key? If localized, the spec should reference a resource key rather than a literal string. [Ambiguity, Spec §FR-009 vs Constitution §Localization]
  - **Resolved**: Same as CHK003. The spec shows the English user-facing text for clarity. Implementation will use a resource key via `IStringLocalizer`. Not a spec-level concern.

## Notes

- All 50 items reviewed and resolved on 2026-03-29
- 39 items self-resolved by reading codebase, frontend docs, and constitution
- 11 items resolved via user Q&A decisions
- Traceability coverage: 50/50 items (100%) include at least one reference
- Focus areas: API Contracts, Business Rules, Data Model, Security & Ownership

### Action items from review:
1. **data-model.md**: Add `ModuleCount` property to `LessonPage` domain model (CHK042)
2. **plan.md**: Remove stale `GetLessonsWithPageCountsAsync` reference (CHK041/047)
3. **contracts/endpoints.md**: Fix `details` field to `null` (not `{}`) in LAST_PAGE_DELETION error (CHK049)
4. **spec.md**: Add edge case for cross-lesson pageId mismatch → 404 (CHK027)
5. **spec.md**: Add edge case for repeat delete → 404 (CHK029)
