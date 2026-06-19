> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Domain Entities (Classes in `Financial.Domain/`)

## What to test

- State changes after operations (e.g., `AddTransaction` recalculates `AveragePrice` and `Quantity`)
- Guard clauses that throw on invalid input (empty Id, null arguments, business rule violations)
- Factory method validation (correct initial state after `Create(...)`)
- State after multiple sequential operations (ordering matters for aggregate invariants)
- Boolean flags that change with lifecycle (e.g., `Active` becomes false after a full sell)

## Layer assignment

**Unit only** — domain entities have zero external dependencies (no I/O, no framework). Instantiate → call method → assert observable state. No mocks, no temp files, no async setup.

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
```

## When to skip

- Properties that are simple auto-properties with no logic
- Factory methods that assign fields with no validation (covered implicitly by behavior tests)
- Framework-managed lifecycle (EF Core tracking, etc.)

## Examples from project

| Instance | What to test |
|---|---|
| `Asset.AddTransaction(Buy)` | `Quantity` and `AveragePrice` recalculate correctly; `Active` = true |
| `Asset.AddTransaction(Sell all)` | `Quantity` reaches 0; `Active` = false |
| `Asset.UpdateTransaction(empty Id)` | `ArgumentException` thrown |
| `Asset.AddTransaction` × 2 | Weighted average price across two buys |
| `Broker.Create(...)` | Initial state: name and currency set correctly |
| `Portfolio.AddAsset(...)` | Asset appears in `Assets` collection |
| `Transaction.Create(...)` | Type, quantity, price, fees set correctly |
| `Credit.Create(...)` | Date, type, value set correctly |
