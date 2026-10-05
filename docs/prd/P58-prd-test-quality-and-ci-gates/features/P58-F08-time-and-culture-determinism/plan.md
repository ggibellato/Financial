# Implementation Plan: Time and Culture Determinism

**Prerequisites:**
- An up-to-date `main`, `gh` authenticated, `npm install` done in `Financial.Web`
- Stages merge in order; each is one PR that leaves `main` deployable and the suite green
- Do not change any date rule while migrating: `DateTime.Today` becomes `GetLocalNow()` and `DateTimeOffset.UtcNow` becomes `GetUtcNow()`, nothing else (decision D3)
- Count wall-clock reads by grep at the start of each stage, not from the spec's numbers

### Stage 1: Test Clock, Culture Switch and Delay Seams (PR1)

**1. Test clock** - Extend the hand-written fake so the instant can be set and advanced and the local zone is configurable, and add the named pinned instants used by later stages.

**2. Test culture** - Add the module initializer that applies `TEST_CULTURE` (default en-GB) as the default thread culture for every test project that references the shared utilities.

**3. Retry delay seam** - Give the shared retry policy and the Google wrapper an injectable delay (the synchronous variant stops sleeping the thread) and rewrite `GoogleRetryPolicyTests` to assert the requested delays without waiting.

**4. Remove real waits in two named tests** - Replace the 2 s polling in `CreditCardCalendarSyncServiceTests` and the `Task.Delay(200)` in `DebouncedJsonStorageTests` with deterministic completion signals.

### Stage 2: Investment Domain and Application (PR2)

**5. Domain as-of** - Replace each wall-clock read in the Investment Domain entities with an as-of argument supplied by the caller, and update the Domain tests to pass a literal date.

**6. Application clock** - Inject `TimeProvider` into the Investment Application services that read the clock, pass the as-of values into Domain, and migrate their ~130 tests to the pinned clock with explicit January, month-end and midnight cases where the logic branches.

### Stage 3: Remaining Backend (PR3)

**7. CashFlow and shared sites** - Inject `TimeProvider` into the four CashFlow Application services, the FX provider and store, and the Yahoo service; pass the registered provider at the composition roots.

**8. Backend test migration** - Move the remaining Api, CashFlow, Shared and Investment.Infrastructure tests off the wall clock.

### Stage 4: WPF CashFlow View Models (PR4)

**9. Inject the clock** - Give each CashFlow view model, and the month/year control's defaults, the clock per decisions D1 and D7, and update `App.xaml.cs`.

**10. WPF CashFlow tests** - Move those view models' tests to the pinned clock; fix `ControleMaeViewModelTests.RefreshEntriesAsync_LoadsEntriesFromDate` to assert a literal and await the refresh.

### Stage 5: WPF Investment View Models (PR5)

**11. Inject the clock** - Same change for the Investment view models and the period-filter callers.

**12. WPF Investment tests** - Migrate their tests to the pinned clock.

### Stage 6: WPF Waits (PR6)

**13. Awaitable work** - Expose the task of the fire-and-forget work behind the ten delayed synchronisation sites so tests await it.

**14. Remove the workaround** - Replace every `Task.Delay` in the Presentation tests with the awaited task and delete `ThreadPoolWarmup.cs`; run the suite ten times locally and on CI.

### Stage 7: Culture and Web Waits (PR7)

**15. Machine-format parsing** - Parse machine-format values invariantly (Sheets, Google Finance, persisted/API strings in WPF), keeping user-input parsing culture-aware.

**16. pt-BR and locale proof** - Add the pt-BR theories and the web formatter locale tests, and run the whole .NET suite once with `TEST_CULTURE=pt-BR` and the migrated suites under `TZ=UTC`.

**17. Web waits** - Replace the enlarged `waitFor` timeouts with awaits on the specific state at the default timeout.

### Stage 8: Gate, Docs and PRD Ticks (PR8)

**18. Permanent gate** - Add the source-scan architecture test and widen the hygiene rule for `Financial.*`, with their self-tests.

**19. Documentation** - Update the implementation rules, the testing guide, CLAUDE.md and the pipeline document for the injected clock, the `TEST_CULTURE` switch and the gate.

**20. PRD amendments and ticks** - Amend the F08 boxes for the confirmed scope and the `TEST_CULTURE` / pinned-case wording, then tick the satisfied boxes in a separate commit.
