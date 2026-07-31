# Implementation Plan: F01. Investment Account Registry

**Prerequisites:**
- .NET 10 SDK (existing solution target)
- No new NuGet packages, environment variables, or configuration files required
- Branch `feat/P18-F01-investment-account-registry`, already created from `main`

### Stage 1: Domain Layer

**1. Investment Account Entity** - Create the `InvestmentAccount` entity to replace the retired enum, holding an identity, canonical name, active flag, and liability flag, following the existing entity conventions (private setters, static factory, validation) used by `Bank`.

**2. Investment Snapshot Account Reference** - Change `InvestmentSnapshot.Account` from the enum type to a plain string reference matching an account's canonical name, updating its factory and update methods accordingly while leaving every other field untouched.

**3. CashFlow Data Aggregate** - Add an `InvestmentAccounts` collection to `CashFlowData` with an add method, mirroring the existing collection/add-method pairs already present for the other aggregate members.

**4. Retire Superseded Domain Code** - Remove the enum and the static liability-classification rule now that both responsibilities live on the new entity.

### Stage 2: Infrastructure Layer

**5. Repository Contract and Implementation** - Extend the CashFlow repository interface and its JSON-backed implementation with read/add operations for the new account collection, matching the shape of the existing read/add operations for incomes.

**6. JSON Serialization Wiring** - Register the new entity type with the JSON type-info resolver so its private constructor and properties serialize/deserialize the same way every other managed entity already does.

### Stage 3: Import Pipeline Migration

**7. Investment Account Migrator** - Add an idempotent migrator that seeds the registry with the eleven currently-active accounts and their liability flags, and audits existing snapshot account references against the seeded set, following the two-phase seed-then-audit shape already established by the bank migrator.

**8. Migration Summary** - Add a summary type reporting seeded/already-present account counts and any unresolved snapshot references, rendered in the same style as the existing migration summaries.

**9. Pipeline Registration** - Wire the new migrator into the spreadsheet import's always-run migration block alongside the existing bank, income, and payment-state migrators, and print its summary in the run output.

**10. Resumo Sheet Importer Compile Fix** - Update the Resumo sheet account-label alias lookup to key off the account's name string instead of the retired enum, preserving the exact same alias values and matching behavior; keep the liability determination for import sign-inversion sourced from the same eleven known accounts, without wiring it to the new registry yet.

### Stage 4: Application Layer

**11. Investment Snapshot Service** - Replace the enum-based account enumeration with a query against the repository's account collection, and source each snapshot's liability flag from the matching account entity instead of the retired static rule.

**12. Annual Summary Service** - Apply the same enumeration and liability-flag sourcing change to the annual investment diff computation, preserving its existing output shape and values.

**13. Remove Unused Validation Helper** - Delete the account-name parser that referenced the retired enum, since it has no production call sites.

### Stage 5: Test Suite Alignment

**14. Domain Test Updates** - Add coverage for the new entity's creation and validation, update the snapshot entity's existing tests for the new account type, extend the aggregate's tests for the new collection, and remove the tests for the deleted classification rule.

**15. Infrastructure Test Updates** - Extend the repository and JSON serializer test suites to cover the new account collection and its round-trip through serialization.

**16. Import Pipeline Test Updates** - Add a migrator test suite mirroring the existing bank migrator's coverage (first run, idempotent re-run, partial seed, unresolved snapshot audit), and update the Resumo sheet importer's existing tests for the new string-based account type.

**17. Application Test Updates** - Update the snapshot service and annual summary service test suites to seed accounts through the repository rather than relying on the enum, and remove the tests for the deleted validation helper.

**18. Full Suite and Manual Import Verification** - Run the complete test suite to confirm nothing else references the retired enum or classification rule, then run the spreadsheet import command against a copy of the existing data file to confirm the migration seeds all eleven accounts on first run, is a no-op on a second run, and leaves every pre-existing snapshot value unchanged.
