> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Application Parsers & Validators (`*Parser.cs`, `*Validator.cs`, `*Resolver.cs` in `*.Application/Validation/`)

Examples: `CreditTypeParser` (Investment); `AreaParser`, `BankNameResolver`, `BillStatusParser`, `CategoryParser`, `CreditCardParser`, `CurrencyParser`, `EnumParser`, `IncomeSourceParser`, `ReserveBucketParser` (CashFlow).

## What to test

- **Normalization**: valid inputs map to their canonical form (case-insensitive matching, trimming, etc.)
- **Rejection**: invalid or unknown inputs return `false` / throw, with no output value
- **Null and empty**: explicit cases for `null`, `""`, `"   "` (whitespace-only)
- **All known branches**: each recognized value in a `switch`/`if` chain has at least one passing test, including documented historical aliases/typos (e.g., `CategoryParser` mapping "Casas" → `Category.Casa`)

## Layer assignment

**Unit only** — parsers and validators are pure logic with no external dependencies or I/O. No setup beyond constructing the input string.

## Setup pattern

```csharp
// Parameterized happy path — one [InlineData] row per canonical value + case variant
[Theory]
[InlineData("Dividend", "Dividend")]
[InlineData("DIVIDEND", "Dividend")]
[InlineData("Mercado", "Mercado")]
[InlineData("Casas", "Casa")] // historical typo
public void TryNormalize_WhenValueMatches_ReturnsCanonicalValue(string input, string expected)
{
    var result = Parser.TryNormalize(input, out var normalized);

    result.Should().BeTrue();
    normalized.Should().Be(expected);
}

// Null, empty, whitespace, and unknown — grouped in one theory
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
[InlineData("NotAKnownType")]
public void TryNormalize_WhenInvalid_ReturnsFalse(string? input)
{
    var result = Parser.TryNormalize(input, out _);

    result.Should().BeFalse();
}
```

**`[MemberData]` rule**: when data doesn't fit `[InlineData]` (non-primitive values), always use `nameof()` — `[MemberData(nameof(NullValues))]` not `[MemberData("NullValues")]`, for rename safety.

## When to skip

- Validation that simply delegates to a .NET framework attribute (`[Required]`, `[Range]`) — the framework tests its own behavior
- A parser that's a 1:1 `Enum.Parse` wrapper with no aliasing/normalization logic

## Examples from project

| Instance | Test focus |
|---|---|
| `CreditTypeParser.TryNormalize` | All known types (Dividend, Rent, ...) in multiple casing variants; null; empty string; whitespace; unknown string |
| `CategoryParser.TryResolve` | Recognized names + documented historical typo ("Casas") + unknown/blank |
| `CurrencyParser` | BRL/GBP/USD symbols and codes, plus one invalid case |

When adding new parsers, use `CreditTypeParser` and its test file (`CreditTypeParserTests.cs`) as the reference implementation.
