# Implementation Plan: F01. Bank Entity & Payment-Source Migration

**Prerequisites:**
- None (Wave 1, no feature dependencies)
- .NET SDK; no new packages

### Stage 1: Domain Model

**1. `Bank` entity** - Create the new entity carrying a name and a round-up flag, immutable after creation, following this codebase's existing entity-with-private-setters-and-factory pattern. See spec Section 4.

**2. `Expense` bank reference retype** - Change the entity's bank field from the fixed enum to a plain validated string, updating every method that reads or writes it (creation, editing, settling, unsettling) while preserving the existing payment-shape invariant exactly. See spec Sections 3 and 4.

**3. `CashFlowData` bank collection** - Add a `Banks` collection to the aggregate root following the same private-list/read-only-property/`Add*` pattern already used for every other collection it holds. See spec Section 4.

**4. Remove the old enum** - Delete the `PaymentSource` enum now that nothing references it. See spec Section 4.

### Stage 2: Application and Infrastructure Wiring

**5. Bank name resolution** - Replace the old enum parser with a resolver that looks up a bank by name (case-insensitive) against the live, repository-provided bank list. See spec Section 3.

**6. Repository contract and implementation** - Expose read access to the bank list through the repository interface and its JSON-backed implementation. See spec Section 4.

**7. Expense and settlement services** - Update expense creation/editing validation and credit-card statement settlement to resolve and validate bank names through the new resolver instead of the old enum parser, preserving every existing validation and rollback behavior. See spec Sections 3 and 4.

**8. Serializer wiring** - Register the new entity with the JSON serializer's private-member wiring so it deserializes correctly. See spec Section 4.

### Stage 3: Migration Tool

**9. Console project scaffold** - Create the migration console project under `Integrations/`, mirroring the P12 legacy-data-migration tool's project shape, and register it plus its test project in the solution file. See spec Section 4.

**10. Seed-and-audit migrator** - Implement the pure migration logic: idempotently seed the 3 known banks with their correct round-up flags, then audit every expense's existing bank tag against the seeded banks, producing a run summary that flags anything unresolved for manual review. See spec Sections 3 and 6.

**11. Backup helper and program flow** - Implement the timestamped backup helper and the entry point: resolve the data path, back up, load, migrate, save, and print the summary. See spec Sections 3 and 4.

### Stage 4: Historical Import Compatibility

**12. Spreadsheet importer update** - Update the historical importer's bank-tag resolution to emit the new string shape instead of the old enum, preserving its existing tag-to-bank mapping. See spec Section 4.

### Stage 5: Verification

**13. Full-solution verification** - Run the complete .NET test suite, exercise the migration tool end-to-end against a copy of the live data file, and confirm card statement settlement and expense creation/editing still behave exactly as before against the new bank-backed data. See spec Section 7.
