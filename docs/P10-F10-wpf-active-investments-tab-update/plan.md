# Implementation Plan: WPF — Active Investments Tab Update

**Prerequisites:**
- F01 (Position Type Domain Model) and F05 (Scoped Navigation & Summary API) already implemented — `Asset.PositionType` and `InvestmentScope`-parameterized service methods already exist
- No new tools, packages, or configuration required

### Stage 1: Position-Type Color Converter

**1. PositionTypeToColorConverter** - Add a new WPF value converter that maps the three position-type states to brushes, replacing the old boolean active/inactive converter. Register it as an `App.xaml` resource in place of the converter it replaces. Reference the spec's Technical Decisions and Component Overview for the exact mapping and fallback behavior.

**2. Converter unit tests** - Add the first test file under a new `Converters` test folder, covering all three position states plus the unrecognized-input fallback.

### Stage 2: Tree Indicator and Tab Label

**3. Tree icon binding** - Update the asset tree's icon template to use the new converter against the position-type metadata field instead of the old active/inactive metadata field, and simplify the glyph itself per the spec's interview decision. Remove the now-unused old converter and its binding.

**4. Tab relabel** - Update the main window's tab header text to "Active Investments".

### Stage 3: Explicit Active Scope

**5. Navigation and asset-details scope** - Update the navigation view model's tree-loading and asset-details-loading calls to pass the active scope explicitly instead of relying on the service default.

**6. Summary and breakdown scope** - Update the navigation view model's portfolio/broker summary and asset-summary calls, and the asset-details view model's broker-breakdown call, to pass the active scope explicitly.

**7. Scope-explicitness test coverage** - Extend the existing view-model test stubs to capture the scope argument they receive, and add assertions confirming every call site now requests the active scope explicitly.
