# External Integrations

See legend in [README.md](README.md).

## Confirmed asymmetry between contexts

**CONFIRMED** — Investment has roughly ten external-integration classes; CashFlow has exactly one. This reflects a real domain difference (Investment inherently needs live market/price data; CashFlow is closer to a static ledger), not an oversight — **INFERRED**.

## Investment context

| Integration | Used for | Mechanism |
|---|---|---|
| Google Finance | Primary price source for standard (non-bond, non-crypto) assets and crypto | HTML scrape via `Integrations/WebPageParser` (`HtmlAgilityPack`) |
| Yahoo Finance | Fallback price source for standard (exchange-based) assets only | Public, unauthenticated HTTP endpoint |
| Status Invest | Price source for Bond-classified assets, looked up by asset **Name** | HTML scrape |
| Google Drive API | `GoogleDrive` storage provider (shared with CashFlow — see [07-data-persistence.md](07-data-persistence.md)); also the legacy Google Sheets import path used only by `Tools/ImportGoogleSpreadSheets` | `Google.Apis.Drive.v3` / `Google.Apis.Sheets.v4`, via `Integrations/GoogleFinancialSupport` |

**Price fetch dispatch — CONFIRMED REQUIREMENT** (matches P08/P09):

- `GlobalAssetClass.Bond` → `BondAssetPriceFetcher` → Status Invest, by Name.
- `Cryptocurrency` → `CryptocurrencyAssetPriceFetcher` → `IFinanceService.GetAssetValue(brokerCurrency, Ticker)`.
- Everything else → `StandardAssetPriceFetcher` → `IFinanceService.GetAssetValue(Exchange, Ticker)`.

**Fallback chain — CONFIRMED REQUIREMENT.** `IFinanceService` is wired as `FallbackFinanceService(primary: GoogleFinanceService, fallback: YahooFinanceService)`. Yahoo is retried **only** for exchange-based (non-crypto) requests. **Confirmed reason: Google does not have cryptocurrency price data** — crypto pricing is architecturally excluded from this fallback chain for that reason.

**Status Invest was chosen over Tesouro Direto's own site — CONFIRMED REQUIREMENT, explicit in P09 §2:** Tesouro Direto serves a Cloudflare JS challenge unreachable by the HTML scraper.

## CashFlow context

| Integration | Used for | Mechanism |
|---|---|---|
| Frankfurter (FX rates) | Historical exchange-rate lookups for Controle Mãe ledger entries (see [10-domain-cashflow.md](10-domain-cashflow.md)) | HTTP, via `FrankfurterExchangeRateProvider` |
| Google Drive API | `GoogleDrive` storage provider (shared with Investment) | Same `GoogleFinancialSupport` integration as above |

**UNKNOWN** — whether `FrankfurterExchangeRateProvider`'s target host is confirmed to be frankfurter.app; inferred from naming only, not independently verified against source.

## Known follow-up

`DividendDataSourceAdapter` (Investment context, wraps `DadosMercadoDividend.GetDividendInfo(ticker)`) currently accepts an `(exchange, ticker)` signature but only forwards `ticker`. **Confirmed by the product owner to be an error** — the method should not accept an `exchange` parameter at all. Not fixed as part of this documentation baseline; see [09-domain-investment.md](09-domain-investment.md).
