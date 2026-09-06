> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Application Parsers and Validators (`Financial.*.Application/Validation/*.cs`)

## What to test

- Every accepted spelling (`"GBP"`, `"gbp"`, `" GBP "`) and every rejected input (`null`, `""`,
  whitespace, unknown value) for `CurrencyParser`, `AreaParser`, `BillStatusParser`,
  `IncomeGroupParser`, `CreditTypeParser`, `TransactionTypeParser`, `InvestmentScopeParser`
  (all thin wrappers over the shared `EnumParser`).
- `EntityIdResolver` / `EntityLookupExtensions`: id found, id missing (`KeyNotFoundException`
  naming the id), inactive entity rejected where the rule says so.
- `DescriptionValidator`, `IsinValidator`, `AssetContextValidator`: the boundary length, the
  malformed check digit, the missing context — one test per rejection, one for the accepted
  boundary.
- The exception **type** and, where the message is part of the contract that
  `DomainExceptionMappingMiddleware` maps to 400/404, the message shape.

## Layer assignment

- **Unit only.** Pure functions over strings/ids; the lookup input is a dictionary or
  `StubCashFlowRepository`. No Integration or E2E of their own — the API's 400 mapping for a bad
  enum string is proven once per endpoint in `api-controllers-and-middleware.md`.

## Setup pattern

```csharp
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class CurrencyParserTests
{
    [Theory]
    [InlineData("GBP", Currency.GBP)]
    [InlineData("brl", Currency.BRL)]
    public void TryParse_KnownCode_ReturnsTrueAndCurrency(string input, Currency expected)
    {
        var result = CurrencyParser.TryParse(input, out var currency);

        result.Should().BeTrue();
        currency.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("XYZ")]
    public void TryParse_UnknownOrBlank_ReturnsFalse(string? input)
    {
        CurrencyParser.TryParse(input, out _).Should().BeFalse();
    }
}
```

`public static bool TryParse(string? value, out Currency currency)` is the real signature
(`Financial.CashFlow.Application/Validation/CurrencyParser.cs:7`); other parsers may expose a
throwing `Parse` — copy the real name from the parser under test. Each parser has its own file (`AreaParserTests.cs`, `BillStatusParserTests.cs`,
`CurrencyParserTests.cs`, `EntityIdResolverTests.cs` under
`Tests/Financial.CashFlow.Application.Tests/Validation/`).

## When to skip

- `EnumParser` itself, once two wrappers over it are tested — a third identical wrapper test is
  variant repetition; test only the wrapper's own added rule.
- Re-asserting the parser inside every service test that calls it.

## Examples from project

- `Tests/Financial.CashFlow.Application.Tests/Validation/CurrencyParserTests.cs` — Unit.
- `Tests/Financial.CashFlow.Application.Tests/Validation/EntityIdResolverTests.cs` — Unit;
  missing-id failure names the id.
- `Tests/Financial.Investment.Application.Tests/Validation/InvestmentScopeParserTests.cs` — Unit.
