# Test inventory, classification, and quality taxonomy

Reference for audit steps 2, 3, 6 and 9.

## 2. Inventory and classify every automated test

Find all test files, test projects, test suites, and test configurations.

Classify each test file, suite, or test project into exactly one primary category:

- Unit
- Integration
- E2E
- Unclear / mixed

Do not classify based only on directory names, project names, naming conventions, or file suffixes. Classify based on test scope, runtime dependencies, and the system boundary exercised.

Use these definitions:

| Level | Definition | Typical dependencies |
|---|---|---|
| Unit | Tests isolated application or domain logic and remains fast and deterministic | In-memory data, pure functions, limited boundary mocks/fakes; no real database, network, browser, filesystem, or process boundary |
| Integration | Tests collaboration between meaningful application boundaries | Real or containerized database, HTTP pipeline, filesystem, queue, service + repository, component + real context/provider, serialization, external adapter contract |
| E2E | Tests a complete user or system flow through public interfaces | Browser-driven flow, running application, complete API pipeline, authentication, persistence, real service boundaries, full workflow |
| Unclear / mixed | Scope is ambiguous or combines layers in a way that obscures intent | Excessive mocking, hidden external resources, browser-like tests without a clear system boundary, unclear setup, or a mixture of unrelated layers |

For each identified test file, suite, or project, record:

- Test file/project path.
- Test suite or test name where practical.
- Test runner/framework.
- Assertion library.
- Mocking, stubbing, or fake library.
- Browser/API/database/container tools.
- Runtime dependencies.
- Application layer or capability exercised.
- Whether it runs in CI.
- Whether it contributes to a coverage report.
- Classification.
- Classification rationale.
- Whether its directory/project name matches its actual scope.

Classification rules:

- If a test is stored under `unit` but uses a real database, network, browser, queue, filesystem, container, or full HTTP server, classify it as Integration or E2E based on actual scope and flag the naming mismatch.
- If a test is stored under `integration` but mocks every meaningful collaborator and only tests a single service or function, classify it as Unit or Unclear / mixed and flag the mismatch.
- If a test uses a browser but does not cross a meaningful user/system boundary, determine whether it is truly E2E or an expensive component test.
- If a test checks only a mock invocation, exact call arguments, internal method calls, or static output, classify its scope honestly and also flag it for quality review.
- If classification is uncertain, use Unclear / mixed and explain precisely what repository evidence is missing.

## 3. Identify tools at each test level

Produce a table using only tools actually found and used in the repository:

| Test level | Framework/runner | Assertions | Mocking/fakes | Runtime dependencies | Coverage tool | Local command | CI command/job | Evidence |
|---|---|---|---|---|---|---|---|---|
| Unit | ... | ... | ... | ... | ... | ... | ... | ... |
| Integration | ... | ... | ... | ... | ... | ... | ... | ... |
| E2E | ... | ... | ... | ... | ... | ... | ... | ... |

Rules:

- If a test level does not exist, say so clearly.
- If no coverage tool exists, state that coverage tooling was not identified.
- If a tool exists in dependencies but has no evidence of actual usage, list it under “Potential but unverified tooling”; do not list it as active test tooling.
- Do not invent commands. Use scripts, project configurations, build documentation, CI workflows, or clearly label a command as a recommended command rather than a discovered command.

## 6. Audit test quality and identify tests to remove

Inspect representative tests from every category, then audit all likely weak, duplicate, excessively mocked, flaky, or incorrectly placed tests.

A test must provide unique, meaningful regression protection to be retained.

### A. Duplicate tests

A test is duplicate when it validates the same behaviour, execution path, and failure mode as another retained test without adding useful fault isolation or protecting a distinct risk.

Examples include:

- The same business rule is covered by a domain unit test, API integration test, and E2E test, but the higher-level tests do not validate a distinct contract, boundary, or workflow risk.
- Several tests use different values but execute the same non-branching path.
- A function is tested directly and repeatedly through callers without a reason to preserve the extra coverage.
- Several frontend tests assert the same visible result after the same underlying flow.
- Multiple validator tests prove the same library-provided rejection behaviour.
- Multiple E2E tests perform the same user journey but differ only in cosmetic or non-branching input values.

For every duplicate finding:

1. Identify all overlapping tests by file path and test name.
2. Identify the exact overlapping behaviour and failure mode.
3. Identify the retained test and explain why it is the best level for that behaviour.
4. Determine whether each duplicate should be removed, merged, or parameterized.
5. Default to **Remove** when an existing test already protects the behaviour adequately.
6. Do not retain a duplicate test solely because it improves line coverage.
7. Retain overlapping tests only when they catch a distinct failure class, for example:
   - Unit test protects a domain-rule or calculation failure.
   - Integration test protects persistence, serialization, authorization middleware, API contract, or transaction failure.
   - E2E test protects browser-to-backend workflow failure, authentication session failure, or cross-layer user journey failure.

Required finding format:

| Classification | Duplicate test(s) to remove | Test to retain | Overlapping behaviour | Unique risk retained test covers | Why removal is safe | Action |
|---|---|---|---|---|---|---|
| Duplicate test | `path:test name` | `path:test name` | ... | ... | ... | Remove / Merge / Parameterize |

### B. Meaningless or low-value tests

A test is meaningless when it would not detect a meaningful application regression, or when it mainly confirms framework behaviour, implementation details, static structure, or test setup.

Flag tests that primarily verify:

- Framework behaviour:
  - Rendering mechanics.
  - Router mechanics.
  - Standard HTTP request handling.
  - ORM persistence mechanics.
  - Framework-provided loading behaviour.
  - Dependency injection mechanics.
- Static output without meaningful behaviour:
  - Labels.
  - Headings.
  - CSS classes.
  - Source text.
  - Snapshot output.
  - Response field presence.
  - Default UI state.
  - Static visual properties.
- Static structure:
  - Property existence.
  - Field existence.
  - Column types.
  - Type checks.
  - Default configuration.
  - Initial state values.
- Trivial code:
  - Getters.
  - Setters.
  - Constructors.
  - One-line wrappers.
  - Pure delegations.
  - Single-path utilities with no branch, error path, edge case, or meaningful transformation.
- Exact private implementation details:
  - Private method calls.
  - Internal object layout.
  - Incidental call order.
  - Internal framework hooks.
- Mock interaction only:
  - A dependency was called.
  - Arguments were forwarded unchanged.
  - A callback was registered.
  - A method was invoked without transformation, policy, conditional logic, or error handling.
- Repeated validation-library behaviour:
  - Every invalid input is tested even though the application adds no custom business validation.
- Mirror tests:
  - The assertion repeats the same formula, branching, or logic as the implementation.
- Test fixtures, setup, configuration, or generated code instead of application behaviour.

For every meaningless-test finding:

1. Identify the file path and test name.
2. Explain why it cannot catch a meaningful defect.
3. State what behaviour it claims to protect, if any.
4. State whether another test already protects that behaviour.
5. State what meaningful regression it would fail to detect.
6. Choose exactly one action:
   - **Remove** — default when the test has no unique value.
   - **Replace** — only when a meaningful behavioural test is missing.
   - **Move** — only when the test checks a valid behaviour at the wrong level.
   - **Retain** — only with a concrete explanation of its unique regression value.
7. Do not propose a replacement test if no meaningful behaviour needs protection.

Required finding format:

| Classification | Test to remove | Why it is meaningless | Regression it cannot detect | Existing meaningful coverage | Replacement needed? | Action |
|---|---|---|---|---|---|---|
| Meaningless test | `path:test name` | ... | ... | ... | No / Yes | Remove / Replace / Move / Retain |

### C. Excessive mocking

Four or more meaningful mocks, stubs, spies, or mocked application collaborators is a strong sign that a test may be verifying wiring rather than behaviour.

For every test with four or more meaningful mocks, stubs, or spies:

1. Count the meaningful test doubles.
2. Identify what each double represents.
3. Determine whether the test asserts a unique domain decision, transformation, conditional branch, security policy, error-handling outcome, or externally meaningful contract.
4. If the test only asserts calls, call order, or forwarded arguments, classify it as meaningless wiring coverage.
5. Recommend **Remove** unless a meaningful behaviour can be tested at a better layer.
6. If the behaviour is valuable but requires collaboration, recommend converting it to an integration test or replacing mocks with real in-process collaborators, fakes, or test implementations.
7. Permit mocks at genuine external boundaries when they enable deterministic tests:
   - HTTP clients.
   - Payment providers.
   - Cloud SDKs.
   - Email/SMS providers.
   - Clocks.
   - Randomness.
   - Filesystems.
   - Queues.
   - External APIs.
8. Do not retain a heavily mocked test solely because it executes application code.

Flag frontend tests that mock several of the following at once:

- Router.
- Context/provider.
- API client.
- Hooks.
- State manager.
- Child components.
- Authentication layer.

Such tests often prove only that mocks can be arranged to render expected static output.

Required action choices:

- **Remove** — wiring-only test with no unique behavioural value.
- **Rewrite** — isolated domain or application logic can be tested with fewer collaborators.
- **Convert to integration** — value comes from collaboration between real components.
- **Replace mocks with fakes** — a deterministic in-memory implementation would prove behaviour more effectively.
- **Retain** — only when mock interaction represents an intentional, externally meaningful policy, security contract, retry strategy, or side effect.

Required finding format:

| Classification | Test | Mock count | What is mocked | Why this is weak or valid | Recommended action | Proposed retained/replacement test |
|---|---|---:|---|---|---|---|
| Excessive mocking | `path:test name` | 5 | ... | ... | Remove / Rewrite / Convert to integration / Retain | ... |

### D. Incorrect layer placement

Flag tests that are in the wrong layer:

- Unit tests that use real database, network, browser, queue, filesystem, container, or full server resources.
- Integration tests that mock all meaningful collaborators and test only a single function.
- E2E tests that only verify a simple API response, static page, or non-critical single component.
- UI tests that are actually domain-rule tests and should move to domain/service unit tests.
- Domain-rule tests that need a full browser flow without a user-interface-specific reason.
- Tests using multiple layers without an identifiable end-to-end workflow or integration contract.

For every finding:

1. Identify the current test location and classification.
2. Identify the actual scope.
3. Explain why it is in the wrong layer.
4. State the recommended layer.
5. Recommend one explicit action:
   - Remove.
   - Move.
   - Rewrite.
   - Split into separate tests.
   - Retain with justification.

Required finding format:

| Classification | Test | Current layer | Recommended layer | Why current placement is wrong | Action |
|---|---|---|---|---|---|
| Incorrect test layer | `path:test name` | Unit | Integration | Uses a real database and HTTP host | Move / Rewrite |

### E. Flaky or non-deterministic tests

Flag tests with potential instability due to:

- Real time or clocks.
- Time zones.
- Randomness.
- Shared mutable state.
- Order-dependent execution.
- Parallel execution conflicts.
- Fixed delays or sleeps.
- Race conditions.
- External live services.
- Network access.
- Uncontrolled database state.
- Hard-coded ports.
- Browser timing assumptions.
- Retries that hide failure.
- Dependence on machine locale or environment variables.
- Tests that pass only when run individually or in a specific order.

For every finding:

1. Identify the source of non-determinism.
2. Explain the risk.
3. State whether the test should be removed, stabilized, isolated, or moved.
4. Recommend a concrete deterministic alternative where appropriate:
   - Inject a clock.
   - Control randomness.
   - Use a fake or emulator.
   - Reset database state.
   - Wait for meaningful state rather than sleeping.
   - Use unique test data.
   - Run dependencies in containers.
   - Separate slow external-contract tests from normal CI.

## 9. Test retention decision

For every existing test reviewed, apply this decision process:

1. Does the test protect a meaningful domain rule, security boundary, data-integrity rule, error path, integration contract, or critical user flow?
   - If no, recommend **Remove**.

2. Would the test fail for a realistic regression that is not already detected by another retained test?
   - If no, recommend **Remove** as duplicate.

3. Is the test at the lowest appropriate level for the behaviour it proves?
   - If no, recommend **Move**, **Rewrite**, **Split**, or **Remove**.

4. Does it rely mainly on framework internals, static output, implementation details, or mock interactions?
   - If yes, recommend **Remove** unless it proves a distinct externally meaningful contract.

5. Does it contain four or more meaningful mocks/stubs/spies?
   - If yes, investigate whether it is wiring-only.
   - Remove it if it provides no unique behavioural value.
   - Convert it to an integration test if collaboration is the behaviour being proven.
   - Replace mocks with fakes or real in-process collaborators if appropriate.

6. Can repeated examples be represented by one parameterized test without losing distinct branches, boundaries, or failure modes?
   - If yes, recommend **Merge** or **Parameterize**.

7. Does the test have a unique failure mode that it protects better than any other test?
   - If yes, retain it and document that value.

A test should be retained only if it provides unique, meaningful regression protection.
