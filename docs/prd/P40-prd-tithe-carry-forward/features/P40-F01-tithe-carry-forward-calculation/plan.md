# Implementation Plan: F01 Tithe Carry-Forward Calculation

**Prerequisites:**
- None — extends the existing CashFlow Tithe feature in a mature codebase.

### Stage 1: Domain Entity and Persistence

**1. TitheCarryForward Entity** - Add a new CashFlow domain entity representing a single month's carry-forward decision, following the existing entity pattern (private setters, static factory, manual validation). Reference the spec for its exact fields and validation rules.

**2. CashFlowData Aggregate Changes** - Extend the aggregate root with the new collection and the one-time effective-from anchor, following the same shape as the most recent similar collection addition in this codebase.

**3. Repository Layer** - Extend the repository contract and its JSON-backed implementation with reads/writes for the new collection and the effective-from value, mirroring the existing pass-through pattern used by other collections.

**4. JSON Serialization** - Extend the document (de)serializer so the new collection and effective-from value round-trip correctly, and remain backward-compatible with existing data files that don't yet have them. Update the example data file to match.

### Stage 2: Application Service Logic

**5. Tithe Summary DTOs** - Add the nested carry-forward read/update DTOs and extend the existing Tithe summary DTO, following the spec's response shape.

**6. Carry-Forward Exception** - Add the business-rule exception for toggling a month with nothing available to carry, wired into the same centralized exception-to-status-code mapping already used elsewhere in the API.

**7. Tithe Service Resolution Algorithm** - Implement the lazy resolve-and-snapshot algorithm inside the Tithe service: the effective-from anchor, the cascading walk-back through unresolved months, the snapshot-immutability guarantee, and the toggle operation. Reference the spec's step-by-step algorithm description.

### Stage 3: API Layer

**8. Tithe Controller Updates** - Adapt the existing Tithe summary endpoint to the new asynchronous service signature and response shape, and add the new toggle endpoint, following this codebase's existing controller conventions for validation and status codes.

**9. API Contract Regeneration** - Regenerate the OpenAPI snapshot and the generated frontend types to reflect the extended response shape and the new endpoint, per this project's existing contract-regeneration workflow.

### Stage 4: Existing Caller Compatibility

**10. WPF Caller Update** - Update the existing WPF month-load call site to the new asynchronous service signature so the desktop app continues to build and load Tithe figures correctly. No new bound properties or visual changes belong in this feature.
