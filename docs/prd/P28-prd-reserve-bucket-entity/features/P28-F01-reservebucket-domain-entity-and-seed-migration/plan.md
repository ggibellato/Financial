# Implementation Plan: ReserveBucket Domain Entity and Seed Migration

**Prerequisites:**
- No new libraries or environment variables
- Builds on the existing `Bank`/`IncomeSource` entity pattern and `BankMigrator`/`IncomeSourceMigrator` migration pattern already in the codebase

### Stage 1: Domain and Persistence Plumbing

**1. ReserveBucket Entity** - Add the `ReserveBucket` domain entity with its four fields and a validating static factory, following the existing `Bank`/`IncomeSource` entity shape (private setters, no public mutators).

**2. Aggregate, Repository, and JSON Wiring** - Extend `CashFlowData` with the new collection and add method, add the read-only accessor to `ICashFlowRepository` and its `CashFlowJsonRepository` passthrough, register the entity for private-setter JSON binding, and wire the new collection into the top-level (de)serializer as a plain named collection.

### Stage 2: Namespace-Collision Mitigation

**3. Qualify Existing Enum References** - Across the production and test files that currently reference the `ReserveBucket` enum unqualified, introduce a local alias to the enum's fully-qualified name and switch those references to it, so the new entity type can coexist with the still-in-use enum without ambiguous-reference errors. No behavior changes.

### Stage 3: Seed Migration

**4. ReserveBucketMigrator and Summary** - Add the idempotent seed migrator (four historical buckets with their historical percentages) and its outcome-reporting summary, following the `BankMigrator`/`IncomeSourceMigrator` shape: seed-if-missing by case-insensitive name match, audit existing reserve movements against the seeded set, and compute the active-buckets split-percentage sum for the non-blocking warning.

**5. Import Tool Orchestration** - Wire the new migrator into `Program.cs`'s existing sequence (seed before the workbook import, re-run after for the final audit) and add the new collection to the routine that carries over data the spreadsheet doesn't own during a full rebuild.

### Stage 4: Tests

**6. Entity and Migrator Tests** - Add unit tests for `ReserveBucket.Create`'s validation paths and for `ReserveBucketMigrator`'s seeding, idempotency, audit, and warning-threshold behavior, following the existing `BankTests`/`IncomeSourceMigratorTests` conventions.

**7. Update Touched Tests** - Update the existing test files affected by the namespace-collision mitigation so the suite continues to compile and pass, and extend `CashFlowDataTests`, `CashFlowJsonRepositoryTests`, and `CashFlowSerializerAdapterTests` to cover the new `ReserveBuckets` collection.
