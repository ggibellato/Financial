# Rename "disposal" to "realised gain" in the back end

Status: PR 1 in progress (branch `refactor/realised-gain-domain`), PR 2 not started.

## Why

The UI now says "Realised gains" where the code says "disposal". The code should use the words the
screen uses, so a name seen in the app can be searched for directly, with no `disposal = realised gain`
mental map. The same rename aligns the two spellings already in the code (`Realized*` and
`Realised*`) on **Realised**, so one search finds everything.

## Decisions (user, 2026-11-22)

1. Spelling is **Realised** (British) everywhere: the tab label, the dashboard and every
   `Unrealised*` name already use it.
2. Existing `Realized*` names are aligned in this rename, not in a follow-up.
3. The tax wording is renamed too (`SourceType.Disposal` and friends): no "disposal" left in code.
4. Past PRDs, specs, mutation-review notes and `docs/completed/` stay as written; they are history.

## Design questions (`docs/rules/design.md`)

1. **Where does it belong?** Investment bounded context only. CashFlow is untouched.
2. **Which layers?** Domain (entity, rule and enum names), Infrastructure (JSON shape + migration),
   Application (DTO names), API (OpenAPI contract), Web (identifiers). Two PRs, see Slicing.
3. **What keeps Domain from learning about Infrastructure?** Nothing new: the JSON keys follow the
   Domain property names through `InvestmentTypeInfoResolver`, and the old-to-new key mapping lives
   only in `InvestmentDataMigrations` (Infrastructure). Domain gets no serialization attributes.
4. **SOLID.** Open/closed on the data versioning: a new migration step is added without changing the
   earlier ones or the serializer's flow. No new abstractions.

## Name map

### Domain (`Financial.Investment.Domain`)

| Today | After |
|---|---|
| `DisposalRecord` | `RealisedGain` |
| `DisposalLotConsumption` | `RealisedGainLot` |
| `DisposalRecordStatus` | `RealisedGainStatus` |
| `DisposalRecord.QuantityDisposed` | `RealisedGain.QuantitySold` |
| `Asset.DisposalRecords` / `_disposalRecords` | `Asset.RealisedGains` / `_realisedGains` |
| `Asset.LatestActiveDisposalDate` | `LatestActiveRealisedGainDate` |
| `Asset.AppendBackfilledDisposalRecord` | `AppendBackfilledRealisedGain` |
| `Asset.RealizedGainLoss` | `RealisedGainLoss` |
| `Asset.RealizedGainLossSharesOnly` | `RealisedGainLossSharesOnly` |
| `Transactions.RealizedCapitalGain` / `SetRealizedCapitalGain` / `Asset.RefreshRealizedCapitalGain` | `RealisedCapitalGain` / `SetRealisedCapitalGain` / `RefreshRealisedCapitalGain` |
| `DisposalRecordCalculator` | `RealisedGainCalculator` |
| `DisposalRecordRegenerator` | `RealisedGainRegenerator` |
| `DisposalRecordBackfill` / `DisposalRecordBackfillFailure` | `RealisedGainBackfill` / `RealisedGainBackfillFailure` |
| `SourceType.Disposal` | `SourceType.RealisedGain` |
| `TaxClassification.CreateForDisposal(disposalRecordId, …)` | `CreateForRealisedGain(realisedGainId, …)` |
| `TaxClassificationCalculator.CalculateForDisposal` | `CalculateForRealisedGain` |
| locals `isDisposing`, `disposingTransaction`, `disposalRecord` | `realisesGain`, `sellingTransaction`, `realisedGain` |
| messages "a later disposal depends on…", "A disposal record requires…" | "a later sale depends on…", "A realised gain requires…" |

### Application and API

| Today | After |
|---|---|
| `DisposalRecordDTO` | `RealisedGainDTO` |
| `DisposalLotConsumptionDTO` | `RealisedGainLotDTO` |
| `AssetDetailsDTO.DisposalRecords` (`disposalRecords`) | `RealisedGains` (`realisedGains`) |
| `quantityDisposed` | `quantitySold` |
| `realizedGainLoss`, `realizedGainLossSharesOnly` (asset details, portfolio asset summary) | `realisedGainLoss`, `realisedGainLossSharesOnly` |
| `NavigationMapper.MapDisposalRecord` / `MapDisposalLotConsumption` | `MapRealisedGain` / `MapRealisedGainLot` (done in PR 1) |
| `BrokersController` cost-basis summary "every disposal record" | "every realised gain" (done in PR 1) |

Routes do not change: realised gains travel inside the asset details response.

### Web (`Financial.Web/src`)

| Today | After |
|---|---|
| `DisposalsTab.tsx` / `.css`, class prefix `disposals-tab__` | `RealisedGainsTab.tsx` / `.css`, `realised-gains-tab__` |
| `useDisposals` and its `DisposalChain`, `DisposalsState`, `DisposalsData`, `DisposalsAction`, `DisposalsViewState`, `DisposalsViewAction`, `DisposalRow`, `DisposalRowProps` | `useRealisedGains`, `RealisedGainChain`, `RealisedGainsState`, … |
| tab id `'disposals'`, `DISPOSALS_TAB` | `'realisedGains'`, `REALISED_GAINS_TAB` (in-memory only, nothing stored) |
| `DisposalRecordDto` alias in `types.ts` | `RealisedGainDto` |
| `realizedGainLoss*` reads, `realizedGainLossLabel`, `realizedTitleId`, `realizedIndex` | `realisedGainLoss*`, … |
| `stryker.components.config.json` entry for `DisposalsTab.tsx` | `RealisedGainsTab.tsx` |

Test files and test method names follow the same map (`DisposalRecordTests` → `RealisedGainTests`, …).

## Data file: version 6

`data-investment.json` is at `"Version": 5`. The live file holds `DisposalRecords` (161),
`QuantityDisposed` (147), `"SourceType": "Disposal"` (142) and a stored `RealizedGainLossSharesOnly` (161).

Add a `fromVersion < 6` step to `InvestmentDataMigrations` and set `CurrentVersion = 6`. On every asset of
both broker groups it:

1. renames `DisposalRecords` to `RealisedGains`, and inside each record `QuantityDisposed` to `QuantitySold`
   (ids, `LotsConsumed`, `Method`, `Status`, `SupersededByRecordId` copied unchanged);
2. sets `SourceType` `"Disposal"` to `"RealisedGain"` on each tax classification;
3. removes `RealizedGainLossSharesOnly`. It is computed, so `RealisedGainLossSharesOnly` joins
   `ExcludedProperties` in `InvestmentTypeInfoResolver` and is no longer written (derive, don't store).

No separate tool runs: the upgrade happens on load and the file is written back as version 6 on the next save.

**Why the step is not optional.** Without it the reader ignores the unknown `DisposalRecords` key, every
asset loads with no realised gains, and `RealisedGainBackfill` silently rebuilds them all under
`AverageCost` with new ids. That loses each broker's cost-basis method and the superseded history, and
orphans every tax classification's `SourceId`.

**One-way.** A version-6 file cannot be read by an older build. Back up the live file before the first
deploy of PR 1, and rehearse the load on a temp copy of it (never the live file).

## Slicing

Both PRs are mostly one mechanical rename repeated across many files, so they use the "one change
repeated" exception to the 8-file limit. The judgement parts are called out so review can focus on them.

### PR 1: `refactor: rename disposal to realised gain in the Investment domain and data file`

- Domain and Infrastructure renames from the Domain table, plus the `Realized*` to `Realised*` alignment
  inside Domain and Application services.
- Data version 6 step and the `ExcludedProperties` addition (the judgement part).
- Application DTO **property names and wire names stay as they are**: `NavigationMapper` maps the new
  Domain names onto the existing DTOs, so no field the web reads changes and `main` stays deployable on
  its own. Two things in the OpenAPI snapshot do change, and the generated web types are regenerated for
  them: `DisposalRecordDTO.Status` uses the Domain enum, so its schema is now named `RealisedGainStatus`
  (values unchanged), and the `BrokersController` summary says "every realised gain".
- Test names and messages use "sale" where they mean the selling event ("a later sale depends on lots
  created by this split"); that 409 message reaches the screen unchanged in shape. `[Trait("AC", …)]`
  ids such as `P50-F02-disposal-record-05` stay as they are: they point at PRD acceptance criteria.
- `docs/rules/implementation.md` example (it quotes `DisposalRecords`).

Tests (Infrastructure, `InvestmentSerializerAdapterTests` / `InvestmentLoaderTests`):
- a version-5 document with realised gains under two cost-basis methods, a superseded record and tax
  classifications loads with the same record ids, methods, statuses and quantities, and every tax
  classification still resolves its source record;
- loading it creates no extra records through the backfill;
- saving it writes `"Version": 6`, `RealisedGains`, `QuantitySold`, `"SourceType": "RealisedGain"`, and
  none of `DisposalRecords`, `QuantityDisposed`, `"Disposal"`, `RealizedGainLossSharesOnly`,
  `RealisedGainLossSharesOnly`;
- a version-6 document loads unchanged (step not re-applied).
- Every renamed Domain/Application test keeps its assertions; only names change.

Done when: `dotnet build` and `dotnet test` pass, grep for `(?i)dispos` in Investment Domain/Application/
Infrastructure source finds only `Dispose`/`IDisposable`, the temp-copy load of the live file shows the
same realised gain totals per asset as before.

### PR 2: `refactor: rename disposal to realised gain in the API and web`

- Application DTO renames from the Application table, `NavigationMapper` map names, `BrokersController`
  summary text.
- Regenerate the OpenAPI snapshot and `npm run generate-api-types`; review the snapshot diff (only
  renames, no shape change).
- Web renames from the Web table, test files included; `stryker.components.config.json`.
- The web tests that mock "Cannot delete: a later disposal depends on…" (`CorporateActionsTab.test.tsx`,
  `useCorporateActions.test.ts`) take the new "a later sale depends on…" wording.

Tests: `OpenApiContractTests` and `openapiFreshness.test.ts` pin the new names; existing API endpoint,
component and hook tests keep their assertions under the new names. Run `npm run build` (not just
vitest) and the local Playwright smoke (`--workers=2`) since the tab is renamed.

Done when: backend and web CI green, grep for `(?i)dispos|realiz` in `Financial.Investment.*`,
`Financial.Api` and `Financial.Web/src` (excluding `Dispose`/`IDisposable` and the generated file's
history) finds nothing, and the "Realised gains" tab shows the same rows as before on the dev data.

## Follow-ups found in PR 1's review (not part of this rename)

- `InvestmentTypeInfoResolver.ExcludedProperties` is a hand-kept list of computed getters and has drifted:
  `RealizedGainLossSharesOnly` was written to the file for months, and `CorporateAction.IsReceivingRole` is
  still written. A general rule (a property with no setter of any visibility is never serialized, in
  `ReflectionJsonTypeInfoHelpers.WirePropertySetter`) would replace this list and CashFlow's
  `PropertiesNeverWritten`, after auditing CashFlow's computed getters.
- The older steps in `InvestmentDataMigrations` (versions 2–4) can use the new `RenameProperty` and
  `EnumerateObjects` helpers.

## Deploy

After PR 1 merges: back up `data-investment.json` (local and the Google Drive copy if that provider is in
use), deploy, restart, and confirm the file now says `"Version": 6`. PR 2 deploys like any other change.
