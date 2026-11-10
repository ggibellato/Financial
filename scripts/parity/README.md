# Parity toolkit

Checks that a change keeps every CashFlow figure the same on real data. It runs two builds of
`Financial.Api` on two copies of the same `data-cashflow.json`, reads the same figures from both
through the API and compares them exactly. Every P60 feature (accounts generalization) must end
with **0 differences** on the latest copy of the live data.

- **Before:** the git tag of the last P60 deploy, `p60-deployed-f12` (the version in use), on port 5291. Runs before that deploy used `p60-baseline`, the app before P60. After each later deploy, the default moves to that deploy's tag (`p60-deployed-<feature>`).
- **After:** the current working tree, including uncommitted changes, on port 5292. Its copy goes
  through the migration chain first.

## Prerequisites

- PowerShell 7.4 or later (`pwsh`), .NET 10 SDK and git on PATH.
- Node 20+ and network access, only for `-KeepRunning`.
- The latest live CashFlow file, downloaded from Google Drive to `data/data-cashflow.json`
  (`data/*` is gitignored). The script reads it and never writes to it. Run from a worktree, it
  reads the main repo folder's `data/data-cashflow.json`.

## Commands

```powershell
.\scripts\parity\Invoke-Parity.ps1 -Step All
```

| Step | What it does |
|------|--------------|
| `Baseline` | Creates a new work folder and a temp worktree at `-BaselineTag`. Publishes the API, removes the worktree, starts the API on `-BeforePort` with `before\data-cashflow.json` and writes `before-report.json`. |
| `Migrate` | Runs `CashFlowSpreadsheetImport migrate --input source.json --output after\data-cashflow.json`, logged to `after\migrate.log`. A failure stops the run with `Migrate failed; see <log>`. |
| `After` | Publishes the working tree, starts it on `-AfterPort` with the migrated copy and writes `after-report.json`. |
| `Compare` | Compares both reports through `figure-map.json`. Prints `key \| before \| after` for each difference and writes `differences.txt`. Exits 1 if there is any difference. |
| `All` | The four steps in order. |

| Option | Default | Notes |
|--------|---------|-------|
| `-LiveCopy` | `data/data-cashflow.json` | The input file, read only. The run prints its version, SHA-256 and save time, and works on a copy, `source.json`, in the work folder |
| `-WorkFolder` | `All` and `Baseline` create `%TEMP%\financial-parity\<yyyyMMdd-HHmmss>`; the other steps use the newest folder there | Lets you rerun one step, e.g. `-Step After` then `-Step Compare` while fixing a difference. Several figures depend on the API's current date, so `-Step After` refuses a work folder whose Baseline ran on another day; rerun `-Step All` then. |
| `-BeforePort`, `-AfterPort` | 5291, 5292 | 8080, a busy port and two equal ports are refused |
| `-BaselineTag` | `p60-deployed-f12` | Must exist locally |
| `-KeepRunning` | off | Also builds each version's web app into its API and leaves the API(s) running. Use it for the [screen checklist](screen-checklist.md); the script prints the URLs and the `Stop-Process` line. The baseline worktree always runs `npm ci` (about 5 minutes). The working tree reuses its existing `node_modules` and runs `npm ci` only when the folder is missing. Run it straight from a terminal: piping its output (e.g. `\| Select-Object`) keeps the pipe open until the APIs are stopped. |

`Parity OK: 0 differences in <n> figures across <m> months` means exit 0. `Parity FAILED: <n>
difference(s)` means exit 1, and the work folder is kept for inspection.

## Safety

- The input file is never opened for writing. The script records its SHA-256 at the start and
  checks it at the end of every step (`liveCopySha256Start` / `End` in `run.json`). If the hash
  changed, the run is discarded.
- Every step reads the work folder's `source.json`, never the input itself.
- Both APIs run with `ASPNETCORE_ENVIRONMENT=Parity` and local JSON files for CashFlow,
  Investment (`data-investment.example.json`) and FX rates (`data-fx-rates.example.json`). The
  default Production environment would point at Google Drive. A step fails if `/health` does not
  report `LocalJson`.
- Started APIs are stopped and the worktree is removed when the script ends, on success and on
  failure; the only exception is `-KeepRunning` after a successful run.

## Work folder

| Path | Content |
|------|---------|
| `run.json` | Input path, hashes and `sourceCopy`, month range, baseline tag and commit, after commit and dirty flag, figure and difference counts. `copyChangedDuringRead` is a hint only: the baseline's card-statement GET and both sides' tithe GETs write to the copy, but the API may not have flushed those writes when the hash is taken. |
| `before\`, `after\` | The data copies, the published `app\`, `build.log`, `api.log` and `api.err.log` |
| `before-report.json`, `after-report.json` | Figures reports: one sorted JSON object, `key: value` |
| `differences.txt` | Only when Compare fails |

## Figure catalogue

The month range runs from the first month with any record in the input to the later of the
current month and the last month with any record (`SettlementTransactions` and the V4 `MaeLedgerEntries` included), so future invoice months are included. It is
computed once from the input, before migration, and both sides use it. Keys use display names.
Two figures with the same key stop the run. Income sources are keyed by name, so two sources sharing a name would stop it too. Names are assumed not to contain `/`, which separates
key segments; none do in the live data.

| Family | Endpoint (under `/api/v1/financial/`) | Keys |
|--------|----------------------------------------|------|
| `banks` | `banks/month/{y}/{m}/balances`; `banks` + `expenses/month/{y}/{m}`, before side only (`Get-ParityReport -RetiredRoutes`; the routes are gone from the working tree) | `<yyyy-MM>/banks/<bank>/balance`, `/roundUpTotal` (sum of `roundUpAmount` of expenses paid from that bank, as Monthly did) |
| `cards` | `card-statements/{y}/{m}` and `expenses/month/{y}/{m}/unpaid-card-charges`, before side only (`Get-ParityReport -RetiredRoutes`; the routes are gone from the working tree). The statements route returns stored statements only, so after all months are read (`-FillMissingStatements`) the before side adds, for every month and every active card account without a row, outstanding 0, unpaid and that card's accumulated outstanding from any stored row (0 when it has none). The card warning has been null since F08 and is not emitted | `<yyyy-MM>/cards/<card>/outstandingTotal`, `/accumulatedOutstandingTotal`, `/isPaid`; `<yyyy-MM>/cards/unpaidChargesTotal` |
| `categories` | `expenses/month/{y}/{m}/category-totals` | `<yyyy-MM>/categories/<category>/total` |
| `incomes` | `incomes/month/{y}/{m}` | `<yyyy-MM>/incomes/<source>/net`, `/gross` (null when no income of the source has a gross value); `<yyyy-MM>/incomes/totalIncoming` |
| `tithe` | `tithe/month/{y}/{m}` | `<yyyy-MM>/tithe/calculatedTithe`, `/titheBalance`, `/carryForward` (null) or `/carryForward/<field>` |
| `snapshots` | `investment-snapshots/{y}/{m}/suggestions` | `<yyyy-MM>/snapshots/<account>/suggestedValue`, `/currentValue`, `/source`, `/skippedReason` |
| `monthly` | Computed by the script from the responses above, as `useMonthly.ts` did, with exact decimals. `bankTotalsSum`, `/roundUpTotalsSum` and `/cardOutstandingTotal` come from the `banks` and `cards` families, so before side only; `figure-map.json` maps the `summary` and `templateGroups` totals to them | `<yyyy-MM>/monthly/bankTotalsSum`, `/roundUpTotalsSum`, `/cardOutstandingTotal`, `/categoryTotalsSum` |
| `summary` | `accounts/groups/default/summary/{y}/{m}`, after side only (`Get-ParityReport -IncludeSummary`; the baseline has no such endpoint). `figure-map.json` maps each key to its `banks`, `cards`, `incomes` or `monthly` key. A member with a `statement` (a card) is emitted under `cardMembers`, its balance only in the run's current month and mapped, negated, to `cards/<card>/accumulatedOutstandingTotal` for the same reason as `cardAccount` | `<yyyy-MM>/summary/members/<account>/balance`, `/roundUpTotal`; `<yyyy-MM>/summary/cardMembers/<card>/outstandingTotal`, `/isPaid`, `/balance`; `<yyyy-MM>/summary/totals/<currency>/balance`, `/roundUpTotal`, `/cardOutstandingTotal`; `<yyyy-MM>/summary/categoryTotalsSum`; `<yyyy-MM>/summary/incomes/<source>/net`, `/gross`; `<yyyy-MM>/summary/totalIncoming` |
| `templateGroups` | `accounts/groups/by-template/{y}/{m}`, after side only (`Get-ParityReport -IncludeSummary`; the response is the one the `settlement` family fetches). Only the figures with a baseline equivalent are emitted: each Bank member's balance, the Bank and Pots groups' GBP totals, and each CreditCard member's `totalOwed`. `figure-map.json` maps them to `banks/<bank>/balance`, `monthly/bankTotalsSum`, `pots/total` and `cards/<card>/accumulatedOutstandingTotal`. The next statement, the card group total and Pots member balances have no baseline figure and are covered by tests | `<yyyy-MM>/templateGroups/Bank/members/<bank>/balance`; `<yyyy-MM>/templateGroups/Bank/totals/GBP`; `<yyyy-MM>/templateGroups/CreditCard/members/<card>/totalOwed`; `<yyyy-MM>/templateGroups/Pots/totals/GBP` |
| `account` | `accounts` once, then `accounts/{id}/{y}/{m}` per bank account (template `Bank`), after side only (`Get-ParityReport -IncludeAccounts`; the baseline has no such endpoint). The round-up total is minus the sum of `transaction.roundUp` over the account's rows in `accounts/transactions/{y}/{m}` (the response the `monthTransactions` family reads), which the account screen shows as money leaving the bank. `figure-map.json` maps each key to its `banks` key | `<yyyy-MM>/account/<account>/balance`, `/roundUpTotal` |
| `cardAccount` | `accounts/{id}/{y}/{m}` per account whose `templateName` is `CreditCard`, after side only, its balance only in the run's current month (`Get-ParityReport -CurrentMonth`). `figure-map.json` maps each key, negated, to its `cards/<card>/accumulatedOutstandingTotal` key: accumulated outstanding counts every unpaid charge whatever month is viewed, so it equals the month-end card balance only in the current month. Each active card's `totalOwed` is emitted in every month and maps, not negated, to the same key | `<yyyy-MM>/cardAccount/<card>/balance`, `/totalOwed` |
| `cardStatement` | `accounts/{id}/{y}/{m}` (the response `cardAccount` reads) for every `CreditCard` account in every month, active or not, after side only (`Get-ParityReport -IncludeAccounts`), when `viewedStatement.id` is set (a stored statement): `viewedStatement.outstandingTotal`, the account's `totalOwed` and `viewedStatement.isPaid`. After all months are read (`-FillMissingStatements`) it is filled as the before side's `cards` family is. `figure-map.json` maps each key to its `cards` key | `<yyyy-MM>/cardStatement/<card>/outstandingTotal`, `/accumulatedOutstandingTotal`, `/isPaid` |
| `monthTransactions` | `accounts/transactions/{y}/{m}`, after side only (`Get-ParityReport -IncludeAccounts`; the baseline has no such endpoint). The All transactions grid's spend per category: minus the sum of `transaction.amount` of the rows with that `categoryName` (rows without one are ignored), every currency together as Category totals do, plus 0 for every category the month's `categories` family lists that the grid lacks, so a category the grid dropped shows as a difference. `figure-map.json` maps each key, not negated, to its `categories/<category>/total` key; a grid-only category shows as `(missing)`. The unpaid card charges total is the sum of `transaction.expense.value` of the rows whose `expense.creditCardId` is set and whose `expense.paymentStatus` is `CreditCardCharge` (a charge on a paid statement reads `CreditCardStatementPaid`), refunds included; `figure-map.json` maps it to `cards/unpaidChargesTotal` | `<yyyy-MM>/monthTransactions/categories/<category>/spend`; `<yyyy-MM>/monthTransactions/unpaidCardChargesTotal` |
| `pots` | `reserve/movements` (no as-of endpoint exists), before side only (`Get-ParityReport -RetiredRoutes`; the route is gone from the working tree). A pot's balance is emitted only when it is not zero | `<yyyy-MM>/pots/<bucket>/balance` (movements dated up to the month's last day), `<yyyy-MM>/pots/total` |
| `potsTab` | `accounts/{id}/pots/{y}/{m}` for the account with `replacesPage` `reserva`, after side only (`Get-ParityReport -IncludeAccounts`; the baseline has no such endpoint). `figure-map.json` maps each key to its `pots` key. As on the before side, a pot's balance is emitted only when it is not zero: a pot whose balance differs is non-zero on at least one side, so it still shows as a difference | `<yyyy-MM>/potsTab/<pot>/balance`, `<yyyy-MM>/potsTab/total` |
| `settlement` | `accounts/groups/by-template/{y}/{m}`, fetched on both sides: each `Settlement` group member's `currencyBalances`. Every baseline from `p60-deployed-f11` serves the group | `<yyyy-MM>/settlement/<account>/<currency>` |
| `current` | Before side only (`Get-ParityReport -RetiredRoutes`, which `Invoke-Parity.ps1` passes for the baseline alone, because the routes are gone from the working tree): `reserve/balances` and `controle-mae/entries/totals`. After side only (`-IncludeAccounts`): every pot of `accounts/{id}/pots/{y}/{m}` at the run's last month (the range ends at or after the last record) for the account with `replacesPage` `reserva`, and the `currentBalances` of `accounts/{id}/{y}/{m}`, current month, for the account with `replacesPage` `controle-mae`. `figure-map.json` maps `potsTab` to `pots` and `maeAccount` to `mae` | `current/pots/<bucket>/balance`, `current/mae/totalBrl`, `/totalGbp` (before); `current/potsTab/<pot>/balance`, `current/maeAccount/totalBrl`, `/totalGbp` (after) |
| `annual` | `annual-summary/{y}/category-totals` | `<yyyy>/annual/categories/<category>/annualTotal`, `/average`, `/monthly/<01-12>`; `<yyyy>/annual/income/<field>[/<01-12>]`; `<yyyy>/annual/totals/<field>[/<01-12>]` |
| `averages` | `annual-summary/{y}/historic-summary-averages` | `<yyyy>/averages/<year>/<category>` |
| `investment` | `annual-summary/{y}/investment-annual-result` | `<yyyy>/investment/<account>/isLiability`, `/value/<01-12>`, `/diff/<01-12>`; `<yyyy>/investment/net/...` |

## Extending it

- **New figure:** add it to its family's function in `Parity.psm1` (`Add-ParityBankFigures`,
  `Add-ParityCardFigures`, … or `Add-ParityYearFigures`), or add a new family function and call it
  from `Add-ParityMonthFigures`, add the family to `$figureFamilies` in `Parity.test.ps1` and
  the table above, and add a case to `Parity.test.ps1`.
- **Figure moved to a new source:** when a feature serves a figure from a new endpoint or key, add
  a mapping to `figure-map.json`. The compare then checks the new key against the old one:

  ```json
  { "mappings": [ { "after": "{month}/summary/members/*/balance", "before": "{month}/banks/*/balance", "negate": false, "reason": "F04 group summary" } ] }
  ```

  `*` matches one key segment and `{month}` matches a `yyyy-MM` segment; their values fill the
  `before` pattern in order. `negate` compares `after = -before`. A mapping only renames. It
  never lets a value differ, and an unmapped key that exists on only one side is a difference.
- **Migration:** a feature that changes the data file format adds its step to the `migrate` chain;
  the Migrate step already runs the whole chain on the live copy.
- **Screens:** each UI feature adds its rows to [screen-checklist.md](screen-checklist.md).

## Self-test

`pwsh scripts/parity/Parity.test.ps1` covers the report builder, compare, figure map, month range,
guards and the `Compare` step without building or starting anything. CI runs it in the `changes`
job.
