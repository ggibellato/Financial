# Implementation Plan: IncomeSource Domain Entity and Seed Migration

**Prerequisites:**
- .NET solution builds today (`Financial.slnx`), xUnit + FluentAssertions test stack already in place
- No new NuGet packages required

### Stage 1: Domain Layer

**1. IncomeSource Entity** - Add the new `IncomeSource` domain entity (`Id`, `Name`, `IsActive`, `Group`) with a private constructor and a static `Create` factory, immutable after creation. Remove the old `Domain.Enums.IncomeSource` enum it replaces.

**2. Income Entity Rework** - Change `Income.IncomeSource` from the enum to a plain string, remove the `Group` computed property and its `IncomeClassifier` dependency, and update `Create`/`UpdateDetails` accordingly. Delete `IncomeClassifier` and its dedicated unit tests.

**3. CashFlowData Aggregate** - Add an `IncomeSources` read-only collection and an `AddIncomeSource` method to `CashFlowData`, mirroring the existing `Banks`/`AddBank` shape (no removal method).

### Stage 2: Application and Infrastructure Wiring

**4. Repository Contract and Implementation** - Add `GetIncomeSources()` to `ICashFlowRepository` and implement it in the sole repository class, matching the read-only, no-CRUD shape of `GetBanks()`.

**5. JSON Serialization Registration** - Register the new `IncomeSource` entity type in `CashFlowTypeInfoResolver`'s managed-types list so its private setters (de)serialize correctly, matching `Bank`'s registration.

**6. Income Service Compile Fix** - Update `IncomeService`'s field validation to work with a plain source string instead of the deleted enum parser, keeping only a not-null/not-blank check (full seeded-list validation is a later feature). Delete the now-unused `IncomeSourceParser`.

**7. Annual Summary Group Lookup** - Update `AnnualSummaryService` to resolve each income's group through a lookup built from the seeded `IncomeSource` list instead of the removed `Income.Group` property, preserving today's Salary/DividendoJuros/NonReportable computation.

### Stage 3: Seed Migration Tool

**8. IncomeSource Migrator** - Add an idempotent migrator that seeds the four existing income sources with their correct group and active status, skipping any name that already exists, and auditing (without failing) any existing income record whose source name doesn't match a seeded source.

**9. Migration Tool Wiring** - Run the new migrator as part of the existing unconditional migration sequence in the spreadsheet-import tool's entry point, after the data-file backup and before the final save, printing its summary alongside the other migrators' output. Ensure income sources carry over correctly in the tool's full-rebuild path, since they are not owned by the spreadsheet.
