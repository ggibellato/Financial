> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Domain Value Objects (`*.Domain/ValueObjects/*.cs`)

Examples: `AssetValueSnapshot`, `DividendType`, `DividendValue` (Investment); `MonthlySeries` (CashFlow).

## What to test

- **Equality**: two instances with same data are equal; different data are not equal
- **Immutability**: operations return new instances rather than mutating the original
- **Construction validation**: invalid inputs are rejected at construction time (e.g., negative amounts, null strings)
- **Canonical form**: if a VO normalizes or transforms input (e.g., trims whitespace, normalizes currency code), assert the stored form
- **Computed properties**: e.g., a `MonthlySeries` total or average derived from underlying values

## Layer assignment

**Unit only** — value objects are pure data types with no external dependencies, same reasoning as `artifacts/domain-entities.md`.

## Setup pattern

```csharp
// Equality
[Fact]
public void TwoInstances_WithSameData_AreEqual()
{
    var a = new DividendValue(10m, DividendType.Dividend);
    var b = new DividendValue(10m, DividendType.Dividend);

    a.Should().Be(b);
}

// Invalid construction
[Fact]
public void Constructor_WithNegativeAmount_ThrowsArgumentException()
{
    Action act = () => new DividendValue(-1m, DividendType.Dividend);

    act.Should().Throw<ArgumentException>();
}

// Immutability — operation returns new instance
[Fact]
public void Operation_ReturnsNewInstance_OriginalUnchanged()
{
    var original = ValueObject.Create("value");

    var result = original.DoOperation();

    result.Should().NotBeSameAs(original);
    original.Property.Should().Be("value"); // unchanged
}

// Canonical form
[Theory]
[InlineData("usd", "USD")]
[InlineData(" USD ", "USD")]
public void Create_NormalizesInput(string input, string expected)
{
    var vo = ValueObject.Create(input);

    vo.Value.Should().Be(expected);
}
```

## When to skip

- Static structure assertions (field exists, field has expected type) — test behavior/validation instead
- C# `record` types where the compiler generates structural equality and there is no custom validation or normalization logic

## Examples from project

- `MonthlySeries` — has a computed average/total → unit test the computation, not the raw stored values
- `AssetValueSnapshot` — carries a snapshot price + timestamp with no branching logic → construction test only, skip further coverage
