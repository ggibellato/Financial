# Implementation Plan: F01. Payments Due Aggregation (Backend)

**Prerequisites:**
- .NET 8 SDK, existing `Financial.CashFlow.Application`/`Financial.Api` projects building cleanly
- No new NuGet packages or environment variables required

### Stage 1: Application Layer

**1. Payment Due DTO** - Add the flat response record for one aggregated payment, following the existing CashFlow DTO conventions.

**2. Payments Due Service and Interface** - Add the service that queries Mensais bills and credit cards independently, computes the host-local "today", filters and clamps due dates into the notification window, calculates days remaining, sorts the combined list, and applies fail-safe error handling per source. Reference the spec for the exact filtering, clamping, sorting, and error-handling rules.

**3. Dependency Injection Registration** - Register the new service in the CashFlow Application DI extension alongside the other CashFlow services.

### Stage 2: Test Infrastructure and Application Tests

**4. Shared Stub Read-Failure Hooks** - Extend the shared CashFlow repository test stub with the ability to simulate a Mensais or credit card read failure, mirroring its existing write-failure hook.

**5. Payments Due Service Tests** - Write the Application-layer test suite covering the full aggregation, filtering, clamping, sorting, and fail-safe behavior described in the spec's testing strategy, including boundary-date and injected-time-provider cases.

### Stage 3: Presentation Layer and Contract

**6. Payments Due Controller** - Add the GET endpoint exposing the aggregated list at the route the PRD specifies, delegating to the new service.

**7. Api Endpoint Tests** - Write the integration test suite verifying the endpoint responds correctly, including the empty-array case.

**8. OpenAPI Snapshot and Generated Frontend Types** - Regenerate the OpenAPI contract snapshot to include the new endpoint, then regenerate the frontend's generated API types from that snapshot so the frontend freshness check stays green.
