# Implementation Plan: Bank Transfer Domain & API

**Prerequisites:**
- .NET 10 SDK (existing solution target)
- No new external dependencies or environment variables

### Stage 1: Domain

**1. Transfer Entity** - Add the `Transfer` entity (date, source bank, destination bank, amount, optional note), validating its own value-shape invariants (source and destination must differ, amount must be strictly positive) on creation and update, following the existing entity patterns in this domain.

**2. CashFlowData Transfer Collection** - Extend `CashFlowData` with a `Transfers` collection and add/update/remove operations, following the same private-list-plus-readonly-property pattern used for every other collection on that aggregate, with update implemented as a find-by-id replace.

### Stage 2: Application

**3. Repository Contract** - Extend the repository interface with transfer query/add/update/delete operations.

**4. Transfer DTOs** - Add the read, create, and update data transfer objects for a transfer entry, matching the field set and nullability established in the domain entity.

**5. Transfer Service** - Implement the create/update/delete/get-by-month/get-by-bank workflow: resolving both bank names against the live bank list, delegating same-bank and amount validation to the entity, and mapping to the read DTO. Register the service for dependency injection.

### Stage 3: Infrastructure and Presentation

**6. Repository and Serializer Wiring** - Implement the new repository operations against the JSON-backed data store, and register the `Transfer` entity with the serializer's private-member wiring so it persists and reloads correctly.

**7. Transfers API Endpoints** - Add the HTTP endpoints for creating, updating, deleting, and listing transfers by month and by bank, following the existing controller's routing, status code, and error response conventions.
