# Recommendation formats

Reference for audit step 10.

## 10. Produce actionable recommendations

Rank findings by priority:

- **P0 — Critical:** Security vulnerability, authorization bypass, tenant isolation failure, data loss/corruption, payment/financial integrity failure, irreversible workflow failure, critical compliance issue.
- **P1 — High:** Core business rule, critical user flow, major integration contract, important error path, concurrency/idempotency risk, high-value domain invariant, ineffective or bypassable required CI gate.
- **P2 — Medium:** Important edge case, resilience improvement, meaningful duplicated test cleanup, maintainability concern, non-critical integration gap, weak CI gate, missing regression ratchet, missing hygiene gate.
- **P3 — Low:** Naming, organization, optional simplification, low-risk cleanup, non-critical coverage improvement, advisory-only reporting improvements.

For every recommendation include:

| Priority | Area | Classification | Evidence | Risk/bug prevented | Recommended test level or pipeline stage | Proposed scenario or removal rationale | Suggested action |
|---|---|---|---|---|---|---|---|

Use one of these classifications only:

- Missing coverage
- Weak assertion
- Duplicate test
- Meaningless test
- Excessive mocking
- Incorrect test layer
- Flaky/non-deterministic test
- Configuration/CI gap
- Coverage-reporting gap
- Intentional non-coverage
- Ineffective CI gate
- Missing CI gate
- Advisory-only CI check
- Gate bypass risk
- Coverage metric mismatch
- Missing regression ratchet
- Missing test-strength gate
- Missing test-hygiene gate
- Missing contract/architecture gate

For each proposed new test, provide a short test charter:

- **Behaviour:** The domain rule, workflow, policy, or user outcome.
- **Given:** Relevant state and preconditions.
- **When:** The action, event, request, or user interaction.
- **Then:** Observable outcome.
- **Why this layer:** Why Unit, Integration, or E2E is the lowest suitable level.
- **Bug it would catch:** A concrete regression.
- **Existing overlap:** Tests reviewed that do or do not already cover the behaviour.
- **Unique value:** Why this test is not duplicate coverage.
- **Dependencies:** Required database, emulator, container, fake, browser, or other setup if applicable.

For each removal recommendation, provide:

- **Test to remove:** File path and test name.
- **Classification:** Duplicate, meaningless, wiring-only, framework-only, static structure, or other.
- **Reason:** Why it lacks unique meaningful regression protection.
- **Retained coverage:** The test that already covers the same behaviour, if applicable.
- **Removal safety:** Why deleting it does not leave an important risk unprotected.
- **Replacement needed:** Yes or no.
- **Action:** Remove, merge, parameterize, move, rewrite, or retain.

For every CI gate recommendation, provide:

- **Current state:** Missing, advisory, partially enforced, ineffective, or effective.
- **Target state:** The desired enforced gate behaviour.
- **Risk addressed:** What regression, quality failure, or CI failure mode it prevents.
- **Scope:** All code, changed code, high-risk modules, selected projects, scheduled analysis, or release pipeline.
- **Execution stage:** Pull request, merge/main, nightly, release, or manual audit.
- **Failure mode:** Exact condition that must produce a non-zero exit code.
- **Merge enforcement:** Whether required status-check/branch protection evidence exists or must be configured outside the repository.
- **Bypass protection:** How missing artifacts, skipped jobs, soft failures, or configuration flags are handled.
- **Performance:** Expected runtime and how the gate remains practical.
- **Rollout:** Observe-only baseline, warning, soft gate, hard gate, and threshold ratchet where appropriate.

Do not generate implementation code unless explicitly asked. Focus first on the audit and prioritized test plan.
