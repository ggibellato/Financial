# TDD assessment and risk-based coverage checklist

Reference for audit steps 5 and 7.

## 5. Assess coverage using TDD principles

Assess existing tests against Red–Green–Refactor intent.

### Red: behaviour is specified before or independently of implementation

Determine whether the test describes a meaningful behaviour, rule, failure mode, or user outcome that could have failed before implementation or before a defect was fixed.

A good test should express a requirement such as:

- An inactive customer cannot place an order.
- A user cannot access another tenant’s data.
- A booking cannot transition from cancelled to confirmed.
- A retryable external-service failure is retried up to the configured limit.
- A malformed webhook is rejected without persisting data.
- A user sees a meaningful error when a critical service is unavailable.

A weak test often expresses implementation rather than behaviour:

- A private method was called.
- A repository method was called with these exact arguments.
- A component rendered a static heading.
- A hook was called.
- A setter changes a property.

### Green: observable outcome is proven

Determine whether the test asserts a concrete, externally observable outcome:

- Return value.
- Domain state transition.
- Persisted data.
- Domain event.
- Authorization result.
- Error contract.
- HTTP response status and body where relevant.
- Message published.
- External request contract.
- User-visible workflow outcome.

Do not treat code execution, mock invocation, snapshot generation, or static markup as proof of meaningful behaviour unless they represent a deliberate public contract.

### Refactor: test resilience

Determine whether the test survives safe internal refactoring.

Flag tests tightly coupled to:

- Private methods.
- Internal class structure.
- Exact call order.
- Exact collaborator calls without behavioural significance.
- CSS classes.
- Static markup.
- Incidental component tree structure.
- Snapshot output with no meaningful behavioural assertion.
- Framework internals.
- Exact implementation details rather than outcomes.

For every meaningful test gap, determine:

1. What domain behaviour, risk, or user journey is currently unprotected?
2. What concrete bug could ship because of this gap?
3. Which test level is the lowest-cost level that can prove the behaviour?
4. Is an existing higher- or lower-level test already sufficient?
5. What is the smallest valuable scenario to add?
6. Which branch, failure mode, boundary, or state transition would the test protect?
7. Why would the proposed test fail if the intended behaviour regressed?

Do not use simplistic rules such as:

- Every source file needs a test.
- Every method needs a test.
- Every line needs coverage.
- All uncovered lines must be tested.
- Higher coverage percentage automatically means better quality.

## 7. Evaluate coverage by risk

Review production source code and compare it with existing tests. Prioritize gaps by impact and likelihood.

Do not recommend tests merely to improve numerical coverage.

### Worth testing

Prioritize the following areas.

#### Business logic with branching

- Conditionals.
- Calculations.
- Pricing.
- Discounts.
- Eligibility decisions.
- Domain invariants.
- State machines.
- Workflow transitions.
- Permission decisions.
- Feature entitlement.
- Rule evaluation.
- Complex mapping or transformation.
- Time/date rules.
- Multi-tenant rules.
- Domain event creation.

#### Security boundaries

- Authentication.
- Authorization.
- Role checks.
- Permission checks.
- Tenant isolation.
- Resource ownership checks.
- Session handling.
- Password reset.
- Account recovery.
- Rate limiting.
- Input sanitization.
- XSS prevention.
- CSRF protections.
- SQL, command, path, and template injection.
- File upload validation.
- Secrets handling.
- Privilege escalation prevention.

#### Data integrity

- Transformations.
- Serialization/deserialization.
- Data imports/exports.
- Migrations.
- Calculations.
- Idempotency.
- Duplicate handling.
- Concurrency controls.
- Optimistic locking.
- Transaction boundaries.
- Precision, rounding, and currency rules.
- Date/time and time-zone handling.
- Referential integrity.
- Data-loss prevention.

#### Error handling and resilience

- Database unavailable.
- Network failure.
- Timeout.
- Cancellation.
- Retry behaviour.
- Circuit-breaker or fallback behaviour.
- Malformed external response.
- Authentication failure.
- Forbidden access.
- Validation failure that must map to a public contract.
- Partial failure.
- Queue/message processing failure.
- Poison messages.
- Logging/auditing of critical failure paths.

#### Critical user and business flows

- Sign-up.
- Sign-in.
- Sign-out.
- Password reset.
- Payment.
- Checkout.
- Upload/download.
- Core create/read/update/delete workflow.
- User/tenant administration.
- Account deletion.
- Irreversible actions.
- Approval workflow.
- Critical search/filter workflow where incorrect output causes meaningful harm.
- Key desktop or mobile workflow if applicable.

#### Race conditions and asynchronous workflows

- Queues.
- Event consumers.
- Scheduled jobs.
- Concurrent updates.
- Duplicate delivery.
- Idempotent processing.
- Retries.
- Cancellation.
- Out-of-order events.
- Optimistic concurrency conflicts.
- Long-running workflows.
- Background processing.

#### Boundary and edge conditions

- Null/undefined.
- Empty values.
- Missing optional values.
- Minimum and maximum values.
- Off-by-one conditions.
- Overflow/underflow.
- Large inputs.
- Invalid encodings.
- Pagination boundaries.
- Date/month/year boundaries.
- Daylight-saving transitions.
- Time-zone boundaries.
- Unicode and locale-sensitive input.
- Duplicate or replayed requests.

#### External integrations

- Webhooks.
- API clients.
- Request and response mappings.
- Contract mismatches.
- Unexpected status codes.
- Malformed payloads.
- Missing fields.
- Versioning changes.
- Timeout/retry handling.
- Partial success.
- Authentication/credential errors.
- Idempotency keys.
- Signature verification.
- Callback processing.

### Usually not worth testing

Do not recommend tests solely for the following.

#### Framework guarantees

- Rendering machinery.
- Standard routing mechanics.
- Standard request handling.
- ORM persistence mechanics where no custom mapping, transaction, query logic, or policy exists.
- Framework loading states.
- Dependency injection mechanics.
- Default framework serialization behaviour unless application-specific contracts or custom converters exist.

#### Validation-library passthrough

- One test proving valid input passes and application-specific validation wiring works may be sufficient.
- Do not duplicate every rejection rule already guaranteed by a validation library where no custom domain rule exists.
- Test custom validation policies and business invariants, not the validation framework itself.

#### Mirror tests

- Assertions that reproduce production formulas, conditions, or implementation branches using the same logic.
- Tests that merely restate the implementation without independently validating a business outcome.

#### Duplicate coverage across layers

- Do not retain the same path at multiple layers unless each layer catches a distinct class of failure.
- Do not write unit, integration, and E2E tests for every case by default.

#### Wiring-only tests

- Tests that only verify a side-effect call was made with expected arguments.
- Tests that only verify argument forwarding.
- Tests that only verify method invocation without transformation, branching, error handling, policy, or contract behaviour.

#### Static structure assertions

- Field existence.
- Column type.
- Initial state.
- Default configuration.
- Type checks.
- Source file content.
- Class member existence.

#### Static output assertions

- Rendered text.
- Response field presence.
- Visual properties.
- Snapshot output.
- Static page content.

Do not recommend them unless they validate a deliberate public contract or meaningful business/security behaviour.

#### Variant repetition without branching

- Multiple tests that exercise the same path with different values.
- Prefer one representative test or a parameterized test when multiple values cover genuinely distinct boundaries or branches.

#### Single-path utilities

- Simple getters.
- Simple setters.
- One-line delegations.
- Trivial wrappers.
- Functions with no conditionals, error handling, edge cases, or meaningful transformations.

Use this decision question for every potential test:

> If this behaviour regressed, would this test catch a meaningful defect that another retained test would not catch?

If the answer is no, do not recommend a new test. If the test already exists, recommend removing, merging, or parameterizing it.
