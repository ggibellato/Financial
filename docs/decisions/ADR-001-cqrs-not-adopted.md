# ADR-001: CQRS Not Adopted for Backend Architecture

## Status

Accepted

## Date

2026-08-28

## Author

Gleison Gibellato da Silva

## Context

We evaluated whether adopting CQRS (Command Query Responsibility Segregation) would
benefit the `Financial.Investment.*` and `Financial.CashFlow.*` bounded contexts, based on
the current persistence model, read/write workload, domain complexity, and how both front
ends (`Financial.App` WPF and `Financial.Web` React) consume the backend.

**Persistence has no read/write split to make.** Both bounded contexts load a single JSON
document into an in-memory singleton object graph once at process startup
(`Financial.Investment.Infrastructure/Repositories/InvestmentJsonRepository.cs:10-25`,
`Financial.CashFlow.Infrastructure/Repositories/CashFlowJsonRepository.cs`) — confirmed by
`CLAUDE.md`'s own documented behavior that the process must be restarted after any
out-of-band edit to the data file. Every write re-serializes and rewrites the **entire**
document, not a delta, gated by a single-writer `SemaphoreSlim`
(`CashFlowJsonRepository.cs:75-99`, comment: *"Serializing the document walks every
collection in the graph, so one writer at a time"*; `InvestmentJsonRepository.cs` has the
identical pattern). There is no database, no indexes, no query engine — CQRS's central
value proposition, scaling or optimizing reads independently of writes, has nothing to
attach to in this architecture.

**No evidence of a query-performance problem.** Reads are plain LINQ over in-memory
collections. Real dataset sizes are small: `data-cashflow.json` is ~6.2 MB across ~15.6k
records, `data-investment.json` is ~0.7 MB across ~2.4k records — trivial for in-process
LINQ. The one documented performance issue in this codebase
(`docs/prd/P31-prd-async-persistence`) was a **write**-path problem — a synchronous
full-document Google Drive upload blocking the HTTP request — already solved by debouncing
(`Financial.Shared.Infrastructure/Persistence/DebouncedJsonStorage.cs`). It is not a
read-model problem CQRS would have addressed.

**Command/query separation already exists informally, everywhere.** Across the ~57 public
Application-service methods in CashFlow and ~30 in Investment, every method is clearly
either a command (`Add…`, `Update…`, `Delete…`, `Post…`, `Mark…Paid`) or a query (`Get…`,
`List…`, `…Summary`), and every aggregate has a distinct write-shaped DTO
(`ExpenseCreateDTO`, `TransactionCreateDTO`) versus a richer, computed read-shaped DTO
(`ExpenseDTO`, `AssetDetailsDTO`) — never the same type reused for both directions. This is
Meyer's CQS (Command-Query Separation) principle, already followed by convention across the
codebase, delivering the intent-clarity benefit people usually reach for formal CQRS to get
— without a command bus, MediatR, or separate read/write models.

**Domain complexity is concentrated, not systemic.** Real algorithmic complexity lives in a
handful of `Domain/Rules` classes — `XirrCalculator`'s bisection solver,
`CreditFrequencyAnalyzer`'s date-gap cadence detection, `Asset`'s weighted-average cost
basis and price-history concurrency handling, `Expense`'s payment-status state machine —
and it is calculation/business-rule complexity, not concurrency or consistency complexity
that a separate write model would help with. Most entities (`Portfolio`, `Broker`,
`Credit`, `Transfer`, `Income`, `Bank`, `CreditCard`, …) are close to plain CRUD with
light field-level validation.

**Both front ends already share one contract.** `Financial.App` (WPF) calls Application
service interfaces directly and in-process — e.g. `ExpenseWorkflowViewModel` holds an
`IExpenseService` and calls it exactly like `ExpensesController` does, with **no HTTP
client anywhere in the WPF codebase**. `Financial.Web` calls the same operations over REST,
through the same controllers backed by the same services. Neither front end has a
read-vs-write shape need that the existing DTO split doesn't already meet, so introducing a
command/query bus would add indirection with no decoupling benefit — WPF is coupled
in-process either way.

**This direction was already implicitly considered and declined once.**
`docs/prd/P36-F02-…/spec.md` records removing an `IncomeDTO` composition specifically
because keeping it "would have meant deciding *now* whether Income and Reserve read-models
should compose (edging toward a CQRS-style read-model split) for a need nobody has yet."

All of this matches `CLAUDE.md`'s explicit architecture invariant: this is a single-user,
self-hosted application, and the project is "right-sized, not over-engineered... don't
build for scale that will never arrive."

## Decision

- Do not adopt formal CQRS (separate read/write models, a command/query bus or mediator,
  event sourcing, or a separate read datastore) anywhere in the backend.
- Keep and continue the existing informal CQS convention as the standing pattern: one
  Application service per aggregate, command methods and query methods clearly named,
  distinct `*CreateDTO`/`*UpdateDTO` request types versus `*DTO`/`*SummaryDTO` response
  types per aggregate. This ADR documents that convention as intentional, not accidental.
- If a specific aggregation/read endpoint becomes measurably slow as data grows (there is
  no evidence of this today), fix it narrowly — e.g. an in-memory memoized/cached
  projection for that one read, invalidated on the relevant write — rather than
  introducing a general read-model/CQRS layer.
- Revisit only on a concrete, evidenced need: genuine multi-writer contention (not
  applicable to a single-user app), a measured production query-latency problem, or a hard
  requirement for an audit/event log — not speculatively.

## Consequences

- No new abstractions (command bus, mediator, separate handler classes, separate
  read/write repository interfaces) are introduced; `IInvestmentRepository` and
  `ICashFlowRepository` remain the single interface per context for both reads and writes.
- Future evaluation of "should this be split into command/query classes" can point to this
  ADR instead of re-litigating it from scratch; the existing DTO-naming convention is the
  intended and sufficient expression of command/query separation in this codebase.
- If the project's constraints change (multi-user, a real database, measured
  query-latency problems), this ADR's *reasoning* — not its conclusion — is what should be
  re-evaluated first.
