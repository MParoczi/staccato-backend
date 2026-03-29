# Comprehensive Requirements Quality Checklist: Module Management

**Purpose**: Validate specification completeness, clarity, and consistency across all requirement dimensions before implementation
**Created**: 2026-03-29
**Feature**: [spec.md](../spec.md)
**Depth**: Standard
**Audience**: PR Reviewer
**Focus**: Grid validation, API contracts, authorization, content validation, edge cases
**Status**: All 33 items resolved (2026-03-29)

---

## Requirement Completeness

- [x] CHK001 - Is `MODULE_TYPE_IMMUTABLE` included in the FR-013 error code list? ~~Currently FR-013 lists 6 codes but omits this 7th code added during clarification.~~ → Added to FR-013. [Gap, Spec §FR-013]
- [x] CHK002 - Are the specific endpoints to which each validation rule applies explicitly stated? → Added "Validation Rule Applicability by Endpoint" table to spec. [Completeness, Spec §FR-006–008]
- [x] CHK003 - Is the response model (fields and types) formally required in the spec, or only defined in the contracts artifact? → Decision: contracts-only. Spec is business-focused. [Gap, Spec §FR-001–005]
- [x] CHK004 - Are FluentValidation-level input constraints (e.g., gridWidth >= 1, gridHeight >= 1) specified as requirements? → Decision: contracts-only per Q2. [Gap, Spec §FR-006]
- [x] CHK005 - Is the hard-delete behavior for modules explicitly stated as a requirement? → Already clear: FR-004 says "permanently removing" + project convention = hard delete for all non-account entities. [Completeness, Spec §FR-004]
- [x] CHK006 - Are localization requirements specified for the new error messages introduced by this feature? → Added FR-017. [Gap]

## Requirement Clarity

- [x] CHK007 - Is "ordered by grid position" in FR-002 defined precisely? → Updated to "GridY ascending then GridX ascending". [Clarity, Spec §FR-002]
- [x] CHK008 - Is the relationship between FR-015 (POST content must be empty) and FR-009 (building block type validation) explicitly clarified? → FR-009 now states "on PUT" with parenthetical explaining POST/PATCH exemption. [Clarity, Spec §FR-009/FR-015]
- [x] CHK009 - Is the overlap formula formally stated in a functional requirement? → AABB formula added to FR-008. [Clarity, Spec §FR-008]
- [x] CHK010 - Does FR-010 (Breadcrumb empty content) explicitly state it applies to both POST and PUT? → Updated to "on both POST and PUT". Acceptance scenario added to US2. [Clarity, Spec §FR-010]
- [x] CHK011 - Is the content field serialization format in API responses specified? → Decision: contracts-only per Q2. [Clarity, Gap]

## Requirement Consistency

- [x] CHK012 - Does SC-002 accurately reference the validation rule count? → Updated to "All 7 error codes" instead of "6 validation rules". [Consistency, Spec §SC-002]
- [x] CHK013 - Does SC-004 accurately describe consistent enforcement? → Rewritten to reference the Validation Rule Applicability table with per-endpoint detail. [Consistency, Spec §SC-004]
- [x] CHK014 - Is the FR-013 error code list consistent with the full set of business errors documented across all artifacts? → FR-013 now lists all 7 codes, matching contracts. [Consistency, Spec §FR-013 vs Contracts]
- [x] CHK015 - Are the edge case descriptions consistent with the functional requirements they reference? → Edge case updated: "400 FluentValidation error (field-level), not a business rule exception". [Consistency, Spec §Edge Cases vs FR-012]

## Acceptance Criteria Quality

- [x] CHK016 - Are SC-001 and SC-003 defined with a measurement point? → Updated both to "server-side p95, normal load". [Measurability, Spec §SC-001/SC-003]
- [x] CHK017 - Can SC-004 be objectively verified? → Now references the Validation Rule Applicability table for per-endpoint enumeration. [Measurability, Spec §SC-004]
- [x] CHK018 - Are acceptance scenarios defined for PUT on a Breadcrumb module? → Added US2 scenario 4. [Coverage, Spec §US2]
- [x] CHK019 - Are acceptance scenarios defined for PUT with moduleType mismatch? → Added US2 scenario 5. [Coverage, Spec §US2]

## Scenario Coverage

- [x] CHK020 - Must grid validation always run on PUT regardless of position change? → Yes. Added FR-018. [Coverage, Spec §FR-003]
- [x] CHK021 - Is FreeText "accepts all building block types" documented? → Already defined in ModuleTypeConstraints (FreeText → all enum values). No additional spec requirement needed. [Coverage, Gap]
- [x] CHK022 - Should Title uniqueness be checked on PUT? → No. Type is immutable, so PUT can't create a new Title. Validation table shows "No" for PUT. [Coverage, Spec §FR-011]
- [x] CHK023 - Is the ownership check order specified (403 vs 404)? → FR-014 says 403. Project convention: "return 403, never 404" for other user's resources. Consistent. [Consistency, Spec §FR-014]

## Edge Case Coverage

- [x] CHK024 - Are requirements defined for malformed ContentJson on PUT? → Added FR-016 (400 for malformed JSON). [Edge Case, Gap]
- [x] CHK025 - Are requirements defined for unrecognized type discriminator? → Added to FR-016 (422 INVALID_BUILDING_BLOCK). Edge case also documents this. [Edge Case, Spec §Edge Cases]
- [x] CHK026 - Is behavior specified when Title module is deleted? → Added edge case: "uniqueness slot is freed". [Edge Case, Gap]
- [x] CHK027 - Are requirements defined for zero-dimension edge cases? → FluentValidation >= 1 in contracts handles this. Per Q2 decision, contracts-only. [Edge Case, Spec §FR-006]
- [x] CHK028 - Is ZIndex upper bound intentionally unbounded? → Yes, intentionally unbounded (>= 0 only), matching CSS z-index conventions. [Edge Case, Spec §FR-012]

## Non-Functional Requirements

- [x] CHK029 - Are concurrency requirements defined for simultaneous PATCH updates? → Added assumption: last-write-wins, consistent with project pattern. [Gap, Assumption]
- [x] CHK030 - Are performance requirements specified for overlap check scaling? → Added assumption: module count per page expected under 50, O(n) acceptable. [Gap]

## Dependencies & Assumptions

- [x] CHK031 - Is "module creation always starts with empty content" validated? → Confirmed by frontend docs POST body: `"content": []`. [Assumption, Spec §Assumptions]
- [x] CHK032 - Is PATCH content validation exemption formally documented? → FR-009 parenthetical and Validation Rule Applicability table both document this. [Assumption, Spec §FR-005 vs FR-009]
- [x] CHK033 - Are ModuleTypeConstraints and PageSizeDimensions formally referenced? → Already referenced by name in FR-006, FR-007, FR-009. [Dependency, Spec §FR-006/FR-007]

## Notes

- All 33 items resolved in a single pass.
- Key spec additions: FR-016 (malformed JSON), FR-017 (localization), FR-018 (always-run grid validation), Validation Rule Applicability table, 2 new US2 acceptance scenarios, 3 new edge cases, 2 new assumptions.
- Decisions to keep in contracts-only: response model fields, FluentValidation constraints, content serialization format.
