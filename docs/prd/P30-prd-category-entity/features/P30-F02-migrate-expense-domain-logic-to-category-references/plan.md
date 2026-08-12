# Implementation Plan: Migrate Expense & Domain Logic to Category References

**Prerequisites:**
- F01 (Category domain entity, seed migration, JSON persistence) merged to `main`
- No new tools or packages

### Stage 1: Domain Entity Reference

**1. Expense Category Reference** - Change `Expense.Category`'s type from the legacy enum to a non-nullable reference to the `Category` entity, updating every constructor/update signature that carries the old type, and derive `IsInvestment` directly from the referenced category's flag instead of the deleted classifier.

**2. Category Classifier Removal** - Delete the standalone classifier rule and its test, now fully superseded by a flag on the entity itself.

### Stage 2: Persistence Wiring

**3. Category Reference Converter** - Add a reference converter for `Category` following the established pattern for Bank/CreditCard, and register it for the new reference property under the shared `CategoryId` wire name.

**4. Resolution Context and Deserialization Ordering** - Extend the shared per-read lookup context with a `Category` table, and move category resolution into the early cross-referenced pass so every expense reference points at the same in-memory instance as the seeded collection.

### Stage 3: One-Time Data File Migration

**5. Category Reference Migrator** - Add a dedicated one-time raw-JSON migrator that detects the legacy `Category` string shape, bootstraps the seeded categories if the file predates F01, resolves every legacy value by name, and rewrites the file — aborting with a clear error if any value has no match. Wire it into the import tool's startup sequence alongside the existing reference migrators.

**6. Legacy Migrator Compatibility** - Fix the older, pre-F01-era reference migrator's existing category handling so it resolves against the seeded collection by name (bootstrapping it if needed) instead of parsing the removed enum-compatible shape, flagging any unresolved value for manual review rather than failing outright, consistent with how that migrator already handles every other legacy reference.

### Stage 4: Application-Layer Consumers

**7. Category Resolution Utility** - Add an Id-based category resolver mirroring the existing Bank/CreditCard resolver pattern.

**8. Expense Service Validation** - Update expense creation and update to resolve the incoming category reference through the new resolver, rejecting an unknown or inactive category with the PRD's specified error messages, and update the read-model mapping and monthly category-total grouping to use the category's name.

**9. Tithe Calculation** - Replace the hardcoded category comparison in the tithe summary calculation with the referenced category's tithe classification flag.

**10. Annual Summary Reporting** - Replace every enumeration of the legacy category type with a query over the seeded category list (including inactive categories, so historical totals remain complete), replace the hardcoded investment-category lookup with the entity's investment classification flag, and preserve today's category display ordering using the seeded list's own order.

**11. DTO Contract Updates** - Replace the string-based category field on the expense request/response models with the Id-based contract, adding a display-name field to the read model.

### Stage 5: Spreadsheet Importer Adaptation

**12. Importer Compatibility** - Adapt the monthly expense sheet importer to build and pass against the new entity-reference type, resolving its already-computed category value to the matching seeded entity by name, without reworking the importer's underlying label-resolution mechanism (left for a later feature).

### Stage 6: Tests

**13. Domain and Persistence Tests** - Update existing entity, serialization, and type-resolver tests to construct and assert against `Category` entity references instead of the legacy enum, and remove the deleted classifier's tests.

**14. Application Service Tests** - Add coverage for active/inactive/unknown category validation on expense creation and update, the reworked annual summary reporting (including inactive-category history and the investment-flag lookup), and the tithe calculation's flag-based comparison; replace enum-based fixtures across affected service tests with entity instances.

**15. Migration and Importer Tests** - Add coverage for the new reference migrator's no-op, rewrite, bootstrap, and abort-on-unresolved-name behaviors; update the older legacy migrator's existing tests for its fixed category handling; update existing importer tests for the new type.
