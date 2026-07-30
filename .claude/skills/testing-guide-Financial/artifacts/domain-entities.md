> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Domain Entities & Rules (`*.Domain/Entities/*.cs`, `*.Domain/Rules/*.cs`)

Examples: `Asset`, `Broker`, `Portfolio`, `Transaction`, `Credit` (Investment); `Bank`, `Expense`, `Income`, `CardStatement`, `RecurringBill`, `ReserveMovement`, `InvestmentSnapshot`, `MaeLedgerEntry`, `InvestmentAccount` (CashFlow); calculators/classifiers `XirrCalculator`, `ProfitCalculator`, `DividendValuationRules`, `GlobalAssetClassMapping` (Investment) and `AnnualResultCalculator`, `CategoryClassifier`, `IncomeClassifier`, `ReserveSplitCalculator`, `YearScopedInvestmentAccountResolver` (CashFlow).

## What to test

- State changes after operations (e.g., `AddTransaction` recalculates `AveragePrice` and `Quantity`)
- Guard clauses that throw on invalid input (empty Id, null arguments, business rule violations)
- Factory method validation (correct initial state after `Create(...)`)
- State after multiple sequential operations (ordering matters for aggregate invariants)
- Boolean flags that change with lifecycle (e.g., `Active` becomes false after a full sell)
- Every branch in a calculator/classifier in `Rules/` (e.g., `ReserveSplitCalculator` splitting across buckets, `CategoryClassifier` mapping historical typos like "Casas" → `Casa`)

## Layer assignment

**Unit only** — domain entities and rules have zero external dependencies (no I/O, no framework), per the architecture rule that Domain must never depend on Infrastructure. Instantiate → call method → assert observable state. No mocks, no temp files, no async setup. If a "domain" test needs a stub or fake, the type has leaked infrastructure concerns and belongs in Application/Infrastructure instead.

## Setup pattern

```csharp
// Basic state change
[Fact]
public void MethodName_Condition_ExpectedResult()
{
    // Arrange — use factory methods, not constructors directly
    var entity = Entity.Create("Name", "Param");
    var dependency = DependentEntity.Create(/* params */);

    // Act
    entity.DoOperation(dependency);

    // Assert
    entity.Property.Should().Be(expectedValue);
    entity.Collection.Should().HaveCount(1);
}

// Guard clause
[Fact]
public void MethodName_WhenInputInvalid_Throws()
{
    var entity = Entity.Create("Name", "Param");
    var invalid = CreateInvalidInput();

    Action act = () => entity.DoOperation(invalid);

    act.Should().Throw<ArgumentException>();
}

// Multiple properties — use AssertionScope so all failures are reported
[Fact]
public void MethodName_AllPropertiesUpdated()
{
    var entity = Entity.Create(/* params */);

    entity.DoOperation(/* params */);

    using (new AssertionScope())
    {
        entity.Property1.Should().Be(expected1);
        entity.Property2.Should().Be(expected2);
        entity.Property3.Should().Be(expected3);
    }
}

// Rule/calculator — static or stateless, called directly
[Fact]
public void Split_WithPositiveAmount_AllocatesAcrossBuckets()
{
    var result = ReserveSplitCalculator.Split(1000m, buckets: [/* ... */]);

    result.Should().ContainSingle(r => r.Bucket == ReserveBucket.Investimento && r.Amount == 400m);
}
```

## When to skip

- Properties that are simple auto-properties with no logic
- Factory methods that assign fields with no validation (covered implicitly by behavior tests)
- Framework-managed lifecycle (EF Core tracking, etc. — not applicable here since persistence is JSON files, not an ORM)

## Examples from project

| Instance | What to test |
|---|---|
| `Asset.AddTransaction(Buy)` | `Quantity` and `AveragePrice` recalculate correctly; `Active` = true |
| `Asset.AddTransaction(Sell all)` | `Quantity` reaches 0; `Active` = false |
| `Asset.UpdateTransaction(empty Id)` | `ArgumentException` thrown |
| `Broker.Create(...)` | Initial state: name and currency set correctly |
| `Portfolio.AddAsset(...)` | Asset appears in `Assets` collection |
| `CategoryClassifier` | Branching over category strings including legacy typos → `[Theory]`+`[InlineData]` per mapping |
| `ReserveSplitCalculator.Split` | Every bucket-allocation branch, including zero-amount edge case |
