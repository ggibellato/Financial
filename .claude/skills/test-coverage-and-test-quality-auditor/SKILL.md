---
name: test-coverage-and-test-quality-auditor
description: Use when auditing a repository's automated test coverage, test quality, and CI test gates. Produces an evidence-based audit of existing tests, tooling, gaps, tests to remove or rewrite, and whether CI quality gates are effective.
disable-model-invocation: true
---

# Purpose

Investigate the current repository's automated test coverage, test quality, and CI test-quality gates. Produce an evidence-based audit of the tests that exist, the technology and frameworks in use, what is missing, which tests should be improved, moved, consolidated, or removed, and whether CI quality gates are effective.

Do not make assumptions from directory names alone. Inspect repository files, package manifests, project files, test configuration, CI workflows, coverage reports, gate scripts, and representative test files before reaching conclusions.

The purpose is not to maximize line coverage. The purpose is to improve confidence that important application behaviour will not regress. A test that is meaningless, redundant, or fails to protect a unique meaningful behaviour is technical debt. Do not preserve tests merely because they contribute to a coverage percentage.

# Workflow

Perform the steps in order. Before each step, read the reference file named in it. After each step, append that step's findings to a scratch notes file (one section per step). Do not write the final report until every step 1-10 has a notes section; if a step does not apply, record why. Assemble the final report from the notes using the Required output format below.

1. **Map the application and technology stack.** Detailed instructions below.
2. **Inventory and classify every automated test** as Unit, Integration, E2E, or Unclear / mixed by actual scope. Read `references/test-quality-taxonomy.md` (§2).
3. **Identify tools at each test level** using only tools found and used. Read `references/test-quality-taxonomy.md` (§3).
4. **Audit CI test and quality gates**: execution, coverage, test-strength, behaviour/delivery gates, and whether each is enforceable and merge-blocking. Read `references/gate-quality-rules.md` (§4).
5. **Assess coverage using TDD principles** (Red-Green-Refactor). Read `references/risk-checklist.md` (§5).
6. **Audit test quality and identify tests to remove**: duplicate, meaningless, excessive mocking, wrong layer, flaky. Read `references/test-quality-taxonomy.md` (§6).
7. **Evaluate coverage by risk**: what is and is not worth testing. Read `references/risk-checklist.md` (§7).
8. **Interpret coverage reports correctly**, separating reported from enforced. Read `references/gate-quality-rules.md` (§8).
9. **Apply the test retention decision** to every reviewed test. Read `references/test-quality-taxonomy.md` (§9).
10. **Produce actionable recommendations** with priorities, charters, removal and gate recommendations. Read `references/recommendation-formats.md` (§10).

## Step 1 detail

## 1. Map the application and technology stack

Inspect, as applicable:

- Root and nested `package.json` files.
- `.sln`, `.csproj`, `.fsproj`, `.vbproj`, `Directory.Build.*`, and `global.json`.
- Python, Java, Go, Ruby, PHP, Rust, or other language manifests.
- Dockerfiles and Docker Compose files.
- Infrastructure files, including Terraform, CloudFormation, CDK, Pulumi, Serverless Framework, SAM, Kubernetes manifests, and deployment scripts.
- Frontend build configuration.
- Backend, API, worker, serverless, desktop, and application startup/configuration files.
- CI/CD workflows, including GitHub Actions, Azure DevOps, GitLab CI, CircleCI, Jenkins, and similar tools.
- Test runner configuration.
- Coverage configuration.
- Gate scripts and quality-check scripts.
- Existing coverage reports, mutation reports, test-result artifacts, and quality-gate artifacts, if available.

Identify:

1. Languages and runtimes.
2. Application types:
   - API
   - SPA
   - SSR application
   - Desktop application
   - Mobile application
   - Background worker
   - Queue consumer
   - CLI
   - Serverless function
   - Library
   - Infrastructure-as-code
3. Architectural boundaries:
   - Domain layer
   - Application/service layer
   - API/controllers/handlers
   - Persistence/repositories
   - External service adapters
   - Queue/message handlers
   - Frontend components/hooks/state
   - Infrastructure/deployment code
4. Test runners and assertion libraries.
5. Mocking, stubbing, and fake libraries.
6. Browser automation tools.
7. API/integration test tools.
8. Database/container/emulator tools.
9. Coverage tools and coverage output formats.
10. Mutation-testing tools and configuration.
11. Test commands available locally and in CI.
12. CI workflows, jobs, reusable actions, scripts, and stages that run tests or quality checks.
13. Whether separate test projects, folders, tags, naming conventions, scripts, or CI jobs exist for Unit, Integration, and E2E tests.

For every identified tool, framework, command, workflow, or technology, include repository evidence:

- File path.
- Package/project reference.
- Configuration section.
- Script name.
- CI workflow/job name.
- Test import or usage example.

If the repository does not contain sufficient evidence, state exactly:

> Not identified from repository evidence.

Do not infer that a package is actively used merely because it exists in dependencies. Confirm use from configuration, scripts, imports, test files, or CI.

# Core principles

Apply these principles throughout the audit:

- Test observable behaviour, business outcomes, and domain rules rather than implementation details.
- Prefer tests that fail when a real defect is introduced.
- Treat line, statement, function, and branch coverage as discovery signals, not quality measures.
- Prioritize branch coverage for business logic, permission checks, error handling, boundary conditions, and state transitions.
- Avoid recommending tests merely because a source file has low coverage.
- Do not recommend tests for framework guarantees, trivial delegation, static structure, or framework rendering behaviour.
- Distinguish clearly between:
  - Missing test coverage for meaningful behaviour.
  - Existing but weak or meaningless tests.
  - Duplicate coverage.
  - Deliberate non-coverage that is appropriate.
- Prefer a smaller suite of high-signal tests over a large suite of brittle or repetitive tests.
- Every retained test must justify its existence by identifying the unique bug, risk, branch, contract, or workflow failure it can detect.
- When a test is duplicate or meaningless, recommend removing it unless it has a clearly documented unique purpose.
- Prefer deletion over rewriting when the test cannot be made behaviour-focused without duplicating coverage already provided elsewhere.
- Do not preserve a test merely because it increases a coverage percentage.
- Every recommended new test must state the concrete bug or regression it would catch.
- A test should test application behaviour, not framework behaviour.
- Tests should focus on the application domain, domain rules, business workflows, security boundaries, data integrity, and externally observable outcomes.
- Prefer testing domain rules below the UI whenever the behaviour can be proven without rendering a component or driving a browser.
- Use UI and E2E tests only when they prove a meaningful user-facing or cross-layer workflow that cannot be adequately proven at a lower level.
- Treat four or more meaningful mocks, stubs, or spies in a test as a strong warning that the test may be testing wiring rather than behaviour.
- A high mock count is a heuristic, not an automatic failure. Determine whether the test proves a meaningful policy, branch, transformation, error path, or security decision before deciding to retain, rewrite, convert, or remove it.
- For each valuable test, ask: “Would this test fail if the relevant business branch, comparison, permission condition, error mapping, or state transition were intentionally broken?” If not, classify it as weak, duplicate, or meaningless unless another clear purpose exists.
- CI test gates must be evaluated separately from test execution. A pipeline that runs tests or produces a coverage report is not necessarily enforcing a quality gate.
- A test gate is effective only if it has a defined threshold or failure condition, runs on relevant pull requests or protected branches, and returns a non-zero exit code that blocks merge or deployment when the condition is not met.
- Do not treat overall line coverage as the primary measure of test quality. Line coverage shows that code executed; it does not prove that assertions would detect a behavioural regression.
- Prefer risk-based, diff-aware gates for changed code over repository-wide percentage targets that penalize unrelated work in legacy areas.
- Use branch coverage to assess conditional and decision-heavy code. A line can execute while important branches remain untested.
- Use mutation testing selectively for high-risk domain/application logic to assess whether tests detect deliberately introduced behavioural changes.
- Do not recommend one universal coverage percentage as a substitute for test quality. Use layered gates that each address a distinct risk.
- A report-only CI check is not a gate. If it cannot fail the relevant pipeline, classify it as observability only.
- A gate that can be bypassed by a non-blocking job, `continue-on-error`, `allow_failure`, ignored exit code, absent branch protection, or an optional workflow is not effective for merge protection.
- Gates must be proportionate to feedback cost. Fast checks belong on pull requests; expensive checks such as full mutation testing may belong on changed high-risk modules, scheduled workflows, or release pipelines.

# Required output format

Return the audit using exactly these sections.

## 1. Executive Summary

Include:

- Overall assessment of test health.
- Overall assessment of CI test-gate health.
- Main application technologies identified.
- Test frameworks and tools identified.
- Whether Unit, Integration, and E2E coverage exists.
- Whether coverage data was found and whether it is trustworthy.
- Whether a CI quality gate exists, what it measures, and whether it can be verified as merge-blocking.
- The three most important test risks or gaps.
- The three most important CI gate risks or gaps.
- A concise statement about whether current coverage appears behaviour-focused or coverage-number-focused.
- Number of tests/suites recommended for removal.
- Number of tests/suites recommended for merge or parameterization.
- Number of tests/suites recommended for rewrite, move, or conversion to integration.
- Estimated test-suite simplification opportunity.
- The highest-priority actions.

## 2. Technology and Test Tooling

Include:

- Application stack table.
- Architecture/boundary map.
- Test tooling table grouped by Unit, Integration, and E2E.
- Test commands.
- Coverage commands.
- Mutation-testing commands, if discovered.
- CI/CD test execution summary.
- Evidence paths for every technical claim.
- Potential but unverified tools, clearly distinguished from active tools.

## 3. CI Test Gate Assessment

Include a complete inventory of discovered CI test and quality checks.

Start with this table:

| Gate/check | CI workflow/job | Trigger | Metric or rule | Scope | Threshold/baseline | Can fail CI? | Merge-blocking? | Bypass risks | Effectiveness | Recommendation | Evidence |
|---|---|---|---|---|---|---|---|---|---|---|---|
| ... | ... | ... | ... | ... | ... | Yes / No / Unclear | Yes / No / Unclear | ... | Effective / Partial / Ineffective / Missing | Retain / Strengthen / Replace / Add / Remove | ... |

### Current gate verdict

State clearly:

- Whether a test-quality gate exists.
- Whether it is a real enforced gate, an advisory check, or cannot be verified.
- Whether it primarily measures line execution, branch execution, assertion strength, contract compatibility, architecture, or determinism.
- Whether it is appropriately scoped to changed code and high-risk modules.
- Whether it blocks merges/deployments, or whether merge blocking cannot be verified from repository evidence.
- Whether it fails closed when expected test, coverage, or mutation artifacts are absent.

### Gate weaknesses

Identify weaknesses such as:

- Line-only coverage gate.
- Repository-wide threshold that blocks unrelated legacy work.
- No branch coverage signal.
- No diff coverage.
- No ratchet/regression protection.
- Mutation testing that is report-only or has no fail threshold.
- Mutation testing that is too broad or slow for pull requests.
- Missing artifact validation.
- Optional or skipped CI job.
- `continue-on-error`, `allow_failure`, ignored exit code, or unconditional success.
- Missing branch-protection evidence.
- Missing gates for critical contracts, architecture, or determinism.
- A gate that incentivizes meaningless tests.
- A gate that does not run on relevant changed paths or branches.

### Recommended gate design

Provide a layered, risk-based recommendation. Do not propose a single replacement percentage as the answer.

For each recommendation, include:

| Priority | Proposed gate | Risk addressed | Scope | PR/main/nightly/release stage | Blocking or advisory | Initial threshold/baseline | Tooling evidence or candidate tool | Runtime impact | Rollout approach |
|---|---|---|---|---|---|---|---|---|---|

Use this default ordering unless repository evidence justifies another sequence:

1. Ensure tests themselves are required and fail the pipeline on test failure.
2. Replace or supplement line-only gates with diff-aware coverage, preferring branch coverage for changed decision-heavy code.
3. Add a coverage ratchet so coverage cannot silently regress.
4. Add targeted mutation testing for changed high-risk domain/application code or run it on a scheduled pipeline.
5. Add contract, architecture, acceptance-criteria traceability, and test-hygiene gates where relevant to the repository.
6. Keep expensive checks out of the critical PR path unless runtime is proven acceptable.

Clearly label all proposed thresholds as:

- Discovered existing threshold.
- Suggested starting threshold.
- Baseline to measure before enforcing.
- Not enough repository evidence to recommend a threshold.

Do not invent a specific target such as 80%, 90%, or 100% without labelling it as a suggested starting point and explaining why it is appropriate.

## 4. Test Suite Classification

Include:

- Count of test files/suites by Unit, Integration, E2E, and Unclear/Mixed.
- A table of all identified test projects/suites and their classification.
- Classification rationale.
- Runtime dependencies for each suite.
- Any mismatch between folder/project naming and actual scope.
- Missing test levels.
- Tests that are incorrectly categorized or mix layers.

## 5. Coverage and TDD Assessment

Include:

- Available coverage metrics and their limitations.
- Coverage thresholds, exclusions, and CI gates.
- Important domain, security, data-integrity, error-handling, concurrency, external-integration, and workflow areas reviewed.
- Gaps organized by risk and priority.
- Existing tests that demonstrate good TDD-aligned behavioural testing.
- Tests coupled to implementation details or unable to catch meaningful regressions.
- Whether tests are likely to survive internal refactoring.
- Areas where branch coverage matters more than line coverage.
- Areas intentionally not recommended for testing and why.
- Whether the current CI gate rewards meaningful behavioural tests or can be satisfied by meaningless execution-focused tests.

## 6. Test Quality Findings and Removal Plan

Start with this summary table:

| Action | Count | Reason |
|---|---:|---|
| Remove | ... | Meaningless, duplicate, wiring-only, framework-only, static-output, or no-value tests |
| Merge/parameterize | ... | Repeated cases without distinct branches, boundaries, or risks |
| Rewrite | ... | Valuable intent but implementation-coupled, weakly asserted, or over-mocked |
| Move/convert | ... | Valid behaviour tested at the wrong layer |
| Retain | ... | Unique, meaningful regression protection |

Include the following subsections:

### Duplicate coverage

For each duplicate finding, state exactly which test should be removed, merged, or parameterized and which test should remain.

### Meaningless or framework-focused tests

For each finding, state whether the test should be removed or replaced by a specific behavioural test. Do not propose replacements when there is no meaningful behaviour to protect.

### Excessive mocking

For each finding, state mock count, what is mocked, and whether to remove, rewrite, use fakes, or convert to integration testing.

### Incorrect test layer

For each finding, state current layer, recommended layer, and the action: remove, move, split, rewrite, or retain.

### Flaky or non-deterministic tests

For each finding, state the instability source, risk, and concrete stabilization or removal action.

## 7. Prioritized Test and Gate Plan

Provide the prioritized recommendations table.

Then provide the top five proposed test charters in Given/When/Then form.

For every proposed test, explain:

- The exact domain behaviour or critical flow.
- Why the chosen test layer is appropriate.
- The concrete bug it would catch.
- Why it does not duplicate existing coverage.

Then provide the recommended CI gate rollout sequence:

1. Immediate P0/P1 CI enforcement fixes.
2. Fast pull-request gates.
3. Main-branch or nightly checks.
4. Release-pipeline checks.
5. Baseline/ratchet and threshold tightening plan.
6. Tests to remove or consolidate before adding coverage targets.

## 8. Coverage Commands and Next Steps

Include:

- Exact discovered commands for:
  - Unit tests.
  - Integration tests.
  - E2E tests.
  - Coverage generation.
  - Mutation testing.
  - CI-equivalent test execution.
- Clearly labelled recommended commands if the repository does not define commands.
- Required local dependencies:
  - Database.
  - Containers.
  - Environment variables.
  - Browser binaries.
  - Service emulators.
  - Test credentials.
  - Seed data.
- The smallest high-value sequence of changes, starting with P0 and P1 risks.
- Recommended removal/consolidation sequence before adding new tests.
- Recommended CI gate implementation sequence, including what should be measured before it becomes merge-blocking.

# Guardrails

- Never fabricate technologies, test frameworks, coverage results, mutation results, file paths, commands, CI behaviour, classifications, gate enforcement, or test findings.
- Cite repository evidence for every technical claim.
- Do not call a CI step a gate merely because it runs tests, uploads coverage, posts a PR comment, or has “gate” in its name.
- Do not claim a CI check blocks merges unless repository evidence proves required status-check, branch-protection, merge-policy, or equivalent enforcement. Otherwise state that merge-blocking enforcement cannot be verified from repository evidence.
- Treat `continue-on-error`, `allow_failure`, ignored exit statuses, unconditional success steps, optional jobs, path-filter exclusions, manual-only triggers, missing artifact fallbacks, and skipped jobs as potential gate bypasses.
- A missing, empty, stale, malformed, or incomplete test/coverage/mutation artifact must not result in a successful gate unless the repository explicitly documents why that is safe.
- Do not classify a test as duplicate without naming the overlapping behavioural coverage and the retained test.
- Do not label a test meaningless merely because it is simple; explain why it cannot detect a meaningful regression.
- If a test is classified as meaningless or duplicate, the default recommendation must be **Remove**.
- Do not recommend “keep for coverage” as a justification.
- Do not replace a deleted test unless a specific meaningful behaviour would otherwise become unprotected.
- A replacement test must be behaviour-focused and identify the concrete regression it detects.
- Do not recommend broad UI snapshots, shallow-render tests, static-markup tests, or source-text tests by default.
- Do not recommend tests for framework behaviour unless the application adds a custom policy, mapping, contract, security boundary, or meaningful behaviour on top.
- Prefer testing domain rules beneath the UI when the rule can be isolated.
- Use browser E2E tests only for a small set of critical workflows that cannot be proven adequately below the UI.
- Treat four or more mocks as an investigation trigger, not an automatic failure.
- Do not treat a high coverage percentage as evidence of test quality.
- Do not recommend an overall line-coverage percentage as the sole or primary test-quality gate.
- When recommending coverage gates, prefer changed-code/diff coverage and branch coverage for changed decision-heavy code where tooling supports it.
- Do not claim branch coverage proves assertion quality.
- Do not claim mutation testing is automatically suitable for every pull request; assess runtime, scope, equivalent-mutant noise, and developer feedback cost.
- Do not call mutation testing a gate unless it has an explicit failing threshold or non-zero exit condition.
- Do not recommend a numeric threshold without marking it as discovered, baseline-derived, or a suggested starting point.
- Do not allow a coverage target to incentivize framework tests, static UI tests, snapshots, mock-interaction tests, or other meaningless coverage.
- Prefer layered gates: test execution, diff/branch coverage, regression ratchet, targeted mutation testing, contract/architecture validation, and test-hygiene checks where relevant.
- Keep fast, actionable checks in pull requests. Move expensive whole-repository analysis to main-branch, nightly, or release workflows when necessary.
- Do not recommend test additions simply to meet a numerical coverage threshold.
- If a conclusion is uncertain, label it as uncertain and explain what evidence would resolve it.
- If coverage cannot be run, state that clearly and provide a static audit rather than inventing metrics.
- Before recommending removal, identify the retained test that covers the same behaviour, or explicitly state that the removed test protected no meaningful behaviour.
- Prefer safe deletion of redundant test debt over retaining noisy tests that slow refactoring and obscure important coverage.
