# Implementation Plan: F01. IncomeSource Auto-Split Eligibility Flag

**Prerequisites:**
- .NET SDK / existing `Financial.slnx` build toolchain
- Node/npm for `Financial.Web`
- No new libraries, no new environment variables, no data migration tool for existing records

### Stage 1: Domain and Application

**1. IncomeSource Entity** - Add the `AutoSplitToReserve` flag to the `IncomeSource` domain entity, defaulting to `false` both when explicitly created and when an existing record is deserialized without the field.

**2. IncomeSource DTO and Service** - Expose the new flag on the read DTO and update `IncomeSourceService` to map it from the domain entity into the response.

**3. Migration Tool Seed Data** - Update the import tool's seed data so a freshly seeded "Ariana" income source is created with the flag enabled and every other freshly seeded source keeps it disabled, without altering any source that already exists in the data file.

### Stage 2: API Contract

**4. OpenAPI Contract Regeneration** - Regenerate the pinned OpenAPI snapshot and the frontend's generated API types so the new field is reflected in the public contract and the web client's types, keeping the codebase deployable with the changed DTO shape.

### Stage 3: Testing

**5. Domain and Migrator Tests** - Add coverage for the flag's default and explicit values on the entity, and for the migrator's new per-source seed values, including confirming that a source already present in the data file is left untouched rather than corrected.

**6. Application and API Tests** - Extend the income source service and endpoint tests to cover the new field's presence and correctness in the response, and confirm the generated frontend types stay in sync with the regenerated contract.
