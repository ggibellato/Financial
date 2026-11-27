# Implementation Plan: Fund Purchases by Amount

**Note:** `spec.md` §13 (review amendments) is binding and supersedes earlier spec sections where they differ; read it before each stage.

**Prerequisites:**
- F06 (Brazilian CNPJ identifiers) and all six F07 (CVM daily quota price source) PRs merged, including F07 PR 6 (quota on a date). F03 PR 2 merged before PR 3 starts. This feature reads `Asset.IsPricedFromCvm`, the CVM fetcher and its month-file store, `PriceSource.Cvm`, `CnpjRules.Format`, the price-lookup issue enum and exception, the on-date endpoint and DTO, the widened `AssetPriceSnapshot.IsManual`, and the web `formatCnpj`, `formatShortDateUtc` and `daysBetween`. If the built code differs from those specs (F07 and F03 §13 are binding), follow the code and record it in the PR's docs commit.
- .NET 10 SDK and Node with `Financial.Web` dependencies installed (`npm ci`). No new packages, environment variables or configuration keys.
- Work on a branch off `origin/main` per PR. Use a worktree only if another session is using the repo folder (F02 may be built in parallel; it shares `Asset.cs`, `NavigationService.cs`, `TransactionsController.cs`, the web API client and the generated OpenAPI files, spec §12 item 22). Browser checks that can save use a temp copy of the investment data file; never apply a recalculation to the real file.
- Read `spec.md` in this folder. Every rule, name, message, route and test referenced below is defined there.
- No code comments and no XML doc comments in new or changed code (`CLAUDE.md`), except the `Financial.Api` controller actions' OpenAPI docs named in the spec.

### Stage 1: Buy by amount

**1. CVM pricing flag on the asset details** - Expose the existing CVM eligibility of the holding on the asset details contract and map it from the asset. Regenerate the OpenAPI snapshot and the web types, and add the F08 acceptance-criterion ids to the PRD in the docs commit. (PR 1)

**2. Quota arithmetic and on-date client** - Add the web client method for the quota on a date with its type alias, and the shared quota utilities: the 8-decimal rounding, the quota count for an amount and the texts of the quota line. (PR 1)

**3. Debounced quota lookup** - Add the hook that looks up the quota value for the quota date after the investor stops changing it, ignores stale answers and reports the loading, found, unavailable and error states with a retry. (PR 1)

**4. Amount entry mode in the transaction form** - Offer the "Enter by" choice on a new Buy for a CVM-priced holding, defaulting to Amount, with the Amount, Quota date and Fees fields, the quota line, its validation and save rules, the pre-fill when switching to quantity and price, and saving as an ordinary Buy. (PR 1)

### Stage 2: Recalculate quotas from CVM

**5. Quota series read** - Add the series capability to the historical price-fetcher seam and implement it in the CVM fetcher for a set of months plus the latest quota, select it in the price service, add the series use case with its month rules to the lookup service, and expose it as the new read endpoint on the prices controller. Regenerate the OpenAPI snapshot and the web types. (PR 2)

**6. Recalculation rule and flag** - Add to the asset the rule that says when its quotas can be recalculated and the operation that rewrites every Buy from quota values, move the CVM source-reference text into one Domain rule used by the CVM fetcher, and expose the recalculation flag on the asset details. Regenerate the OpenAPI snapshot and the web types. (PR 3)

**7. Recalculation endpoint** - Add the recalculation request DTO, service interface and service that look up every quota date and hand-entered price date, re-check the holding and write all Buys and price replacements in one save, register it, expose it on the transactions controller, and map an unreachable price source to 503 with its problem code. Regenerate the OpenAPI snapshot and the web types. (PR 4)

**8. Recalculation preview rules and client** - Add the client methods and type aliases for the series and the recalculation, and the preview utilities: months needed, the nearest-earlier quota rule, the recalculated quantity and the error-message mapping. (PR 5)

**9. Recalculation dialog** - Add the dialog state hook and the dialog with its grid, per-row quota dates, current-value line, price-replacement option and every state, add the trigger to the Transactions toolbar with the success toast, and list the dialog in the mutation-testing configuration. (PR 5)

**10. PRD and docs** - In the last commit of PR 5, tick the F08 PRD boxes. In each PR, record any deviation from this spec, plan or PRD in its own docs commit and in the PR body, including the flagged grid precision and discard-prompt follow-ups.
