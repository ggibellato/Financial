# Implementation Plan: Cryptocurrency Filter — WPF Desktop

**Prerequisites:**
- .NET SDK targeting `net10.0-windows` (already installed)
- xUnit + FluentAssertions (already referenced by `Tests/Financial.Presentation.Tests`)
- No new packages; no production code changes in this feature

### Stage 1: Filter List Verification

**1. Cryptocurrency Filter List Coverage** - Add a test to the existing `MainNavigationViewModelBase` test suite confirming the asset-class filter list already includes a correctly labeled "Cryptocurrency" entry, since it is populated dynamically from the domain enum and requires no new production code.
