# Implementation Plan: Spreadsheet Import Card Resolution

**Prerequisites:**
- F01 (CreditCard entity, seed migration) and F02 (CreditCardId reference wiring) merged to `main`
- No new tools or packages

### Stage 1: Import-Time Resolution

**1. Row-Position Card Resolution by Name** - Change the monthly expense sheet importer's row-to-card mapping to carry card names instead of the legacy enum, resolve each inferred name against the seeded credit card entities passed into the importer, and flag-and-skip a row whose inferred name has no match instead of silently dropping the reference.

**2. Legacy CardTag Read by Name** - Replace the general entity-reference migrator's enum-typed read of a legacy expense's `CardTag` field with a plain string read, relying on its existing by-name entity lookup (already used for banks) to do the actual validation.

### Stage 2: Tests

**3. Resolution and Fail-Fast Coverage** - Add coverage for a row whose inferred card name has no matching seeded entity, and confirm existing row-position/marker-tag behavior is unchanged.
