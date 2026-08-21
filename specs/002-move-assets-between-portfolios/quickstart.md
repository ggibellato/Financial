# Quickstart: Validating Move Assets Between Portfolios

**Feature**: `specs/002-move-assets-between-portfolios` | **Date**: 2026-08-21

How to prove the feature works end to end. Shapes and signatures live in `contracts/`; rules live in
`data-model.md` §3. This is the run guide.

---

## Safety rules — read before running anything

1. **Never point any of this at `data/data-investment.json`.** Copy it and work on the copy. The live
   file is the user's real financial record and there is no undo in this feature.
2. **Check the port before starting the API.** The deployed Docker app binds **8080**; starting a
   second process on it silently smoke-tests the live deployment.
   ```powershell
   netstat -ano | Select-String ":8080|:5190"
   ```
3. **The JSON document is read once at process startup.** After editing a data file by hand, restart
   the process — re-querying will not pick it up. Restarting never runs a migration (none is needed
   here; see `data-model.md` §6).

---

## Prerequisites

```powershell
dotnet restore
cd Financial.Web; npm install; cd ..
```

Make a scratch data file with both scopes populated and one broker present only in Active — that last
part is what exercises `research.md` §D2:

```powershell
$scratch = "$env:TEMP\financial-move-test"
New-Item -ItemType Directory -Force $scratch | Out-Null
Copy-Item data/data-investment.example.json "$scratch/data-investment.json"
```

---

## 1. Automated checks (run these first)

```powershell
dotnet build --configuration Release

dotnet test Tests/Financial.Investment.Domain.Tests        # move, archive, name, deletion rules
dotnet test Tests/Financial.Investment.Application.Tests   # orchestration; rejected move never writes
dotnet test Tests/Financial.Api.Tests                      # status codes 200/400/404/409
dotnet test Tests/Financial.Presentation.Tests             # WPF drop-target rules, reselection
dotnet test Tests/Financial.Architecture.Tests             # layering still holds

cd Financial.Web
npm run lint
npm test
npm run build            # tsc -b && vite build — the type errors vitest alone will not catch
cd ..
```

`npm run build`, not just `npm test`: a DTO field added in C# and missed in `types.ts` fails only at
`tsc -b`, and it fails the Docker build rather than the test run.

**Expected**: all green. The Domain suite is the one that matters most — every rule in
`data-model.md` §3 should be provable there with no repository, no API, and no UI.

---

## 2. API round trip

```powershell
$env:Investment__DataJsonFile = "$env:TEMP\financial-move-test\data-investment.json"
dotnet run --project Financial.Api      # keep the launch profile: it sets Development, which CORS needs
```

Use the base path `/api/v1/financial`.

### 2a. Move into an existing portfolio (US1)

```powershell
$body = @{
  brokerName = "Trading 212"; scope = "active"; sourcePortfolioName = "ETF"
  assetName = "<an asset in that portfolio>"
  destinationPortfolioName = "ETF ISA"
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://localhost:5190/api/v1/financial/assets/move `
  -ContentType application/json -Body $body
```

**Expect** `200` and the moved `AssetDetailsDTO`. Confirm against the asset's pre-move details that
`quantity`, `averagePrice`, transaction count, credit count, and price history are **identical** —
that is SC-002, and it is the single most important assertion in the feature.

### 2b. Move into a portfolio created by the move (US2)

Repeat with `destinationPortfolioName = "SIPP"`. **Expect** `200`, and `GET /navigation/tree` now
lists `SIPP` under Trading 212 holding exactly that asset.

### 2c. Archive a closed asset (US3)

With an asset whose `quantity` is `0`, use the archive request shape that increment introduces.
**Expect** `200`, the asset present under Historic and absent from Active.

Then try the same with a non-zero-quantity asset. **Expect** `409` with a message about closing the
position first — not a 400, and definitely not a 500.

Archive a **Coinbase** asset (Coinbase has no Historic record in the live data): **expect** `200`,
with a Historic Coinbase broker created carrying the same currency.

### 2d. Rejections (SC-003)

| Request | Expect |
|---|---|
| `destinationPortfolioName` = the source portfolio | `409` |
| Destination already holds an asset of that name | `409` |
| `destinationPortfolioName = "   "` | `400` |
| `destinationPortfolioName = "etf isa"` (differs only by case) | `409` |
| Unknown `assetName` | `404` |

After all of them, the data file must be **unchanged**. Snapshot it first and compare:

```powershell
$before = Get-FileHash "$env:TEMP\financial-move-test\data-investment.json"
# ...run every rejection above...
(Get-FileHash "$env:TEMP\financial-move-test\data-investment.json").Hash -eq $before.Hash   # must be True
```

Any `500` here means `InvestmentRuleViolationException → 409` was not added to the middleware.

### 2e. Delete an emptied portfolio (US5)

```powershell
Invoke-RestMethod -Method Delete `
  -Uri "http://localhost:5190/api/v1/financial/portfolios/Trading%20212/ETF?scope=active"
```

**Expect** `204` when empty, `409` when it still holds assets, `404` when it does not exist.

### 2f. Persistence (FR-010)

Stop the API, restart it against the same scratch file, `GET /navigation/tree`. Every move and
deletion must still be there.

---

## 3. Web front end

```powershell
cd Financial.Web
npm run dev              # 5173, proxies /api to localhost:5190
```

With the API still running against the scratch file:

1. **Dialog route** — move an asset from the asset panel; the tree updates with no manual refresh
   (FR-030) and the moved asset is selected under its new portfolio (FR-037).
2. **Drag onto a portfolio** (US4-1) — the row highlights as a valid target; drop moves the asset.
3. **Drag onto the broker** (US4-2) — dropping asks for a name; entering `SIPP` creates it and moves
   the asset there. Cancelling the prompt changes **nothing** (US4-3).
4. **Invalid targets** (US4-4) — drag over the asset's own portfolio, a different broker's rows,
   another asset, and the tree root. Each must visibly refuse, and releasing must change nothing.
5. **Release outside the tree** (US4-7) — cancels silently, no error.
6. **Emptying a portfolio** (US4-8) — the delete offer appears; declining leaves it listed with zero
   assets and the move still applied.

---

## 4. WPF front end

```powershell
dotnet run --project Financial.App
```

`Financial.App` hosts the Application layer in-process — it does **not** talk to the API — so point
its configured data file at the scratch copy too, and restart it after any external edit.

Repeat every step from §3 in **Active Investments**, then repeat the drag steps in **Historic
Investments**. Parity is a requirement (FR-040, SC-005), including the wording of each rejection:
the message originates in the Domain, so the two front ends should read identically. Any difference
in wording is a defect, not a styling choice.

Confirm the numeric columns in any affected grid stay right-aligned — the app-wide convention.

---

## 5. Full-stack smoke test

```powershell
netstat -ano | Select-String ":8080"    # must be empty, or the live deployment is the target
cd Financial.Web
npm run smoke-test
```

This is the CI safety net (`.github/workflows/build.yml`); it must keep passing.

---

## 6. Deployability (Constitution VIII)

Every PR in this feature must leave `main` deployable, and must record the check in its body:

```powershell
docker-compose up --build
```

The app starts on 8080 serving both the API and the SPA, and existing functionality — the navigation
tree, asset details, transactions, credits, prices — still works.

---

## Definition of Done for this feature

- [ ] Every command in §1 passes.
- [ ] §2d leaves the data file hash unchanged, with no `500` anywhere.
- [ ] §2a shows every figure identical before and after the move (SC-002).
- [ ] §3 and §4 behave identically, including rejection wording (SC-005).
- [ ] §5 smoke test passes and §6 starts under `docker-compose`.
- [ ] The live `data/data-investment.json` was never used as a target.
- [ ] Spec Section 9 acceptance-criteria boxes checked off as their work lands, not batched.
