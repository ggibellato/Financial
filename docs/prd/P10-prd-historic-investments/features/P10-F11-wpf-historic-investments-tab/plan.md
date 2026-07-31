# Implementation Plan: WPF — Historic Investments Tab

**Prerequisites:**
- F05 (Scoped Navigation & Summary API), F06 (Historic Realized Totals Service), and F07 (Historic Broker Breakdown Charts Service) already implemented — every service this feature calls already accepts an `InvestmentScope`
- F10 (WPF — Active Investments Tab Update) already implemented — this feature builds directly on its scope-explicitness and tab-relabeling groundwork
- No new tools, packages, or configuration required

### Stage 1: Scope-Aware View Models

**1. Base navigation view model scope parameter** - Give the shared navigation view model base class a constructor-supplied investment scope, replacing its previously hardcoded active-only behavior at every internal service call, while keeping the existing active view model's construction unchanged via a default.

**2. Asset details view model scope awareness** - Give the asset details view model its own constructor-supplied scope, used to request the correct breakdown scope, to skip the current-value/XIRR refresh entirely for historic assets, and to skip the per-row price fetch for a historic portfolio's asset grid in favor of an explicit "no price data" state.

**3. Row-level "no price data" state** - Add a way for a portfolio asset summary row to be marked as having no price data by design (as opposed to a failed fetch), reusing the same display fallback the existing failed-fetch state already provides.

**4. Scope-awareness test coverage** - Extend the existing view-model tests to cover the new scope parameter and the new "no price data" state, following the same assertion style established for the active-scope explicitness tests.

### Stage 2: Historic Navigation View Model

**5. Historic-scoped view model class** - Add a new navigation view model class mirroring the existing active one, supplying the historic scope to both the base class and its own asset details view model.

**6. Dependency injection registration** - Register the new historic-scoped view model alongside the existing active one.

### Stage 3: Historic Investments Tab UI

**7. Current-value section visibility** - Hide the summary view's live current-value and refresh elements when viewing a historic asset, reusing the existing visibility-binding pattern already used elsewhere in the summary view.

**8. Second tab in the main window** - Add the "Historic Investments" tab, positioned directly after "Active Investments", hosting its own instance of the existing navigation view bound to the new historic-scoped view model, and load its tree on startup exactly as the active tab does.
