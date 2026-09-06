> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Domain Entities, Value Objects and Rules (`Financial.*.Domain/{Entities,ValueObjects,Rules}/*.cs`)

## What to test

- **Factory invariants**: `Expense.Create(date, description, value, category, bank, creditCard)`
  rejects a zero/negative value, a blank description, a missing category — every guard clause
  gets its own test, and the accepted boundary (`0.01m`) gets one too.
- **State transitions and derived values**: `Asset.Create(...)` then `UpdateIdentity(...)`
  normalises `LocalTypeCode`; `Bank.SetOpeningBalance(amount, date)`; `Expense.SetRoundUpAmount`
  enforces `Expense.MinRoundUpAmount` / `Expense.MaxRoundUpAmount` (`0.00m` – `0.99m`) — test
  both inside-range and both out-of-range sides.
- **Rules/calculators**: `XirrCalculator.Calculate(IReadOnlyList<(DateTime Date, decimal Amount)>)`
  returns `null` for degenerate input and the known IRR for a fixture; `TitheRule.CalculateTithe`
  / `NetOfTithe` sum back to the original; `AnnualResultCalculator`,
  `YearScopedInvestmentAccountResolver`, `CreditFrequencyAnalyzer`, `ProfitCalculator`,
  `TransactionFeeCalculator`, `DividendValuationRules`, `GlobalAssetClassMapping`.
- **Collections**: `IdCollection` / `ItemCollection` (`Entities/Collections/`) duplicate-id
  rejection and lookup misses.
- **Value objects**: `MonthlySeries`, `AssetValueSnapshot`, `DividendValue` equality and
  boundary arithmetic.
- **Negative cases are mandatory**: for every `throw` in an entity there is a test asserting the
  exception type and, where the message names the parameter, `.WithParameterName(...)`.

## Layer assignment

- **Unit only.** Domain has no dependencies (architecture rule
  `CashFlowDependencyRuleTests.Domain_Should_Not_Reference_Infrastructure`), so every
  collaborator is another entity or value object — a sociable Unit test with no doubles.
- **No Integration, no E2E** of its own. A domain rule's effect at the feature level is proven by
  the feature's AC-tracing Integration test (`../references/feature-traceability.md`), never by
  re-testing the rule through the host.

## Setup pattern

```csharp
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.Entities;

public class ExpenseTests
{
    private static readonly Bank Barclays = Bank.Create("Barclays", roundUpEnabled: false);
    private static readonly Category Mercado = Category.Create("Mercado");

    private static Expense CreateExpense(decimal value = 54.32m) =>
        Expense.Create(new DateOnly(2026, 7, 15), "Weekly groceries", value, Mercado, Barclays, null);

    [Fact]
    public void Create_WithZeroValue_Throws()
    {
        Action act = () => CreateExpense(0m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.00)]
    public void SetRoundUpAmount_OutsideAllowedRange_Throws(decimal amount)
    {
        var expense = CreateExpense();

        Action act = () => expense.SetRoundUpAmount(amount);

        act.Should().Throw<ArgumentException>();
    }
}
```

(`SetRoundUpAmount` throws `ArgumentException` for both the negative-expense and the
out-of-range case — `Financial.CashFlow.Domain/Entities/Expense.cs:190-199`.)

`Bank.Create(name, roundUpEnabled:)`, `Category.Create(name)` and the six-argument
`Expense.Create` are the real factory signatures used throughout
`Tests/Financial.CashFlow.Domain.Tests`. Static fixtures for immutable entities are fine; a
fixture the test mutates must be built per test. Use `AssertionScope` when one factory sets
many properties (`AssetTests.Create_SetsProperties`).

## When to skip

- A property with no logic (`Name { get; }` set once by the factory) — covered by the factory
  test, do not add a getter test.
- `Enums/` — no behaviour.
- Re-testing an entity through an Application service — the service test stubs the repository,
  not the entity, so the entity's own tests are the only place its rules are proven.

## Examples from project

- `Tests/Financial.CashFlow.Domain.Tests/Entities/ExpenseTests.cs` — Unit; factory guards,
  round-up bounds, payment-status transitions.
- `Tests/Financial.Investment.Domain.Tests/Domain/AssetTests.cs` — Unit; `Create` /
  `UpdateIdentity` with `AssertionScope`, deliberately left without a shared initializer because
  each test builds from its own input (`docs/rules/implementation.md` §Tests item 5).
- `Tests/Financial.Investment.Domain.Tests/Domain/XirrCalculatorTests.cs` — Unit; stateless
  algorithm with its own Domain test class, the criterion that justifies living in `Rules/`.
- `Tests/Financial.CashFlow.Domain.Tests/Rules/TitheRuleTests.cs` — Unit; round-trip property
  `NetOfTithe + CalculateTithe == amount`.
- `Tests/Financial.CashFlow.Domain.Tests/Entities/Collections/IdCollectionTests.cs` — Unit;
  duplicate rejection.
