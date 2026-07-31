# Implementation Plan: F03. Legacy Data Migration

**Prerequisites:**
- F01 merged (`Settle`/`Unsettle` transitions available); F02 merged (real settlement dates may exist and must be preserved)
- .NET SDK; no new packages

### Stage 1: Migration Core

**1. Console project scaffold** - Create the migration console project under Integrations mirroring the spreadsheet importer's project shape, and register it plus its test project in the solution file. See spec Section 4.

**2. Migrator and summary** - Implement the classification rules over the loaded data aggregate using only the domain's settle/unsettle transitions, producing the per-state summary with the manual-review list. See spec Section 3 for the rule decisions.

### Stage 2: Console Entry Point

**3. Backup helper and program flow** - Implement the timestamped backup helper and the entry point: resolve the data path, back up, load, migrate, save, and print the summary. See spec Sections 3 (Backup discipline) and 4.

**4. Full-solution verification** - Run the complete .NET test suite and exercise the tool end-to-end against a copy of the live data file, confirming the summary and the resulting file shape.
