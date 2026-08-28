# CQRS Not Adopted for Backend Architecture

- Status: Accepted
- Date: 2026-08-28
- Deciders: Gleison Gibellato da Silva
- Related: `docs/prd/P31-prd-async-persistence`, `docs/prd/P36-F02-.../spec.md`, `CLAUDE.md` (architecture invariants)

## Status

Accepted.

## Context

The question was whether adopting CQRS (Command Query Responsibility Segregation) would
benefit the `Financial.Investment.*` and `Financial.CashFlow.*` bounded contexts, given the
current persistence model, read/write workload, domain complexity, and how both front ends
(`Financial.App` WPF and `Financial.Web` React) consume the backend.

The first thing that matters is what CQRS would actually have to separate. Both bounded
contexts load a single JSON document into an in-memory singleton object graph once at
process startup (`Financial.Investment.Infrastructure/Repositories/InvestmentJsonRepository.cs:10-25`,
`Financial.CashFlow.Infrastructure/Repositories/CashFlowJsonRepository.cs`) — confirmed by
`CLAUDE.md`'s own documented behavior that the process must be restarted after any
out-of-band edit to the data file. Every write re-serializes and rewrites the **entire**
document, not a delta, gated by a single-writer `SemaphoreSlim`
(`CashFlowJsonRepository.cs:75-99`, comment: *"Serializing the document walks every
collection in the graph, so one writer at a time"*; `InvestmentJsonRepository.cs` has the
identical pattern). There is no database, no indexes, no query engine — CQRS's central
value proposition, scaling or optimizing reads independently of writes, has nothing to
attach to in this architecture.

Reads are plain LINQ over those in-memory collections, and real dataset sizes are small:
`data-cashflow.json` is ~6.2 MB across ~15.6k records, `data-investment.json` is ~0.7 MB
across ~2.4k records — trivial for in-process LINQ, and there is no evidence anywhere in
the project of a query-performance problem. The one documented performance issue in this
codebase (`docs/prd/P31-prd-async-persistence`) was a **write**-path problem — a
synchronous full-document Google Drive upload blocking the HTTP request — already solved by
debouncing (`Financial.Shared.Infrastructure/Persistence/DebouncedJsonStorage.cs`). It is
not a read-model problem CQRS would have addressed.

The next question was whether the domain itself needs separate read and write shapes.
Across the ~57 public Application-service methods in CashFlow and ~30 in Investment, every
method is already clearly either a command (`Add…`, `Update…`, `Delete…`, `Post…`,
`Mark…Paid`) or a query (`Get…`, `List…`, `…Summary`), and every aggregate already has a
distinct write-shaped DTO (`ExpenseCreateDTO`, `TransactionCreateDTO`) versus a richer,
computed read-shaped DTO (`ExpenseDTO`, `AssetDetailsDTO`) — never the same type reused for
both directions. This is Meyer's CQS (Command-Query Separation) principle, already followed
by convention across the codebase, and it delivers the intent-clarity benefit people usually
reach for formal CQRS to get — without a command bus, MediatR, or separate read/write
models. Real algorithmic complexity is also concentrated, not systemic: it lives in a
handful of `Domain/Rules` classes (`XirrCalculator`'s bisection solver,
`CreditFrequencyAnalyzer`'s date-gap cadence detection, `Asset`'s weighted-average cost
basis and price-history concurrency handling, `Expense`'s payment-status state machine),
and it is calculation/business-rule complexity, not concurrency or consistency complexity
that a separate write model would help with. Most entities (`Portfolio`, `Broker`,
`Credit`, `Transfer`, `Income`, `Bank`, `CreditCard`, …) are close to plain CRUD with light
field-level validation.

Finally, both front ends already share one contract, which removes another usual argument
for CQRS (decoupling multiple consumers with different read/write needs).
`Financial.App` (WPF) calls Application service interfaces directly and in-process — e.g.
`ExpenseWorkflowViewModel` holds an `IExpenseService` and calls it exactly like
`ExpensesController` does, with no HTTP client anywhere in the WPF codebase.
`Financial.Web` calls the same operations over REST, through the same controllers backed by
the same services. Neither front end has a read-vs-write shape need the existing DTO split
doesn't already meet.

This direction was already implicitly considered and declined once before: `docs/prd/P36-F02-…/spec.md`
records removing an `IncomeDTO` composition specifically because keeping it "would have
meant deciding *now* whether Income and Reserve read-models should compose (edging toward a
CQRS-style read-model split) for a need nobody has yet." All of this matches `CLAUDE.md`'s
explicit architecture invariant: this is a single-user, self-hosted application, and the
project is "right-sized, not over-engineered... don't build for scale that will never
arrive."

## Decision

Do not adopt formal CQRS — no separate read/write models, no command/query bus or
mediator, no event sourcing, no separate read datastore — anywhere in the backend.

Keep and continue the existing informal CQS convention as the standing pattern: one
Application service per aggregate, command methods and query methods clearly named,
distinct `*CreateDTO`/`*UpdateDTO` request types versus `*DTO`/`*SummaryDTO` response types
per aggregate. This ADR documents that convention as intentional, not accidental.

If a specific aggregation/read endpoint becomes measurably slow as data grows (there is no
evidence of this today), fix it narrowly — e.g. an in-memory memoized/cached projection for
that one read, invalidated on the relevant write — rather than introducing a general
read-model/CQRS layer.

## Alternatives Considered

- **Full CQRS: separate write and read models with a command/query bus (e.g. MediatR) and
  dedicated handler classes.** Rejected. There is no database or query engine to scale
  reads against independently of writes — every read is already an in-memory LINQ query
  over the same object graph the writes mutate, and adding a bus/handler layer on top would
  be new infrastructure solving a scaling problem this app does not have. It would also cut
  across `Financial.App`'s in-process calls into `I*Service` interfaces, which have no
  natural place in a command/handler dispatch pipeline without introducing one just for
  this purpose.
- **CQRS-lite: keep the same services and API, but add a separate materialized/cached
  read-model layer for the heavier aggregation endpoints** (`SummaryController`,
  `NavigationController`, `AnnualSummaryController`), computed ahead of time instead of
  per-request. Rejected for now. No endpoint has a measured latency problem, dataset sizes
  are small, and the project already explicitly declined this exact direction once
  (`docs/prd/P36-F02`) for lack of a real need. Revisiting this narrowly, for one endpoint,
  is preferable to building a general read-model layer speculatively.
- **Status quo: keep the existing informal CQS convention (command/query method naming,
  distinct Create/Update vs. read DTOs) with no new infrastructure.** Accepted — see
  Decision above.

## Consequences

### Positive

- No new abstractions (command bus, mediator, separate handler classes, separate
  read/write repository interfaces) are introduced; `IInvestmentRepository` and
  `ICashFlowRepository` remain the single interface per context for both reads and writes.
- Future evaluation of "should this be split into command/query classes" can point to this
  ADR instead of re-litigating it from scratch; the existing DTO-naming convention is the
  intended and sufficient expression of command/query separation in this codebase.
- Keeps the codebase's complexity proportional to a single-user, self-hosted app, in line
  with `CLAUDE.md`'s "right-sized, not over-engineered" invariant.

### Negative

- If a specific aggregation endpoint does become slow as data grows over years, there is no
  read-model/caching scaffolding in place today to fall back on — that fix would have to be
  built from scratch at that point, narrowly, for the one endpoint that needs it.
- If the project's constraints change materially (multi-user, a real database, a hard
  requirement for an audit/event log), this decision needs a fresh evaluation rather than
  an incremental extension, since no CQRS-adjacent scaffolding (event log, separate models)
  exists to build on.
- `Financial.App` and the API controllers stay tightly coupled to the same `I*Service`
  interfaces directly; any future move toward decoupled command handling would require
  touching both call sites rather than only one.
