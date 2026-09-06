> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Web Scraping Parsers (`Integrations/WebPageParser/*.cs`)

`GoogleFinance`, `StatusInvest` and `DadosMercadoDividend` fetch third-party HTML with
HtmlAgilityPack and pick values out with CSS selectors (`GoogleFinanceSelectors`). The pure
string/number parsing is split out (`GoogleFinanceParsing`, `StatusInvest.DeriveSlug`,
`GoogleFinanceVerifier`) precisely so it can be tested without the page.

## What to test

- `GoogleFinanceParsing.ParsePriceValue("R$ 100")`, `"GBX100"` scaling, thousands separators,
  `TryParseAsOf(null)` → `null`, UTC-offset strings — every accepted format and every
  rejected/unparseable input.
- `StatusInvest.DeriveSlug` for each bond family (`Selic`, `IPCA+`, `Juros Semestrais`) and the
  accent/`+` stripping rules.
- Cryptocurrency URL building (`GoogleFinanceCryptocurrencyUrlTests`).
- `DadosMercadoDividend` row parsing from a saved HTML fragment: a dividend row, a row with a
  missing column, an empty table.
- Negative: null/blank input, a fragment whose structure changed (selector finds nothing) →
  the documented failure (`null`, empty list, or exception) — never a silent `0m`.

## Layer assignment

- **Unit** for all parsing functions and slug/URL builders — pure, deterministic.
- **Integration with the site faked** for the HtmlAgilityPack extraction: feed a saved HTML
  string into the parsing path (the page is an external provider —
  `../references/external-providers.md`). Today only `DadosMercadoDividendTests` does this
  with inline fragments; new selectors should ship with a fixture.
- **Manual, skipped in CI**: `GoogleFinanceVerificationTests` and `StatusInvestVerificationTests`
  carry `[Fact(Skip = "Manual verification test - requires internet connection")]` and hit the
  live pages. Run them by hand when a selector changes; never remove the `Skip`.
- No E2E.

## Setup pattern

```csharp
using Financial.Integrations.WebPageParser;
using FluentAssertions;

namespace Financial.WebPageParser.Tests;

public class GoogleFinanceParsingTests
{
    [Fact]
    public void ParsePriceValue_WithGbxValue_ScalesDown()
    {
        var result = GoogleFinanceParsing.ParsePriceValue("GBX100");

        result.Should().Be(1m);
    }

    [Fact]
    public void TryParseAsOf_WhenValueIsNull_ReturnsNull()
    {
        var result = GoogleFinanceParsing.TryParseAsOf(null);

        result.Should().BeNull();
    }
}
```

(Verbatim from `Tests/Financial.WebPageParser.Tests/GoogleFinanceParsingTests.cs`.) For a
fixture-driven extraction test, keep the HTML fragment as a `const string` next to the test or
under a `TestData/` folder in `Tests/Financial.WebPageParser.Tests`; the assertion is on the
parsed `WebAssetQuote` / `WebDividendRecord`.

## When to skip

- Selector strings themselves (`GoogleFinanceSelectors`) — they are only meaningful against the
  live page; that is the manual verification run's job.
- HtmlAgilityPack's own parsing.

## Examples from project

- `Tests/Financial.WebPageParser.Tests/GoogleFinanceParsingTests.cs` — Unit.
- `Tests/Financial.WebPageParser.Tests/StatusInvestTests.cs` — Unit; `DeriveSlug` families.
- `Tests/Financial.WebPageParser.Tests/GoogleFinanceCryptocurrencyUrlTests.cs` — Unit.
- `Tests/Financial.WebPageParser.Tests/DadosMercadoDividendTests.cs` — Integration (site faked by inline HTML).
- `Tests/Financial.WebPageParser.Tests/GoogleFinanceVerificationTests.cs`, `StatusInvestVerificationTests.cs` — manual, `Skip`ped.
