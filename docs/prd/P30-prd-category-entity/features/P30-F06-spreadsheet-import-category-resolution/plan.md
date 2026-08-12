# Implementation Plan: Spreadsheet Import Category Resolution

**Prerequisites:**
- F01 (Category domain entity, seed migration) and F02 (Expense.Category entity reference) merged to `main`
- No new packages

### Stage 1: Resolver Rework

**1. Category Resolver** - Change the raw-label resolver to look up seeded Category entities by name directly (keeping its existing typo-tolerance mapping), instead of resolving to the legacy enum first.

**2. Importer Call Site** - Update the monthly expense sheet importer to call the reworked resolver with its existing name-keyed category dictionary, removing the now-unnecessary enum-to-entity round-trip.

### Stage 2: Tests

**3. Resolver Tests** - Update the resolver's unit tests for the entity-based signature; verify the existing importer-level tests (unrecognized category, historical typo) still pass unchanged, confirming the mechanism swap preserves behavior.
