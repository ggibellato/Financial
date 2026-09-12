# Quickstart: Validating Valuation Methods and Provenance

Prerequisites: repo built (`dotnet build --configuration Release`), `Financial.Web` dependencies
installed (`cd Financial.Web && npm install`). All commands below assume the repo root unless noted.

## 1. Automated checks (run first, every increment)

```bash
dotnet test --settings coverlet.runsettings --results-directory TestResults
cd Financial.Web && npm run lint && npm test && npm run build
```

`npm run build` (not just `npm test`) is required — `tsc -b` is what catches every call site still
reading the removed `AssetDetailsDTO.priceHistory`/`.isPriceStale` fields after the OpenAPI
regeneration step below (`contracts/api-contract.md`).

## 2. Data version upgrade check (on-the-fly, no separate tool)

`InvestmentSerializerAdapter` upgrades the JSON document on load, the same mechanism P47 already
uses (`InvestmentDataMigrations`, version 2 → 3 for this feature): a document below version 3 gets
its `Asset`-level `"PriceHistory"` key renamed to `"PriceSnapshots"` and each existing snapshot's
`IsManual` bool mapped to a `Source` string (`true`→`"Manual"`, `false`→`"Unknown"`) before the
normal typed deserialization runs — no separate migration tool, dry run, or manual invocation.

Verify against a temp copy first (never the live file):

```bash
cp data/data-investment.json /tmp/data-investment.version-check.json   # or Copy-Item on Windows
```

- Point a local run at the copy and confirm it loads without error, every asset that previously had
  a `"PriceHistory"` array now reports it under `"PriceSnapshots"`, and every existing snapshot
  entry shows `"source": "Manual"` or `"source": "Unknown"` matching its old `isManual` value.
- Save through that same run (any write path against the copy) and confirm the file now has
  `"Version": 3` and no remaining `"PriceHistory"` key anywhere.
- No `Date`/`Price` value in any existing snapshot changed (diff the copy against the original with
  a JSON-aware diff — SC-004).

Once satisfied, **restart every process that reads the real file** (API, and `Financial.App` if
running) — the upgrade runs automatically on that next load.

## 3. Valuing a holding with no market price (User Story 1)

Against a running API (`dotnet run --project Financial.Api`), first classify a holding by valuation
method via the Admin asset endpoint, then record its value directly:

```bash
curl -X PUT http://localhost:5190/assets/XPI/Default/MyIncoHolding \
  -H "Content-Type: application/json" \
  -d '{"name":"MyIncoHolding","valuationMethod":"ProviderValue"}'

curl -X PUT http://localhost:5190/prices \
  -H "Content-Type: application/json" \
  -d '{"brokerName":"XPI","portfolioName":"Default","assetName":"MyIncoHolding","date":"2026-09-12","price":5000,"currency":"BRL"}'
```

Expected: `200 OK` on both; the asset's details response shows `marketValue: 5000` with no
`quantity` change, and repeating the second call with `"price":0` (a write-down) returns
`marketValue: 0`, distinguishable from a holding that has never had a value recorded at all (which
reports `marketStatus: "Unavailable"`, not a zero value) — FR-006, User Story 1 Scenario 2.

Attempt an automatic fetch against the same holding (`GET /prices/current?...&assetName=MyIncoHolding&portfolioName=Default&brokerName=XPI`)
and confirm it is skipped — the response echoes the last manually-recorded value rather than
attempting any provider lookup (User Story 1 Scenario 3, FR-007).

## 4. Provenance and staleness (User Story 2)

```bash
curl http://localhost:5190/assets/XPI/Default/AnyPricedHolding
```

Expected: the response's most recent `priceSnapshots` entry (and the asset-level `marketStatus`)
shows `source`, `retrievedAt`, and a `marketStatus` of `"Current"` for a same-day price. Manually
back-date a holding's only snapshot (via `PUT /prices` with an old `date`) and re-fetch — confirm
`marketStatus` becomes `"Stale"`. Query a holding with no snapshot at all and confirm
`marketStatus: "Unavailable"` is returned as a normal `200 OK` field, not an error (research.md #10).

## 5. Fetcher routing by valuation method (User Story 3)

```bash
curl -X PUT http://localhost:5190/assets/XPI/Default/SomeUnknownClassBond \
  -d '{"name":"SomeUnknownClassBond","valuationMethod":"BondQuote"}'
curl "http://localhost:5190/prices/current?ticker=...&valuationMethod=BondQuote"
```

Expected: even though `SomeUnknownClassBond`'s `class` is `Unknown`, the fetch routes through the
bond price source, not the equity ticker lookup (confirm via the returned `source` value — one of
`StatusInvest`/`DicionarioDoInvestidor`/`Redentia`, never `Google`/`Yahoo`) — the concrete fix for
the roadmap's G11 tail case.

## 6. Front-end parity (User Story 2 + 3)

1. `cd Financial.Web && npm run dev` (port 5173) against the same API.
2. Open a holding's price/value view; confirm source, as-of date, and market status are visible,
   and a stale/unavailable holding shows a visible warning (FR-013, FR-014).
3. Launch `Financial.App` (WPF) against the same data file (after a restart if the API path above
   already wrote to the real file) and confirm the same holding shows identical source, as-of date,
   and market status (FR-015, SC-005).

## 7. Regression guard (SC-004)

Before and after the migration + code changes:

```bash
curl http://localhost:5190/assets/XPI/Default/SomeAsset > before-asset.json
# ...apply migration + code changes, restart...
curl http://localhost:5190/assets/XPI/Default/SomeAsset > after-asset.json
```

Diff `priceSnapshots[].date`/`.price` — no value should move, only `priceHistory`→`priceSnapshots`
and the addition of `source`/`currency`/`valuationMethod`/`retrievedAt`/`sourceReference` per entry.
