# Logging Audit: Current State (2026-08-17)

Produced to satisfy FR-011 / User Story 4 / SC-005. Methodology: exhaustive solution-wide search for `ILogger` injection and `Log*` call sites (`grep -rn "ILogger"` and `grep -rn "LogInformation|LogWarning|LogError|LogDebug|LogCritical|LogTrace"` across all `*.cs` files, excluding `bin/`, `obj/`, and test projects), plus a full enumeration of `catch` blocks solution-wide, both run directly against the repository. Carried forward unchanged across all revisions of this plan — the codebase has not changed in the relevant ways since.

## Headline finding

Of the entire solution — both bounded contexts' Domain/Application/Infrastructure, `Financial.Api`, `Financial.App`, and all `Integrations/`/`Tools/` projects — **exactly one class injects `ILogger<T>`**: `Financial.CashFlow.Application/Services/CardStatementService.cs`. It contains **exactly one** log call:

```csharp
_logger.LogWarning("No active credit cards found while generating statements for {Year}-{Month}.", year, month);
```

No other `LogInformation`, `LogWarning`, `LogError`, `LogDebug`, `LogCritical`, or `LogTrace` call exists anywhere else in application code. The only other logging currently active is framework-internal: Serilog's own request/host bootstrap logging (`ReadFrom.Configuration`/`ReadFrom.Services`) and whatever ASP.NET Core logs by default (e.g. `Microsoft.AspNetCore` category at `Warning` per `appsettings.json`).

## Findings by category

### Missing (dominant category — near-total)

- **Domain layer (both contexts)**: zero logging, which is correct/expected — Domain must contain no framework code (Constitution Principle I) and logging is a cross-cutting infra concern; no finding/action here.
- **Application layer (both contexts)**: zero logging except the one `CardStatementService` call. Every other use-case service (`Expense`, `Income`, `Bank`, `Transfer`, `Reserve`, `IncomeSource`, `Tithe`, `BalanceAdjustment`, `CreditCard`, `Category`, investment services, etc.) has no logging at all — no "use case started/completed", no "validation failed", no "business rule rejected an operation" trail.
- **Infrastructure layer (both contexts)**: zero logging around JSON load/save (`CashFlowLoader`, `DebouncedJsonStorage`, Google Drive storage), external HTTP calls (`YahooFinanceService`, `FrankfurterExchangeRateProvider`, `FallbackFinanceService`), or retry policies (`TransientRetryPolicy`, `GoogleRetryPolicy`) — a retry firing, a fallback provider engaging, or a save failing all currently leave no trace in the logs.
- **Presentation — `Financial.Api`**: `DomainExceptionMappingMiddleware` catches and translates three known exception types into HTTP 4xx responses **without logging any of them** — a rejected request (e.g. an overdraft confirmation, a not-found, a bad argument) is visible to the caller as an HTTP response but invisible in the log stream. Two controller-level `catch (Exception)` blocks (`DividendsController`) exist with the same gap.
- **Presentation — `Financial.App` (WPF)**: every ViewModel `catch (Exception ex)` block (≈20 sites across `MonthlyViewModel`, `ReservaViewModel`, `ControleMaeViewModel`, `MensaisViewModel`, `AssetDetailsViewModel`, `AssetPriceFetchViewModel`, `DividendCheckViewModel`, `TodayInfoTracker`, `ViewModelBase`, `App.xaml.cs` itself) surfaces the error only via a `MessageBox` shown to the user — none of them log it. If the user dismisses the dialog, there is no record the failure ever happened.
- **`Integrations/GoogleFinancialSupport`, `Integrations/WebPageParser`, `Tools/ImportGoogleSpreadSheets`**: same pattern — `catch` blocks exist (some deliberately swallowing specific exceptions, e.g. `GoogleSheetsAssetReader`'s empty `catch (InvalidCastException) { }`), none log.

### Duplicated

- No duplication found — there is too little logging anywhere for duplication to occur. (Recorded as an explicit "no findings" per the Assumptions in spec.md rather than left unstated.)

### Excessive

- No findings — same reason as above; there is no noisy/low-value logging today because there is almost no logging today.

### Insufficient (the one existing call site)

- The single `CardStatementService.LogWarning` call only covers the "no active credit cards" branch of `GetStatementsForMonthAsync`. The same class's `MarkStatementPaidAsync`/`UnmarkStatementPaidAsync` methods have `catch` blocks that roll back domain state on a save failure but do not log the failure that triggered the rollback — a partial-success/rollback path is exactly the kind of event that should be logged and currently isn't.

## Implication for the structured-logging design

Because "insufficient" is the near-universal finding rather than a scattered one, the structured-logging work that lands alongside OpenTelemetry tracing (a natural pairing, since correlated logs need an active trace to attach to) should prioritize, in this order:

1. Log every exception caught by `DomainExceptionMappingMiddleware` and equivalent WPF `catch` blocks before translating/displaying it — this closes the biggest, highest-value gap (failures currently invisible in logs).
2. Add use-case-boundary logging (start/success/business-rule-rejected) to Application services alongside the explicit `ITelemetryTracer.StartSpan(...)` calls this feature already introduces at those same boundaries (research.md Decision D3) — the two land together naturally since they're added at the same call sites.
3. Add logging around retry/fallback engagement in Infrastructure (`TransientRetryPolicy`, `FallbackFinanceService`) so degraded-but-working behavior is visible.

This ordering and the specific call sites are an input to `/speckit-tasks`, not something this planning phase implements.
