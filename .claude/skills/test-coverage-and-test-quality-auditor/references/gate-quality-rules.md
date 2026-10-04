# CI gate quality rules and coverage-report interpretation

Reference for audit steps 4 and 8.

## 4. Audit CI test and quality gates

Inspect all available CI/CD configuration and branch-protection-related repository configuration.

Inspect, where present:

- GitHub Actions workflows under `.github/workflows`.
- GitLab CI configuration.
- Azure DevOps pipelines.
- CircleCI configuration.
- Jenkinsfiles.
- Buildkite configuration.
- Bitbucket pipelines.
- Makefiles, task runners, scripts, and reusable workflow actions invoked by CI.
- Coverage-gate scripts.
- Quality-gate scripts.
- Mutation-testing configuration.
- PR status-check configuration stored in the repository.
- Documentation that defines merge, release, or deployment requirements.

Determine whether each of the following exists.

### Test execution gates

- Unit tests.
- Integration tests.
- E2E tests.
- Architecture tests.
- Contract tests.
- Security-focused tests.
- Migration/database tests.
- Smoke tests.
- Regression tests.

### Coverage gates

- Overall line coverage.
- Overall branch coverage.
- Overall function/method coverage.
- Per-file or per-project coverage.
- Diff/patch coverage for changed code.
- Diff/patch branch coverage.
- Coverage regression prevention.
- Coverage ratchet baseline.
- High-risk module-specific thresholds.
- Coverage exclusions and exclusion validation.

### Test-strength gates

- Mutation testing.
- Mutation score threshold.
- Mutation test-strength threshold.
- Changed-module or changed-file mutation testing.
- Incremental mutation testing.
- Scheduled mutation testing.
- Mutation score regression protection.

### Behaviour and delivery gates

- Acceptance-criteria traceability.
- Contract/OpenAPI snapshot validation.
- API compatibility validation.
- Architecture/layering validation.
- Test-tag or test-category enforcement.
- Test-hygiene checks.
- Flaky-test detection or quarantine controls.
- Static analysis that protects data access or security boundaries.
- Required test-result artifact publication.

### Gate enforceability

For every discovered check, determine:

- Does it run on pull requests?
- Does it run on the target/default branch?
- Does it run before deployment or release?
- Does it evaluate a defined condition?
- Does it return a non-zero exit code when the condition fails?
- Is `continue-on-error`, `allow_failure`, ignored exit status, or equivalent configured?
- Is the job marked required by branch protection or merge policy, where repository evidence is available?
- Is the workflow optional, manually triggered, scheduled only, or conditionally skipped?
- Can the gate be bypassed by path filters, branch filters, missing dependencies, fallback values, environment flags, conditional execution, or unconditional success steps?
- Does the gate inspect the correct test, coverage, or mutation artifact?
- Does the gate fail closed when an expected artifact is missing, malformed, stale, empty, incomplete, or unreadable?
- Does the gate use a trustworthy comparison baseline for diff coverage, ratchet, or mutation checks?
- Is the gate’s runtime appropriate for its pipeline stage?

Do not assume a workflow is a gate merely because it has a name such as `coverage`, `quality`, `test`, `validation`, or `check`.

A CI check is a merge-blocking quality gate only when repository evidence shows that:

1. It executes for relevant pull requests or merge candidates.
2. It evaluates a defined pass/fail condition.
3. It exits non-zero when the condition fails.
4. Its failure blocks merge or deployment through required status checks, branch protection, or an equivalent enforced policy, where evidence is available.

If branch protection or external repository settings are not visible from the repository, state exactly:

> Merge-blocking enforcement cannot be verified from repository evidence.

Do not claim that a check blocks merges unless that enforcement can be verified.

### Gate effectiveness criteria

Evaluate every discovered test or quality gate against the following dimensions:

| Dimension | Questions to answer |
|---|---|
| Purpose | What risk is this gate intended to prevent? |
| Signal | Does it measure execution, branching, assertion strength, behavioural contract, architecture, determinism, or security? |
| Scope | Does it apply to the whole repository, changed code, selected projects, critical modules, or a release candidate? |
| Threshold | Is there a clear threshold, baseline, allowed regression, or explicit pass/fail rule? |
| Enforcement | Can the check fail CI with a non-zero exit code? |
| Merge protection | Is it required for relevant pull requests or deployment? State uncertainty if branch protection cannot be inspected. |
| Bypass risk | Can it be skipped, neutralized, ignored, or bypassed through workflow configuration? |
| Artifact integrity | Does it consume a current, complete, correctly located test/coverage/mutation artifact? |
| Feedback cost | Is the runtime appropriate for PR, main-branch, nightly, or release execution? |
| Actionability | Does failure identify the changed module, branch, behaviour, or test gap that developers should address? |
| False confidence risk | Could the check pass while meaningful behaviour remains untested? |
| Recommendation | Retain, strengthen, replace, split, move to another pipeline stage, or remove. |

Classify every discovered check as exactly one of:

- Effective merge-blocking gate.
- Effective non-blocking observability check.
- Partially effective gate.
- Ineffective gate.
- Missing gate.
- Cannot verify from repository evidence.

A pipeline step that only uploads a report, posts a comment, emits a warning, or marks a job successful regardless of results must be classified as:

> Effective non-blocking observability check

not as a merge-blocking gate.

### Gate quality rules

#### Overall line coverage

Overall line coverage may be retained as a low regression tripwire or historical trend, but it is not sufficient as the primary quality gate.

Flag an overall line-coverage-only gate as weak when it:

- Has no branch coverage signal.
- Has no changed-code/diff coverage signal.
- Rewards execution without meaningful assertions.
- Encourages tests for trivial code, framework behaviour, or static output.
- Blocks unrelated work because of historic uncovered legacy code.
- Can be satisfied by shallow tests, snapshots, or mock-interaction tests.
- Has an arbitrary target without relation to risk or an existing baseline.

Do not recommend removing line coverage reporting. Instead, recommend reducing it to one of:

- A low safety floor.
- A non-blocking trend.
- A ratchet baseline that cannot decrease.
- A supporting metric alongside stronger gates.

#### Branch coverage

Branch coverage is a stronger inexpensive signal for decision-heavy code because it reveals whether alternative conditional paths executed.

Prefer branch coverage over line coverage for:

- Domain rules.
- Permission decisions.
- State machines.
- Error handling.
- Input boundaries.
- Retry/fallback logic.
- Calculations.
- Data transformations.
- Security-sensitive code.

Do not use branch coverage as the sole measure of test quality. It still proves execution, not assertion strength.

#### Diff or patch coverage

Prefer diff-aware coverage for pull requests when tooling supports it.

A diff coverage gate should:

- Evaluate only lines or branches changed by the pull request.
- Avoid penalizing unrelated legacy code.
- Require meaningful coverage for changed logic.
- Treat generated files, migrations, static configuration, and approved exclusions appropriately.
- Be paired with review of uncovered changed branches in high-risk code.
- Fail clearly when the coverage artifact or diff comparison base is unavailable.

Prefer diff branch coverage when available. If only diff line coverage is available, use it as an interim improvement and report the limitation.

Do not impose a universal threshold without repository evidence. Recommend an initial threshold based on the existing baseline, risk profile, and false-positive rate.

#### Coverage ratchet

A coverage ratchet prevents regression without imposing an arbitrary global target.

A ratchet should:

- Store a committed or otherwise trusted baseline.
- Compare current coverage against the baseline.
- Fail when agreed metrics decrease beyond a documented tolerance.
- Be updated intentionally through reviewed changes.
- Prefer branch and/or diff metrics for high-risk modules.
- Avoid silently updating the baseline in the same CI run that evaluates it.

Flag a ratchet as weak if it only tracks line coverage or can be automatically lowered without review.

#### Mutation testing

Mutation testing evaluates whether tests detect deliberately introduced changes, such as altered comparisons, conditions, return values, or statements.

Use mutation testing to assess assertion strength in:

- Domain logic.
- Application services.
- Financial calculations.
- Authorization and policy code.
- Tenant isolation logic.
- Data transformations.
- Parsers.
- State machines.
- High-risk error-handling logic.

Mutation testing is not usually appropriate as a full-repository, every-PR gate when it makes feedback too slow.

Prefer one of:

- Mutation testing on changed high-risk modules in pull requests.
- Incremental mutation testing with a trusted cache or baseline.
- Scheduled mutation testing on selected domain/application projects.
- Release-pipeline mutation testing for critical code.
- Report-only mutation testing initially, followed by a threshold once runtime and equivalent-mutant noise are understood.

A mutation-testing step is not a gate unless it has a configured failure threshold or explicit non-zero exit condition.

When mutation testing is used, verify:

- The baseline test suite passes before mutation execution.
- The mutation tool targets production code, not test code.
- It excludes generated code and known equivalent-mutant areas with documented justification.
- It has an explicit break/fail threshold.
- It runs a suitable subset of tests.
- Runtime is measured and appropriate for the execution stage.
- Surviving mutants are actionable and reviewed.
- The score is not blindly optimized by adding brittle or implementation-coupled tests.

#### Acceptance-criteria traceability

If the repository uses PRDs, user stories, acceptance-criterion identifiers, test traits, tags, annotations, or naming conventions, assess whether critical acceptance criteria are traceable to tests.

A traceability gate may verify that:

- Every active acceptance criterion has at least one relevant automated test.
- Test identifiers reference valid acceptance-criterion identifiers.
- Removed or renamed criteria do not leave stale test tags.
- Critical requirements have the required level of test coverage.

Do not treat tag presence alone as proof that a test meaningfully verifies the criterion. Traceability complements behavioural review; it does not replace it.

#### Contract and architecture gates

Assess whether the repository has gates for:

- OpenAPI/schema compatibility.
- Public API contract changes.
- Consumer/provider contracts.
- Database migration validation.
- Architecture dependency rules.
- Layering boundaries.
- Forbidden references.
- Generated-client freshness.
- API snapshot intentionality.

A useful contract or architecture gate must fail when an unintended contract or layering change occurs. A report-only snapshot or architecture result is not sufficient.

#### Test hygiene and determinism gates

Assess whether CI detects common causes of flaky or low-quality tests, including:

- Fixed delays such as `Task.Delay`, `Thread.Sleep`, `setTimeout`, or equivalent.
- Direct use of wall-clock time such as `DateTime.Now`, `DateTime.Today`, `Date.now`, or equivalent where an injectable clock is expected.
- Randomness without a controlled seed.
- Focused/exclusive tests committed accidentally.
- Skipped tests without a documented reason and expiry.
- Live network calls in unit/integration tests.
- Tests that share mutable state or require execution order.
- Excessive retries that conceal flakiness.
- Missing test-result artifacts.

Prefer analyzers, lint rules, static checks, or focused scripts that fail only on newly introduced violations where legacy cleanup would otherwise be too disruptive.

## 8. Interpret coverage reports correctly

Before interpreting coverage metrics, determine whether they are merely reported or actively enforced by CI.

For each coverage threshold or gate, state:

- Metric: line, branch, function, statement, diff line, diff branch, mutation score, or other.
- Scope: repository-wide, project, directory, module, changed file, changed line, changed branch, or PR diff.
- Threshold or baseline.
- Whether it is a hard failure, soft warning, informational report, or PR comment only.
- CI workflow/job and triggering event.
- Whether enforcement is merge-blocking, non-blocking, or unverifiable from repository evidence.
- Whether the gate can be bypassed.
- Whether the artifact is current and fails closed when missing.

If coverage output exists, report:

- Line coverage.
- Statement coverage.
- Branch coverage.
- Function/method coverage.
- File/module coverage.
- Coverage thresholds, including whether they are enforced, advisory, or inactive.
- CI test and quality gates.
- Gate scope: overall repository, project, directory, module, changed code, or changed branches.
- Gate baselines, ratchets, and tolerated regression.
- Gate failure behaviour and bypass paths.
- Whether gate failures are merge-blocking or only visible as warnings.
- Coverage exclusions.
- Whether exclusions appear reasonable.
- Whether coverage is split by Unit, Integration, and E2E levels.

Identify:

- High-risk modules with low branch coverage.
- High-risk modules with no tests.
- Modules with high line coverage but weak assertions.
- Modules with high function coverage but untested error paths or branch conditions.
- Complex state machines with weak coverage.
- Authorization/security code with missing denial tests.
- Error mappings with missing failure-path tests.
- Modules that appear uncovered but are generated code, framework glue, static configuration, or intentionally excluded code.

Call out situations such as:

- 95% line coverage but no authorization-denial scenario.
- 100% coverage created by shallow rendering, snapshots, or mocked call assertions.
- High function coverage but no tested branches or error paths.
- Full happy-path coverage but no failure, boundary, or concurrency coverage.
- Low coverage in trivial configuration where additional tests have little value.
- Low coverage in generated code where testing may not be useful.
- E2E tests contributing to coverage but providing slow and imprecise feedback.
- A line-only gate that can be passed by meaningless tests.
- A coverage report that is uploaded but does not fail CI.
- A gate that succeeds when coverage or mutation artifacts are absent.

Do not conclude that a higher percentage means better tests.

If no executable coverage report can be generated in the current environment:

1. State that clearly.
2. Do not invent coverage metrics.
3. Provide a static assessment based on repository/test/source inspection.
4. List the exact discovered commands that should generate coverage.
5. If no coverage command is discovered, provide clearly labelled recommended commands based on the detected framework, but distinguish recommendations from discovered commands.
