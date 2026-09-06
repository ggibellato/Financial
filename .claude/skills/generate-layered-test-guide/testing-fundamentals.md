# Testing Fundamentals — Three-Layer Model

These are technology-agnostic testing principles built around a **three-layer** test
taxonomy: Unit, Integration (intra-domain), and E2E (System). The
`/generate-layered-test-guide` skill reads this file as a foundation and combines it
with project-specific analysis to produce a concrete testing rule.

A project that adopts this model applies these three layer definitions consistently
across its tests and its testing documentation, rather than mixing in a different
layer taxonomy partway through.

---

## The Three Layers

| Layer | Scope | What's real | What's mocked | Answers |
|---|---|---|---|---|
| **Unit** | One class/method in isolation from side effects and non-determinism | The unit itself, plus any real, deterministic, in-process collaborator with no side effects of its own (other entities/value objects, pure functions, pure domain services) — a "sociable" unit test | Anything stateful, I/O-bound, non-deterministic, or an external provider | Is this logic correct? |
| **Integration (intra-domain)** | Components/services within one domain/project, real implementations wired together | Everything inside the bounded context — domain, application, infrastructure you own | External providers only (third-party APIs, external services) | Do components within this domain interact correctly, and does the wired-together feature satisfy its acceptance criteria? |
| **E2E (System)** | A full flow across multiple deployed applications/services, in a production-like environment | All of our services, APIs, databases, message buses | External providers only | Does the full system satisfy its acceptance criteria end-to-end, as it actually runs in production? |

Unit provides most line/branch coverage — it is cheap and precise. **Not every
dependency needs to be a stand-in.** A "solitary" unit test (everything faked) and a
"sociable" unit test (real, deterministic, side-effect-free collaborators left in —
e.g. an Entity method that calls another Entity or a Value Object) are both Unit,
as long as nothing stateful, I/O-bound, non-deterministic, or external is involved.
**Do not force something into Integration merely because it "has a dependency."**
The question is whether that dependency has a side effect or hidden state a fake would
need to stand in for — if not, use the real thing and stay at Unit. Reserve Integration
for dependencies that are genuinely stateful/I-O-bound/non-deterministic (storage, a
configured library, another bounded-context service) or a declared external provider.

**Integration does double duty by design:** the same test
shape (real everything-we-own, external providers faked) serves two distinct
purposes —

- **Proving a technical contract** between components: a repository against real
  storage, a DI container resolving its registrations, two services collaborating on
  a transaction, a configured library's contract with your configuration.
- **Proving a feature's acceptance criteria**, via the mandatory AC-tracing subset
  described below.

These are not two layers. They use the identical setup and the identical mock
boundary — the only difference is what a given test asserts and whether it is tagged
as proving a specific AC. The "AC Traceability Is Mandatory" section below is what
keeps AC coverage explicit rather than assumed.

**E2E is the only layer that requires more than one deployed process/application.** A
test that runs everything in a single process — no matter how "end-to-end" it feels,
no matter whether it goes through a real HTTP stack — is Integration, not E2E, under
this model.

> **Boundary note:** "Integration" here includes any test that runs through a real,
> in-process stack — even a full HTTP pipeline (e.g., supertest,
> `WebApplicationFactory`, Django's `TestClient`) — as long as it's a single process
> with no cross-service deployment. "E2E" is reserved for flows that run more than
> one deployed process: a real API process talking to a real frontend build, a real
> consumer reading from a real queue a real producer wrote to. Acceptance-criteria
> coverage does not get its own layer: it is a mandatory, tagged subset of
> Integration tests (see below).

---

## AC Traceability Is Mandatory

Acceptance criteria (from `docs/prd/`, `specs/`, or tickets) do not get their own layer —
they get a mandatory, explicitly-labeled **subset of Integration tests**:

- **Every acceptance criterion needs at least one Integration test that proves it.**
  That test wires together every class/service the feature actually uses, real,
  with only external providers faked — the same shape as any other Integration test.
- **That test must be identifiable as an AC-tracing test — and that identification
  must be mechanical**: a fixed, greppable tag/attribute/prefix (e.g., a test-runner
  trait, a fixed method-name prefix, a structured comment with a documented
  extraction regex), not a free-text comment a reader has to interpret. A convention
  that can't be reduced to a single grep/regex or test-runner filter expression
  listing every AC-tracing test does not satisfy this requirement. If you can't
  point at which AC a test proves — or a machine can't, without a human reading and
  judging the comment — it isn't doing AC-tracing duty, however
  "integration-shaped" it looks.
- **That test must assert the criterion directly**, not merely execute the code path
  incidentally while asserting something else. A test that happens to exercise the
  same lines as an AC does not substitute for the AC-tracing test itself.
- **AC coverage is tracked explicitly** — e.g., a checklist per feature (see the
  generated guide's Feature Implementation Checklist) — never assumed to fall out of
  the general Integration suite as a side effect.

This is the mechanism that keeps acceptance-criteria coverage explicit and
auditable.

---

## Negative-Path Coverage Is Mandatory

A layer that only proves the happy path is half-tested. Whatever layer a piece of
code or a feature is tested at, both directions are required: the **success path**
(the code does what it's supposed to when everything is valid and available) and the
**failure/negative path** (the code behaves correctly when it isn't — invalid or
missing input, a rejected/unauthorized caller, an external provider that times out or
errors, a storage constraint violation, a boundary value).

Concretely, per layer:

- **Unit** — every branch that handles bad input, a guard clause, or an error
  condition needs its own test. A Unit suite that only calls a function with valid
  arguments hasn't tested the function.
- **Integration** — every external-provider call needs at least one test for how the
  system reacts to that provider failing (timeout, non-2xx status, malformed
  response) or rejecting the request; every storage contract needs at least one test
  for a rejected write (constraint violation, conflict). AC-tracing tests must cover
  a negative AC ("the system rejects X") whenever a feature's acceptance criteria
  define one, not only the positive ones.
- **E2E** — at least one failure journey per critical flow: an unauthorized request,
  a degraded or unavailable external dependency, an invalid submission that must be
  rejected end-to-end.

AC coverage and negative-path coverage are tracked the same way — explicitly, per
feature or artifact — never assumed to exist just because the happy path is covered.

---

## The Criteria

### Worth testing

- **Business logic with branching** — conditionals, state machines, permission
  checks, domain rules → **Unit**
- **Security boundaries** — auth, authorization, rate limiting, input sanitization,
  XSS, CSRF, SQL injection → the rule itself at **Unit**, the enforced boundary at
  **Integration** and/or **E2E**
- **Data integrity** — transformations, serialization, migrations, calculations where
  wrong output corrupts data
- **Error handling** — what happens when an external provider fails, storage is down,
  a caller is unauthorized (see "Negative-Path Coverage Is Mandatory" above)
- **A feature's acceptance criteria, one-to-one** — every AC line item in a PRD/spec
  needs at least one **Integration (AC-tracing)** test that exercises the real,
  wired-together feature and asserts that specific criterion holds (see "AC
  Traceability Is Mandatory" above)
- **Critical user flows** — auth, payment, upload, core CRUD the user depends on → at
  least one **E2E** flow per critical journey, run against a production-like
  multi-service environment
- **Race conditions, concurrency, transactional behavior** between real components →
  **Integration**
- **Boundary/edge cases** — null, empty, max values, off-by-one, overflow → **Unit**
- **External provider integrations** — webhooks, unexpected responses, timeouts,
  contract mismatches → **Integration**, with the provider faked
- **System boundary contracts** — queries to external systems, message formats, HTTP
  client requests to third parties → **Integration**
- **Configured dependency contracts** (JWT secret/expiration, cache TTL, throttle
  limits, queue options) — the library's own tests verify its internal mechanics;
  only your configuration is untested. Use real instances with test config at
  **Integration**, never a fake
- **Module/DI configuration** — a missing import, wrong default, or forgotten export
  only fails at runtime. Test that modules compile and resolve for real, at
  **Integration** layer
- **Cross-service contracts** (API ↔ frontend, service ↔ message consumer, service ↔
  service) that only exist once every process is actually running → **E2E**

### NOT worth testing

- **Skipping the AC-tracing test because a general Integration test happens to
  exercise the same code path** — incidental coverage is not AC coverage; the
  criterion needs its own identifiable, direct assertion (see "AC Traceability Is
  Mandatory")
- **Writing an Integration test that can't be identified as AC-tracing when it's
  meant to be one** — an untagged test that merely resembles AC coverage can't be
  audited later; either tag it or don't count it
- **Calling an in-process, single-application test "E2E"** — see the layer table
  above; that test is Integration
- **Framework behavior** — trust what the framework guarantees: rendering, routing,
  request handling, ORM persistence, loading states. Exception: any operation that
  encodes assumptions about an external system's structure is a system boundary
  contract, not framework behavior.
- **Validation passthrough** — one Integration test per endpoint proving the
  validator is wired is enough. Exception: security or business-critical validation
  rules.
- **Mirror tests** — assertions that copy the implementation's return value.
- **Duplicate coverage across layers** — each test must catch a bug no other test
  catches. Integration tests do not make E2E redundant, or vice versa: each answers a
  different question (see the layer table above).
- **Wiring tests** — verifying only that a side-effect call was made with the right
  arguments. Exception: module compilation/DI resolution tests are not wiring tests.
- **Static structure assertions (Unit layer)** — field existence, field types,
  initial state values.
- **Output shape without behavior** — verifying static output without exercising
  logic.
- **Variant repetition without branching** — multiple tests exercising the same code
  path with different inputs.
- **Single-path utilities (Unit layer only)** — functions with no conditionals, no
  error handling, no edge cases. Exception: system boundary contracts.

---

## Mock Health

- **The only thing Integration and E2E are allowed to mock is an external
  provider** — a third-party API, an external service, anything outside this
  codebase's own deployable boundary. Everything else must be real: owned services,
  repositories, the storage layer, other domain components, other services in the
  E2E environment.
- Unit tests mock every dependency that is stateful, I/O-bound, non-deterministic, or
  external — not every dependency the unit "does not own." A real, deterministic,
  side-effect-free collaborator (another entity, a value object, a pure domain
  service) may stay real in a Unit test (a "sociable" unit test — see The Three
  Layers above); mocking it anyway is not wrong, just unnecessary.
- **The litmus test for Integration/E2E:** can you name the specific external
  provider you mocked, and would mocking anything else hide a real bug? If you are
  mocking something you own "to keep the test fast," that is a sign the test belongs
  at Unit layer instead — or that the something-you-own is slow enough to need its
  own investigation, not a mock.
- When an Integration test needs many stand-ins beyond its declared external
  providers, that signals either the domain boundary is too large for one test, or
  something being faked is not actually external.
- **Exception — a same-repo frontend's Integration tests may legitimately fake its
  own backend.** The owned-vs-external boundary is about deployable boundaries, not
  repo ownership: from a single-page app's own Integration-test vantage point, a
  backend that lives in the same repo but runs as a genuinely separate process (a
  different deployable, reached only over HTTP) is outside *that* test's
  single-process scope, even though the team owns both sides. Faking the backend's
  HTTP responses in the frontend's own Integration tests (e.g., MSW, a fake fetch
  handler) is not a "mock something owned" violation — it's the same
  single-process/no-cross-service-deployment boundary the layer table already uses;
  the backend gets its own separate Integration tests against real storage. This
  exception applies only to a genuinely separate deployable reached over the
  network — never to an in-process module within the same deployable.

---

## Real vs Fake — External Providers

This table applies to Integration and E2E: it is the exhaustive list of what is
allowed to be faked. Anything not on this list, if it is part of what "our services,
APIs, databases, and message buses" means for this project, stays real.

| External provider | Strategy | Why |
|---|---|---|
| Database genuinely external to the deployable (managed cloud DB, third-party data platform) | Real (test instance in Docker) where feasible | Fast to spin up, controllable, no rate limits |
| Message queue we don't operate ourselves | Real (broker in Docker) | Contract matters; brokers are cheap to run locally |
| Cache (Redis) we operate ourselves | Real (Docker) | Caching behavior depends on real TTL/eviction — not an external provider, so it should stay real even sooner than this table implies |
| Email (SMTP) | Fake (in-memory) | Real SMTP is slow, unreliable, has side effects |
| External HTTP API (third-party) | Fake (fake server / recorded fixtures) | Rate limits, cost, network flakiness |
| Payment gateway | Fake (sandbox or mock) | Never hit real payment APIs in tests |
| Third-party file storage (cloud storage API) | Fake (in-memory/local emulator) | Avoid network calls and cost |

**Decision rule:** if it is genuinely outside the system's own deployable boundary,
and it cannot run locally in Docker in under 5 seconds with no cost or flakiness risk
→ fake it. If it is something the team owns and deploys as part of "our system" → it
is not an external provider; keep it real at every layer above Unit, including E2E.

---

## Layer Assignment Table

| Code pattern | Unit | Integration (intra-domain) | E2E (System) |
|---|---|---|---|
| Business logic with branching, no system boundary | ✅ mock owned deps | — (covered by Unit; would duplicate) | — |
| One feature's full vertical slice (domain + application + infra wired together), AC-tracing | — | ✅ real everything but external providers; assert the specific AC, tagged as such | — |
| Service ↔ repository/storage contract within a domain | — | ✅ real storage, external providers faked | — |
| Two services collaborating on a transaction inside one bounded context | — | ✅ | — |
| Module/DI wiring (compile + resolve the real container) | — | ✅ | — |
| Service using a configured framework lib (JWT, cache, throttle) | ✅ real lib, test config, if the unit is isolated enough | ✅ if the contract itself is under test | — |
| Service calling a genuine external HTTP API/provider | — | ✅ provider faked | ✅ provider faked, everything else real |
| Failure/negative branch (invalid input, guard clause, error condition) | ✅ same as its success branch | — | — |
| External provider call, failure mode (timeout, error status, malformed response) | — | ✅ provider faked to simulate the failure | ✅ if the journey depends on it, provider faked |
| Critical journey's failure case (unauthorized, rejected input, degraded dependency) | — | — | ✅ |
| HTTP handler / controller behavior (status codes, validation wiring, auth) inside one process | — | ✅ (AC-tracing and/or contract, as applicable) | only if the journey also crosses a real deployment boundary |
| A critical user journey across API + frontend (or service + consumer) as actually deployed | — | — | ✅ |
| Time/clock-dependent logic (retry, debounce, scheduled) using an injectable clock/timer abstraction | ✅ fake/controllable clock, assert exact fire boundary | ✅ if it also touches real storage/output | — |
| A user-facing view/workflow's state matrix (initial/loading/empty/validation/server-error/saving/success/disabled/unsaved-changes) | ✅ per-state component/render test | — (covered by Unit unless a state depends on a real backend response) | ✅ at least one full transition journey per critical workflow |
| Accessibility — keyboard operability, focus, accessible name/role, color-independent meaning | ✅ per-control accessible name/role/keyboard handler | — | ✅ full keyboard-only completion of a critical workflow |
| Contract/generated-type drift (API contract snapshot + generated downstream client/types) | — | ✅ contract snapshot test fails on unintentional shape change, plus a build/typecheck step proving the downstream artifact was regenerated and still compiles | — |
| A same-repo frontend's Integration test exercising its own UI logic against its own backend (a separate deployable reached over HTTP) | — | ✅ backend faked (e.g., MSW) — the frontend's own single-process Integration test; the backend has its own separate Integration tests against real storage | — |
| Fitness-function test (architecture/dependency-direction/naming-convention rule) | — | ✅ real compiled assemblies/source tree, no mocks | — |
| Test-data contract test (example/fixture/template data file must keep loading) | — | ✅ real loader against the real file | — |

This table only sketches shapes — always classify actual instances in the generated
guide using the three-layer scope column from the summary table at the top of this
document, not by pattern-matching this table alone.

---

## Six Recurring Artifact/Behavior Categories Often Missed

None of these reliably has a matching suffix under any artifact-identification
strategy, so an inventory driven purely by suffix/filename/directory convention will
silently miss them. Check for each of these explicitly, independent of that
grouping.

### Time, clock, and concurrency-dependent code

Code that reads the current time, schedules debounced/retried work, or coordinates
concurrent writers. Inject and control time/scheduling explicitly (a fake/
controllable clock or timer abstraction) rather than sleeping in a test or leaving it
untested; test both the exact fire-boundary condition and the concurrent-access
outcome. **Unit** for pure scheduling/backoff logic with a fake clock; **Integration**
for a debounced writer's actual persistence outcome.

### UI state matrix

For any user-facing view/workflow: initial, loading, empty, validation, server-error,
saving/in-progress, success, disabled, and unsaved-changes — not just the happy-path
default. A missing state is a UI bug, the same class of defect as a missed backend
branch. **Unit/component** for state-by-state rendering; **E2E** for at least one full
state-transition journey per critical workflow.

### Accessibility

Keyboard operability, visible focus, accessible name/label, and not relying on color
alone, at minimum to the stack's stated WCAG level. These are assertable, not just
manual-review items. **Unit/component** for a single control's accessible name/role/
keyboard handler; **E2E** for a full keyboard-only completion of a critical workflow.

### Contract / generated-type drift

When a project pins a machine-readable contract (OpenAPI/GraphQL snapshot) with a
generated downstream artifact (generated client/types) that a human-edited
counterpart must track: (1) a contract snapshot test that fails on unintentional
shape change (**Integration**), and (2) a build/typecheck step proving the downstream
artifact was actually regenerated and still compiles — not merely that the snapshot
changed. Treat "regenerate + typecheck" as a required step for any DTO/endpoint/
message-shape change, not optional cleanup.

### Fitness-function tests

Tests that assert a structural/architectural rule rather than a behavior — dependency
direction (Domain never references Infrastructure), layering, naming conventions,
forbidden references between bounded contexts. These cut across every other
category: they don't test what the code does, they test what shape the codebase is
allowed to have. Recognize a dedicated architecture/convention-rule test project or
file (e.g. one with no production-code counterpart, only assertions over the compiled
assemblies or source tree) as this category, not as "untestable" or folded silently
into an unrelated artifact type. **Integration** — it inspects the real compiled
assemblies or real file tree, not a mock of them.

### Test-data contract tests

Tests that assert a fixture, example, or template data file stays structurally valid
— a committed example JSON/CSV that must keep loading successfully, a schema/contract
snapshot that must stay in sync with what it documents (distinct from the
generated-client drift above: this is about the fixture data itself, not a generated
client). A repo that ships `*.example.json` templates or seed/fixture files a real
loader must keep parsing needs this category named, not left as an untested static
asset. **Integration** — it exercises the real loader/parser against the real file.

---

## Principles

- Read the actual test code — don't judge by name alone.
- **Classify by scope, not by vibes:** "one class, everything mocked" is Unit;
  "everything but external providers, no cross-service deployment" is Integration
  (whether it's proving a contract, an AC, or both); "more than one deployed
  app/service" is E2E.
- Apply the criteria consistently — if a test matches NOT worth testing and doesn't
  match Worth testing, remove it.
- Respect project conventions (test configs, directory structure).
- The layer model is a guideline, not a law: some features only need Unit +
  Integration (no cross-service journey) — that's still as many layers as the code
  demands, never a fixed ratio.
- **A Unit test that mocks a dependency does not test the dependency.** That
  dependency needs its own Unit tests, and the contract between the two needs an
  Integration test.
- **An AC-tracing Integration test and a general contract-proving Integration test do
  not substitute for each other, even though both are "Integration."** A contract
  test proving a repository's query is correct does not prove the feature meets its
  AC (it may never exercise the feature's actual branching or wiring); an AC-tracing
  test proving the AC holds does not prove every technical contract along the way is
  correct in every case (it may never exercise a rare storage failure mode). Track AC
  coverage explicitly — don't assume it falls out of the general Integration suite.
- **Happy-path-only coverage for a branch, boundary, or external call is incomplete,
  not "good enough."** See "Negative-Path Coverage Is Mandatory" above — the failure
  case needs its own test at the same layer as the success case.
