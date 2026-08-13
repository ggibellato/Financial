# Implementation Plan: F02. Transient-Failure Retry Helper

**Prerequisites:**
- `Google.Apis.Core` NuGet package, version `1.64.0` (already used elsewhere in the solution)

### Stage 1: Retry Helper

**1. Package Reference** - Add the `Google.Apis.Core` package reference to `Financial.Shared.Infrastructure` and to `Tests/Financial.Shared.Infrastructure.Tests`, matching the version already pinned in `Integrations/GoogleFinancialSupport`.

**2. TransientRetryPolicy** - Add the async-only retry helper to a new `Resilience/` folder in `Financial.Shared.Infrastructure`, per the spec's exception classification (429, Drive 5xx, network/timeout exceptions), backoff shape, and final-failure surfacing behavior. Leave `GoogleRetryPolicy` and its callers untouched.

### Stage 2: Tests

**3. Retry Behavior Tests** - Add `TransientRetryPolicyTests` covering each retryable exception category, immediate-success passthrough, non-retryable passthrough, and 5-attempt exhaustion surfacing the original exception, per the spec's testing strategy.

**4. Regression Check** - Re-run the existing `GoogleRetryPolicyTests` suite unmodified to confirm `GoogleRetryPolicy` and its callers are unaffected.
