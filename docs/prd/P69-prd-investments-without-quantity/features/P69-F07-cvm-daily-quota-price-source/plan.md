# Implementation Plan: CVM Daily Quota Price Source

**Note:** `spec.md` §13 (review amendments) is binding and supersedes earlier spec sections where they differ; read it before each stage.

**Prerequisites:**
- F01 (Balance-tracked holding option) and F06 (Brazilian CNPJ identifiers) merged: this feature reads `Asset.IsTrackedByBalance`, `Asset.Cnpj`, `CnpjRules`, `AssetDetailsDTO.cnpj` and the web `utils/cnpj.ts`. If the built code differs from their specs, follow the code and record it in the PR's docs commit.
- .NET 10 SDK and Node with `Financial.Web` dependencies installed (`npm ci`).
- One new package in `Integrations/WebPageParser`: `Microsoft.Extensions.Logging.Abstractions` 10.0.9 (already used by `Integrations/Frankfurter`). No new environment variables; four optional configuration keys under `Investment:PriceQuotes:Cvm` with defaults.
- Before PR 3: download one recent `inf_diario_fi_YYYYMM.zip` and `inf_diario_fi_202101.zip` by hand into a scratch folder (never into the repo, never from a test), confirm separator, encoding, decimal mark and header, and cut the recorded samples described in the spec.
- Work on a branch off `origin/main` per PR. Use a worktree only if another session is using the repo folder. Browser checks that can save use a temp copy of the investment data file.
- Read `spec.md` in this folder. Every rule, name, message and key referenced below is defined there.
- No code comments and no XML doc comments in new or changed code (`CLAUDE.md`), except the `Financial.Api` controller action's OpenAPI docs named in the spec.

### Stage 1: Price-source seam

**1. CVM eligibility gate** - Add the single rule on the asset that says whether a holding is priced from CVM, combining the tracking mode from F01 with the country, class and CNPJ from F06. (PR 1)

**2. Fund CNPJ on the price request** - Carry the fund's CNPJ on the price request, filled in only when the asset passes the gate, where the lookup service describes the request from the resolved asset. (PR 1)

**3. Request-aware source selection** - Change the price-fetcher seam so a fetcher decides from the whole request, move the three existing fetchers to it with their rules unchanged, and have the price service select through it. Add the F07 acceptance-criterion ids to the PRD in the docs commit. (PR 1)

**4. Dated automatic prices** - Let a fetched value carry its own date and source reference, map them onto the current-price response, and have the lookup record an automatic price on that date with that reference and derive its market status from it with the existing weekday rule. Regenerate the OpenAPI snapshot and the web types. (PR 2)

### Stage 2: CVM latest quota

**5. CVM daily report reader** - Add the reader of the CVM monthly zip to the web-page parser integration: month file naming, layout check, streaming parse filtered by CNPJ digits, the invalid-row and subclass rules, and the latest and on-date selection rules, with the recorded samples pinning the file format. (PR 3)

**6. Month-file cache** - Add the store that downloads month files into the cache folder with the configured timeout, reuses them across restarts, revalidates them after the configured age, shares one download per month, treats an unpublished month as empty, remembers failures for the configured delay and logs a failed download once. Allow-list the month file name in the log redaction test and add the shared zip-building test helper. (PR 3)

**7. CVM price fetcher and configuration** - Add the CVM source to the price-source enum and the masked CNPJ formatter to the CNPJ rules, add the fetcher that reads the current and previous months in parallel and returns the latest quota with its competence date and reference, and register it first together with its options, named HTTP client and store. (PR 4)

**8. Source label, settings and docs** - Label CVM prices in the Price history grid, add the CVM keys to the base and development settings and to the README, regenerate the OpenAPI snapshot and web types, and give the API test factory a fake CVM handler and a temp cache folder. (PR 4)

### Stage 3: CVM fetch status

**9. Typed fetch issues** - Add the price-lookup issue enum and the typed price-source exception to Application, have the CVM fetcher raise them for an unreachable source, a CNPJ CVM does not list and subclass-only funds, and have the lookup put the issue and its source on the fallback, unavailable and not-saved responses. Regenerate the OpenAPI snapshot and web types. (PR 5)

**10. Summary badges and status** - Show the CVM badge for a live CVM quote, the last-saved badge when a saved value is shown instead, and the status text for each issue with the masked CNPJ, keeping the existing error alert first. (PR 5)

### Stage 4: Quota on a date

**11. Historical fetcher capability** - Add the optional price-on-a-date capability to the fetcher seam and implement it in the CVM fetcher, reading the month of the date and, when the look-back window crosses into it, the month before. (PR 6)

**12. Price on a date use case and endpoint** - Add the price-on-a-date method to the price service and the lookup service, with the response DTO, the future-date and unknown-holding rules and the unreachable-source outcome, and expose it as the new read endpoint on the prices controller. Regenerate the OpenAPI snapshot and web types. (PR 6)

**13. PRD and docs** - In the last commit of PR 6, tick the F07 PRD boxes. In each PR, record any deviation from this spec, plan or PRD in its own docs commit and in the PR body, including the confirmed CSV format in PR 3.
