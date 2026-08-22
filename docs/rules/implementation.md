# Implementation rules

Read this before writing or changing any source file. It assumes the design decisions in `design.md` are already made, and that the architecture invariants in `CLAUDE.md` are already binding.

Mandatory, not advisory.

## Clean Code

- One responsibility per function.
- Short methods. Keep cyclomatic complexity low — a method you have to scroll is a method that is doing two things.
- Meaningful names: the name says what the thing *is*, not what type it is or how it is implemented.
- No magic strings or numbers. Name the constant, and put it where the rule lives (see §Domain rules).
- No duplication — but read §Domain rules before extracting anything. Premature extraction is its own defect.

## SOLID

- **Single Responsibility** — one reason to change. A service that both computes and persists has two.
- **Open/Closed** — extend by adding a type, not by adding a branch to an existing `switch`.
- **Liskov Substitution** — a derived type must be usable everywhere the base is, without the caller checking which it got.
- **Interface Segregation** — an interface a consumer only half-implements is two interfaces.
- **Dependency Inversion** — depend on the abstraction the *consuming* layer declares. Application declares the repository interface; Infrastructure implements it. Never the reverse.

## Domain rules

A rule belongs on the entity that owns it. Extract it into `Domain/Rules/` only when one of these is true:

- **Two or more call sites need it.** `YearScopedInvestmentAccountResolver` (used by `AnnualSummaryService` and `InvestmentSnapshotService`) and `DividendValuationRules` (used by `DividendService` and `DividendCheckViewModel`) qualify.
- **It is a stateless algorithm with its own Domain test class.** `XirrCalculator`, `ProfitCalculator`, `CreditFrequencyAnalyzer` and `AnnualResultCalculator` qualify on this ground.

Otherwise keep it on the entity: constants as `public const` on the entity, enforcement in the setter or factory that could violate it. The reference shape is `Expense.MinRoundUpAmount` / `Expense.MaxRoundUpAmount` (`Financial.CashFlow.Domain/Entities/Expense.cs:8-9`), enforced in `SetRoundUpAmount` (`Expense.cs:187-190`), read directly by its one caller (`Financial.App/ViewModels/CashFlow/ExpenseFormValidation.cs:55-58`).

A class that only re-exports another type's constants is indirection, not design. That is why `ExpenseValidationRules` was deleted in #496 — it "carried no rule of its own, so the indirection bought nothing".

**Domain contains no `*Service`, `*Policy` or `*Specification` classes.** There has never been one in either bounded context. Do not add the first without raising it explicitly.

## Public service methods must make failure observable

Every public method on an Application service follows this shape. All 25 services already do:

```csharp
using var span = StartSpan("AddExpense");     // also logs "{Operation} started"
try
{
    …
    span.SetAttribute(TelemetryAttributeKeys.EntityId, expense.Id.ToString());
    span.MarkSuccess();
    _logger.LogInformation("{Operation} completed", "AddExpense");
    return ToDto(expense);
}
catch (Exception ex)
{
    span.MarkFailed(ex);
    throw;
}
```

- **Failure is signalled by exceptions**, never a `Result`/`Either` type — nothing in this solution uses one. `Financial.Api/Middleware/DomainExceptionMappingMiddleware.cs:27-38` maps them to 409/404/400.
- **Rethrowing? Do not log.** `span.MarkFailed(ex); throw;` is the whole catch block. The boundary that finally handles the exception writes the log line; logging in both places doubles every failure.
- **Swallowing? You must log.** Any `catch` that does not rethrow logs the exception **type** before continuing: `_logger.LogWarning("… {ErrorType}", ex.GetType().Name)`. See `Financial.Investment.Infrastructure/Services/FallbackFinanceService.cs:37,50` and `Financial.Shared.Infrastructure/Persistence/DebouncedJsonStorage.cs:180`.
- **Never log an exception message, and never log a value.** Domain messages embed financial data — for example *"exceeds Ariana's balance of 654.27"*. Log the exception type, the operation name, and identifiers from the allow-list in `Financial.Shared.Abstractions/TelemetryAttributeKeys.cs`. Nothing else.
- Use `StartServiceSpan` / `MarkSuccess` / `MarkFailed` from `Financial.Shared.Abstractions`. Never re-inline the convention.

Assert the failure path in tests with `RecordingLogger<T>` and `RecordingTelemetryTracer` — they exist to prove what *is not* logged as much as what is.

## Tests — check for a common initializer first

Before adding a test class, check whether the setup you need already exists. Reuse beats re-creating, and it means the next change to that setup happens in one place.

1. **Shared doubles live in `Tests/Financial.TestUtilities`** — `StubCashFlowRepository`, `StubInvestmentRepository`, `RecordingTelemetryTracer`, `RecordingLogger<T>`, `FakeTimeProvider`, `TestDataPaths`. Every test project references it. Do not write a local copy.
2. **API tests derive from `ApiEndpointTests`** (`Tests/Financial.Api.Tests/ApiEndpointTests.cs:11`) — it owns the factory and exposes `Client` and `Services`. Never construct `ApiTestFactory` inside a test class.
3. **Everywhere else the pattern is constructor + `Create*` helper, not a base class:**

   ```csharp
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
       bool seedDefaultCreditCards = true, bool seedDefaultCategories = true) => …

   private ExpenseService CreateService(
       StubCashFlowRepository? repository = null, ILogger<ExpenseService>? logger = null) => …
   ```

   The optional parameters let the few tests that must differ on one dependency opt out without repeating the whole construction sequence. Reference: `Tests/Financial.CashFlow.Application.Tests/Services/ExpenseServiceTests.cs:23-45`.
4. **Instance fields, never `static`.** A `static readonly` tracer or logger accumulates recorded state across every test in the class.
5. **Extract only when there is something to hoist.** If each test constructs from its own input rather than from identical dependencies, leave it alone and say so in the PR — `AssetTests`, `XirrCalculatorTests` and `DateFormatConverterTests` were deliberately left alone for exactly this reason. Never create a base class for a single consumer.

What to test, and at which layer, is the `testing-guide-Financial` skill's job. This section is only about how the test class is wired.

## Comments

**Priority rule:** never remove a comment used by Swagger or any other tooling. It outranks every removal criterion below.

**Default stance:** do not add comments.

- Prefer self-explanatory code: clear names, small functions, explicit types.
- Only add a comment if:
  - It is required by tooling (e.g., Swagger XML comments), OR
  - It documents a non-obvious business rule / constraint that cannot be expressed in the code, OR
  - It records a critical workaround or historical reason that would otherwise be impossible to infer.

**Never add comments that:**

- Restate what the code already says.
- Explain how something will be used elsewhere (that belongs in the caller or in docs, not inline).
- Describe obvious implementation details.

When editing existing code:

- Do not introduce new comments unless one of the allowed cases above applies.
- If an existing comment is redundant or obvious, you may remove it (as long as it’s not used by tooling).

## Before finishing — self-review

Verify, and fix before you report:

- Clean Code and SOLID as above.
- No layer violations; dependency direction intact.
- No business logic in Presentation; no infrastructure concerns in Domain.
- Tests added, and existing tests still pass.
- This change is a complete working vertical increment — not scaffolding or disconnected infrastructure.
- The application remains deployable after this merge.

If any rule is violated, stop and propose a correction rather than shipping it.

## Definition of Done

A change is **not** complete unless:

- [ ] Architecture reviewed; Clean Architecture and SOLID respected.
- [ ] No layer violations.
- [ ] No business logic in Presentation, no infrastructure concerns in Domain.
- [ ] Unit tests added; integration tests added where appropriate.
- [ ] Existing tests still pass.
- [ ] New code documented where necessary (see §Comments).
- [ ] The change is a complete working vertical increment — implementation, configuration, tests and docs.
- [ ] The application remains deployable after merge, and the PR documents the commands used to verify it (build, test, start-up under production config).

Provide this checklist, filled in, before marking work complete.
