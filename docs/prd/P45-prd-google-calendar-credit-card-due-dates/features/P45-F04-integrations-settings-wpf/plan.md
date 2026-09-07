# Implementation Plan: F04. Integrations Settings — WPF

**Prerequisites:**
- F01 (Google Calendar Account Connection) merged - `ICalendarIntegrationService` exists and is registered by `AddFinancialCashFlowApplication()`.
- F02 (Credit-Card Due-Date Event Sync) merged - `ICreditCardCalendarSyncService` exists and is registered the same way.
- F03 (Integrations Settings — Web) merged - the React reference implementation this feature must match (`docs/ui/wpf.md`'s "inspect the corresponding React feature" rule).
- A connected Google Calendar (via F01/the Web Integrations page) to observe the connected/per-card states during manual verification; a not-connected state is the default starting point.

Each phase is sized to land as its own commit on this feature's single PR, following the project's vertical-slice-per-phase convention (this feature ships as one PR per the project's established precedent, not one PR per phase).

### Phase 1: Browser Launcher and ViewModel

**1. Browser launcher abstraction** - Add `IBrowserLauncher`/`BrowserLauncher`, opening a URL in the OS default browser, following the existing `IDialogService` pattern of wrapping a WPF/OS-specific call behind a small interface.

**2. Status-badge converter** - Add `CalendarSyncStateToBrushConverter`, mapping a sync state string to background/foreground brushes, mirroring `BillStatusToBrushConverter`.

**3. Settings Integrations ViewModel** - Implement `SettingsIntegrationsViewModel`: initial status/sync-list load (via the shared `ExecuteRefreshAsync` pattern), the not-connected/connecting/connected state machine, `ConnectCommand` (opens the browser, starts a short-interval `DispatcherTimer` poll until connected), `DisconnectCommand` (confirms via `IDialogService.Confirm`, then disconnects and refreshes), the per-card `CalendarSyncRow` join (active + due-dated cards against the sync-status list), and the per-row `RetryCommand`.

### Phase 2: View and Navigation Wiring

**4. Settings Integrations View** - Add `SettingsIntegrationsView.xaml`/`.xaml.cs`: the Loading/Error/Content triad, the not-connected/connecting/connected sub-states (account email, calendar name as a browser-opening link, connected-since date, Disconnect button disabled while in flight), and the per-card `DataGrid` (name, due date, status badge, inline Retry on errored rows).

**5. Navigation wiring** - Add the "Integrations" child to the existing `"settings"` category in `NavTree.cs`, constructor-inject the new view into `MainWindow`, and register the new services/ViewModel/View in `App.xaml.cs`.

### Phase 3: Tests and Verification

**6. ViewModel and binding-contract tests** - Add the new test stubs and `SettingsIntegrationsViewModelTests`, plus a grid binding-contract test for the new `DataGrid` following `ExpenseGridBindingTests`'s pattern.

**7. Manual verification and documentation** - Run the built app to exercise every state (not-connected, connecting, connected, error, keyboard-only, light/dark, narrow window) per `docs/rules/ui.md`'s Definition of Done, and update `README.md` if the WPF entry point needs its own mention.
