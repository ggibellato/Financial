# Implementation Plan: F05. Web API Id-Based Endpoints and Routes

**Prerequisites:**
- .NET SDK matching the existing `Financial.CashFlow.*`/`Financial.Api`/`Financial.App` projects
- No new NuGet packages or environment variables
- F04 merged (Application-layer services and DTOs already Guid-based)

### Stage 1: Investment Accounts Endpoint

**1. Read Model and Service** - Add `InvestmentAccountDTO` (Id, Name, IsActive, IsLiability) and `IInvestmentAccountService`/`InvestmentAccountService`, mirroring `IncomeSourceDTO`/`IncomeSourceService` exactly, then register the new service in `CashFlowApplicationServiceCollectionExtensions`.

**2. Controller** - Add `InvestmentAccountsController` with a single `GET /investment-accounts` action returning the full seeded list, mirroring `IncomeSourcesController`.

### Stage 2: Bank-Scoped Route Cutover

**3. BanksController** - Change `UpdateOpeningBalance`, `AddAdjustment`, `UpdateAdjustment`, `DeleteAdjustment`, and `GetAdjustmentsByBank`'s route parameter from `{name}` to `{id:guid}`, passing the route Guid directly into the already-Guid-based service calls. Write actions (opening-balance update, adjustment create/update/delete) return 404 when the Id doesn't match a seeded Bank; `GetAdjustmentsByBank` keeps returning an empty list for an unmatched Id, unchanged from today. Remove the now-unnecessary name-resolution helpers F04 added.

**4. TransfersController** - Change `GetTransfersByBank`'s route parameter from `{name}` to `{id:guid}`, calling `ITransferService.GetTransfersByBank` with the route Guid directly and dropping the controller's now-unnecessary `IBankService` dependency and its name lookup.

### Stage 3: Test Coverage

**5. New Endpoint and Route Tests** - Add unit tests for `InvestmentAccountService` and integration tests for `GET /investment-accounts`; update the existing Banks/BalanceAdjustments/Transfers integration tests to exercise the new Guid routes, covering both a valid seeded Id (same records as the pre-change name-based route) and an unresolvable Id (404 for write routes, empty list for the two list routes).

**6. Guard-Clause and Cross-Endpoint Confirmation** - Confirm `ControllerGuardClauseTests`' stub services still satisfy the (already Guid-based since F04) service interfaces, and spot-check that the income/expense/transfer create/update endpoints already reject a name string in a Guid field with 400 via normal model binding, with no new validation code required.
