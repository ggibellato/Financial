<!--
Sync Impact Report
- Version change: 1.0.0 → 1.1.0
- Rationale: MINOR because two new principles were added (materially expanded governance)
  without removing or redefining any existing principle.
- Modified principles: none
- Added principles:
  - VII. Incremental Vertical Delivery
  - VIII. Production Deployability After Every Merge
- Added sections: none (existing "Development Workflow & Definition of Done" section updated in
  place to cross-reference the two new principles, not added as a new section)
- Removed sections: none
- Templates requiring updates:
  - .specify/templates/plan-template.md — ✅ verified: its "Constitution Check" section is a
    generic placeholder (`[Gates determined based on constitution file]`), filled in dynamically
    per plan from whatever this file currently says. Does not hardcode principle names; no
    update needed.
  - .specify/templates/spec-template.md — ✅ verified: no reference to the constitution at all.
  - .specify/templates/tasks-template.md — ✅ verified: no reference to the constitution at all.
  - CLAUDE.md — ✅ updated in the same change to add matching Incremental Vertical Delivery and
    Production Deployability rules, so runtime AI guidance stays consistent with this document.
- Follow-up TODOs: none.
-->

# Financial Constitution

## Core Principles

### I. Clean Architecture, Strictly Layered

Every bounded context MUST be organized as Domain → Application → Infrastructure, with
Presentation composing both. Dependency direction is one-way and non-negotiable:

- Domain MUST NOT reference Application, Infrastructure, or any framework/UI package. It
  contains entities, value objects, domain rules, and domain events only.
- Application MUST depend only on its own Domain. It contains use cases, commands, queries,
  DTOs, and validators, and MUST NOT contain persistence implementation details.
- Infrastructure MAY depend on its own Application and Domain (plus `Financial.Shared.Infrastructure`
  for persistence primitives). It contains repository implementations, external API clients, and
  file-system access, and MUST depend on abstractions defined by Domain/Application, not the
  reverse.
- Presentation (`Financial.Api`, `Financial.App`) composes Application + Infrastructure for both
  bounded contexts and MUST NOT contain business logic.

This ordering is mechanically checked by `Financial.Architecture.Tests` for the inward edges
(Domain↛Application, Domain↛Infrastructure, Application↛Infrastructure) per bounded context.
That check does not currently cover Presentation-layer boundaries — reviewers MUST treat this as
a manual review responsibility until the architecture tests are extended, not as evidence the
rule doesn't apply there.

**Rationale**: this ordering keeps business rules testable without a UI, a database, or a network
call, and is the load-bearing structural decision that makes a JSON-file-persisted, single-process
app maintainable as it grows. It is already how the codebase is built; this principle keeps it
that way rather than eroding under time pressure.

### II. Bounded Context Isolation (Investment / CashFlow)

The Investment and CashFlow bounded contexts MUST remain fully independent: no entity in one
context may reference an entity in the other, each context owns its own JSON data file and its
own storage-provider configuration (`Investment:*` / `CashFlow:*`), and each has its own
Domain/Application/Infrastructure project triad.

Shared real-world vocabulary between the two contexts (e.g. "Bank" vs. "Broker" both modeling
Trading212/Chase; "Investment Account/Snapshot" meaning different things in each context) MUST
NOT be resolved by merging or cross-referencing the contexts. Any new feature that seems to need
data from both MUST be resolved at the Presentation layer or explicitly documented as an
accepted UI-level aggregation (see `Financial.Web`'s "Monthly" page, which has no backend
capability of its own).

**Rationale**: this separation is a deliberate, confirmed design decision (see `context.md`), not
an accident of folder layout. It keeps each domain's rules simple and prevents one context's
migrations, storage provider, or schema changes from ever forcing a change in the other.

### III. WPF/Web Feature Parity, WPF as UX Source of Truth

`Financial.App` (WPF) is the UX source of truth. `Financial.Web` (React) is expected to reach
and maintain feature parity with it for both bounded contexts. A feature is not "done" if it
ships in only one of the two front ends without an explicit, recorded reason (e.g. a confirmed,
scoped gap — see `docs/discovery-development-status.md`'s Credits Analysis WPF gap as the
canonical example of what an *unresolved* parity gap looks like, not a template to repeat).

`Financial.App` hosts both contexts' Application/Infrastructure layers in-process; it is not an
HTTP client of `Financial.Api`. Changes to Application-layer contracts MUST be evaluated for
impact on both the API surface consumed by `Financial.Web` and the direct in-process usage in
`Financial.App`.

**Rationale**: this project exists to replace a personal spreadsheet workflow end to end; a
feature the user can only reach from one client isn't finished, and the architectural asymmetry
between the two front ends (documented in `docs/baseline/04-wpf-app.md` and
`docs/baseline/02-architecture.md`) is easy to forget mid-implementation if it isn't stated as a
governing rule.

### IV. Right-Sized Engineering for a Single-User Tool

This is a personal, single-user, self-hosted application, installed per person, not built to
scale to multiple tenants and not expected to change hands frequently. Contributors MUST NOT
introduce multi-tenancy, authentication/authorization infrastructure, horizontal scaling
concerns, or defensive abstractions for hypothetical future requirements. No feature flags or
backwards-compatibility shims are needed when the code can simply be changed directly.

This principle does not relax Principle I, V, or VI — it constrains *scope*, not *rigor*. Clean
Architecture, layering, and testing discipline apply precisely because the codebase must remain
maintainable by a small (effectively single-developer, AI-assisted) team over a long lifetime,
not because the system needs to handle scale it will never see.

**Rationale**: stated explicitly in `CLAUDE.md`'s "Application details" section and confirmed
throughout the codebase (no auth anywhere, CORS-only access control, no database). Over-building
for scale this application will never need is itself a violation of Clean Code's single-
responsibility and YAGNI spirit, not an exception to it.

### V. Test-Backed Changes

No feature is complete without tests. Every new feature MUST include unit tests, and integration
tests where applicable (API round-trip tests via `WebApplicationFactory`, WPF ViewModel tests,
Web component/hook tests as appropriate to the layer touched). Existing test conventions MUST be
followed rather than introduced ad hoc:

- .NET: xUnit + FluentAssertions. No mocking framework — use or extend the hand-written fakes in
  `Financial.TestUtilities` (e.g. `StubCashFlowRepository`, `StubInvestmentRepository`) rather
  than adding Moq/NSubstitute.
- Web: Vitest + React Testing Library for component/hook tests; the Playwright smoke test
  (`npm run smoke-test`) is the end-to-end safety net and MUST keep passing.

Existing tests MUST still pass after a change. A change that requires disabling, deleting, or
loosening an existing test to pass MUST treat that as a signal to re-examine the change, not the
test, unless the test itself is demonstrably wrong.

**Rationale**: matches the `CLAUDE.md` Definition of Done and the testing discipline already
observed throughout the codebase (`docs/baseline/11-testing.md`). A no-mocking, hand-written-fake
convention is already established project-wide; introducing a mocking library would fragment that
convention for no benefit.

### VI. Evidence-Based, Spec-Driven Change

Do not treat current code behavior as a business requirement without evidence, and do not invent
requirements. When a change touches an area where the specification, the code, the tests, and the
UI disagree, surface the disagreement as a question for the user rather than silently picking one
source as authoritative. Classify claims about the system as CONFIRMED (backed by a PRD, an
explicit test, or direct user confirmation), OBSERVED (present in code with no stated intent),
INFERRED (a reasonable but unverified interpretation), or UNKNOWN — matching the convention
already established in `docs/baseline/` and `docs/discovery-*.md`.

New features SHOULD be preceded by a PRD/spec (`docs/prd/P<NN>-...`) before implementation,
following the vertical-slice-per-PR pattern already in use (Domain → Application →
Infrastructure → API → WPF → Web → tests, one PR per slice). Acceptance-criteria checkboxes in a
PRD MUST be checked off as their corresponding work lands, not left permanently unchecked or
batch-checked after the fact without verification.

**Rationale**: this project has a real, confirmed history of documentation drifting from
implementation (e.g. `context.md`'s stale WPF/CashFlow claim, found and corrected during initial
discovery) and of PRD checklists never being marked despite the underlying feature shipping. This
principle exists to keep the gap between what's documented and what's true from growing again,
and to make the discovery work already done (`docs/baseline/`, `docs/discovery-*.md`) a living
reference rather than a one-time snapshot.

### VII. Incremental Vertical Delivery

Every feature MUST be implemented as a sequence of small, independently reviewable increments.

Each increment MUST deliver a complete, working slice of the feature, or a working compatibility
boundary. An increment MUST NOT consist only of scaffolding, placeholder code, disconnected
infrastructure, or code that cannot be exercised or verified — if it can't be run, tested, or
reviewed as a working unit, it isn't an increment yet, it's still in progress.

Each increment MUST include the implementation, configuration, tests, and documentation required
for that increment to be usable and reviewable on its own. The implementation plan for a feature
MUST identify the scope, dependencies, acceptance criteria, and review boundary of every
increment before work on it begins.

**Rationale**: this formalizes the vertical-slice-per-PR pattern already named in Principle VI
(Domain → Application → Infrastructure → API → WPF → Web → tests, one PR per slice) as a hard
requirement rather than a stated preference. Scaffolding-only or disconnected-infrastructure PRs
are hard to review meaningfully and hard to verify — a reviewer can't exercise code that isn't
wired up, so defects hide until a later PR "connects" several unverified increments at once.

### VIII. Production Deployability After Every Merge

After every pull request for a feature is merged into `main`, the application MUST remain in a
deployable state. "Deployable state" means, at minimum:

- The application builds successfully using the repository's standard build process.
- The automated test suite required by CI passes.
- The application can start using the standard production configuration (Docker/`docker-compose`,
  not a dev-only profile or local override).
- The application does not require unfinished feature increments, local-only tools, development
  containers, or unavailable external services in order to start.
- Existing production functionality remains available — a merge MUST NOT regress a capability
  users already rely on.
- No known implementation defect introduced by the pull request prevents a production deployment
  of the application.

A pull request MUST NOT be merged if it leaves the application unable to build, test, start, or
provide its existing production functionality. Each pull request MUST document the commands or
checks used to verify that the application remains deployable (e.g. the build/test commands run,
and confirmation the app starts under `docker-compose up` or equivalent production config).

**Rationale**: this project is deployed as a single long-running process per installation
(Principle IV) with no staging environment or gradual rollout — `main` effectively *is*
production the next time someone updates their deployment. A broken `main` isn't caught by a
release process; it's caught by the user's own app failing to start. This principle makes
explicit what Principle I's CI gates already imply, and requires the verification itself to be
visible in the PR rather than assumed from "CI is green."

## Technology & Persistence Constraints

- **Stack**: .NET 10 (API, WPF app, Integrations/Tools, all backend tests — xUnit +
  FluentAssertions), React + TypeScript + Vite (`Financial.Web`).
- **Persistence**: no relational database. Each bounded context persists to exactly one JSON
  document, loaded once at process startup and held in memory for the process lifetime. Any
  change to a data file (migration, manual edit) requires restarting every process that reads it
  — restarting alone never runs a migration. Writes are full-document rewrites; there is no
  file locking or cross-process write coordination between `Financial.Api` and `Financial.App`
  today, so changes that assume otherwise MUST NOT be made without first addressing that gap
  explicitly.
- **Storage providers**: `LocalJson` (default) or `GoogleDrive`, selected independently per
  bounded context. Never run import/migration tools against the live data file — verify against a
  temp copy first.
- **No authentication or authorization**: CORS origin allowlisting is the only access-control
  mechanism, consistent with the single-user/self-hosted framing in Principle IV. Do not add auth
  infrastructure without an explicit, separate decision to do so.
- **API contract**: Application-layer DTOs are the literal wire format for `Financial.Api` — there
  is no separate API contract/mapping layer. A DTO shape change is a wire-format change; treat it
  accordingly for both `Financial.Web` and any other consumer.

## Development Workflow & Definition of Done

- **Branching**: never commit directly to `main`. Branch per feature/fix, open a PR, merge once CI
  is green. PR titles MUST follow Conventional Commits (`feat|fix|docs|chore|refactor|test|perf|
  ci|build`), enforced by `semantic-pr.yml`.
- **CI gates**: every PR runs .NET build+test, Web lint+test+build, and a browser smoke test
  (Playwright, against a seeded, published build). All three MUST pass before merge.
- **Definition of Done** for any feature or fix:
  - Clean Architecture respected; no layer violations; no business logic in Presentation; no
    infrastructure concerns in Domain.
  - SOLID principles respected.
  - Unit tests added; integration tests added where appropriate; existing tests still pass.
  - No feature flags or unnecessary backwards-compatibility shims.
  - New code documented only where the *why* is non-obvious — no restating what the code already
    says, no comments referencing the current task/fix/PR number.
  - The PR is a complete, working vertical slice (or working compatibility boundary) per
    Principle VII — not scaffolding-only or disconnected infrastructure.
  - The application remains deployable after this PR merges, per Principle VIII, and the PR
    description documents the commands/checks used to verify that (build, test, and a start-up
    check under production configuration).
  - Self-review performed against this checklist before marking work complete.
- **Commit hygiene**: never skip hooks (`--no-verify`) or force-push to `main` without explicit
  instruction. Prefer new commits over `--amend` once a commit has been pushed or a hook has run.

## Governance

This constitution supersedes ad hoc practice and prior undocumented convention for any conflict
between "how it's always been done" and what is written here. `CLAUDE.md` remains the
day-to-day runtime guidance file for AI-assisted development in this repository and MUST stay
consistent with this document; where they conflict, this constitution governs and `CLAUDE.md`
MUST be updated to match.

**Amendment procedure**: propose the change (principle text, section, or governance rule),
state the rationale, and record it via the `/speckit-constitution` workflow so the Sync Impact
Report and version bump are generated together with the change — amendments MUST NOT be made by
directly hand-editing this file outside that workflow.

**Versioning policy** (semantic versioning for this document):
- **MAJOR**: backward-incompatible governance change, or removal/redefinition of an existing
  principle.
- **MINOR**: a new principle or section added, or materially expanded guidance on an existing one.
- **PATCH**: wording clarifications, typo fixes, non-semantic refinements.

**Compliance review**: every PR is expected to be reviewable against this constitution;
`.claude/agents/architecture-reviewer.md` enforces Clean Architecture/DDD/SOLID/Clean Code on
every change and MUST be treated as the automated first pass of that review, not a substitute for
human judgment on scope (Principle IV) or spec fidelity (Principle VI).

**Version**: 1.1.0 | **Ratified**: 2026-08-17 | **Last Amended**: 2026-08-17
