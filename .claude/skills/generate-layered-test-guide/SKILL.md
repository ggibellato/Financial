---
name: generate-layered-test-guide
description: >
  Analyzes a project's tech stack, searches the web for testing best practices,
  asks the user clarifying questions, and generates a project-specific testing
  multi-file skill at `.claude/skills/testing-guide-<project>/` built around a
  three-layer model — Unit, Integration (intra-domain, including mandatory
  AC-tracing and negative-path tests), and E2E (System) — with main SKILL.md and
  artifact/reference sub-files. Invoke with `/generate-layered-test-guide
  <project-folder>`.
disable-model-invocation: true
---

# Generate Layered Test Guide

You are a testing architecture expert. Your job is to analyze a project, research its
stack, consult the user, and produce a **concrete, project-specific testing skill**
built around three test layers: **Unit**, **Integration (intra-domain)**, and **E2E
(System)** — where Integration carries a mandatory, explicitly-tagged subset of
tests that trace to acceptance criteria, and every layer requires negative-path
coverage alongside the happy path.

**Input:** `$ARGUMENTS` is the path to the project folder (default: current working
directory).

**Output:** a multi-file skill at `.claude/skills/testing-guide-<project>/` (e.g.,
`testing-guide-nestjs-project`) with a main `SKILL.md` (~230-290 lines) and detailed
guides in `artifacts/` and `references/` subdirectories. Each project gets its own
skill, enabling monorepo support.

"Integration" here means intra-domain, real-everything-but-external-providers,
doing double duty for both technical contracts and AC-tracing; "E2E" means genuine
multi-service only. A generated guide commits to this vocabulary consistently —
never blend in a different set of layer definitions partway through.

---

## Phase 1 — Project Analysis

First, read `.claude/skills/generate-layered-test-guide/testing-fundamentals.md` —
this contains the three-layer universal principles that serve as the foundation.
Having these loaded early lets you filter web research results against them in
Phase 2 and classify existing tests against them in 1.5.

Then explore the project at the given path. Collect the following:

### 1.1 Languages & Frameworks
- Read manifest files: `package.json`, `go.mod`, `requirements.txt`, `pyproject.toml`,
  `Cargo.toml`, `pom.xml`, `build.gradle`, `composer.json`, `Gemfile`, `.slnx`/`.sln`,
  etc.
- Identify the primary language(s), framework(s), and their versions.

### 1.2 Test Runner & Framework
- Detect test tooling: jest, vitest, pytest, go test, mocha, jasmine, RSpec, PHPUnit,
  JUnit, xUnit, cargo test, etc.
- Read test configuration files: `jest.config.*`, `vitest.config.*`, `pytest.ini`,
  `setup.cfg`, `tsconfig.spec.json`, `*.runsettings`, test sections in
  `package.json`, etc.

### 1.3 Application Artifacts

Detect the ecosystem's **artifact identification strategy** — how does this ecosystem
organize and name its artifact types? This determines how artifact types are grouped
and labeled throughout the guide.

Identify which strategy applies using this reference table:

| Strategy | When it applies | Example |
|---|---|---|
| **Suffix convention** | Framework enforces naming (NestJS, Angular, Laravel, .NET) | `*.service.ts`, `*.controller.cs` |
| **Filename convention** | Framework uses well-known filenames (Django, Flask) | `views.py`, `models.py` |
| **Directory convention** | Structure determined by folder (Go packages, some Python) | `handlers/*.go` |
| **Partial suffix** | Framework uses trailing name pattern (Rails) | `*_controller.rb` |
| **Decorator/base-class** | No naming convention; type identified by code pattern (plain Python/JS, FastAPI) | `@router.get` decorated functions |
| **Mixed** | Ecosystem uses a combination | Use the most specific identifier available for each type |

When a single file contains multiple artifact types, identify types by **code
construct** (class, function, decorator, signature) rather than file pattern.

**0. Full folder enumeration (mandatory, independent of the identification
strategy above) — do this before grouping by suffix/filename/directory convention.**
Run a literal directory listing (Glob/ls — not the manifest file's declared
structure, not memory) of the project root and every immediate subdirectory, and —
for a multi-project solution/monorepo — every project/package folder found via the
solution file, workspace config, or `packages/*`/`apps/*`/`src/*` convention (found
by reading that file, not assumed). Produce a flat list of every top-level folder
name across every project/sub-project.

For **every** folder in that list, assign one of:
- (a) an already-detected artifact type,
- (b) a newly-detected artifact type worth adding to the inventory,
- (c) explicitly out of scope, with a one-line reason (e.g., "generated output,"
  "static assets," "build config"),
- (d) "uninventoried — flag for Phase 3" if you cannot confidently classify it.

No folder may be silently omitted. Pay particular attention to folders that carry
**no matching suffix at all** under the detected strategy (e.g., a `Middleware/`,
`Behaviors/`, `Views/`, `navigation/`, or `theme/`-style folder) and to entire
sibling projects beside the main application (a secondary tools/console app, an
integrations/observability-style project) — these are exactly what suffix-matching
misses, because it never looks at them, not because it misclassifies them.

**Hard gate — unmatched folders block generation.** If a folder classified (c)
out-of-scope or (d) uninventoried contains more than ~3-5 source files, do not let it
quietly ride into Phase 4 as "flag for Phase 3" filler text. Either resolve its
classification yourself before Phase 3 (read a couple of its files, determine the
artifact type), or, if you genuinely cannot, it becomes a **named, specific Phase 3
question** ("`Middleware/` has 6 files with no obvious artifact-type match — what
should these be tested as?") that blocks Phase 4 until answered — never a bullet
buried in the summary that the user could miss. A handful of tiny (1-2 file) folders
can still go into the summary as FYI.

**Also explicitly check for each of these seven recurring categories**, independent
of the suffix-strategy grouping, since none of them reliably has a matching suffix:
time/clock-dependent and concurrency/debounce code; a UI state matrix for user-facing
views (initial/loading/empty/validation/server-error/saving/success/disabled/
unsaved-changes); accessibility-specific test patterns; contract/generated-type-drift
workflow (an API contract snapshot plus a generated downstream client/types, or
equivalent); a same-repo frontend that mocks its own backend in its own Integration
tests; fitness-function tests (architecture/dependency-direction/naming-convention
rules); and test-data contract tests (fixture/example/template data files a real
loader must keep parsing). Record presence/absence of each explicitly — see
`testing-fundamentals.md`'s "Six Recurring Artifact/Behavior Categories Often
Missed" section for what each looks like and how to classify it.

Then perform the artifact inventory:

1. **Group artifacts by type** using the detected identification strategy.
2. **For each type:** list instances, classify each against the fundamentals' Layer
   Assignment Table patterns (has branching? crosses a system boundary? part of an
   AC-tracing slice? configured lib? etc.).
3. **Identify the dominant layer(s)** per type — Unit, Integration, and/or E2E. A
   type can legitimately need more than one layer (e.g., a service tested at Unit
   for its branches AND at Integration for its storage contract).
4. **Note exceptions** — instances that deviate from the dominant pattern for their
   type.
5. **Identify common framework artifact types NOT yet present** in the project (e.g.,
   NestJS pipes/interceptors/filters, Django signals/management commands). These
   receive proactive guidance in the generated guide.

### 1.3b Feature & Acceptance-Criteria Sources

Acceptance-criteria coverage is mandatory under this model (see the fundamentals'
"AC Traceability Is Mandatory"), delivered as a tagged subset of Integration tests —
this subsection locates where those criteria live so the generated guide can point
developers at them and so AC-tracing tests can cite something concrete.

- Look for `docs/prd/`, `docs/spec/`, `docs/specs/`, `specs/`, feature-flag
  definitions, or any structured requirements folder. Read a couple of examples to
  see how acceptance criteria are numbered/labeled (e.g., "Section 9 AC checklist",
  Gherkin `Given/When/Then`, plain numbered lists).
- **Check specifically whether each AC already carries a stable, explicit identifier**
  (e.g. a literal "AC1"/"AC-03" label) as opposed to being an unlabeled bullet/checkbox
  whose only "id" would be its position in the list. **Do not silently invent a
  positional numbering scheme** (e.g. "3rd bullet under heading F01 = id 3") and
  present it in the generated guide as if it were a stable identifier — a positional
  id silently renumbers every time a bullet is inserted, removed, or reordered,
  quietly breaking every existing tag with no error. If no stable id convention
  exists, this is a genuine gap: record it and raise it explicitly in Phase 3 (see
  Q4) rather than deciding unilaterally to fabricate one.
- If no such folder exists, check issue-tracker conventions the team already uses
  (linked tickets, PR templates with an AC checklist) as the fallback source of truth.
- Note the granularity: is a "feature" one PRD file, one use case, one API endpoint,
  one UI workflow? This determines how AC-tracing tests should be organized within
  the Integration suite — dedicated files per feature vs. interleaved with other
  Integration tests (this is a Phase 3 question, not a decision to make here).
- If genuinely nothing documents acceptance criteria anywhere in the project, record
  this as a gap to raise explicitly in Phase 3 — AC-tracing tests still get written,
  but the guide must say what they cite instead (e.g., the PR description).

### 1.4 External Providers

Under this model, "external" has a narrower meaning than in a generic three-layer
guide: it means genuinely outside this codebase's own deployable boundary — not just
"a database" or "a queue," if the team operates that database or queue as part of its
own system.

- Identify true external providers: third-party HTTP APIs, payment gateways, email/
  SMS providers, cloud object storage APIs, external OAuth providers, SaaS
  integrations, web-scraped third-party sites.
- Separately identify **owned infrastructure** the project deploys itself (its own
  database, its own message broker, its own cache, its own file storage) — this stays
  real at Integration and E2E layers; it is never in the "mock" column.
- Check Docker Compose files, `.env` files, and configuration modules for clues on
  which category each dependency falls into.
- **Also detect providers directly from code — compose/env files alone under-report
  them.** Grep production code for HTTP-client usage
  (`HttpClient`, `IHttpClientFactory`, `new HttpClient(`, an injected `HttpClient`
  constructor parameter, the ecosystem's equivalent — e.g. `axios`/`fetch`/`requests`)
  and treat every distinct call site as a candidate external provider unless it's
  proven to target owned infrastructure. Cross-check by grepping test code for the
  corresponding fake-transport pattern (`HttpMessageHandler`, a mocked/fake handler,
  a recorded-fixture HTTP client) — a class already being faked at the transport
  layer in existing tests is strong independent evidence it's genuinely external,
  even if no compose/env file mentions it.
- If the boundary is ambiguous for a given dependency (e.g., a managed cloud database
  the team fully controls the schema and lifecycle of), flag it for the user in
  Phase 3 rather than guessing.

### 1.5 Existing Test Patterns
- Find existing test files and identify naming conventions.
- Note directory structure: colocated tests vs `test/`/`tests/` directories.
- For each existing test file (or a representative sample), classify it against the
  three-layer scope table in the fundamentals: single class/everything mocked
  (Unit), everything real except external providers with no cross-service deployment
  (Integration — whether contract-shaped, AC-shaped, or both), or multiple deployed
  processes (E2E). Note any existing tests mislabeled under a different vocabulary
  (e.g., a suite calling an in-process `WebApplicationFactory`/supertest test "E2E"
  when, under this model, it is Integration; or a suite that already separates
  "component"/"feature" tests from "integration" tests, which should be merged in the
  generated guide) — these need explicit reclassification, not silent renaming.
- Count existing tests per layer to understand current coverage level, and note
  whether any AC-tracing tests already exist and whether they're identifiable as such.
  These counts feed both the Phase 3 summary **and** the generated guide's §9 Test
  Baseline (see 4.1a) — do not compute them only for the chat summary and then
  discard them.

### 1.6 Existing Test Skills & Rules
- Check whether `.claude/skills/testing-guide-<project>/` already exists. If its
  layer vocabulary doesn't match this skill's three-layer model, flag this
  explicitly for the user in Phase 3 rather than silently overwriting.
- **Locate the project's actual testing-related rules, wherever they live — do not
  assume a fixed path.** Read the project's root `CLAUDE.md` (or equivalent
  AGENTS.md/README) first: many projects route to rules via a table (e.g., a "Rule
  files" section mapping trigger → file, such as "Writing or changing tests →
  `docs/rules/implementation.md` §Tests"). If such a table exists, follow it to the
  actual rule file(s) it names — even if that path is `docs/rules/*.md`,
  `CONTRIBUTING.md`, or anywhere else — and read them in full for any
  testing-related rule (shared fixture/helper location rules, required base
  classes, mocking policy, coverage policy). Only if no such routing table exists,
  check the conventional `.claude/rules/` directory, and only report on it after
  verifying with Glob/ls that it actually exists — never report "no rules found"
  after checking a path that isn't even this repo's rule-routing mechanism. Record
  which rule file(s) you actually read, by exact path, in the internal findings
  summary.

Compile all findings into a structured summary (keep it internal — do not output to
the user yet).

---

## Phase 2 — Web Research

For each major technology/framework detected, use **WebSearch** to find:

1. **Testing best practices** for the framework — search both broadly and
   version-specifically (e.g., "NestJS testing best practices" AND "NestJS 11 testing
   best practices", using the version detected in Phase 1).
2. **How to test each artifact type** — search for testing guidance specific to each
   detected type, and for types not yet present.
3. **In-process integration testing patterns** for the framework — how the ecosystem
   sets up tests that wire real collaborators together and mock only true external
   boundaries (e.g., "NestJS testing module with real providers", "ASP.NET Core
   in-memory host real services fake HttpClient", "Django TestCase with real ORM real
   services"), including how teams organize or tag feature/AC-proving tests within
   that same kind of suite (e.g., naming conventions, BDD-style test names,
   traceability comments).
4. **Multi-service / production-like E2E orchestration** for the stack — search for
   how the ecosystem runs true cross-process system tests (e.g., "docker-compose
   based end-to-end testing", "Testcontainers multi-service integration",
   "Playwright against a full docker-compose stack").
5. **Mock vs real strategies** for the true external providers detected in 1.4.
6. **Common testing pitfalls** for the stack.

**Version-aware curation:** discard advice referencing APIs, decorators,
configuration options, or patterns newer than the versions detected in Phase 1. If a
result doesn't specify a version, cross-check its recommendations against the
project's actual dependency versions before including them.

Extract actionable insights — concrete patterns, recommended libraries,
configuration tips, pitfalls. Discard generic advice that applies regardless of
technology; keep only what is specific to the detected stack and goes beyond what the
fundamentals already cover.

**Record a research log as you go (required).** For each search performed, log: the
query text, the single most load-bearing finding (or "nothing useful found" — a
documented negative result still counts as evidence the topic was checked), the
version it's scoped to, and a forward pointer to where in the Phase 4 output it will
land. This log isn't shown to the user in Phase 3, but it is condensed into
`references/research-notes.md` in Phase 4 (see 4.1c) — do not discard it.

---

## Phase 3 — User Questions

Output a structured message to the user with your findings and all questions below.
Then use **AskUserQuestion** to wait for their response before proceeding to Phase 4.

### Summary to present (required elements)

1. Detected languages, framework names, and versions.
2. Detected test runner and configuration files.
3. Type-grouped artifact summary showing instance counts and dominant layer(s) per
   type. Also list common framework artifact types not yet present, and every
   top-level folder classified out-of-scope or uninventoried during the Phase 1.3
   full folder enumeration, with reasons — so the user can catch a missed artifact
   type before generation.
4. Where features/acceptance criteria live (from 1.3b), or the gap if none exist.
5. List of true external providers found (1.4) vs owned infrastructure that stays
   real at every layer — flag any dependency whose category was ambiguous.
6. Summary of existing test file patterns, counts per (inferred) layer, and any
   existing tests that were labeled under a different vocabulary (a separate
   "component"/"feature" tier, or an in-process suite called "E2E") and need
   reclassification.
7. If `testing-guide-<project>` already exists with a different layer vocabulary:
   call this out and confirm the user wants to replace it.
8. Any other contradictions found between existing skills/rules.

### Questions to ask

1. **Confirmation** — "I detected [technologies/frameworks/external providers]. Is
   this accurate? Anything missing or miscategorized (especially owned infra I may
   have flagged as external, or vice versa)?"
2. **Layer scope** — "This guide uses three layers: Unit, Integration (intra-domain —
   including mandatory AC-tracing tests), and E2E (System). Do you want all three in
   scope, or should E2E be scoped down for now (e.g., no multi-service environment
   yet, so E2E is aspirational only)?"
   - Note: module/DI resolution tests and AC-tracing Integration tests are always
     included regardless of the user's answer — they are mandatory per the
     fundamentals.
3. **External provider strategy** — "For each true external provider, how should
   tests interact with it?"
   - Provide a pre-filled table based on your analysis with recommended defaults from
     the fundamentals' "Real vs Fake — External Providers" table.
   - For any provider not covered there, apply the decision rule: if it can run
     locally in Docker in under 5 seconds with no external cost or flakiness risk,
     default to real; otherwise default to fake.
   - Ask the user to confirm or override each row, and to confirm the owned-vs-
     external classification from 1.4 for any ambiguous dependency.
4. **AC-tracing organization** — if Phase 1.3b found ACs with **no stable identifier**
   (unlabeled bullets/checkboxes), lead with that gap: "Your PRD/spec's acceptance
   criteria aren't individually labeled — should we (a) add a stable id to each AC in
   the source document going forward (recommended — a positional id silently
   renumbers whenever a bullet is added, removed, or reordered, breaking every
   existing tag with no error), or (b) accept a positional id anyway, understanding
   that risk?" Then: "Every acceptance criterion needs an identifiable Integration
   test that proves it (see the fundamentals' AC Traceability rule). Should these
   live in dedicated test files/classes per feature (recommended, so they're easy to
   audit), or interleaved with other Integration tests? **The tagging convention must
   be mechanically queryable, not a free-text comment a human has to read to
   recognize.** Propose one of: (a) a fixed literal substring in the test/class name
   that a single grep/regex isolates (e.g., a method prefix like `AC_P27F03_03`, or a
   test-framework trait/tag such as `[Trait("AC","P27-F03-03")]` if the framework
   supports categorization), or (b) a structured comment with a fixed
   machine-parseable prefix at a fixed position (e.g., `// AC: P27-F03#3` as the test
   body's first line) with a documented extraction regex. Whichever is chosen, it
   must let you answer mechanically: *given an AC id, which test(s) prove it?* and
   *given a test, which AC does it prove?* What should these cite when there's no PRD
   (PR description, ticket) — and what convention marks *that* case just as
   mechanically (e.g., a literal `AC: PR#123` tag)?"
5. **E2E environment** — "Does a production-like, multi-service environment already
   exist for E2E tests (docker-compose, staging, ephemeral k8s, a smoke-test job in
   CI)? If not, what's the closest thing we can realistically stand up so E2E tests
   have real multiple processes to run against, and how should they be run locally vs
   in CI?"
6. **Team conventions** — "Are there any team-specific testing rules, naming
   conventions, or policies I should incorporate?"
7. **Coverage philosophy** — "What's the team's testing philosophy?"
   - **Pragmatic** — focus on business-critical paths and system boundaries; skip
     trivial/low-risk code.
   - **Thorough** — include coverage targets in File Conventions; test more edge
     cases.
   - **Specific guidance** — let the user describe their own approach.

---

## Phase 4 — Generate the Testing Skill

**Source-of-truth rule for every code sample (applies to all of 4.1a–4.1c).** Any
constructor signature, method signature, property name, or code sample presented as
real (not clearly labeled "illustrative/pseudocode") must be copied or directly
transcribed from an actual Read/Grep of the source file it claims to represent,
performed in this same session — never reconstructed from a similar example
elsewhere in the guide, from memory, or by pattern-matching another class's shape.
When the same class/signature is referenced in more than one generated file,
re-derive it from source each time and cross-check that all occurrences agree with
each other, not only with your first draft.

### 4.1a Compose the main SKILL.md

The skill directory and name are derived from the project folder name:
`.claude/skills/testing-guide-<project>/`.

The generated skill uses a **multi-file structure** — the main `SKILL.md` stays
compact (~230-290 lines) while detailed guides live in sub-files:

```
.claude/skills/testing-guide-<project>/
├── SKILL.md                          (~230-290 lines — core rules + quick reference)
├── artifacts/                        (1 file per artifact type)
│   ├── entities.md
│   ├── services.md
│   ├── modules.md
│   ├── controllers.md
│   └── ...                           (one file per detected or anticipated type)
└── references/                       (supporting content)
    ├── external-providers.md
    ├── mock-health-rules.md
    ├── feature-traceability.md       (AC-tracing tests: where ACs live, how they're tagged)
    ├── negative-path-testing.md      (failure-case patterns per layer)
    ├── e2e-environment.md
    ├── file-conventions.md
    ├── gotchas.md
    ├── cross-cutting-concerns.md     (time/concurrency, UI state matrix, accessibility, contract drift)
    ├── research-notes.md             (Phase 2 web research log/audit trail)
    └── inventory.md                  (Phase 1.3 full folder-by-folder classification)
```

The exact set of reference files is whatever §4.1c specifies for this run — treat the
list above as the baseline, not a fixed count; do not hardcode "seven" (or any other
number) anywhere when referring to "all reference files."

Sub-files are **not loaded automatically** — Claude reads them only when the
SKILL.md's instructions direct it to, based on the artifact type or layer being
worked on.

**Critical: use code-inline references, NOT markdown links.** All references to
sub-files must use backtick notation (`` `artifacts/entities.md` ``), never markdown
links — markdown links risk the agent proactively loading all linked files, defeating
the lazy-loading design.

**Critical: the generated skill is a reference document only.** No Writing mode,
Audit mode, or workflow/orchestration sections — the generated skill contains only
concrete rules and tables for the specific project. (If a separate skill for guided
test-writing or test-auditing workflows exists in this repo's `.claude/skills/`,
verify its actual name via a directory listing before naming it anywhere in the
generated guide — do not assume a name like `test-guide` exists. If no such skill
exists, simply state that writing/audit workflows are out of scope for this
generated guide, without inventing a sibling skill name.)

Generate the main `SKILL.md` with the following structure:

#### Frontmatter

```yaml
---
name: testing-guide-<project>
description: >
  Testing guide for <project>. Reference this skill when planning features,
  implementing code, creating tests, or reviewing changes in <project>. Covers
  what to test, at which of three layers (Unit, Integration — including
  mandatory AC-tracing tests, E2E), and how to set up each test — organized by
  artifact type.
  Triggers on: planning <project> features, implementing <project> features,
  writing tests for <project>, reviewing <project> code, reviewing <project> tests,
  what should I test in <project>, how to test <project>, <project> test guide.
---
```

Replace `<project>` with the actual project folder name. Trigger phrases must be
**project-scoped** — do NOT duplicate generic triggers already registered by
another testing/workflow skill actually present in `.claude/skills/` (confirm by
listing the directory — do not assume a skill named `test-guide` exists) or by
another testing-guide skill for a different sub-project. Technologies belong in the
guide body, not the description.

#### Body Sections

**§0. Purpose**

State the guide's objective, name the three layers explicitly, and note that
AC-tracing is a mandatory, tagged subset of Integration rather than its own layer.
Explain the multi-file structure. Template:

> This guide helps you decide **what to test**, at **which of three layers** — Unit,
> Integration (intra-domain), or E2E (System) — and **how to set up tests** for each
> type of artifact and each feature in `<project>`. Integration carries a mandatory,
> explicitly-tagged subset of tests that trace to acceptance criteria — see
> `references/feature-traceability.md`. Every layer also requires negative-path
> coverage, not just the happy path — see `references/negative-path-testing.md`.
> When working on a specific artifact type, read the corresponding guide in
> `artifacts/`. Supporting references are in `references/`.

**§1. Testability Foundations**

Bridge the three-layer fundamentals with the framework-specific research findings.
Must include:

- What counts as an "external provider" vs "owned infrastructure" in THIS project,
  concretely (e.g., "Postgres runs in our own docker-compose — it is owned
  infrastructure, real at Integration and E2E. Stripe is a true external provider —
  faked everywhere above Unit.").
- The mock boundary translated to framework terms for Integration tests (e.g., "In
  NestJS, build the `TestingModule` with the real `UsersService` and real repository
  against a test DB; only `StripeService` gets a fake provider.").
- Integration's dual role in this project, with one concrete example of each: a
  plain contract-proving Integration test (names the components and the contract),
  and an AC-tracing Integration test (names the feature, the AC it cites, and how
  it's tagged) — both using the identical setup pattern.
- What the production-like environment for E2E actually is here (from the Phase 3
  answer) — e.g., "E2E tests run against `docker-compose up` with seeded fixture
  data via the `smoke` CI job" — and what to do if no such environment exists yet.
- Why module/DI configuration tests exist and which layer they occupy (Integration)
  — a missing import or wrong provider only fails when the module initializes at
  runtime, not at compile time.
- Version-specific behaviors or limitations relevant to the detected framework
  versions.
- Negative-path coverage for this project, concretely: one example of a Unit test's
  failure branch, one example of an Integration test simulating an external
  provider's failure mode, and (if E2E is in scope) one example of a rejected/failed
  E2E journey.
- Any binding testing-related rule discovered in Phase 1.6 (shared fixture/helper
  locations, required base classes, mocking policy) — cited by exact file path,
  translated into a concrete anti-pattern or setup-pattern instruction, not just
  mentioned in passing.

Do NOT write generic platitudes — every statement must be concrete reasoning for
THIS stack and THIS codebase's actual boundaries.

**§2. Testing Criteria**

Adapt "Worth testing" / "NOT worth testing" from the fundamentals to the project's
specific context, anchored to artifact types, code patterns, or named features —
never to a bullet with no project-specific anchor. Include the AC-traceability
requirement as one of the "Worth testing" bullets, naming where ACs live in this
project and citing `references/feature-traceability.md`. Also include the
negative-path coverage requirement as a "Worth testing" bullet, citing
`references/negative-path-testing.md`.

**§3. Feature Implementation Checklist**

A checklist mapping each artifact type — and features themselves — to required test
layers, with backtick paths to the corresponding guides. Format:

```
## 3. Feature Implementation Checklist

When implementing or changing a feature, walk this checklist. Every feature gets an
AC-tracing Integration row; every artifact you create or modify gets its own row.

| Created/modified | Required tests | Guide |
|---|---|---|
| A feature (new or changed AC) | Integration (AC-tracing): one identifiable test per AC line item incl. any negative AC, real wiring, providers faked | `references/feature-traceability.md` |
| Entity (`*.entity.ts`) | Integration: constraints, defaults, and a rejected/invalid case | `artifacts/entities.md` |
| Service with branching + storage | Unit: each branch incl. invalid/edge-case inputs (mock deps) + Integration: storage contract incl. a rejected/failing case | `artifacts/services.md` |
| Service with storage only, no branching | Integration: storage contract incl. a rejected/failing case | `artifacts/services.md` |
| Service with configured lib (JWT, cache) | Unit or Integration: real lib with test config | `artifacts/services.md` |
| Module with configured imports | Integration: compilation + resolution | `artifacts/modules.md` |
| Controller | Integration for contract behavior — do NOT unit test | `artifacts/controllers.md` |
| Cross-service journey (new critical path, or one touching another deployed app) | E2E: real multi-service environment incl. one failure journey, providers faked | `references/e2e-environment.md` |

**How to use:** after implementing a feature, first add its AC-tracing Integration
row, then walk every artifact row for anything you created or modified — for each
row, confirm both the success case and its failure/negative case are covered, not
only the happy path. Skip rows that don't apply.
```

Adapt artifact types, identification patterns, and guide paths to the project's
stack (from Phase 1.3).

**§4. Artifact Type Quick Reference**

A compact navigation table, including a row for AC-tracing coverage. Format:

```markdown
## 4. Artifact Type Quick Reference

When creating or modifying an artifact — or implementing a feature — read the
corresponding guide for the complete recipe.

| Artifact Type | Pattern | Layer(s) | Guide |
|---|---|---|---|
| Feature AC coverage | one PRD/spec per feature under `docs/prd/` | Integration (AC-tracing) | `references/feature-traceability.md` |
| Entities | `*.entity.ts` | Integration | `artifacts/entities.md` |
| Services | `*.service.ts` | Unit and/or Integration | `artifacts/services.md` |
| Modules | `*.module.ts` | Integration (compilation/resolution) | `artifacts/modules.md` |
| Controllers | `*.controller.ts` | Integration | `artifacts/controllers.md` |
| Critical cross-service journeys | — | E2E | `references/e2e-environment.md` |
| Future types | — | — | `artifacts/future-types.md` |
```

Include all types found in the project AND common framework types not yet present.
Use filenames derived from the type name and follow the same naming convention. The identification pattern
column must use the strategy detected in Phase 1.3.

**§5. Anti-patterns — Do NOT Do This**

Consolidate prohibitions, referencing artifact/reference guides instead of section
numbers. Must include, at minimum, adapted to the stack:

```
## 5. Anti-patterns — Do NOT Do This

- ❌ **Call an in-process test "E2E"** — a `WebApplicationFactory`/supertest-style
  test that runs one process is Integration under this model, not E2E (§1, `references/e2e-environment.md`)
- ❌ **Mock anything other than a declared external provider** in Integration or E2E
  tests — owned services, storage, and other domain components stay real
  (`references/mock-health-rules.md`)
- ❌ **Skip an explicit AC-tracing test because a general Integration test happens to
  exercise the same code path** — incidental coverage isn't AC coverage
  (`references/feature-traceability.md`)
- ❌ **Write an Integration test that can't be identified as proving a specific AC
  when it's meant to** — untagged tests can't be audited for AC coverage
  (`references/feature-traceability.md`)
- ❌ **Unit test a controller's business behavior** — thin delegation layers are
  proven at Integration (see `artifacts/controllers.md`)
- ❌ **Mock a configured library** (JWT, cache, throttle) — use a real instance with
  test config; a mock never catches a wrong secret or a bad TTL (§1)
- ❌ **Skip Integration tests for module/DI wiring** — a missing registration only
  fails at runtime (`artifacts/modules.md`)
- ❌ **Test only the happy path for a branch, boundary, or external call** — every
  branch/boundary needs its failure case too (`references/negative-path-testing.md`)
- ❌ **Write mirror tests** — an assertion that copies the return value proves nothing (§2)
```

Derive additional anti-patterns from the Layer Assignment Table prohibitions,
stack-specific gotchas from web research, and the mock boundary principle.

**§6. Layer Boundary Note**

Explain the boundaries of all three layers, since "Integration" and "E2E" have
narrower meanings here than common usage. Template:

> This guide uses a three-layer model — Unit, Integration (intra-domain), E2E
> (System) — but "Integration" and "E2E" mean something narrower than in many
> guides. What most guides call "integration" or "e2e" for an in-process test
> through the real HTTP stack (e.g., supertest, `WebApplicationFactory`) is
> **Integration** here — real wiring, one process, no cross-service deployment.
> **E2E** is reserved for tests that run more than one deployed process (e.g., [name
> the project's actual multi-service setup from Phase 3]). Acceptance-criteria
> coverage does not get its own layer either: it's a mandatory, explicitly-tagged
> subset of Integration tests (see `references/feature-traceability.md`) — same
> setup and mock boundary as any other Integration test, distinguished only by what
> it asserts and how it's tagged.

**§7. References**

```markdown
## 7. References

| Topic | File |
|---|---|
| External provider strategies (what's faked, what's owned) | `references/external-providers.md` |
| Mock/stub health rules & the external-provider-only boundary | `references/mock-health-rules.md` |
| Where features/ACs live and how AC-tracing Integration tests cite them | `references/feature-traceability.md` |
| Negative-path/failure-case patterns per layer | `references/negative-path-testing.md` |
| The production-like multi-service E2E environment | `references/e2e-environment.md` |
| File naming, directory structure, coverage philosophy | `references/file-conventions.md` |
| Stack-specific gotchas & pitfalls | `references/gotchas.md` |
| Time/concurrency, UI state matrix, accessibility, contract drift, and the frontend-mocks-own-backend exception | `references/cross-cutting-concerns.md` |
| Phase 2 web research log — what was searched, found, and where it landed | `references/research-notes.md` |
| Full folder-by-folder classification from Phase 1.3 | `references/inventory.md` |
```

**§8. How to Use This Guide**

```markdown
## 8. How to Use This Guide

This guide is organized as a multi-file skill:
- **This file (SKILL.md)** — always loaded. Core rules, quick reference, anti-patterns.
- **`artifacts/`** — one file per artifact type. Read the relevant file for the type
  you're touching.
- **`references/`** — supporting content, including `feature-traceability.md` for how
  to write and organize the mandatory AC-tracing Integration tests for a feature, and
  `negative-path-testing.md` for the failure-case patterns required alongside the
  happy path at every layer.

When working on a feature:
1. Check §3 (Feature Implementation Checklist) — add the feature's own AC-tracing
   row, then a row per artifact touched.
2. Read `references/feature-traceability.md` for how to structure the AC-tracing
   tests, `references/negative-path-testing.md` for the failure cases each layer
   needs, and the relevant `artifacts/*.md` file(s) for each artifact.
3. Consult other `references/` files as needed.
```

**§9. Test Baseline**

A compact, dated snapshot from Phase 1.5 — the point of this section is to give a
later re-run or human reviewer something concrete to diff against for drift. Format:

```markdown
## 9. Test Baseline (as of generation)

Snapshot from Phase 1.5, captured on <generation date>. Re-run this skill's Phase 1.5
counts later and compare against this table to spot drift — layers losing coverage,
or AC-tracing tests not keeping pace with new features.

| Artifact type / area | Existing test count | Layer breakdown | AC-tracing tests identifiable? |
|---|---|---|---|
| <type> | <n> | Unit: n, Integration: n, E2E: n | Yes (n) / No — none tagged |

Total: <n> tests across <n> files, as of <date>.
```

---

### 4.1b Compose artifact guide files

Create one file per artifact type in `artifacts/`. Each follows this template:

```markdown
> Part of the `testing-guide-<project>` skill (see `../SKILL.md`).

# [Artifact Type] (`<identification pattern>`)

## What to test
(cross fundamentals + web research — concrete aspects to verify for this type,
covering both the success path and its failure/negative cases)

## Layer assignment
(when Unit / Integration / E2E, with conditions — ALL applicable combinations; most
artifact types will need Unit and/or Integration, with E2E reserved for
cross-service journeys — say explicitly when a type is NOT expected to have its own
dedicated E2E tests. If instances of this type commonly carry AC-tracing
responsibility, note it here and point to `references/feature-traceability.md`
rather than duplicating that guidance.)

## Setup pattern
(reusable code template — not tied to a specific instance)

## When to skip
(NOT worth testing criteria applied to this type)

## Examples from project
(instances classified with reasoning)
```

Key principles:

- The `<identification pattern>` uses the strategy detected in Phase 1.3.
- No YAML frontmatter — these are reference documents, not standalone skills.
- Include a back-reference to the main SKILL.md at the top.
- Setup patterns must be reusable templates, not instance-specific.
- Layer assignments must trace back to the fundamentals' Layer Assignment Table.
- **Multi-layer coverage:** a type can legitimately require more than one of the
  three layers. State explicitly which combinations apply and what each layer
  validates that the others don't (e.g., "Unit tests the branch logic with a mocked
  repo; Integration tests the same service's storage contract with a real repository
  — neither substitutes the other").
- **Negative-path coverage:** for every behavior listed under "What to test" that has
  a defined failure mode (bad input, rejected write, provider failure), include it
  explicitly — don't stop at the success case (see `references/negative-path-testing.md`).

Start with the most common artifact types by instance count. Always generate a
**`future-types.md`** file last, covering common framework types not yet present.

The list of artifact files generated must match exactly the types listed in the §4
Quick Reference table in the main SKILL.md (excluding the "Feature AC coverage" row,
which points to `references/feature-traceability.md` instead).

---

### 4.1c Compose reference files

Each reference file: no YAML frontmatter, back-reference at the top using
`> Part of the testing-guide-<project> skill (see ../SKILL.md).`

**`references/external-providers.md`**
For each true external provider (1.4): real (Docker) or fake, which library/approach,
setup/teardown. For each piece of owned infrastructure: an explicit statement that it
stays real at Integration/E2E and is never in the mock column. Base this on the Phase
3 answers, informed by the fundamentals' "Real vs Fake — External Providers" table.

**`references/mock-health-rules.md`**
- The external-provider-only mock boundary translated to project-specific terms.
- Framework-specific mocking patterns for the ONE thing that gets faked above Unit
  (e.g., `jest.mock` for the payment SDK module, a fake `HttpMessageHandler` for the
  exchange-rate client).
- What Unit tests mock vs what Integration/E2E leave real.
- The signal that a test needs too many fakes: it's mocking something owned, not
  something external — split the unit or reclassify the test.
- **If the project has a same-repo frontend that mocks its own same-repo backend in
  its own Integration tests**, state this explicitly as the sanctioned exception
  (see `testing-fundamentals.md`'s Mock Health section) — not as a boundary
  violation. Name the backend, the mocking mechanism used (e.g., MSW, a fake fetch
  handler), and point out that the backend gets its own separate Integration tests
  against real storage.

**`references/feature-traceability.md`**

This file carries the full recipe for the mandatory AC-tracing subset of Integration
tests — there is no separate artifact file for it, since these tests are organized by
feature, not by artifact type. Include:

- Where features/ACs live (from 1.3b), with a concrete example path.
- **Setup pattern** — identical to any other Integration test: wire the real feature
  graph (every class/service it uses), fake only external providers. State the
  project's actual composition pattern (e.g., "the real DI container with the module
  under test," "direct instantiation of the real service graph").
- **The one-test-per-AC rule** and the naming/tagging convention this project uses to
  cite the AC a test proves (from Phase 3 Q4).
- Whether AC-tracing tests live in dedicated files per feature or interleaved with
  other Integration tests (from Phase 3 Q4), with an example either way.
- **The audit command (required, dry-run verified)** — the literal, copy-pasteable
  grep/ripgrep pattern or test-runner filter/tag expression that lists every
  AC-tracing test by its cited AC id, using the exact Phase 3 Q4 convention. Before
  including it, actually run it (or, if it's a test-runner filter and no tagged test
  exists yet, run it with `--list-tests`/the framework's dry-run equivalent) via the
  shell tool and confirm it doesn't error — a filter expression assembled by
  analogy with the framework's docs but never executed is exactly how a syntax error
  (e.g. an invalid `--filter` expression) reaches the generated guide undetected.
  Include one worked example showing what it returns for an existing or example
  AC-tracing test. A convention that doesn't reduce to a single runnable command has
  failed this requirement.
- **When a general Integration test is NOT enough** — a technical contract test that
  incidentally exercises an AC's code path does not count as AC coverage; the
  criterion needs its own identifiable, direct assertion.
- Examples from the project (features classified with reasoning), or "none yet —
  the first one added should follow this template" if the project has no AC-tracing
  tests yet.

**`references/negative-path-testing.md`**

Concrete negative/failure-case patterns for this stack, per layer:

- **Unit** — how to structure a failure-branch test (invalid input, guard clause,
  error condition) using the project's actual test framework idioms.
- **Integration** — how to simulate each true external provider's failure modes
  (timeout, non-2xx status, malformed response) using the real/fake setup from
  `references/external-providers.md`, and how to test a rejected storage write
  (constraint violation, conflict).
- **E2E** (if in scope) — the failure journeys required per critical flow, and how
  the production-like environment simulates them (e.g., stopping a dependency, an
  invalid payload).
- The rule: every branch, external-provider call, and critical journey needs its
  failure case tested — never assumed from the happy-path test passing.
- Examples from the project, or "none yet — the first one added should follow this
  template" if the project has no negative-path tests yet.

**`references/e2e-environment.md`**
- The actual production-like multi-service environment for this project (from Phase
  3 Q5): what's real, how it's started locally and in CI, how test data is seeded and
  torn down.
- If no such environment exists yet: what would need to exist for E2E tests to be
  meaningful under this model, and what to do in the meantime (e.g., "no E2E tests
  yet — the closest available layer is Integration; add E2E once `docker-compose`
  based smoke testing exists").

**`references/file-conventions.md`**
- Naming convention for each of the three layers (and for AC-tracing tests
  specifically, if they use a distinct suffix/tag), directory placement,
  configuration files.
- If "thorough" coverage philosophy was chosen, concrete coverage targets here.

**`references/gotchas.md`**
- Concrete pitfalls from web research, existing test pattern analysis, and known
  issues with the framework + test runner combination.

**`references/cross-cutting-concerns.md`**

One subsection per category from `testing-fundamentals.md`'s "Six Recurring
Artifact/Behavior Categories Often Missed" section — time/concurrency, UI state
matrix, accessibility, contract/generated-type drift, fitness-function tests, and
test-data contract tests. For each, either populate it with this project's concrete
pattern (real classes/files, real commands) or mark it "Not applicable to this
project" with a one-line reason — never silently omit a
category.

**`references/research-notes.md`**

The audit trail proving Phase 2 happened and what it changed — not a formality.

```markdown
| Topic searched | Query / search terms | Key finding (or "nothing usable found") | Version scope | Informed |
|---|---|---|---|---|
```

One row per Phase 2 topic category (all six from Phase 2, even if a category yielded
nothing — state that explicitly).

**`references/inventory.md`**

The persisted output of Phase 1.3's full folder enumeration — not just a Phase 3 chat
mention. One row per top-level folder of every project/sub-project:

```markdown
| Folder | Project | Classification | Notes |
|---|---|---|---|
```

`Classification` is one of: an artifact type name (linking to its `artifacts/*.md`
file), "out of scope — <reason>", or "resolved in Phase 3 — <what was decided>" for
anything that started as uninventoried/unmatched. Nothing stays "uninventoried" in
the final file — every row must resolve to one of the first three states.

---

### 4.2 Quality Checks

**Reference verification (mechanical, not visual, and not self-attested) — required
before checking any box below.** A prior run of this skill produced a tally of
"verified" claims that were not actually checked against a tool result — this step
must be a literal, logged sequence of tool calls, not a mental re-read of the prose.
Concretely:
1. Extract every backtick-quoted string from all drafted files that looks like a
   file path, class name, method name, or directory (e.g. `` `contracts/foo.md` ``,
   `` `ControleMaeService` ``, `` `../testing-fundamentals.md` ``) into an explicit
   checklist before verifying any of it.
2. Go through the checklist one item at a time, each backed by an actual tool call
   whose result you quote as the evidence — not a claim of having done so:
   (a) a sub-file this run is generating → confirm it's in the exact 4.3 write list;
   (b) a path into the **target project's own repo** → an actual Read or Glob call,
   and quote what it returned;
   (c) a class/method/constructor cited as fact → an actual Grep call, and quote the
   matched line verbatim as evidence it matches character-for-character.
3. Any reference that fails must be corrected or removed before proceeding. The
   final tally (e.g., "14 cross-references checked, 14 verified, 0 broken") must be
   arithmetic over the logged per-item results from step 2 — a tally not traceable to
   an actual tool call per item is not a completed verification pass.

Before writing the files, verify:
- [ ] Frontmatter has a clear `name` and description naming all three layers, not
      listing technologies
- [ ] Trigger phrases are project-scoped and don't duplicate another testing/
      workflow skill's triggers actually present in `.claude/skills/` (confirmed via
      directory listing, not assumed) or another project's testing-guide skill's
      triggers
- [ ] No Writing mode or Audit mode workflow sections
- [ ] No contradiction with existing skills in `.claude/skills/` or with the
      project's actual testing-related rules, wherever Phase 1.6 located them (name
      the specific file(s) checked — do not reference `.claude/rules/` unless that's
      genuinely where this project's rules live)
- [ ] Every binding rule found in Phase 1.6 is reflected as a concrete rule/
      anti-pattern/setup-pattern in the generated guide, not merely acknowledged
      internally and dropped
- [ ] **Main SKILL.md is under 290 lines**
- [ ] Every top-level folder of every project/sub-project was explicitly classified
      (inventoried / out-of-scope-with-reason / flagged) during the Phase 1.3 full
      folder enumeration — none silently skipped for lacking a matching suffix
- [ ] `references/inventory.md` exists and contains that full folder-by-folder
      classification table verbatim — the enumeration is not left to live only in
      the ephemeral Phase 3 chat message; any folder marked out-of-scope or
      uninventoried above the size threshold was actually resolved or turned into a
      blocking Phase 3 question, not left as filler text in this file
- [ ] Every artifact type is in its own file in `artifacts/` — no
      feature-scoped artifact file exists (that guidance lives in
      `references/feature-traceability.md` instead)
- [ ] All reference content is in individual files in `references/`, including all
      reference files listed in §4.1c
- [ ] §4 Quick Reference includes the "Feature AC coverage" row pointing at
      `references/feature-traceability.md`
- [ ] `references/negative-path-testing.md` exists and gives concrete failure-mode
      patterns per layer for this stack
- [ ] `references/cross-cutting-concerns.md` exists and explicitly addresses all
      six recurring categories (time/concurrency, UI state matrix, accessibility,
      contract/generated-type drift, fitness-function tests, test-data contract
      tests), each marked covered-for-this-project or explicitly not-applicable
- [ ] If the project has a same-repo frontend faking its own backend, mock-health-
      rules.md names this as the sanctioned exception
- [ ] `references/research-notes.md` exists with one row per Phase 2 topic category
      (six minimum), each naming the actual query, the finding or explicit "nothing
      usable found," its version scope, and which part of the generated guide it
      informed — and each "informed" pointer was actually checked (Read/Grep) to
      confirm that finding really landed in the named file/section, not just a
      plausible-sounding pointer written without checking
- [ ] §9 Test Baseline is present, dated, and its counts match Phase 1.5's findings —
      no test-count data is computed and then discarded before reaching the
      generated guide
- [ ] The AC-tracing convention (Phase 3 Q4) is mechanically queryable (a fixed
      literal tag/attribute/prefix) — reject and re-ask Q4 if the answer is a
      free-text comment requiring human judgment
- [ ] `references/feature-traceability.md` includes the literal audit command and a
      worked example — and that exact command (or its `--list-tests`/dry-run
      equivalent, so it doesn't require a tagged test to already exist) was actually
      executed via the shell tool during this generation to confirm it parses
      without a syntax error; a command that was only composed by pattern-matching
      the test framework's docs, never run, does not satisfy this check
- [ ] All sub-file references use backtick notation, never markdown links
- [ ] Every generated sub-file path referenced anywhere in SKILL.md or another
      sub-file corresponds to a file actually generated in this run
- [ ] Every reference to a path, file, class, or symbol inside the TARGET PROJECT's
      own repo (not a generated sub-file) was verified to exist via Read/Glob in
      this session before being cited as evidence — none asserted from memory or
      inference
- [ ] Every constructor/method signature or code sample presented as real was
      copied verbatim from a Read/Grep of its actual source file in this session;
      where the same signature appears in more than one generated file, all
      occurrences match each other and the source exactly
- [ ] Every sub-file includes a back-reference to the main SKILL.md
- [ ] Sub-files have no YAML frontmatter
- [ ] Every true external provider has a real/fake strategy in
      `references/external-providers.md`, and owned infrastructure is explicitly
      called out as staying real
- [ ] §1 concretely states what counts as external vs owned for THIS project, with
      a named example of each, and gives one example each of a contract-proving and
      an AC-tracing Integration test
- [ ] §1 names the actual (or aspirational, if absent) E2E environment for this
      project
- [ ] Artifact guides cover all types from Phase 1.3 AND common framework types not
      yet present
- [ ] Every Testing Criteria bullet references an artifact type, code pattern, or
      named feature — never an unanchored generic bullet
- [ ] Each artifact guide includes: what to test (success AND failure/negative
      cases), layer assignment (three-layer, explicit combinations), setup pattern,
      when to skip, project examples
- [ ] `references/feature-traceability.md` states where ACs live, the setup pattern,
      the one-test-per-AC rule, and the traceability convention from Phase 3 Q4
- [ ] Testability Foundations bridges the three-layer fundamentals with concrete,
      project-specific reasoning — not generic platitudes
- [ ] Gotchas are specific and actionable
- [ ] Feature Implementation Checklist includes the mandatory AC-tracing row plus a
      row per artifact type with correct guide paths
- [ ] Anti-patterns section includes at minimum: no in-process test called "E2E", no
      mocking anything but a declared external provider above Unit, no skipping an
      explicit AC-tracing test because a general Integration test exercises the same
      path, no untagged test standing in for AC coverage, no happy-path-only
      coverage for a branch/boundary/external call, no controller unit tests, no
      mocking configured libs, no skipping module/DI resolution tests
- [ ] §6 Layer Boundary Note is present and explicitly distinguishes all three
      layers, including that AC-tracing is a tagged subset of Integration, not a
      separate layer
- [ ] §8 explains the three-tier navigation (SKILL.md → artifacts/ → references/)
      and names `references/feature-traceability.md` specifically
- [ ] Generated advice is compatible with the project's detected framework/library
      versions

### 4.3 Write the Files

1. Create `.claude/skills/testing-guide-<project>/` (if it doesn't already exist).
2. Create `artifacts/` and `references/` subdirectories.
3. Write the main `SKILL.md`.
4. Write each artifact guide file in `artifacts/`.
5. Write each reference file listed in §4.1c, including
   `references/feature-traceability.md`, `references/negative-path-testing.md`,
   `references/cross-cutting-concerns.md`, `references/research-notes.md`, and
   `references/inventory.md`.

If a guide already exists in this directory, inform the user explicitly what will
be replaced and confirm before proceeding (see Phase 1.6/3).

Replace `<project>` with the actual project folder name throughout all files.

---

## Constraints

- **Do not modify** any existing files in `.claude/skills/` other than the generated
  `.claude/skills/testing-guide-<project>/` directory.
- **Do not modify** any existing files containing the project's testing/
  implementation rules, wherever Phase 1.6 located them (e.g., `.claude/rules/`,
  `docs/rules/`, or another project-specific location).
- **Do not create** test files — this skill only generates the testing skill
  documents.
- **Do not skip** any phase — all four phases (analysis, web research, user
  questions, generation) are mandatory.
- If `$ARGUMENTS` is empty, use the current working directory as the project path.
  If the current directory contains multiple sub-projects, ask the user to specify
  the target sub-project rather than analyzing the root automatically.
- If the project path doesn't exist or has no recognizable project structure, inform
  the user and stop.
