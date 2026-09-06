> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# WPF ViewModels (`Financial.App/ViewModels/**/*.cs`, incl. `*FormValidation.cs`)

## What to test

- **Initial state**, `ApplyRefresh`/load population, filtering (`ColumnFilterViewModel<T>`),
  sorting, totals.
- **Commands**: `CanExecute` transitions (disabled until a selection exists, disabled while
  saving), execution calls the right `I*Service` method with the right DTO, refresh callback
  invoked, confirm callback honoured (`confirm: _ => false` → nothing deleted).
- **Validation** (`*FormValidation`): every rule from the React counterpart —
  `Financial.Web` is the UX source of truth — same wording and meaning; required fields, range
  (`Expense.MinRoundUpAmount`/`MaxRoundUpAmount` read directly by `ExpenseFormValidation`),
  same-bank transfer rejection.
- **State matrix** per `docs/rules/ui.md`: loading, empty, validation, server error (service
  throws → error text set, entered data preserved), saving (duplicate submit blocked), success,
  disabled, unsaved-changes prompt.
- **Observability**: `RecordingTelemetryTracer` records a span per workflow operation with the
  failure recorded on exception.
- **Selection through the tree**: `TreeNodeViewModel.IsSelected = true`
  (`Financial.App/ViewModels/Investment/TreeNodeViewModel.cs`), never by assigning
  `SelectedNode` — a directly assigned selection bypassed `CanExecute` re-evaluation and shipped
  a permanently disabled Move button past 743 green tests (`feedback_wpf_test_through_isselected`).
- **Time**: `TodayInfoTracker` and anything computing "today" takes a `TimeProvider` /
  injected date — no `DateTime.Today` in assertions except through
  `DateOnly.FromDateTime(DateTime.Today)` fixtures that do not depend on the day.
- Negative: service throws `KeyNotFoundException` / `OverdraftConfirmationRequiredException`
  → the ViewModel surfaces it as an error state, keeps input, records the failed span.

## Layer assignment

- **Unit** (default): ViewModel + stub services from `Tests/Financial.Presentation.Tests/ViewModels/**/TestStubs.cs`
  (`StubExpenseService`, `StubDialogService`, `StubTransactionService`, …) +
  `RecordingTelemetryTracer`; `confirm` and `refresh` are plain delegates. Runs on xUnit MTA
  threads — ViewModels must not touch `Dispatcher`/`Application.Current`
  (`Financial.App/CLAUDE.md`: keep WPF mechanics out of ViewModels; `IDialogService` wraps them).
- **Integration**: WPF composes the backend in-process, so a WPF feature's AC-tracing test
  builds the real `ServiceCollection` (`CashFlowServiceRegistrationTests.BuildServiceProvider`
  shape, temp `CashFlow:DataJsonFile`), resolves the real service, and hands it to the
  ViewModel — `../references/feature-traceability.md`.
- **No E2E** — there is no WPF UI-automation harness; the manual run required by
  `docs/rules/ui.md` "Completion requirement" is the closest thing.

## Setup pattern

From `Tests/Financial.Presentation.Tests/ViewModels/CashFlow/ExpenseWorkflowViewModelTests.cs`:

```csharp
private static (ExpenseWorkflowViewModel ViewModel, StubExpenseService Service, ObservableCollection<BankDTO> Banks) CreateViewModel(
    bool confirmDeletes = true, RecordingTelemetryTracer? tracer = null, Func<Task>? refresh = null)
{
    var expenseService = new StubExpenseService();
    var categories = new ObservableCollection<CategoryDTO>(DefaultCategories);
    var banks = new ObservableCollection<BankDTO>(DefaultBanks);
    var creditCards = new ObservableCollection<CreditCardDTO>(DefaultCreditCards);
    var viewModel = new ExpenseWorkflowViewModel(
        expenseService, categories, banks, creditCards,
        confirm: _ => confirmDeletes, tracer ?? new RecordingTelemetryTracer(), refresh ?? (() => Task.CompletedTask));
    return (viewModel, expenseService, banks);
}
```

The constructor is `ExpenseWorkflowViewModel(IExpenseService expenseService, ObservableCollection<CategoryDTO> categories, ObservableCollection<BankDTO> banks, ObservableCollection<CreditCardDTO> creditCards, Func<string, bool> confirm, ITelemetryTracer tracer, Func<Task> refresh)`
(`Financial.App/ViewModels/CashFlow/ExpenseWorkflowViewModel.cs:314`). Fire-and-forget
`Task.Run` inside ViewModels is awaited in tests with a short timeout; `ThreadPoolWarmup.cs`
raises the pool minimum so this does not flake on 2-core CI runners — do not add sleeps.

## When to skip

- `INotifyPropertyChanged` plumbing in `ViewModelBase` — tested once (`ViewModelBaseTests`).
- Row/record types with no logic (`AnnualSummaryRow`, `BankOperationRow`, `BankTotalRow`).
- `AdminEntityPlaceholderViewModel`.
- Re-testing a service rule through the ViewModel — stub the service to return the outcome and
  test the ViewModel's reaction.

## Examples from project

- `Tests/Financial.Presentation.Tests/ViewModels/CashFlow/ExpenseWorkflowViewModelTests.cs` — Unit; reference factory, filters, confirm/refresh delegates.
- `Tests/Financial.Presentation.Tests/ViewModels/MainNavigationViewModelMoveTests.cs` — Unit; `AssetNode(sut, "AAAA").IsSelected = true` selection, `MoveAssetCommand_IsUnavailableUntilAnAssetIsSelected`.
- `Tests/Financial.Presentation.Tests/ViewModels/PaymentDueBannerViewModelTests.cs` — Unit; `StubPaymentsDueService` from `Financial.TestUtilities`, empty → `IsVisible == false`.
- `Tests/Financial.Presentation.Tests/ViewModels/CashFlow/ExpenseFormValidationTests.cs`, `TransferFormValidationTests.cs` — Unit; parity with the React forms.
- `Tests/Financial.Presentation.Tests/ViewModels/Admin/BanksViewModelTests.cs` + `Admin/TestStubs.cs` — Unit; admin CRUD dialogs via `StubDialogService`.
- `Tests/Financial.Presentation.Tests/ViewModels/TodayInfoTrackerTests.cs` — Unit; date-dependent state.
