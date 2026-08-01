# Implementation Plan: F01. WPF CashFlow Foundation & Navigation Shell

**Prerequisites:**
- .NET 10 SDK, `Financial.App` (WPF, `net10.0-windows`) buildable locally
- `Financial.CashFlow.Application` / `Financial.CashFlow.Infrastructure` already implemented and consumed by `Financial.Api` (source of the DI extension methods and service interfaces this feature wires up)
- A local copy of `data-cashflow.json` for manual smoke testing (never the live file)

### Stage 1: Per-Domain Folder Reorganization

**1. Relocate Investment Views and ViewModels** - Move every file currently in `Financial.App/Views/` into `Financial.App/Views/Investment/`, and every file in `Financial.App/ViewModels/` except `ViewModelBase.cs`/`RelayCommand.cs` into `Financial.App/ViewModels/Investment/`, updating each file's namespace/`x:Class` declaration accordingly.

**2. Restore build after the move** - Add the new `ViewModels.Investment` global using to `SharedUsings.cs`, and update the small number of files whose `using` directives don't already resolve through it (`MainWindow.xaml.cs`, `App.xaml.cs`, `Components/NavigationView.xaml.cs`, the root-level dialog code-behind files), until `dotnet build` succeeds again with zero behavior change.

**3. Update existing tests for the new namespace** - Update the `using` directives in `Tests/Financial.Presentation.Tests/ViewModels/*.cs` to reference `Financial.Presentation.App.ViewModels.Investment`, and confirm the full existing test suite still passes unchanged.

### Stage 2: CashFlow Service Wiring

**4. Reference the CashFlow projects** - Add `ProjectReference`s from `Financial.App.csproj` to `Financial.CashFlow.Application` and `Financial.CashFlow.Infrastructure`.

**5. Register CashFlow services in DI** - Call `AddFinancialCashFlowApplication()` and `AddFinancialCashFlowInfrastructure(configuration)` from `App.xaml.cs`'s `ConfigureServices`, alongside the existing Investment registrations.

**6. Add CashFlow configuration** - Add a `CashFlow` section to `appsettings.json` and `appsettings.Development.json`, mirroring the existing `Investment` section's shape and the already-present (but previously unused) `CashFlow` section in `appsettings.Production.json`.

### Stage 3: Cash Flow Navigation Shell

**7. Add the Cash Flow tab and nested shell** - Add a new "Cash Flow" `TabItem` to `MainWindow.xaml`, positioned after the existing Investment tabs, containing a nested `TabControl` with 6 empty `TabItem`s in order: Monthly, Reserva, Mensais, Controle Mãe, Investment Snapshots, Annual Summary.

**8. Verify existing Investment tabs are unaffected** - Confirm the 4 existing top-level tabs (Active Investments, Historic Investments, Shares Dividend check, Read Assets current values) still function identically after the folder move and the new tab's addition.

### Stage 4: Verification

**9. Add the CashFlow DI resolution test** - Add `Tests/Financial.Presentation.Tests/DependencyInjection/CashFlowServiceRegistrationTests.cs`, asserting all 12 CashFlow service interfaces resolve from a `ServiceCollection` built the same way `App.xaml.cs` builds it.

**10. Full solution build and test pass** - Run `dotnet build` across the solution and `dotnet test` for `Financial.Presentation.Tests`, confirming zero regressions from the folder move and the new registrations/test.

**11. Manual smoke test** - Launch `Financial.App` against a temporary copy of `data-cashflow.json` and confirm the Cash Flow tab and its 6 empty nested tabs render correctly alongside the unaffected Investment tabs.
