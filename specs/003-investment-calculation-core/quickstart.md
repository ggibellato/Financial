# Quickstart: Validating Investment Calculation Core

**Branch**: `003-investment-calculation-core` | **Date**: 2026-09-10

How to prove this feature works end to end. Shapes and rules live in
[data-model.md](./data-model.md) and [contracts/](./contracts/README.md); this is the run guide.

---

## Prerequisites

```bash
dotnet restore
cd Financial.Web && npm install
```

**Never run anything here against `data/data-investment.json`.** Every verification below uses a
copy. The live file is also *live* — the application is usually running and fetching, and it gained
five prices during a single working session while this spec was being written.

```bash
cp data/data-investment.json /tmp/verify-investment.json
```

---

## The build and test loop

```bash
dotnet build --configuration Release
dotnet test                                              # all .NET test projects
dotnet test Tests/Financial.Investment.Domain.Tests      # a single project

cd Financial.Web
npm run lint
npm test                # vitest run
npm run build           # tsc -b && vite build — run this, not just vitest; it catches type errors
```

Coverage the way CI measures it:

```bash
dotnet test --settings coverlet.runsettings --results-directory TestResults
cd Financial.Web && npm run test:coverage
```

The gate bands green (100%) / yellow (95–99.99%) / amber (90–94.99%) / red (<90%) and **fails only on
red**. The blocking floor is 90%; 95–99.99% is accepted and asks the PR to note what is uncovered.

After any DTO change, in the same PR:

```powershell
$env:UPDATE_OPENAPI_SNAPSHOT=1; dotnet test Tests/Financial.Api.Tests; Remove-Item Env:\UPDATE_OPENAPI_SNAPSHOT
cd Financial.Web; npm run generate-api-types
```

---

## Scenario 1 — Order-independence (US1)

**Prove**: the same transactions in any order give the same figures.

Record against one holding chronologically and against another shuffled — including at least one
back-dated purchase entered after a sale — then compare quantity, average price, realised gain and
average sell price. Then edit a transaction's date backwards and confirm the figures move to what
chronological entry would have produced.

**Same-date check**: record a purchase of 100 and a sale of 100 on one date. The purchase applies
first, the position ends flat, and the sale is *not* refused as selling units not held.

**The bounded-change check — the one that guards SC-002.** Nothing in the test tree asserts the real
holdings' figures today, so this needs a fixture reproducing the two same-date shapes:

| Holding | Figure | Before | After |
|---|---|---|---|
| Bitcoin | average price | 62,713.15 | **62,709.05** |
| Bitcoin | realised gain | 0.25 | **0.17** |
| AGNC (AGNC) 2 ISA | realised gain | 9.98 | **9.66** |

State the precision the assertion runs at. At full stored precision *both* average prices move; at the
two decimal places both front ends display, AGNC's rounds to 7.09 either way. Without that note the
test reads as contradicting FR-006.

Verify the other 158 are untouched by replaying the temp copy before and after.

---

## Scenario 2 — Refusing an impossible sale (US2)

**Prove**: it is refused everywhere, and the three existing breaches still load.

From **each** front end: attempt a sale larger than the units held → refused, the message names the
held quantity, nothing stored. Then attempt a sale of *exactly* the units held → accepted.

Round-trip precision: for a holding of `2128.37599271` units, attempt `2128.38`. The refusal must
state the held quantity **to full precision**, so the user can copy it back. A message reading
`2,128.38` has failed FR-012.

Back-dating: on a holding that bought 100 in January and sold 80 in June, record a back-dated sale of
50 in March. It is refused *even though 50 were held in March*, because June would be left 30 short,
and the message names the June sale.

Deletion: delete the purchase that funded a later sale → refused.

**The load-tolerance half — do this from a real start-up**, not a unit test:

```bash
dotnet run --project Financial.Api      # or launch Financial.App
```

The application must start, list all 160 holdings, and open the three whose stored history already
sells more than it holds. All three are in **Historic Investments**, so exercise them there.

**WPF specifically**: confirm the refusal appears as a message and the app does **not** crash.
`TransactionsTabViewModel` has no `try`/`catch` today and there is no global handler, so this is the
scenario most likely to fail on first run.

---

## Scenario 3 — One meaning for invested (US3)

**Prove**: totals reconcile to rows, in both scopes.

For every portfolio, sum the rows and compare with the total above them. Today 15 of 15 Historic
portfolios disagree; afterwards none should.

Expect these to move, and say so in the PR:

- 5 of 28 active holdings' invested amounts, one from **−683.09 to 28.65**
- 4 active portfolio totals, by up to 711.74
- all 15 historic portfolio totals — `XPI/FII` **2,949.87 → 61,413.07**
- the allocation chart gains one slice as a holding crosses `> 0`
- 4 holdings' income-yield percentages, one flipping sign (the amended FR-049)

Removing the `Quantity != 0` filter changes **no** displayed number today, because no active holding
has zero quantity. That is the divergence being closed before the first position closes.

---

## Scenario 4 — One owner for valuation (US4)

**Prove**: both front ends show identical figures, and unavailable never reads as nought.

Open the same holding in the web app and the desktop app. Market value, cost of units held, unrealised
gain and both returns must be identical **to the last digit displayed and in the same format**. Each
must name the date its price came from.

Then open one of the seven holdings with no recorded price. Both front ends must say the value is
unavailable — `—`, not `0.00`.

**Staleness**: a price dated the valuation date is current; a Friday price is current on Monday and
stale on Tuesday. Pin this with a `FakeTimeProvider` rather than waiting for a Monday.

**Refresh wiring**: run a bulk price fetch and confirm the summary figures update without a manual
reload. The DTO is computed before the fetch writes the new price, so this is the step most likely to
be missed.

---

## Scenario 5 — Level totals and returns (US5)

**Prove**: incompleteness is stated, never hidden.

- A fully-valued portfolio: market value equals the sum of its rows; both returns compute.
- A partially-valued portfolio: the total is published and marked incomplete with the count stated,
  and **both returns are withheld**.
- `Trading 212 / ETF ISA`, `Trading 212 / ETF SIPP`, `XPI / Previdencia` have **no** valued holding at
  all — each must be distinguishable from an empty portfolio.
- No total spanning more than one broker exists anywhere in either front end.

---

## Scenario 6 — Market-based allocation (US6)

**Prove**: an unvaluable holding never reads as `0.0%`.

A holding that has appreciated shows a larger share than its cost-based share did. An unvaluable one
shows `—` and the portfolio states once that its shares do not total 100%. The share is formatted the
same way everywhere — two decimal places in both grids and both detail views.

Income-yield percentages must still be labelled *on cost* and must not have followed the share basis
to market value.

---

## Scenario 7 — The data-quality report (US7)

**Prove**: it names everything and changes nothing.

```bash
cp data/data-investment.json /tmp/verify-investment.json
# run the report against the copy
```

It must name: **3** holdings selling more than they hold (with each shortfall), **7** open holdings
with no recorded price, **90** unclassified split **3 active / 87 historic**, and **4** historic
holdings that still carry a quantity.

Then: run it twice and diff the copy against the original. Byte-identical both times. A report that
writes is a failed report.

Classify a holding through the app and confirm it disappears from the report **without a restart** —
and that correcting a holding's country and local type code re-derives its class rather than leaving
it `Unknown`.

---

## Before calling any increment done

```bash
docker-compose up          # must start under production config, not a dev profile
cd Financial.Web && npm run smoke-test
```

The smoke test asserts a **CashFlow** figure, so it cannot catch an Investment regression — treat a
green smoke run as evidence the app boots, not that this feature works.

For any increment touching UI, walk `docs/ui/review-checklist.md` against every touched view — not
only the items tied to the change — and actually look at the view on both platforms. A clean build and
green tests do not prove it renders.
