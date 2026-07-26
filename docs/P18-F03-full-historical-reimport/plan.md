# Implementation Plan: F03. Full Historical Re-Import (2017-2026)

**Prerequisites:**
- .NET 10 SDK (existing solution target)
- F01 and F02 merged to main
- No new NuGet packages, environment variables, or configuration files required
- Branch `feat/P18-F03-full-historical-reimport`, already created from `main`

### Stage 1: Import Pipeline Behavior Change

**1. Explicit Zero and Malformed-Cell Handling** - Change the Resumo row importer so a matched account-row writes a snapshot for every one of the 12 months: a genuinely blank cell becomes an explicit zero value, while a non-blank cell that fails to parse as a number is logged as a validation warning and produces no snapshot for that month, distinguishing the two cases instead of treating both as silently skipped.

**2. Report Wiring** - Thread the existing import report object into the row importer so the new malformed-cell warning surfaces in the same run summary as the other validation warnings already produced during import.

### Stage 2: Test Suite Alignment

**3. Importer Test Updates** - Update the existing blank-cell test for the new explicit-zero behavior and add coverage for a malformed cell producing a warning and no snapshot, while the rest of that row's months still resolve normally.

### Stage 3: Verification

**4. Full Suite and Manual Import Verification** - Run the complete test suite, then run the spreadsheet import command against a copy of the existing data file (never the live file) to confirm every matched account-year now has exactly 12 snapshot records, the run is idempotent across two invocations, and the total snapshot count reflects the newly-written zeros.
