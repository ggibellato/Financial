> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Application Parsers / Validators (`*Parser.cs`, `*Validator.cs` in `Financial.Application/`)

## What to test

- **Normalization**: valid inputs map to their canonical form (case-insensitive matching, trimming, etc.)
- **Rejection**: invalid or unknown inputs return `false` / throw, with no output value
- **Null and empty**: explicit cases for `null`, `""`, `"   "` (whitespace-only)
- **All known branches**: each recognized value in a `switch`/`if` chain has at least one passing test

## Layer assignment

**Unit only** — parsers and validators are pure logic with no external dependencies or I/O. No setup beyond constructing the input string.

## Setup pattern

```csharp
// Parameterized happy path — one [InlineData] row per canonical value + case variant
[Theory]
[InlineData("Dividend", "Dividend")]
[InlineData("DIVIDEND", "Dividend")]
[InlineData("dividend", "Dividend")]
[InlineData("Rent", "Rent")]
[InlineData("rENT", "Rent")]
public void TryNormalize_WhenValueMatches_ReturnsCanonicalValue(string input, string expected)
{
    var result = Parser.TryNormalize(input, out var normalized);

    result.Should().BeTrue();
    normalized.Should().Be(expected);
}

// Null — cannot use [InlineData] for null; use [MemberData] with nameof
public static IEnumerable<object?[]> NullValues => new[] { new object?[] { null } };

[Theory]
[MemberData(nameof(NullValues))]
public void TryNormalize_WhenNull_ReturnsFalseAndEmpty(string? input)
{
    var result = Parser.TryNormalize(input, out var normalized);

    result.Should().BeFalse();
    normalized.Should().BeEmpty();
}

// Empty and whitespace
[Theory]
[InlineData("")]
[InlineData("   ")]
public void TryNormalize_WhenEmptyOrWhitespace_ReturnsFalse(string input)
{
    var result = Parser.TryNormalize(input, out _);

    result.Should().BeFalse();
}

// Unknown value
[Fact]
public void TryNormalize_WhenUnknownValue_ReturnsFalse()
{
    var result = Parser.TryNormalize("NotAKnownType", out _);

    result.Should().BeFalse();
}
```

**`[MemberData]` rule**: always use `nameof()` — `[MemberData(nameof(NullValues))]` not `[MemberData("NullValues")]`.

## When to skip

- Validation that simply delegates to a .NET framework attribute (`[Required]`, `[Range]`) — the framework tests its own behavior

## Examples from project

| Instance | Test focus |
|---|---|
| `CreditTypeParser.TryNormalize` | All known types (Dividend, Rent, ...) in multiple casing variants; null; empty string; whitespace; unknown string |

When adding new parsers, use `CreditTypeParser` and its test file (`CreditTypeParserTests.cs`) as the reference implementation.
