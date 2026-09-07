# F04. Integrations Settings — WPF — Technical Specification

## 1. Technical Overview

**What:** A new "Settings > Integrations" view in `Financial.App` (WPF), functionally equivalent to F03's Web page: connection status (not connected / connecting / connected / server error), account email, dedicated calendar name (as a clickable link, opened via the OS default browser), connected-since date, a Disconnect action behind a confirmation dialog, and a per-card DataGrid (name, due date, sync-status badge, inline Retry on errored rows) - adapted to WPF controls, navigation, and MVVM conventions.

**Why:** Unlike `Financial.Web`, `Financial.App` composes `Financial.CashFlow.Application`/`Infrastructure` directly in-process (confirmed: `App.xaml.cs` calls `AddFinancialCashFlowApplication()`/`AddFinancialCashFlowInfrastructure()`, and every existing CashFlow ViewModel - e.g. `CreditCardsViewModel` - injects the Application-layer service interface directly, not an HTTP client). So `SettingsIntegrationsViewModel` calls `ICalendarIntegrationService`/`ICreditCardCalendarSyncService` (both already registered by F01/F02) directly - no new HTTP client, no DTO duplication. The one genuinely new mechanic is opening the OS default browser to the Google consent URL: no `Process.Start`-based browser launch exists anywhere in this codebase today, so this feature introduces a small `IBrowserLauncher` abstraction (mirroring the existing `IDialogService` pattern: an interface wrapping a WPF/OS-specific call, with a `Stub*` test double) rather than calling `Process.Start` directly from the ViewModel.

**Scope:**
- Included: the new "Integrations" entry under the existing WPF "Settings" nav category, the connection panel (all states from PRD Experience), the per-card DataGrid with per-row Retry, the Disconnect confirmation (reusing the existing `IDialogService.Confirm`), the new `IBrowserLauncher` abstraction, and the connecting-state poll.
- Excluded: any change to F01/F02's Application/Infrastructure services (consumed as-is); the OAuth consent screen and callback landing page (served by `Financial.Api`, out of WPF's control - see Technical Decisions for the deployment implication).

The PRD has no Core Scope / Full Scope split for this feature - full scope as written applies.

## 2. Architecture Impact

**Affected components:**
- `Financial.App/Services/IBrowserLauncher.cs` + `BrowserLauncher.cs` (new) - opens a URL in the OS default browser via `Process.Start`.
- `Financial.App/ViewModels/Settings/SettingsIntegrationsViewModel.cs` (new) - the view's state/commands, injecting `ICalendarIntegrationService`, `ICreditCardCalendarSyncService`, `ICreditCardService` (to join card name/due date the same way F03's `useCalendarSyncStatuses` does against `GET /credit-cards`), `IBrowserLauncher`, `IDialogService`, `ILogger<SettingsIntegrationsViewModel>`.
- `Financial.App/Views/Settings/SettingsIntegrationsView.xaml` + `.xaml.cs` (new) - the view.
- `Financial.App/Converters/CalendarSyncStateToBrushConverter.cs` (new) - status-badge background/foreground brushes, mirroring `BillStatusToBrushConverter`.
- `Financial.App/Navigation/NavTree.cs` - one new `NavChild` under the existing `"settings"` `NavCategory`.
- `Financial.App/MainWindow.xaml.cs` - constructor-inject the new view, add it to `viewsByKey`.
- `Financial.App/App.xaml.cs` - DI registrations for the new ViewModel, View, and `IBrowserLauncher`.

```mermaid
graph TD
  NAV["NavTree (settings category)"] --> VK["MainWindow viewsByKey[\"settings-integrations\"]"]
  VK --> V["SettingsIntegrationsView"]
  V --> VM["SettingsIntegrationsViewModel"]
  VM --> CIS["ICalendarIntegrationService (in-process, F01)"]
  VM --> SYNC["ICreditCardCalendarSyncService (in-process, F02)"]
  VM --> CCS["ICreditCardService (in-process, existing)"]
  VM --> BL["IBrowserLauncher (new)"]
  VM --> DS["IDialogService.Confirm (existing)"]
  BL -->|"Process.Start"| BROWSER["OS default browser"]
  BROWSER -->|"Google consent, then redirect"| API["Financial.Api /integrations/calendar/callback (must be running)"]
```

## 3. Technical Decisions

| Decision | Chosen Approach | Alternative Considered | Trade-off |
|----------|----------------|----------------------|-----------|
| Service access | `SettingsIntegrationsViewModel` injects `ICalendarIntegrationService`/`ICreditCardCalendarSyncService`/`ICreditCardService` directly (in-process DI) | An `HttpClient` calling `Financial.Api`, mirroring F03's Web client | Confirmed via `App.xaml.cs` and every existing CashFlow ViewModel: `Financial.App` always composes CashFlow's Application/Infrastructure in-process (`SyncStatusViewModel`'s own doc comment states this explicitly). Introducing an HTTP client here would be the only one in the whole WPF app and would duplicate DTOs Financial.CashFlow.Application already exposes for free. |
| Browser launch | New `IBrowserLauncher.OpenUrl(string url)`, real implementation `Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })`, registered singleton, stubbed as `StubBrowserLauncher` in tests | Call `Process.Start` directly from the ViewModel | No existing precedent for launching the OS browser exists in this codebase (confirmed: zero `Process.Start`/`Hyperlink` hits). `IDialogService` already establishes the pattern of wrapping a WPF/OS-specific call behind a small interface so ViewModels stay unit-testable without a real window/process; `IBrowserLauncher` follows the same shape for the same reason (needed for both the Connect button and the calendar-name link). |
| OAuth callback reachability | Out of scope for F04, inherited from F01: the browser is opened to a Google consent URL whose registered `RedirectUri` points at `Financial.Api`'s `/integrations/calendar/callback`, so completing a real connect from the desktop app still requires `Financial.Api` to be running and reachable (normally true in this single-process-per-install, self-hosted deployment) | WPF hosting its own OAuth callback listener | F01 already fixed the redirect URI at the API process for both front ends; duplicating an HTTP listener inside `Financial.App` for this one flow would be significant new scope with no benefit, since the always-on API process already serves it. Documented here so it's not mistaken for an F04 gap. |
| Detecting connect completion | A `DispatcherTimer` (short interval, `ConnectingPollInterval = 3s`) starts when `Connect()` runs, re-fetching status on each tick; stops once `Connected` is true or a hard error (`DisconnectReason` set) is observed | Wiring `Window.Activated` (mirroring Web's `window.addEventListener('focus', ...)`), per the PRD's literal "on window-focus-regained" wording | `Window.Activated` has zero precedent in this codebase and firing it into a specific child ViewModel would need new coupling in the shared `MainWindow.xaml.cs`/`MainShellViewModel` for one feature. The existing `DispatcherTimer` poll (`SyncStatusViewModel`, same 15s-interval shape) is the established WPF pattern for "keep this in sync without a manual refresh"; a short interval while `IsConnecting` achieves the PRD's actual outcome (AC-03: status updates without an explicit manual refresh) without introducing an unprecedented event-forwarding path. Documented per `docs/ui/wpf.md`'s "document intentional differences" rule. |
| Disconnect confirmation | Reuse the existing `IDialogService.Confirm(message, caption)` (the same mechanism `CreditCardsViewModel`'s delete confirmation uses) | A new bespoke `*DialogViewModel` (per `docs/ui/wpf.md`'s form-dialog pattern) | The confirmation is a plain Confirm/Cancel with static warning text - no fields, no third outcome - exactly what `IDialogService.Confirm` already exists for. No new interface member needed on `IDialogService`/`DialogService`/`StubDialogService`. |
| Status badge | `DataGridTemplateColumn` with a `Border`+`TextBlock` bound through `CalendarSyncStateToBrushConverter` (background/foreground pair, mirroring `BillStatusToBrushConverter`'s shape) - text always one of "Synced" / "Syncing…" / "Sync failed: {reason}", matching F03's Web badge text exactly | Reusing `StatusSplitButton` | `StatusSplitButton` is a *clickable* status-change control (opens a menu to change status) - this column is read-only per-row (Retry is a separate, explicit button), so the simpler `Border`+`TextBlock`+converter combo (the same primitives `StatusSplitButton` itself is built from) is the right-sized match, avoiding a click-to-change affordance nothing in this feature uses. |
| Parity with F03's actual shipped columns | The DataGrid shows exactly what F03 ships today: Card / Due Date / Sync Status (badge carries the error reason, no separate "last synced" timestamp column) / Retry | Also rendering a separate `LastSuccessfulSyncUtc` column, per the PRD prose ("last-synced time") | `docs/ui/wpf.md`: "inspect the corresponding React feature" as the source of truth ahead of PRD prose when the two diverge. F03's merged `IntegrationsPage.tsx` never rendered a separate last-synced-time column (folded into the badge text) - WPF parity means matching what F03 actually shipped, not the PRD's original wording. |
| PRD AC-04 ("Currency/balance values in the per-card list are right-aligned") | Left unmapped to any UI element - documented here rather than silently dropped | Inventing a balance/currency column to satisfy the AC literally | F03's shipped per-card list has no currency/balance value at all (only name, due date, status) - this AC appears to predate F03's final design, which the PRD itself never gave a balance column to F03's Capabilities section either. Per `docs/rules/ui.md`'s React-source-of-truth rule, F04 matches F03's real shape; this AC is reported as "no matching UI element" in the final report rather than forced or silently checked off. |
| Row shape | A local `CalendarSyncRow` record (CreditCardId, Name, DueDate, State, LastError) constructed by joining `ICreditCardService.GetCreditCards()` (filtered to `IsActive && NextInvoiceDueDate != null`) with `ICreditCardCalendarSyncService.GetSyncStatuses()` by id - mirrors F03's `useCalendarSyncStatuses` join exactly | A join done inside a new Application-layer method | Matches F03's precedent (client-side join, since `GetSyncStatuses()` deliberately excludes card name/due date per F02's own spec) and needs no backend change. |

## 4. Component Overview

**`Financial.App/Services`:**

| File Path | New/Modified | Purpose | Key Responsibilities |
|-----------|--------------|---------|---------------------|
| `IBrowserLauncher.cs` | New | Contract | `void OpenUrl(string url)` |
| `BrowserLauncher.cs` | New | Implementation | `Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })` |

**`Financial.App/ViewModels/Settings`:**

| File Path | New/Modified | Purpose | Key Responsibilities |
|-----------|--------------|---------|---------------------|
| `SettingsIntegrationsViewModel.cs` | New | View state/commands | `IsLoading`/`Error`/`ShowContent` (via `ExecuteRefreshAsync`, matching `CreditCardsViewModel`'s pattern); `Status` (`CalendarConnectionStatusDTO?`); `IsConnecting`; `ConnectCommand` (opens the browser via `IBrowserLauncher`, starts the `DispatcherTimer` poll); `IsDisconnecting`/`DisconnectError`; `DisconnectCommand` (calls `IDialogService.Confirm` first); `SyncRows` (`ObservableCollection<CalendarSyncRow>`); `RetryingCardId`; `RetryCommand` (per-row, `RelayCommand<CalendarSyncRow>`) |

**`Financial.App/Views/Settings`:**

| File Path | New/Modified | Purpose | Key Responsibilities |
|-----------|--------------|---------|---------------------|
| `SettingsIntegrationsView.xaml` | New | The view | Loading/Error/Content triad (matching `CreditCardsView.xaml`'s `Visibility` pattern); not-connected/connecting/connected sub-states; the per-card `DataGrid`; inline Disconnect action |
| `SettingsIntegrationsView.xaml.cs` | New | Wiring | Constructor takes `SettingsIntegrationsViewModel`, sets `DataContext` (matching `AppearanceView.xaml.cs`) |

**`Financial.App/Converters`:**

| File Path | New/Modified | Purpose | Key Responsibilities |
|-----------|--------------|---------|---------------------|
| `CalendarSyncStateToBrushConverter.cs` | New | Status badge colors | Maps `"Synced"`/`"Error"`/anything else (`"Pending"` or absent) to background/foreground `SolidColorBrush` pairs via a `parameter`-switched `IValueConverter`, mirroring `BillStatusToBrushConverter` |

**`Financial.App/Navigation`:**

| File Path | New/Modified | Purpose | Key Responsibilities |
|-----------|--------------|---------|---------------------|
| `NavTree.cs` | Modified | Sidebar entry | Adds `new NavChild("integrations", "Integrations", "settings-integrations")` to the existing `"settings"` category's `Children` |

**`Financial.App` root:**

| File Path | New/Modified | Purpose | Key Responsibilities |
|-----------|--------------|---------|---------------------|
| `MainWindow.xaml.cs` | Modified | Wire the view | New constructor parameter `SettingsIntegrationsView settingsIntegrationsView` (null-checked); `viewsByKey["settings-integrations"] = settingsIntegrationsView` |
| `App.xaml.cs` | Modified | DI registration | `AddSingleton<IBrowserLauncher, BrowserLauncher>()`; `AddTransient<SettingsIntegrationsViewModel>()`; `AddTransient<SettingsIntegrationsView>()` |

## 5. API Contracts

No HTTP API surface - `Financial.App` calls `ICalendarIntegrationService`/`ICreditCardCalendarSyncService`/`ICreditCardService` in-process (all already exist, unchanged by this feature):

- `ICalendarIntegrationService.GetStatusAsync()` → `CalendarConnectionStatusDTO` (`Connected`, `AccountEmail`, `CalendarName`, `CalendarId`, `ConnectedAtUtc`, `DisconnectReason`)
- `ICalendarIntegrationService.BuildAuthorizationUrl()` → `string` (passed to `IBrowserLauncher.OpenUrl`)
- `ICalendarIntegrationService.DisconnectAsync()` → `CalendarDisconnectResultDTO`
- `ICreditCardCalendarSyncService.GetSyncStatuses()` → `IReadOnlyList<CreditCardCalendarSyncStatusDTO>`
- `ICreditCardCalendarSyncService.ResyncAsync(Guid, CancellationToken)` → `CreditCardCalendarSyncStatusDTO`
- `ICreditCardService.GetCreditCards()` → `IReadOnlyList<CreditCardDTO>` (existing, unrelated feature - used only for the name/due-date join)

## 6. Data Model

No new persistence. `CalendarSyncRow` is a ViewModel-only projection (not persisted), matching F03's `CalendarSyncStatusRow` shape:

| Field | Type | Source |
|-------|------|--------|
| `CreditCardId` | `Guid` | `CreditCardDTO.Id` |
| `Name` | `string` | `CreditCardDTO.Name` |
| `DueDate` | `DateOnly` | `CreditCardDTO.NextInvoiceDueDate` (non-null; rows are pre-filtered) |
| `State` | `string` | `CreditCardCalendarSyncStatusDTO.State`, or `"Pending"` when the card is absent from `GetSyncStatuses()` (never synced yet) |
| `LastError` | `string?` | `CreditCardCalendarSyncStatusDTO.LastError` |

## 7. Testing Strategy

| Test File | Test Type | Target | Coverage Goal |
|-----------|-----------|--------|---------------|
| `Tests/Financial.Presentation.Tests/ViewModels/Settings/TestStubs.cs` | — (test infrastructure) | New stubs | `StubCalendarIntegrationService : ICalendarIntegrationService`, `StubCreditCardCalendarSyncService : ICreditCardCalendarSyncService`, `StubBrowserLauncher : IBrowserLauncher` (records `LastOpenedUrl`), a minimal `StubCreditCardService` (read-only shape); reuses `Financial.Presentation.Tests.ViewModels.Admin.StubDialogService` for `Confirm` |
| `Tests/Financial.Presentation.Tests/ViewModels/Settings/SettingsIntegrationsViewModelTests.cs` | Unit | `SettingsIntegrationsViewModel` | Initial load success/error; not-connected → Connect opens the browser via `IBrowserLauncher` and starts polling; a poll tick that observes `Connected: true` stops `IsConnecting`; Disconnect always calls `IDialogService.Confirm` first, cancel leaves state untouched, confirm calls `DisconnectAsync` and refreshes; a disconnect failure surfaces `DisconnectError`; `SyncRows` joins active/due-dated cards with their status, defaulting an absent status to `"Pending"`; Retry calls `ResyncAsync` for the right card id and updates only that row, tracking `RetryingCardId` while in flight |
| `Tests/Financial.Presentation.Tests/Views/Settings/SettingsIntegrationsGridBindingTests.cs` | Contract (Integration-shaped) | `SettingsIntegrationsView.xaml` | Every `DataGridTextColumn`/templated binding in the sync-status grid resolves to a real `CalendarSyncRow` property, following `ExpenseGridBindingTests`'s regex-over-XAML-plus-reflection pattern; also asserts no `<Run Text="{Binding` appears anywhere in the new view |
| Manual run (`docs/rules/ui.md` Definition of Done) | Manual | `SettingsIntegrationsView` | Launch the built app, exercise not-connected/connecting/connected/error states, keyboard-only path, light/dark theme, narrow window; confirm the calendar-name link and Connect button open the OS default browser |

No automated E2E exists for WPF (no UI-automation harness in this codebase) - the manual run is the closest equivalent to F03's Playwright smoke coverage, per `references/e2e-environment.md`.

**Cross-Feature Integration criteria (PRD §9) naming F04:** both require F04 to be implemented before they can be checked off - "F03 and F04 both correctly display F01's live connection status..." and "...F02's per-card sync status...". This spec's `SettingsIntegrationsViewModel` satisfies both by construction (reads the same `ICalendarIntegrationService`/`ICreditCardCalendarSyncService` F03 reads, just in-process instead of over HTTP), verified by the Unit tests above; `implement-feature`'s Step 7 checks these PRD boxes once F04's own ACs pass, since F03 is already merged.
