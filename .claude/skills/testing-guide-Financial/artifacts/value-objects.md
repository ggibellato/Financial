> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Value Objects (Immutable types in `Financial.Domain/`)

## What to test

- **Equality**: two instances with same data are equal; different data are not equal
- **Immutability**: operations return new instances rather than mutating the original
- **Construction validation**: invalid inputs are rejected at construction time (e.g., negative amounts, null strings)
- **Canonical form**: if a VO normalizes or transforms input (e.g., trims whitespace, normalizes currency code), assert the stored form

## Layer assignment

**Unit only** — value objects are pure data types with no external dependencies.

## Setup pattern

```csharp
// Equality
[Fact]
public void TwoInstances_WithSameData_AreEqual()
{
    var a = ValueObject.Create("same");
    var b = ValueObject.Create("same");

    a.Should().Be(b);
}

[Fact]
public void TwoInstances_WithDifferentData_AreNotEqual()
{
    var a = ValueObject.Create("one");
    var b = ValueObject.Create("two");

    a.Should().NotBe(b);
}

// Invalid construction
[Fact]
public void Create_WithNullInput_Throws()
{
    Action act = () => ValueObject.Create(null!);

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

- C# `record` types where the compiler generates structural equality and there is no custom validation or normalization logic

## Examples from project

Document value object instances here as they are added to the domain. As of the current codebase, value objects are embedded within entity classes rather than as standalone types. If a value type is extracted into its own class, add it here with its specific test scenarios.
