# Implementation Plan: Category Domain Entity & Seed Migration

**Prerequisites:**
- None — this is the first feature of P30, with no dependency on other in-progress work
- No new tools or packages

### Stage 1: Domain Entity

**1. Category Entity** - Add the `Category` domain entity with an identifier, a name, an active flag, and the two classification flags, constructed only through a validating factory with no update methods, and register it on the `CashFlowData` aggregate as a new collection.

### Stage 2: Persistence Wiring

**2. JSON Serialization Registration** - Register the new entity with the serialization metadata resolver so its private constructor and setters can be used by the reflection-based (de)serializer, matching how every other seeded entity in this codebase is wired.

**3. Top-Level Collection Read/Write** - Add the new collection to the top-level data file reader and writer under its own JSON property, without moving it into the early cross-referenced resolution pass, since nothing references it yet.

**4. Repository Access** - Expose a read method for the new collection through the repository interface and its JSON-backed implementation.

### Stage 3: Seed Migration

**5. Category Seed Migrator** - Add an idempotent migration tool that seeds the existing set of category names into the collection, marking exactly one as the investment classification and exactly one as the tithe classification, and skipping any name already present.

**6. Migration Tool Wiring** - Invoke the new seed migrator from the spreadsheet import tool's existing idempotent migration sequence, and surface its outcome in the tool's console summary output.

### Stage 4: Tests

**7. Domain and Persistence Tests** - Add unit coverage for the entity's construction and validation, its aggregate registration, its round-trip through the JSON serializer, and the repository's new read method.

**8. Migration Tests** - Add unit coverage for the seed migrator's full-seed, no-duplicate-on-rerun, and partial-seed behaviors, and for its classification-flag assignment.
