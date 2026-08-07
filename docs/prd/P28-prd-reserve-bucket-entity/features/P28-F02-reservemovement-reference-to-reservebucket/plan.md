# Implementation Plan: ReserveMovement Reference to ReserveBucket

**Prerequisites:**
- F01 (ReserveBucket entity + seed migration) merged
- Builds on the existing `Bank`/`IncomeSource` reference-converter pattern (`ReferenceConverter<T>`, `ReferenceResolutionContext`) and the existing `EntityReferenceMigrator` raw-JSON-rewrite pattern

### Stage 1: Domain Entity Reference Type

**1. ReserveMovement Retyping** - Change `ReserveMovement.Bucket`, `Create`, and `Update` from the enum to the `ReserveBucket` entity, and remove the now-unneeded F01 enum-alias workaround from this file.

### Stage 2: Persistence Wiring

**2. Reference Converter and Resolution Context** - Add `ReserveBucketReferenceConverter` and extend `ReferenceResolutionContext` with a `ReserveBuckets` lookup, following the existing `Bank`/`IncomeSource` converter pattern exactly.

**3. Type Resolver and Data Converter** - Register `ReserveMovement.Bucket`'s wire name and converter in `CashFlowTypeInfoResolver`, and move `ReserveBuckets` from a plain top-level collection into the early resolution block in `CashFlowDataConverter` so `ReserveMovements` can resolve against it during the same read.

### Stage 3: One-Time Data File Migration

**4. Raw-JSON Rewrite Migrator** - Add a dedicated migrator that detects a data file still carrying `ReserveMovement.Bucket` as a name string, bootstraps `ReserveBuckets` if the file doesn't have them yet, rewrites every movement to the `BucketId` reference shape, and reports a summary — mirroring the existing `EntityReferenceMigrator`'s detect/backup/rewrite/save structure.

**5. Import Tool Wiring** - Wire the new migrator into `Program.cs` at the same early, pre-typed-load stage as the existing `EntityReferenceMigrator` call.

### Stage 4: Application-Layer Consumers

**6. Name Resolver** - Add `ReserveBucketNameResolver` and delete `ReserveBucketParser` and its test.

**7. ReserveService Adaptation** - Update `PostWithdrawalAsync`/`UpdateMovementAsync` to resolve bucket entities via the new resolver, update `PostIncomeSplitAsync`/`GetBucketBalances` to resolve the same 4 canonical buckets by name so they keep compiling and behaving identically, and fix `ToDto` to read the bucket's name correctly from the entity.

### Stage 5: Spreadsheet Importer Adaptation

**8. ReservasSheetImporter Signature Change** - Add a buckets parameter to `Import` and resolve each column's expected bucket name against it, then update the `Program.cs` call site to pass the loaded bucket collection.

### Stage 6: Tests

**9. New Component Tests** - Add tests for `ReserveBucketReferenceConverter`, `ReserveBucketNameResolver`, and `ReserveBucketReferenceMigrator`, following the existing `BankReferenceConverterTests`/`BankNameResolverTests`/`EntityReferenceMigratorTests` conventions.

**10. Update Touched Tests** - Update `ReserveMovementTests`, `ReserveServiceTests`, `CashFlowDataTests`, `CashFlowJsonRepositoryTests`, `CashFlowSerializerAdapterTests`, and `ReservasSheetImporterTests` for the new entity-reference type and signature changes.
