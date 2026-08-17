# WPF Desktop Client (Financial.App)

See legend in [README.md](README.md).

## Scope — client for both bounded contexts

**CONFIRMED.** `Financial.App` is a full client for **both** the Investment and CashFlow bounded contexts. `Views/CashFlow/` (26 views) and `ViewModels/CashFlow/` (27 files) implement Monthly, Reserva, Mensais, Controle Mãe, Investment Snapshots, and Annual Summary in full, alongside the Investment-side UI. (An earlier version of `context.md` incorrectly stated CashFlow was "React pages only, not available in the WPF app" — that line was stale and has been removed.)

## Not an HTTP client of Financial.Api

**CONFIRMED — the single most important architectural fact about this project.** `Financial.App.csproj` references both bounded contexts' `Application`/`Infrastructure` projects (plus `Investment.Domain`, `GoogleFinancialSupport`) directly. There is no `HttpClient`/API-base-URL wiring anywhere in `App.xaml.cs`. `Financial.App` is a **second, independent in-process composition root** hosting the same Domain/Application code as `Financial.Api`, with its **own** Infrastructure instance — its own file/Google Drive I/O, entirely separate from whatever `Financial.Api` is doing. See [02-architecture.md](02-architecture.md) and [07-data-persistence.md](07-data-persistence.md) for the consequences (no synchronization between the two processes if run concurrently).

## MVVM pattern

**CONFIRMED** — MVVM via .NET Generic Host (`Microsoft.Extensions.Hosting`/`DependencyInjection`), not a third-party MVVM toolkit (no CommunityToolkit.Mvvm, no Prism).

- `App.xaml.cs` builds an `IHost`, registers all Views/ViewModels as `Transient` (the one exception: `SyncStatusViewModel` as `Singleton`), resolves `MainWindow` from the container.
- `MainWindow.xaml.cs` builds a `Dictionary<string, object>` mapping navigation keys directly to **already-constructed** View instances (not a lazy `DataTemplate`-resolved View-per-ViewModel pattern). `MainShellViewModel` swaps the displayed content by dictionary lookup on navigation.
- Commanding: custom `RelayCommand`/`RelayCommand<T>` (`ViewModels/RelayCommand.cs`), hooked to WPF's `CommandManager.RequerySuggested`.
- Validation: per-form static classes (`static class XFormValidation { static string BuildValidationMessage(...) }`) — procedural, not `IValidatableObject`/FluentValidation/DataAnnotations.
- Shared `ViewModelBase` provides `INotifyPropertyChanged`, `SetProperty<T>`, and a shared `ExecuteSaveAsync` validate→save→error sequence used by CashFlow forms.

## Navigation

**CONFIRMED** — `Navigation/NavTree.cs` defines two `NavCategory`s ("Investments", "CashFlow") with 10 leaf routes total, structurally mirroring `Financial.Web/src/navigation/navTree.ts` (a code comment explicitly notes the icon geometry is deliberately kept in sync with `Sidebar.tsx`'s inline SVGs). Single collapsible sidebar (`Components/Sidebar.xaml`) driving one content area — not tab-based, not a tree-per-page.

## Views/ViewModels organization

**OBSERVED, with a confirmed asymmetry between the two contexts:**

- CashFlow: all views consistently under `Views/CashFlow/`; all ViewModels under `ViewModels/CashFlow/`.
- Investment: most XAML actually sits at the project root (`MainWindow.xaml`, `CreditDialog.xaml`, `PriceDialog.xaml`, `TransactionDialog.xaml`), not under `Views/Investment/` (which holds only 2 of the many Investment views: `AssetPriceView`, `DividendCheckView`). ViewModels are under `ViewModels/Investment/`.
- `ViewModels/CashFlow/` mixes three kinds of types in one flat folder: true ViewModels, plain row/DTO presentation models (`AnnualSummaryRow`, `BankTotalRow`, `IncomeTotalRow`, `ReserveMovementRow`, `SnapshotRow`, `CreditCardManagementRow`), and static validator classes (`*FormValidation.cs`).
- A shared `MainNavigationViewModelBase` exists only on the Investment side (`MainNavigationViewModel`/`MainNavigationViewModelHistoric` both derive from it, supporting the Active/Historic split — see [09-domain-investment.md](09-domain-investment.md)). No equivalent base exists for CashFlow ViewModels, despite many sharing a similar constructor shape (injected `Func<string,bool>`/`Action<string>` confirm/error callbacks).

## State management

**CONFIRMED** — one shared shell ViewModel (`MainShellViewModel`, for sidebar/selection/breadcrumb state) plus one cross-cutting `Singleton` (`SyncStatusViewModel`, surfacing save-sync status — see [07-data-persistence.md](07-data-persistence.md)). Feature ViewModels are independently `Transient`-scoped; each receives injected confirm/error closures wired directly in `App.xaml.cs` (meaning the composition root itself contains WPF UI calls, e.g. `MessageBox.Show`). **UNKNOWN** whether Investment-side tree/selection ViewModels share a "selected node" concept analogous to Web's `SelectedNodeContext` — not verified.

## Feature parity with Web

**CONFIRMED at the navigation-category level** — all 6 Web CashFlow nav entries have exact `ViewKey`/View/ViewModel counterparts in WPF, and both apps fold "Income/Expenses," "Banks & Cards," and "Recurring Bills" into the single "Monthly" page/view rather than exposing them as separate top-level nav entries. **UNKNOWN** at the sub-feature/component level — not exhaustively cross-checked component-by-component.

## Tests

**CONFIRMED — substantial.** `Financial.Presentation.Tests` references `Financial.App.csproj` directly, ~45+ files covering Converters, Helpers, Input, `Navigation/NavTreeTests.cs`, and ViewModels for both contexts (including form-validation tests and at least one XAML binding test). See [11-testing.md](11-testing.md).
