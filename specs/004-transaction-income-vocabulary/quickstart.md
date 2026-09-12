# Quickstart: Validating Transaction and Income Event Vocabulary

Prerequisites: repo built (`dotnet build --configuration Release`), `Financial.Web` dependencies
installed (`cd Financial.Web && npm install`). All commands below assume the repo root unless noted.

## 1. Automated checks (run first, every increment)

```bash
dotnet test --settings coverlet.runsettings --results-directory TestResults
cd Financial.Web && npm run lint && npm test && npm run build
```

`npm run build` (not just `npm test`) is required — `tsc -b` is what catches every call site still
reading the removed `TransactionDTO.totalPrice` field after the OpenAPI regeneration step below.

## 2. Migration dry run (before touching real data)

Per `docs/rules/*` and the Constitution: never run a migration against the live file first.

```bash
cp data/data-investment.json /tmp/data-investment.migration-check.json   # or Copy-Item on Windows
dotnet run --project Tools/InvestmentTransactionIncomeVocabularyMigration -- \
  /tmp/data-investment.migration-check.json
```

(Matches `Tools/InvestmentDataQualityReport`'s own "optional data-path as `args[0]`" convention —
see research.md #6.)

Verify against the copy:
- Every `Credit.Type` string `"Rent"` in the copy is now `"SecuritiesLendingIncome"` — confirm with
  `grep -c '"Type": "Rent"'` (expect the count in the migration tool's own summary output, not left
  at the pre-migration count) and `grep -c '"Type": "SecuritiesLendingIncome"'` (expect the sum of
  both counts to match the pre-migration `"Rent"` count).
- No other field in any existing `Transaction`/`Credit` row changed value (diff the copy against
  the original with a JSON-aware diff, or the migration tool's own report — SC-003).
- The migration tool's summary reports the same count of rewritten rows as the pre-migration `Rent`
  count established in the clarification session (52 at the time this spec was written — re-count
  against the current file, since the live data changes over time).

Only after this passes: run the same command against `data/data-investment.json` directly, then
**restart every process that reads it** (API, and `Financial.App` if running) — a restart alone
never runs a migration (Constitution: Technology & Persistence Constraints).

## 3. New transaction types (User Story 1)

Against a running API (`dotnet run --project Financial.Api`) with a real or seeded asset:

```bash
curl -X POST http://localhost:5190/transactions \
  -H "Content-Type: application/json" \
  -d '{"brokerName":"XPI","portfolioName":"...","assetName":"...","date":"2026-01-15","type":"Fee","quantity":0,"unitPrice":0,"fees":10,"withheld":0}'
```

Expected: `200 OK`; the asset's quantity/average price in the response are unchanged from before
the call; the transaction list includes a `Fee` entry distinct from any `Buy`/`Sell`.

Repeat for `Redemption`, `TransferIn`, `TransferOut`, `CapitalCall`, `ReturnOfCapital` against a
holding with existing quantity, matching each Acceptance Scenario in User Story 1. For
`TransferOut`/`Redemption`, also verify the oversell-parity rule (FR-004): attempting to record one
for more units than held returns `409 Conflict` with a message naming the shortfall, the same
shape `SaleCoverageRule` already produces for an oversell `Sell`.

## 4. Gross/fees/withheld/net visibility (User Story 2)

```bash
curl -X POST http://localhost:5190/credits \
  -d '{"brokerName":"XPI","portfolioName":"...","assetName":"...","date":"2026-01-15","type":"Dividend","value":100,"withheld":15}'
curl http://localhost:5190/credits/portfolio/XPI/...
```

Expected: the returned `CreditDTO` shows `value: 100`, `withheld: 15`, `netAmount: 85`. Fetch an
existing (pre-migration) credit and confirm `withheld: 0`, `netAmount` equal to its `value` — SC-003.

## 5. Gross vs net-of-tax return (User Story 3)

```bash
curl http://localhost:5190/summary/broker/XPI
```

Expected: `totalReturn` (gross) and `totalReturnNetOfTax` both present; on a broker with at least
one withheld income record they differ; re-run against a broker/portfolio with zero withholding and
confirm the two figures are numerically identical (FR-018), not merely close (compare as decimals,
not with a tolerance).

## 6. Front-end parity (User Story 4)

1. `cd Financial.Web && npm run dev` (port 5173) against the same API.
2. Record a `Fee` and a `Dividend` with `withheld > 0` from the web UI.
3. Launch `Financial.App` (WPF) against the same data file (after a restart if the API path above
   already wrote to the real file) and confirm the same holding shows identical figures,
   terminology ("Securities Lending Income", not "Rent"), and field order for the new types and the
   gross/withheld/net breakdown.
4. In the web transaction entry form, select `TransferIn`/`CapitalCall`/etc. and confirm the
   Quantity/UnitPrice fields are not shown as required (FR-022); confirm the same in the WPF
   transaction dialog.

## 7. Regression guard (SC-003)

Before and after the migration, capture:

```bash
curl http://localhost:5190/transactions/broker/XPI > before-xpi-transactions.json
curl http://localhost:5190/credits/broker/XPI > before-xpi-credits.json
# ...apply migration + code changes, restart...
curl http://localhost:5190/transactions/broker/XPI > after-xpi-transactions.json
curl http://localhost:5190/credits/broker/XPI > after-xpi-credits.json
```

Diff `date`, `type` (`Rent` rows now `SecuritiesLendingIncome`, everything else unchanged),
`quantity`, `unitPrice`, `fees`, and the net-cash/net-amount figures — no other value should move.
