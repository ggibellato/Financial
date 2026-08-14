# Implementation Plan: F12. WPF Sync Status Indicator

**Prerequisites:**
- F11 (`SyncStatusViewModel`, threaded through `MainShellViewModel`) merged to `main`
- No new libraries

### Stage 1: Indicator Data

**1. SyncStatusViewModel Computed Properties** - Add `IsIndicatorVisible` and `IndicatorMessages` to F11's `SyncStatusViewModel`, deriving them from `CashFlowStatus`/`InvestmentStatus` and raising property-change notifications whenever `RefreshStatus()` updates the underlying status, using the same message wording as F10's web banner.

**2. ViewModel Test Coverage** - Cover the visibility rule, per-context naming (including the simultaneous-failure case), the last-error/save-time content, the "no prior successful save" fallback, and the disappear-on-recovery transition.

### Stage 2: Indicator UI

**3. SyncStatusIndicator UserControl** - Create a UserControl that binds its visibility to `IsIndicatorVisible` and renders one line per entry in `IndicatorMessages`, matching the existing warning-banner visual style already used elsewhere in the app.

**4. MainWindow Wiring** - Add the indicator to `MainWindow.xaml` alongside the existing sidebar and breadcrumb, bound to the `SyncStatus` property already exposed by `MainShellViewModel`, so it stays visible regardless of which page is active.
