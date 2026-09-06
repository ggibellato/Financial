# Implementation Plan: F02. Investment Snapshot Suggested Values

**Prerequisites:**
- .NET 8 SDK and Node/npm toolchains, existing solution and `Financial.Web` building cleanly, F01 (InvestmentAccount Source Configuration) merged
- No new NuGet or npm packages required

### Stage 1: Application, Contract, and Backend Tests

**1. Reserve Bucket As-Of-Date Domain Rule** - Add a domain-level rule that computes the last day of the month immediately before a given year/month, and sums reserve bucket movements up to that date. Reference the spec for the exact function shape.

**2. Suggestion DTOs and Service Computation** - Add the new suggestion and skipped-account response DTOs, and extend the investment snapshot service with a suggestions computation that reuses the existing month-scoping logic, branches per account source, and builds each row's human-readable source description.

**3. Endpoint and Contract Regeneration** - Add the new suggestions GET endpoint to the investment snapshots controller, then regenerate the OpenAPI snapshot and the frontend's generated API types from it.

**4. Domain, Application, and Api Tests** - Write the domain rule, service, and endpoint test coverage described in the spec's testing strategy, including the settled-expense inclusion rule, the no-statement gate, the reserve as-of-date computation, and the cross-feature-integration fixture covering one account of each source type.

### Stage 2: Web

**5. Suggested Values Hook** - Add a hook managing the suggest-review-apply state machine: fetching suggestions, per-row include/edit state defaulted from the current value, sequential apply with per-row progress and outcome tracking, and a retry-failed action that resubmits prior values without refetching.

**6. Suggested Values Panel and Page Wiring** - Add the inline review panel covering every state from the spec's UX flows, and wire the "Suggest Values" button and panel into the Investment Snapshot page, ensuring it collapses the existing Edit Snapshot panel and vice versa.

**7. Web Hook and Component Tests** - Write the hook, panel, and page test coverage described in the spec's testing strategy, covering every state, the default include rule, partial-failure handling, and the mutual-exclusivity behavior with the edit panel.

### Stage 3: WPF

**8. Suggestion Row and ViewModel State** - Add the mutable suggestion row type and extend the Investment Snapshot ViewModel with the panel's open/loading/populated/applying/completion state, commands, and progress reporting, mirroring the Web hook's behavior.

**9. View and WPF Tests** - Add the equivalent inline panel to the Investment Snapshot view, and write the ViewModel test coverage mirroring the Web test set for the same acceptance criteria.
