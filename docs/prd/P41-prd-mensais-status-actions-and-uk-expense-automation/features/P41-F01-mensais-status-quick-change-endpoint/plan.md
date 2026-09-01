# Implementation Plan: F01. Mensais Status Quick-Change Endpoint

**Prerequisites:**
- .NET 10 SDK, existing solution builds and existing test suites pass on `main`
- No new packages or environment variables required

### Stage 1: Domain and Application Layer

**1. Recurring Bill Status Mutation** - Add a narrow domain method to `RecurringBill` that updates only its status, following the existing `ResetToUnset()` precedent, and cover it with domain-level tests confirming every other field stays untouched.

**2. Mensais Service Status Update** - Extend the Mensais application service and its interface with a status-only update operation: validate the requested status, look up the bill, apply the domain mutation, and persist the change, following the same validation, telemetry, and error-handling pattern already used by the service's existing methods. Cover it with service-level tests for the success path, an unknown bill id, an invalid status value, and field isolation.

### Stage 2: API Endpoint and Contract

**3. Status Endpoint** - Add a new controller action exposing the status-only update as a POST sub-resource action on the Mensais controller, matching the existing mark-paid/unmark-paid route style, relying on the existing exception-mapping middleware for error responses. Cover it with API integration tests for the success path, a missing bill, an invalid status value, and field isolation over a real HTTP round trip.

**4. Contract Regeneration** - Regenerate the OpenAPI snapshot and the generated frontend TypeScript types to reflect the new endpoint, per the project's documented contract-change workflow, and confirm the existing contract and freshness tests pass against the regenerated files.
