# BR Bond Finance Service Integration

## 1. Executive Summary

BR Bond Finance Service Integration closes a gap explicitly left open by the P08 Asset Snapshot Fetch Strategy refactor: `GlobalAssetClass.Bond` assets — Brazilian Tesouro Direto government bonds — currently fall through to `StandardAssetPriceFetcher` and are priced via Google Finance, a source that does not list these instruments. Every Bond-class asset in the application today either fails to resolve a price or resolves one that has nothing to do with its actual Tesouro Direto value.

This feature fixes that by adding two new public data sources — tesourodireto.com.br's official redemption-price table and statusinvest.com.br's per-bond page — and wiring them into a dedicated Bond fetcher that plugs into the existing `IAssetPriceFetcher` strategy dispatch from P08. Getting there also means paying down a piece of technical debt the current architecture wasn't yet asked to solve: `GoogleFinance` is a static class called directly by fetchers, with no shared shape that a second or third scraping source could reuse. This feature introduces a common `IFinanceService` contract, refactors `GoogleFinance` behind it with no behavior change, and gives the two new Tesouro Direto sources the same first-class, independently-testable shape Google Finance now has.

The result: Bond assets get a real, live current price — Tesouro Direto as the authoritative source, Status Invest as an automatic fallback when Tesouro Direto can't resolve the bond — and the next website-based finance source after this one is a new class plus a DI registration line, not a rewrite of existing fetchers.

---

## 2. Problem and Opportunity

### The Problem

**Bond assets resolve to the wrong price source**
- `StandardAssetPriceFetcher.Supports` returns `true` for every `GlobalAssetClass` except `Cryptocurrency` — including `Bond` — so every Tesouro Direto bond today is priced via `GoogleFinance.GetFinancialInfoSnapshot`, a source that has no listing for these instruments
- This was a known, documented trade-off: P08 explicitly locked in "Bond assets dispatch to Standard" as today's guarantee and called out Tesouro Direto integration as its own future feature
- Every Bond asset the user tracks today either fails to fetch a price or silently returns an unrelated value

**Website-scraping logic has no shared contract**
- `GoogleFinance` is a static class in `Integrations/WebPageParser/`, called directly by `StandardAssetPriceFetcher` and `CryptocurrencyAssetPriceFetcher` — there is no interface a fetcher depends on, only a concrete static type
- Adding a second scraping source today means inventing its shape from scratch, with no shared seam for injecting a fake in tests the way `IAssetPriceFetcher` already allows for asset-class strategies
- This feature needs two new scraping sources (Tesouro Direto, Status Invest); building both without first isolating the pattern Google Finance already established would mean three inconsistent shapes instead of one shared one

**No resilience across the two public BR bond sources**
- Tesouro Direto's own site is the authoritative source for redemption prices, but its table is keyed by the bond's official title and depends on a page structure that can change
- Status Invest exposes the same figure on a per-bond page keyed by a URL slug, but that slug doesn't always map predictably to the bond's title
- With only one source wired in, any single scrape failure — page down, structure changed, title not found — means no price at all for that bond, with no automatic second attempt

### The Opportunity

- Wrong price source → introduce a dedicated `IAssetPriceFetcher` implementation for `Bond` that is tried before falling back to Standard, so Bond assets stop being priced via Google Finance entirely
- No shared scraping contract → introduce `IFinanceService`, implemented first by a refactored `GoogleFinanceService` (zero behavior change), then by `TesouroDiretoFinanceService` and `StatusInvestFinanceService` — three sources sharing one contract, one testing pattern, one DI convention
- No resilience → the new Bond fetcher tries `TesouroDiretoFinanceService` first and automatically falls back to `StatusInvestFinanceService` when the bond can't be resolved there, so a single source's failure no longer means no price

---

## 3. Target Audience

### Primary Users

**Developer-Maintainer**
- Sole developer and sole end user of this personal-use financial application, who holds Brazilian Tesouro Direto bonds and currently sees them priced incorrectly (or not at all) in the portfolio view
- Values low-friction extension points over upfront generality — per this project's standing architecture rules, the bar is "cheap to add the next source," not "infinitely generic"
- Directly benefits from this feature every time the app refreshes current values: the difference between a real Tesouro Direto redemption price and a meaningless Google Finance lookup is the entire point

---

## 4. Objectives

**Resolve real current prices for Tesouro Direto bond assets**
- Metric: 100% of `GlobalAssetClass.Bond` price requests are dispatched to the new Bond fetcher, and zero are dispatched to `StandardAssetPriceFetcher`/Google Finance

**Unify website-scraping sources behind one contract**
- Metric: no fetcher or service calls `GoogleFinance`'s static methods directly; `StandardAssetPriceFetcher` and `CryptocurrencyAssetPriceFetcher` depend on an injected `IFinanceService`-shaped dependency instead

**Make Tesouro Direto the authoritative source, with automatic fallback**
- Metric: when a bond's title matches a row in the Tesouro Direto table, its price always comes from that source; only when no match is found (or the fetch fails) does the price come from Status Invest

**Preserve 100% of existing Equity and Cryptocurrency price-fetch behavior**
- Metric: every existing `AssetPriceServiceTests`, `StandardAssetPriceFetcherTests`, and `CryptocurrencyAssetPriceFetcherTests` scenario still passes with unchanged expected outcomes, except the one test locking in "Bond dispatches to Standard," which is intentionally flipped by this feature

**Make the next website-based finance source cheap to add**
- Metric: adding a hypothetical fourth `IFinanceService` implementation requires exactly one new class and one DI registration line, with zero edits to `GoogleFinanceService`, `TesouroDiretoFinanceService`, or `StatusInvestFinanceService`

---

## 5. User Stories

### F01. Finance Service Common Interface
- As the developer, I want a common `IFinanceService` contract so that Google Finance and any future website-based price source share one shape, testable and injectable the same way
- As the developer, I want today's Google Finance scraping logic wrapped in a `GoogleFinanceService` implementing that contract, with zero change to its URL-building or HTML-parsing logic, so today's Equity and Cryptocurrency price fetching keeps working exactly as it does now

### F02. Tesouro Direto Finance Service
- As the system, I want to fetch the official Tesouro Direto redemption-price table and find the row whose title matches a given bond, so the bond's authoritative current price is available
- As the system, I want a bond whose title has no matching row in that table to be reported as not found, rather than erroring, so the caller can try a fallback source

### F03. Status Invest Finance Service
- As the system, I want to derive a Status Invest URL slug from a bond's title and fetch that bond's page, so its current unit value ("Valor Unitário") is available as a fallback price source
- As the system, I want a bond whose derived slug doesn't resolve to a valid page to be reported as not found, consistent with how the Tesouro Direto source reports a miss

### F04. BR Bond Price Fetcher
- As the system, I want `Bond`-class price requests to be dispatched to a dedicated fetcher instead of falling through to Google Finance, so Tesouro Direto bonds get an accurate price
- As the system, I want that fetcher to try the Tesouro Direto source first and automatically fall back to Status Invest when the bond isn't found there, so a single source's gap doesn't mean no price
- As the developer, I want `StandardAssetPriceFetcher` to stop claiming `Bond` in its `Supports` check once this fetcher exists, so dispatch ordering can't accidentally route bonds back to Google Finance

---

## 6. Functionalities

### F01. Finance Service Common Interface

**Capabilities:**
- New `IFinanceService` interface in `Financial.Infrastructure/Interfaces/`, with a single `AssetValueSnapshot GetAssetValue(AssetValueRequest request)` member. This interface — like the existing `IAssetPriceFetcher` (relocated here from `Financial.Application/Interfaces/` as part of this feature) — is referenced exclusively within `Financial.Infrastructure`, never by Application or Presentation, so it is placed in Infrastructure rather than following this codebase's default convention of declaring every interface in Application regardless of consumer
- New `AssetValueRequest` type carrying every field a website-based source might need to resolve a current asset value: `Ticker`, `Exchange`, `Currency`, and `Name` (the last added specifically so title-keyed sources — Tesouro Direto, Status Invest — don't need a ticker/exchange pair at all); each concrete service reads only the fields relevant to it and ignores the rest
- New `GoogleFinanceService` in `Financial.Infrastructure/Services/` implementing `IFinanceService`: `GetAssetValue` inspects the request and calls `GoogleFinance.GetFinancialInfoSnapshot(exchange, ticker)` when `Exchange` is populated, or `GoogleFinance.GetCryptocurrencyFinancialInfoSnapshot(currency, ticker)` when `Currency` is populated — the existing static `GoogleFinance` class's URL-building, HTML parsing, and selectors are not modified
- `StandardAssetPriceFetcher` and `CryptocurrencyAssetPriceFetcher` are updated to take `GoogleFinanceService` (via `IFinanceService`) as a constructor dependency instead of calling the static `GoogleFinance` class directly; the request they build for it carries exactly the `Exchange`/`Ticker` or `Currency`/`Ticker` pair each already builds today
- Registered in DI (`InfrastructureServiceCollectionExtensions`) as a singleton, resolved by the two existing fetchers through the constructor change above

**Experience:**
- No caller-visible change: every existing Equity and Cryptocurrency price fetch produces the exact same request, scrape, and response shape as before this feature — only the internal call path (through `IFinanceService` instead of a static class reference) changes

### F02. Tesouro Direto Finance Service

**Provides:**
- Current unit price ("Preço Unit.") and as-of date for a bond matched by title, or a not-found signal when no row matches (used by F04)

**Capabilities:**
- New `TesouroDiretoFinanceService` in `Financial.Infrastructure/Services/` (backed by a new `Integrations/WebPageParser/TesouroDireto.cs` scraper, alongside the existing `GoogleFinance.cs` and `DadosMercadoDividend.cs`), implementing `IFinanceService`
- `GetAssetValue` fetches the tesourodireto.com.br rendimento-dos-titulos redemption dataset (the data shown under the page's "Resgatar" view) and looks for the row whose `Titulo` column matches `request.Name` case-insensitively, ignoring leading/trailing whitespace differences
- On a match, returns the row's "Preço Unit." value as the price and the data's reference date as the as-of timestamp
- On no match, returns a not-found result (no exception) so `F04` can attempt the fallback source
- If the page's Resgatar dataset is rendered client-side rather than present in the static HTML, the implementation targets whichever underlying data source (API response or embedded data block) the page uses to populate that view, following the project's existing per-source scraping convention of isolating the exact selectors/endpoint in one reviewable place (mirroring `GoogleFinanceSelectors.cs`)
- Registered in DI as a singleton `TesouroDiretoFinanceService`

**Experience:**
- No caller-visible change on its own: this feature introduces a new type and DI registration that only `F04` consumes

### F03. Status Invest Finance Service

**Provides:**
- Current unit value ("Valor Unitário") and as-of date for a bond matched by derived slug, or a not-found signal when the slug doesn't resolve (used by F04)

**Capabilities:**
- New `StatusInvestFinanceService` in `Financial.Infrastructure/Services/` (backed by a new `Integrations/WebPageParser/StatusInvest.cs` scraper), implementing `IFinanceService`
- `GetAssetValue` derives a URL slug from `request.Name` using a generic rule: lowercase the title, strip accents, remove `+` and other punctuation, collapse whitespace, and join words with hyphens (e.g., "TESOURO IPCA+ 2029" → "tesouro-ipca-2029")
- Fetches `https://statusinvest.com.br/tesouro/{slug}` and extracts the "Valor Unitário" field as the price, with the page's displayed reference date (or the fetch time, if none is shown) as the as-of timestamp
- If the derived slug's page returns a 404 or the page loads but has no "Valor Unitário" element, returns a not-found result (no exception), consistent with F02's not-found signal
- Registered in DI as a singleton `StatusInvestFinanceService`

**Experience:**
- No caller-visible change on its own: this feature introduces a new type and DI registration that only `F04` consumes

### F04. BR Bond Price Fetcher

**Consumes:**
- F02: current unit price and as-of date for a bond matched by title, or not-found
- F03: current unit value and as-of date for a bond matched by derived slug, or not-found

**Capabilities:**
- New `BondAssetPriceFetcher` in `Financial.Infrastructure/Services/` implementing `IAssetPriceFetcher`; `Supports` returns `true` only for `GlobalAssetClass.Bond`
- `AssetPriceRequestDTO` gains a `Name` field, populated from the requested asset's `Asset.Name`; this field is used only by the Bond dispatch path — Equity and Cryptocurrency call sites leave it unpopulated, and `StandardAssetPriceFetcher`/`CryptocurrencyAssetPriceFetcher` ignore it, so no behavior change for non-Bond requests
- `GetSnapshot` validates that `Name` is non-blank (throwing `ArgumentException` otherwise, matching the existing validation style of the other fetchers), builds a `AssetValueRequest` with `Name` set, and calls `TesouroDiretoFinanceService.GetAssetValue` first
- If Tesouro Direto returns a match, that result is returned as the `AssetValueSnapshot` and Status Invest is not called
- If Tesouro Direto returns not-found, `StatusInvestFinanceService.GetAssetValue` is called with the same request; its result (match or not-found) becomes the fetcher's outcome
- If neither source finds the bond, `GetSnapshot` fails in the same manner `StandardAssetPriceFetcher` does today when Google Finance cannot resolve a ticker — no bond-specific silent fallback price is introduced
- `StandardAssetPriceFetcher.Supports` is updated to also exclude `GlobalAssetClass.Bond` (alongside its existing `Cryptocurrency` exclusion), so dispatch ordering can't route a Bond request back to Google Finance even as a fallback
- Registered in DI alongside the existing two fetchers as another `IAssetPriceFetcher`

**Experience:**
- From every caller's perspective — the `/prices/current` API endpoint, the WPF "Current Values" refresh, the WPF Asset Details "Refresh" action — fetching a Bond asset's price now returns its real Tesouro Direto (or Status Invest) redemption value instead of a Google Finance lookup; the request/response shape (`AssetPriceDTO`) is unchanged
- A bond correctly priced via Tesouro Direto shows no visible difference from one that had to fall back to Status Invest — both look like a normal successful price refresh

---

## 7. Out of Scope

**Persisting or caching bond prices**
- Not addressed by this feature; every price fetch remains a live, uncached network call to Tesouro Direto and/or Status Invest, exactly as Google Finance fetches work today

**Historical prices, yield curves, or indexer metadata**
- Only the current unit price ("Preço Unit." / "Valor Unitário") is fetched; historical redemption prices, indicative rates, indexer type (IPCA/Selic/Prefixado), and maturity yield are not part of this feature

**Authenticated Tesouro Direto purchase, sale, or redemption flows**
- This feature only reads public price data from tesourodireto.com.br and statusinvest.com.br; it does not integrate with any brokerage or authenticated Tesouro Direto account action

**Additional finance sources beyond Google Finance, Tesouro Direto, and Status Invest**
- The `IFinanceService` contract is designed to make a future fourth source cheap to add, but no other source (e.g., other countries' government bond sites, alternative BR data providers) is built as part of this feature

**UI changes to show price provenance**
- The application does not indicate in the UI whether a Bond's price came from Tesouro Direto or Status Invest; both are surfaced identically through the existing `AssetPriceDTO` shape

**Automatic detection of source page structure changes**
- Consistent with today's Google Finance convention, if tesourodireto.com.br or statusinvest.com.br change their page structure, the break is caught by manual verification/code review, not by an automated alerting mechanism

**Changes to `GoogleFinance.cs`'s internal scraping logic**
- `GoogleFinance.GetFinancialInfoSnapshot` and `GoogleFinance.GetCryptocurrencyFinancialInfoSnapshot` are wrapped, not modified; their URL-building, HTML parsing, and selector logic stay exactly as they are today

---

## 8. Dependency Graph

| # | Feature | Priority | Dependencies |
|---|---------|----------|--------------|
| F01 | Finance Service Common Interface | 1 | None |
| F02 | Tesouro Direto Finance Service | 1 | F01 |
| F03 | Status Invest Finance Service | 1 | F01 |
| F04 | BR Bond Price Fetcher | 1 | F02, F03 |

### Foundation Features
These features set up shared project infrastructure. In a greenfield project they must be implemented sequentially before or alongside any feature that depends on them:
- **F01 Finance Service Common Interface** — introduces the `IFinanceService` contract and `AssetValueRequest` shape every website-based price source (existing and new) implements, and refactors Google Finance behind it

### Execution Waves
Features within the same wave can be built in parallel. A wave starts only after every feature in earlier waves is complete.

**Note:** Foundation features (see "Foundation Features" above) cannot run in parallel in a greenfield project even if they appear together in a wave — they share scaffolding files and must be implemented sequentially until the base is in place.

- **Wave 1**: F01
- **Wave 2**: F02, F03
- **Wave 3**: F04

### Priority levels
- **1** = Essential — product does not work without it
- **2** = Important — significant value addition
- **3** = Desirable — incremental improvement

```mermaid
graph TD
  F01[Finance Interface] --> F02[Tesouro Direto]
  F01 --> F03[Status Invest]
  F02 --> F04[Bond Fetcher]
  F03 --> F04
```

---

## 9. Acceptance Criteria

### F01. Finance Service Common Interface
- [ ] `IFinanceService` exists in `Financial.Infrastructure/Interfaces/` with a `GetAssetValue(AssetValueRequest)` member returning `AssetValueSnapshot`
- [ ] `IAssetPriceFetcher` is relocated to `Financial.Infrastructure/Interfaces/`, with no change to its members
- [ ] `AssetValueRequest` carries `Ticker`, `Exchange`, `Currency`, and `Name` fields
- [ ] `GoogleFinanceService.GetAssetValue` calls `GoogleFinance.GetFinancialInfoSnapshot(exchange, ticker)` when `Exchange` is populated, and `GoogleFinance.GetCryptocurrencyFinancialInfoSnapshot(currency, ticker)` when `Currency` is populated
- [ ] `StandardAssetPriceFetcher` and `CryptocurrencyAssetPriceFetcher` no longer reference the static `GoogleFinance` class directly; both depend on `GoogleFinanceService` via `IFinanceService`
- [ ] Every existing `StandardAssetPriceFetcherTests` and `CryptocurrencyAssetPriceFetcherTests` scenario (other than the Bond-dispatch test flipped by F04) still passes with unchanged expected outcomes
- [ ] `GoogleFinanceService` is registered in DI as a singleton

### F02. Tesouro Direto Finance Service
- [ ] `TesouroDiretoFinanceService.GetAssetValue` returns a matching price and as-of date when `request.Name` matches a `Titulo` row case-insensitively (ignoring leading/trailing whitespace)
- [ ] `TesouroDiretoFinanceService.GetAssetValue` returns a not-found result, not an exception, when no row matches
- [ ] `TesouroDiretoFinanceService` is registered in DI as a singleton

### F03. Status Invest Finance Service
- [ ] `StatusInvestFinanceService` derives the correct slug for representative bond titles (e.g., "TESOURO IPCA+ 2029" → "tesouro-ipca-2029")
- [ ] `StatusInvestFinanceService.GetAssetValue` returns the page's "Valor Unitário" value and an as-of date when the derived slug resolves to a valid page
- [ ] `StatusInvestFinanceService.GetAssetValue` returns a not-found result, not an exception, when the derived slug's page 404s or has no "Valor Unitário" element
- [ ] `StatusInvestFinanceService` is registered in DI as a singleton

### F04. BR Bond Price Fetcher
- [ ] `BondAssetPriceFetcher.Supports` returns `true` only for `GlobalAssetClass.Bond`
- [ ] `AssetPriceRequestDTO` has a `Name` field, and `BondAssetPriceFetcher.GetSnapshot` throws `ArgumentException` when it is blank
- [ ] A request whose bond title matches a Tesouro Direto row returns that source's price, without calling Status Invest
- [ ] A request whose bond title has no Tesouro Direto match falls back to Status Invest and returns its price when found
- [ ] A request not found by either source fails the same way `StandardAssetPriceFetcher` fails today when Google Finance can't resolve a ticker
- [ ] `StandardAssetPriceFetcher.Supports` returns `false` for `GlobalAssetClass.Bond` (in addition to its existing `Cryptocurrency` exclusion)
- [ ] `BondAssetPriceFetcher` is registered in DI as another `IAssetPriceFetcher`
- [ ] A request with `AssetClass = Cryptocurrency` and a request with any other non-Bond, non-Cryptocurrency `AssetClass` still dispatch to `CryptocurrencyAssetPriceFetcher` and `StandardAssetPriceFetcher` respectively, unchanged

### Cross-Feature Integration
- [ ] A bond found on Tesouro Direto (F02) is correctly returned as the price by `BondAssetPriceFetcher` (F04) without a Status Invest call
- [ ] A bond not found on Tesouro Direto (F02) but found on Status Invest (F03) is correctly returned as the price by `BondAssetPriceFetcher` (F04)
- [ ] A bond not found on either Tesouro Direto (F02) or Status Invest (F03) causes `BondAssetPriceFetcher` (F04) to fail rather than return a partial or default price
