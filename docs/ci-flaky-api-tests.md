# CI flakiness: `Financial.Api.Tests` request timeouts

Status: open, not investigated. Observed once (2026-09-29); passed on rerun without code changes.

## What happened

PR #921 (calendar event time-zone change, touches only `Integrations/GoogleCalendar` and its tests) failed the `backend` job in run `36536700587`, attempt 1. Five `Financial.Api.Tests` tests failed, all Investment endpoints, none related to the change:

| Test | Duration |
|---|---|
| `Acceptance.TaxYearWorkbookAcceptanceTests.Workbook_AggregateStatus_IsFinalWhenARuleCoversEveryEntry` | 2 m 5 s |
| `Acceptance.TaxProfileAndClassificationAcceptanceTests.AssetDetails_TaxJurisdictions_ReflectsTheDistinctJurisdictionsOfItsClassifications` | 3 m |
| `DashboardEndpointsTests.GetDashboard_Returns200WithTheAggregate` | 3 m 21 s |
| `TransactionEndpointsTests.AddTransaction_NoQuantityEffectType_ReturnsOk(type: "ReturnOfCapital", quantity: 0, unitPrice: 0)` | 1 m 59 s |
| `Acceptance.CorporateActionSpinOffAcceptanceTests.EveryOpenLotOnAFifoCostedParent_IsProportionallyReducedByTheAllocationPercentage` | 1 m 43 s |

Common error on every failure:

```
System.Threading.Tasks.TaskCanceledException : The operation was canceled.
---- System.Net.Http.HttpRequestException : Error while copying content to a stream.
-------- System.IO.IOException : The client aborted the request.
```

The test `HttpClient` gave up waiting for the in-process API to respond; the server never returned. The `backend` job ran 11m16s on attempt 1 versus 3m43s on attempt 2 (same commit, all green), so roughly 7-8 minutes were lost to these hangs. `wpf` passed and `web` was skipped in the same run.

## What is known

- Unrelated to the PR: no calendar code is on the failing paths.
- Not deterministic: `gh run rerun --failed` passed with no change.
- The failures are hangs (1-3 minute waits), not assertion mismatches, so a request stalled inside the `WebApplicationFactory` host.

## What is not known

- Whether a specific test or shared fixture is the culprit, or the Windows runner was just starved. Only five tests failed, so something specific to Investment endpoints is plausible.
- Whether any of these five tests reach the network (price providers, FX rates, Google Finance scraping via `Integrations/WebPageParser`, Frankfurter) and stall on an outbound call. `Financial.Api.Tests/ApiTestFactory.cs` is the place to check what is stubbed.
- Whether parallel test execution across classes sharing the factory or the JSON data file contributes.

## Suggested next steps

1. Search for prior occurrences: `gh run list --workflow build.yml --status failure` and grep the logs for `The client aborted the request`. Recurrence on the same tests points at the tests; different tests each time points at the runner.
2. Check whether the five tests (or the code they call) make outbound HTTP calls; if so, replace with test doubles in `ApiTestFactory`.
3. Look for shared mutable state or startup work per test class (JSON file load, background hosted services that start with the factory).
4. Give the test `HttpClient` a short explicit timeout (e.g. 30 s) so a hang fails in seconds instead of minutes and produces a clearer error.
5. If it does not reproduce locally, run the `Financial.Api.Tests` project in a loop on CI (or `dotnet test --blame-hang-timeout 60s`) to capture a hang dump.

## Until it is fixed

If `backend` fails with only `Financial.Api.Tests` timeouts of this shape, rerun the failed jobs (`gh run rerun <run-id> --failed`) and append the occurrence to the table below.

## Occurrences

| Date | Run | PR | Tests failed | Outcome |
|---|---|---|---|---|
| 2026-09-29 | 36536700587 | #921 | 5 (listed above) | Passed on rerun |
