# Implementation Plan: Broker & Portfolio Totals Display — WPF

**Prerequisites:**
- F01 (Aggregated Totals Enhancement — Application Layer) already merged to `main`; `TotalInvested` is already returned by `ISummaryQueryService.GetBrokerSummary`/`GetPortfolioSummary`
- No new tools, libraries, or environment variables required

### Stage 1: ViewModel and Interface

**1. AssetDetailsViewModel view state and totals** - Add `IsBrokerView` and `TotalInvested` properties to `AssetDetailsViewModel`, mirroring the existing `IsPortfolioView` pattern. Add a new `LoadBrokerSummary` method that populates Credits-tab state, sets `IsBrokerView`, and sets `TotalInvested` from the summary DTO, replacing the existing `LoadBrokerCredits` method. Extend `LoadPortfolioSummary` to also set `TotalInvested`, and ensure `Clear()`, `LoadAssetDetails`, and the shared credits-loading helper correctly reset the new Broker view state. Reference the spec's Component Overview for exact reset points.

**2. IAssetDetailsViewModel contract update** - Update the interface to expose `IsBrokerView` and `LoadBrokerSummary`, removing the now-unused `LoadBrokerCredits` member. Reference the spec's Technical Decisions for the rationale on what does and doesn't belong on the interface.

### Stage 2: WPF Template

**3. BrokerSummaryTemplate** - Add a new DataTemplate showing only the four colour-coded totals (Total Bought, Total Sold, Total Credits, Total Invested) in a 2×2 grid layout, with no asset-specific fields and no DataGrid. Reference the spec's Technical Decisions for the chosen layout and colour-coding approach.

**4. Template selection and PortfolioSummaryTemplate extension** - Wire up template selection so Broker node selection renders the new template instead of falling back to the asset template, and extend the existing Portfolio totals display with the fourth Total Invested field using the same colour rule. Reference the spec's Component Overview for the exact binding and trigger pattern to mirror.

### Stage 3: Routing and Tests

**5. Broker node dispatch** - Update the Broker node selection routing to call the new `LoadBrokerSummary` method instead of the old `LoadBrokerCredits` method, with no change to how the summary and credits data are fetched.

**6. ViewModel test coverage** - Add a new test file covering `LoadBrokerSummary`'s behaviour (view state, totals, Credits-tab population, asset-field clearing, and reset behaviour on `Clear()`/node-switching), and extend the existing Portfolio summary test file with `TotalInvested` coverage. Reference the spec's Testing Strategy for the full list of test functions.

**7. Routing test coverage** - Update the existing Broker-dispatch test double to implement the new interface method, and confirm existing Broker-selection routing tests still pass unchanged.
