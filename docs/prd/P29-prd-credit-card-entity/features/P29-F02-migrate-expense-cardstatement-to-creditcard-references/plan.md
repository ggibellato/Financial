# Implementation Plan: Migrate Expense & CardStatement to CreditCard References

**Prerequisites:**
- F01 (CreditCard domain entity, seed migration, JSON persistence) merged to `main`
- No new tools or packages

### Stage 1: Domain Entity Reference

**1. Expense Card Reference** - Rename `Expense.CardTag` to `Expense.CreditCard` and change its type from the legacy enum to a nullable reference to the `CreditCard` entity, updating every constructor/update/validation signature that carries the old type.

**2. CardStatement Card Reference** - Rename `CardStatement.Card` to `CardStatement.CreditCard` and change its type from the legacy enum to a non-nullable reference to the `CreditCard` entity, updating its factory signature.

### Stage 2: Persistence Wiring

**3. CreditCard Reference Converter** - Add a reference converter for `CreditCard` following the established pattern for Bank/IncomeSource/ReserveBucket, and register it for both new reference properties under the shared `CreditCardId` wire name.

**4. Resolution Context and Deserialization Ordering** - Extend the shared per-read lookup context with a `CreditCard` table, and ensure credit cards are resolved before expenses and card statements are read so every reference points at the same in-memory instance as the seeded collection.

### Stage 3: One-Time Data File Migration

**5. CreditCard Reference Migrator** - Add a dedicated one-time raw-JSON migrator that detects the legacy `CardTag`/`Card` string shape, bootstraps the seeded cards if the file predates F01, resolves every legacy value by name, and rewrites the file — aborting with a clear error if any value has no match. Wire it into the import tool's startup sequence alongside the existing reference migrators.

### Stage 4: Application-Layer Consumers

**6. Card Resolution Utility** - Replace the string-based card parser with an Id-based resolver mirroring the existing Bank/IncomeSource resolver pattern, and remove the superseded parser.

**7. Expense Service Validation** - Update expense creation and update to resolve the incoming card reference through the new resolver, rejecting an unknown or inactive card with the PRD's specified error messages, and update the read-model mapping to expose the card's Id and name.

**8. Card Statement Service Generation** - Change monthly statement auto-generation to query only active cards at generation time instead of iterating every legacy enum value, update expense-to-statement matching to compare by the card's Id, and surface a warning when a period has no active cards to generate against.

**9. DTO Contract Updates** - Replace the string-based card fields on the expense and card-statement request/response models with the Id-based contract, adding a display-name field to the read models.

### Stage 5: Spreadsheet Importer Adaptation

**10. Importer and Charge-Date Migrator Compatibility** - Adapt the monthly expense sheet importer and the charge-date backfill migrator to build and pass against the new entity-reference type, without reworking the importer's row-position resolution mechanism (left for a later feature).

### Stage 6: Tests

**11. Domain and Persistence Tests** - Update existing entity, serialization, and type-resolver tests to construct and assert against `CreditCard` entity references instead of the legacy enum.

**12. Application Service Tests** - Add coverage for active/inactive/unknown card validation on expense creation and update, active-cards-only statement generation, and the zero-active-cards warning path; replace the deleted parser's tests with resolver tests.

**13. Migration and Importer Tests** - Add coverage for the new reference migrator's no-op, rewrite, bootstrap, and abort-on-unresolved-name behaviors, and update existing importer/charge-date-migrator tests for the new type.
