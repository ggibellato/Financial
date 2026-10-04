# Implementation Plan: Deterministic Test Infrastructure

**Prerequisites:**
- .NET 10 SDK, Node 24, an up-to-date `main`
- Each stage below is one PR; the three are independent and may merge in any order. Stage 2 should be reviewed knowing Stage 1's default stub removes most Frankfurter traffic from tests.

### Stage 1: Network-Free API Tests (PR1)

**1. Default stub rate** - Make the shared stub exchange-rate provider, with a fixed rate, the API test factory default; no new provider class.

**2. Factory default and opt-in** - Make the API test factory register that provider by default, keep explicit overrides winning, and add the named opt-in for the real exchange-rate chain.

**3. Factory guard tests** - Add the tests that prove the default, the opt-in and the precedence, then run the whole API test project with outbound network disabled and record the result.

### Stage 2: Bounded Frankfurter Client (PR2)

**4. Failure policy and time budget** - Give each Frankfurter lookup an overall time budget, stop on transport failures, and keep stepping back to earlier dates only when the service answers cleanly without a rate.

**5. Current service address** - Point the provider at the current Frankfurter host and update the testing-guide references to the old address.

**6. Provider tests** - Add the new cases from the spec and rewrite the existing tests whose expectations depended on the old fallback behaviour.

### Stage 3: Pinned Test Environment (PR3)

**7. CI environment pin** - Set the timezone and language variables on the four jobs, and add the Windows timezone and culture step to the two Windows jobs.

**8. Vitest pin** - Add the global setup that sets timezone and language before the test workers start, wire it into the vitest configuration, and exclude it from coverage.

**9. Pin checks** - Add the web test and the CI-only .NET test that fail when the environment is not the pinned one.
