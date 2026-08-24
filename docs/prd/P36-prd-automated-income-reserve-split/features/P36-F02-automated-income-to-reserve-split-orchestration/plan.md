# Implementation Plan: F02. Automated Income-to-Reserve Split Orchestration

**Prerequisites:**
- .NET SDK / existing `Financial.slnx` build toolchain
- Node/npm for `Financial.Web` (contract regeneration only)
- No new libraries, no new environment variables, no data migration tool

### Stage 1: Domain

**1. Income Entity** - Add the `SplitToReserve` flag to the `Income` domain entity, defaulting to `false` both when explicitly created and when an existing record is deserialized without the field.

**2. ReserveMovement Entity** - Add a nullable `Income` back-reference to the `ReserveMovement` domain entity, defaulting to `null`, settable only at creation.

### Stage 2: Persistence

**3. Reference Resolution Ordering** - Reorder the top-level JSON reader so `Income` records (and a new lookup built from them) are resolved before `ReserveMovement` records are read, since a linked movement now needs its parent income already available.

**4. Optional Reference Property Support** - Extend the reference-property configuration mechanism to support a wire key that may be entirely absent from the JSON (rather than always required), and apply it to the new `ReserveMovement → Income` reference so every pre-existing movement in any data file keeps loading unchanged.

### Stage 3: Application

**5. Shared Split Fan-Out** - Extract the existing manual split's per-bucket movement creation into a small shared primitive, and have the manual flow call it unchanged, so both the manual and the new automated path compute a split identically.

**6. Locked Reserve Movement Exception** - Add a new domain exception raised when a caller tries to change a reserve movement that's linked to an income, and map it to a 409 response alongside the app's existing domain-exception mappings.

**7. Income Split Orchestration** - Extend income creation to validate split eligibility against the income source's flag, compute the split base from the net value, create the linked reserve movements in the same atomic save as the income, and roll both back together if the save fails.

**8. Income Update and Delete Cascade** - Extend income update to delete and recreate its linked reserve movements whenever the income's values or split flag change, rolling the income and its movements back to their prior state if the save fails; extend income delete to remove its linked movements in the same save.

**9. Reserve Movement Locking** - Reject direct update or delete of a reserve movement that's linked to an income, and exclude linked movements from the existing same-day-and-description group-delete so they can never be swept up as collateral damage.

### Stage 4: API Contract

**10. Income and Reserve Movement DTOs** - Extend the income create/update/read DTOs with the split flag and its resulting movement summary, and the reserve movement read DTO with its income link, so the new state is visible through the existing endpoints.

**11. OpenAPI Contract Regeneration** - Regenerate the pinned OpenAPI snapshot and the frontend's generated API types so the new fields and the new 409 response are reflected in the public contract and the web client's types.

### Stage 5: Testing

**12. Domain and Persistence Tests** - Add coverage for the new entity properties' defaults and explicit values, for the reference round-trip of a linked movement, and for a pre-existing movement record with no income-reference key at all still loading successfully.

**13. Income Service Tests** - Cover split-eligibility validation, the split computation and fan-out on create, the delete-and-recreate behavior on update (including a toggled-off and a toggled-on case), the cascade delete on income removal, and rollback to the prior state when a save fails during create or update.

**14. Reserve Service Tests** - Cover the locked-movement rejection on direct update and delete, the group-delete exclusion for linked movements, and a regression check that the manual split still produces unlinked movements after the shared fan-out extraction.

**15. API and Cross-Feature Integration Tests** - Extend the income and reserve movement endpoint tests for the new fields and the new 400/409 responses, and add a test confirming the split-eligibility check reads the real income-source flag from F01 rather than a duplicated rule.
