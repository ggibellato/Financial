# Implementation Plan: Income Entity and Migration

**Prerequisites:**
- .NET 10 SDK (existing solution target)
- No new external dependencies or environment variables

### Stage 1: Domain

**1. Income Entity and IncomeSource Enum** - Add the `Income` entity (date, income source, optional gross value, net value, destination bank) and the new `IncomeSource` enum, with the entity validating its own value-shape invariant (gross not below net, net not negative) on creation and update, following the existing entity patterns in this domain.

**2. CashFlowData Income Collection** - Extend `CashFlowData` with an `Incomes` collection and add/remove operations, following the same private-list-plus-readonly-property pattern used for every other collection on that aggregate.

### Stage 2: Application

**3. Repository Contract and Income Source Parser** - Extend the repository interface with income query/add/delete operations, and add a parser that resolves an income source string against the new enum, mirroring the existing category parser.

**4. Income DTOs** - Add the read, create, and update data transfer objects for an income entry, matching the field set and nullability established in the domain entity.

**5. Income Service** - Implement the create/update/delete/get-by-month workflow: validating the income source, resolving the destination bank against the live bank list, delegating value-shape validation to the entity, and mapping to the read DTO. Register the service for dependency injection.

### Stage 3: Infrastructure and Presentation

**6. Repository and Serializer Wiring** - Implement the new repository operations against the JSON-backed data store, and register the `Income` entity with the serializer's private-member wiring so it persists and reloads correctly.

**7. Incomes API Endpoints** - Add the HTTP endpoints for creating, updating, deleting, and listing income entries by month, following the existing controller's routing, status code, and error response conventions.

### Stage 4: Migration Tool

**8. Income Migration Console Project** - Create a new console project that takes a timestamped backup of the data file before writing, loads the data, confirms the income collection is present, and prints a run summary — following the exact structure of the existing bank migration tool.

**9. Solution Registration** - Register the new console project and its test project in the solution file.
