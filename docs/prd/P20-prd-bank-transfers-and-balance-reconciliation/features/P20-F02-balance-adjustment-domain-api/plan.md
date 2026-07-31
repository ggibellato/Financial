# Implementation Plan: Balance Adjustment Domain & API

**Prerequisites:**
- .NET 10 SDK (existing solution target)
- No new external dependencies or environment variables

### Stage 1: Domain

**1. BalanceAdjustment Entity** - Add the `BalanceAdjustment` entity (date, bank, target balance, computed delta, optional note), validating the target-balance-not-negative invariant on creation and update, following the existing entity patterns in this domain.

**2. CashFlowData Balance Adjustment Collection** - Extend `CashFlowData` with a `BalanceAdjustments` collection and add/update/remove operations, following the same private-list-plus-readonly-property pattern used for every other collection on that aggregate, with update implemented as a find-by-id replace.

### Stage 2: Application

**3. Repository Contract** - Extend the repository interface with balance adjustment query/add/update/delete operations.

**4. Balance Adjustment DTOs** - Add the read, create, and update data transfer objects for a balance adjustment, matching the field set established in the domain entity.

**5. Balance Adjustment Service** - Implement the create/update/delete/get-by-bank workflow: resolving the bank name against the live bank list, computing the delta from an as-of-date balance calculation that also accounts for any other adjustments already recorded for that bank, delegating the target-balance invariant to the entity, and mapping to the read DTO. Register the service for dependency injection.

### Stage 3: Infrastructure and Presentation

**6. Repository and Serializer Wiring** - Implement the new repository operations against the JSON-backed data store, and register the `BalanceAdjustment` entity with the serializer's private-member wiring so it persists and reloads correctly.

**7. Balance Adjustment Endpoints on BanksController** - Add the HTTP endpoints for creating, updating, deleting, and listing a bank's balance adjustments, folded into the existing banks controller and following its routing, status code, and error response conventions.
