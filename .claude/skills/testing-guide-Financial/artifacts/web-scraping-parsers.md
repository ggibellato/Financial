> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Web-scraping Parsers (`GoogleFinance.cs`, `StatusInvest.cs` and their parsing helpers)

Located in `Integrations/WebPageParser`. These classes scrape live financial pages (Google Finance, StatusInvest) for asset prices/dividend data.

## What to test — and what NOT to

**Test:** the pure string-parsing functions in isolation — `GoogleFinanceParsing.ParsePriceValue`, `TryParseAsOf`, and similar. These take a string (a price fragment, a timestamp fragment) and return a parsed value; no HTTP, no HTML, no CSS selectors involved.

**Do NOT write automated tests for CSS selector correctness.** Whether `GoogleFinance.cs`'s selectors still match the live page's current HTML structure is verified **manually**, per `Integrations/WebPageParser/HOW_TO_VERIFY_SELECTORS.md` and `GoogleFinance.Selectors.md`. This is intentional: a fixture-based snapshot test only proves the selector matched the fixture, not that it still matches the live site — the real risk (silent selector drift when Google/StatusInvest change their markup) isn't something any recorded fixture can catch. Encoding this as an automated test would give false confidence.

## Layer assignment

Unit only, and only for the parsing-logic slice:

| Concern | Test? | How |
|---|---|---|
| String → value parsing (currency symbols, GBX scaling, "As of ..." timestamps) | Yes | Unit, `[Theory]`/`[Fact]` on literal string inputs |
| CSS selector still matches live page markup | No — manual | Follow `HOW_TO_VERIFY_SELECTORS.md` when the scraper starts returning wrong/empty data |
| Actual HTTP fetch of the live page | No | Not exercised in CI; would be flaky, slow, and blocked by anti-scraping measures |

## Setup pattern

```csharp
public class GoogleFinanceParsingTests
{
    [Fact]
    public void ParsePriceValue_WithCurrencySymbol_ReturnsExpectedValue()
    {
        var result = GoogleFinanceParsing.ParsePriceValue("R$ 100");

        result.Should().Be(100m);
    }

    [Fact]
    public void ParsePriceValue_WithGbxValue_ScalesDown()
    {
        var result = GoogleFinanceParsing.ParsePriceValue("GBX100");

        result.Should().Be(1m); // GBX (pence) -> GBP
    }

    [Fact]
    public void TryParseAsOf_WithUtcOffset_ReturnsParsedValue()
    {
        var result = GoogleFinanceParsing.TryParseAsOf("As of Sep 1, 3:45:00 PM UTC+1");

        result!.Value.Offset.Should().Be(TimeSpan.FromHours(1));
    }
}
```

## When to skip

- Don't build an HTML-fixture harness "to be thorough" — it tests the wrong thing (see above) and adds maintenance burden with no corresponding safety benefit
- If a scraper starts silently returning nulls/wrong values in production, that's a signal to re-run the manual selector verification, not to add a fixture test

## Examples from project

- `GoogleFinanceParsingTests`, `StatusInvestTests` — pure parsing function coverage
- `GoogleFinanceVerificationTests`, `StatusInvestVerificationTests` — check the doc/checklist framework the manual process itself uses, not the scraper's live correctness
