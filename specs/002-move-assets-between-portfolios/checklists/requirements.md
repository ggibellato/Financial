# Specification Quality Checklist: Move Assets Between Portfolios

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-08-21
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

**Iteration 1 (2026-08-21)** — one item failed: three `[NEEDS CLARIFICATION]` markers remained on
decisions with no safe default (cross-broker moves, duplicate asset in the destination, and how
empty-portfolio deletion is offered).

**Iteration 2 (2026-08-21)** — all items pass. The three questions were put to the user and each
answer confirmed the spec's working assumption:

| Question | Answer | Encoded in |
|----------|--------|------------|
| Cross-broker moves | Same broker only | FR-007, Assumptions |
| Destination already holds the same asset | Reject the move; never merge | FR-008, Assumptions, US1 scenario 5 |
| Empty-source deletion | Offered as the move finishes **and** available standalone at any time | FR-023 – FR-027, US4 (rewritten narrative + 7 scenarios) |

Requirements were renumbered when the third answer split FR-023 into four (FR-023 – FR-027).

**Iteration 3 (2026-08-21)** — all items still pass after the user added drag-and-drop as an
explicit requirement for both front ends: drop on a portfolio to move into it, drop on the broker to
be asked for a new portfolio name.

Added: User Story 4 (P4, 8 scenarios), FR-028 – FR-039, SC-008 – SC-010, six edge cases, and four
assumptions. The pre-existing cleanup story moved to User Story 5 (P5) and the availability
requirements to FR-040 – FR-042. FR-001 – FR-042 and SC-001 – SC-010 are contiguous with no gaps or
duplicates, and each is defined exactly once.

One consequence is recorded in Assumptions rather than resolved: Active Investments and Historic
Investments are separate views that are never on screen together, so no drag can express the
Active → Historic crossing. Archiving a closed asset (User Story 3) stays with the dialog route.

**Iteration 4 (2026-08-21)** — spec amended after Phase 0 research verified the live data: brokers
are stored per scope and the two sets are not mirrors, so archiving a broker's first closed asset must
create its Historic record. Added FR-043, a sixth User Story 3 scenario, and corrected the
"No broker lifecycle" assumption. All items still pass; FR-001 – FR-043 contiguous.

Spec is ready for `/speckit-tasks`.
