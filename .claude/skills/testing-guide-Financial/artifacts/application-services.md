> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Application Services (`Financial.*.Application/Services/*Service.cs`)

## What to test

- **Every branch** of each public method: found / not found (`KeyNotFoundException`), duplicate
  name (`DuplicateNameException`), entity in use (`EntityInUseException`), overdraft
  confirmation (`OverdraftConfirmationRequiredException`), scope filtering (`InvestmentScope`),
  currency-conversion fallback when the rate is `null`.
- **The observability contract from `docs/rules/implementation.md`**: a successful call records
  one span named `CashFlow.ExpenseService.AddExpense` (bounded context + service + operation)
  with `TelemetryAttributeKeys.OperationResult == TelemetryOperationResults.Success`; a failing
  call records the same span with `RecordedException` set and rethrows. Assert with
  `RecordingTelemetryTracer.Spans`. Assert that a swallowed exception logs its **type name** via
  `RecordingLogger<T>.Entries`, and that no financial value or entity name is in any message.
- **Constructor guards**: `Constructor_WithNullRepository_Throws` and one per dependency, with
  `.WithParameterName(...)`.
- **Time-dependent services** (`PaymentsDueService`, `CategorySummaryService`,
  `HistoricAverageService`, `IncomeSummaryService`, `InvestmentAnnualResultService`) take an
  optional `TimeProvider` — pass `new FakeTimeProvider(DateTimeOffset)` and test the exact
  boundary day (due today, due in 5 days, due in 6 days).
- **Negative paths**: invalid DTO values (zero amount, unknown id), repository read failure
  (the stub's failure hooks), provider returning `null`.

## Layer assignment

- **Unit** (the bulk): service + real domain entities + `StubCashFlowRepository` /
  `StubInvestmentRepository` (in-memory lists, no I/O) + `RecordingTelemetryTracer` +
  `NullLogger<T>` or `RecordingLogger<T>`. The stub repository is a hand-written owned double
  used only because the real one is file-bound — that is the Unit layer's job.
- **Integration**: the same service wired for real inside the API host (`ApiEndpointTests`) or
  the WPF composition (`AddFinancialCashFlowApplication` + `AddFinancialCashFlowInfrastructure`
  on a temp file). Every feature's AC-tracing tests live here
  (`../references/feature-traceability.md`); a service change that alters a §9 criterion needs
  that test updated, not just the Unit suite.
- **No E2E of its own**; a critical journey through the SPA is covered in
  `../references/e2e-environment.md`.

## Setup pattern

The binding shape from `docs/rules/implementation.md` §Tests (reference:
`Tests/Financial.CashFlow.Application.Tests/Services/ExpenseServiceTests.cs`):

```csharp
using Financial.CashFlow.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class ExpenseServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<ExpenseService> Logger = NullLogger<ExpenseService>.Instance;

    private readonly StubCashFlowRepository _repository;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly ExpenseService _sut;

    public ExpenseServiceTests()
    {
        _repository = CreateRepository();
        _tracer = new RecordingTelemetryTracer();
        _sut = CreateService();
    }

    private static StubCashFlowRepository CreateRepository(
        bool seedDefaultCreditCards = true, bool seedDefaultCategories = true) =>
        new(seedDefaultBanks: true, seedDefaultCreditCards: seedDefaultCreditCards, seedDefaultCategories: seedDefaultCategories);

    private ExpenseService CreateService(
        StubCashFlowRepository? repository = null,
        Microsoft.Extensions.Logging.ILogger<ExpenseService>? logger = null) =>
        new(repository ?? _repository, _tracer, logger ?? Logger);

    [Fact]
    public async Task AddExpenseAsync_WithZeroValue_RecordsFailedSpanWithException()
    {
        var request = ToCreateDto(_repository, ValidCreateRequest() with { Value = 0m });

        var act = async () => await _sut.AddExpenseAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
        _tracer.Spans.Should().ContainSingle().Which.RecordedException.Should().BeOfType<ArgumentException>();
    }
}
```

`ExpenseService(ICashFlowRepository repository, ITelemetryTracer tracer, ILogger<ExpenseService> logger)`
and `StubCashFlowRepository(bool seedDefaultBanks = false, bool seedDefaultIncomeSources = false, bool seedDefaultReserveBuckets = false, bool seedDefaultCreditCards = false, bool seedDefaultCategories = false)`
are the real signatures. `ToCreateDto` / `ValidCreateRequest` are that file's private helpers.
Recorders are instance fields — never `static readonly`, which leaks spans across tests.

## When to skip

- `CompensatingSaveHelper`, `IncomeGroupLookupBuilder`, `NavigationMapper`,
  `*Builder`/`*Selector` helpers: test them through the service that owns them unless they have
  their own branching worth an isolated class (`PortfolioAssetSummaryBuilder` does).
- Pure DTO mapping with no branch (`ToDto`) — proven by the branch tests that return it.
- A service-level re-test of a Domain rule already covered in
  `domain-entities-and-rules.md` — one representative case is enough.

## Examples from project

- `Tests/Financial.CashFlow.Application.Tests/Services/ExpenseServiceTests.cs` — Unit; reference
  initializer, span success/failure assertions.
- `Tests/Financial.CashFlow.Application.Tests/Services/PaymentsDueServiceTests.cs` — Unit;
  `FakeTimeProvider` boundaries, fail-safe empty result when the repository read throws.
- `Tests/Financial.CashFlow.Application.Tests/Services/ControleMaeServiceTests.cs` — Unit;
  currency conversion branches with a stub `IExchangeRateProvider` (external provider, faked).
- `Tests/Financial.Investment.Application.Tests/Services/TransactionServiceMutationTests.cs` and
  `TransactionServiceQueryTests.cs` — Unit; split by mutation/query to keep files scrollable.
- `Tests/Financial.Api.Tests/ExpenseEndpointsTests.cs` — Integration; the same `ExpenseService`
  real inside the host, asserting HTTP status and stored result.
