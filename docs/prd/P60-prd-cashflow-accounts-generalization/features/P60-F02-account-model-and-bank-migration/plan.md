# Implementation Plan: Account Model and Bank Migration

**Prerequisites:**
- .NET 10 SDK and PowerShell 7, as for F01.
- `TngTech.ArchUnitNET` 0.13.4, pinned exactly, provided it was released at least two weeks before implementation; otherwise the newest version that was.
- A fresh copy of the live `data-cashflow.json` downloaded from Google Drive, for the parity run at the end of each PR. The parity baseline is the `p60-baseline` tag.
- Each PR is implemented on its own branch off an up-to-date `main`. PR B starts only after PR A is merged.

### Stage 1: PR A — Account model on the unchanged V1 file

**1. Account aggregate, templates and capabilities** - Add the `Account` aggregate, the template and capability enums, the template table with the Bank capability set, and the opening-balance and round-up value objects, following the existing entity conventions. See spec §4 Domain and §6 Business rules.

**2. Shared balance calculation** - Add the account-transaction role and the balance rule, and make Expense, Income, Transfer and BalanceAdjustment implement the role by declaring their own contribution. The rule must reproduce today's bank balance exactly, start-date window included.

**3. References become accounts across the model** - Replace `Bank` with `Account` in the CashFlow document root, the four record types and their capability guards, moving the four record types into the transactions folder, then delete the `Bank` entity. Keep every existing user-facing message unchanged.

**4. Application compatibility** - Serve `/banks`, balances, adjustments, transfers, expenses, incomes and recurring-bill payments from accounts through the existing services and DTOs, with balances from the shared rule. Remove the old balance calculation, and update the repository contract and the shared test stub.

**5. V1 persistence mapping** - Read and write accounts in the V1 bank shape and V1 reference names, so the data file stays byte-for-byte as it is today. Rename the reference converter and the seeded lookup context to accounts.

**6. Import builds accounts** - Make the spreadsheet import's bank seeding, opening-balance step, expense sheet resolution and raw-migration helpers create and resolve Bank-template accounts. The seeded names and round-up flags stay as they are.

**7. PR A verification and hand-off** - Run the full .NET suite and confirm the OpenAPI snapshot is untouched. Run `Invoke-Parity.ps1 -Step All` on the fresh live copy with the Migrate step still a plain copy, expecting 0 differences. Then run `/simplify` and the architecture-reviewer, and commit locally for the user's push and review.

### Stage 2: PR B — V2 format, migrate command and version guard

**8. V2 file shape** - Switch the account converter, the root converter and the reference wire names to the V2 shape in spec §6, keeping the property order with `Accounts` in the place of `Banks`.

**9. Version guard** - Make the serializer write version 2 and refuse any other stored version with the PRD message. Confirm the API stops at startup on an old file.

**10. Migrate command and V1→V2 step** - Add the `migrate` arguments, the migration chain runner and the V1→V2 step in the import tool, with the guards, backup, validation and exit codes from spec §5. Route `migrate` in the tool's entry point.

**11. Import on an existing output** - Make the import refuse an existing output file that isn't the current version, with the "run the migrate command first" message, before any backup or write.

**12. Seed, test and example data to V2** - Move the API test seed, the web-e2e test data and the example data file to the V2 shape with the same Ids.

**13. Architecture test** - Add ArchUnitNET to the architecture test project behind its one-method query interface and single adapter class. Reference the import tool project from the test project, and add the template-read rule with its allow-list.

**14. Parity Migrate step and docs** - Make the parity Migrate step run `migrate` on the live copy into the after-side file. Update the parity README and the README deploy section: download from Drive, migrate, upload, deploy.

**15. PR B verification, PRD ticks and hand-off** - Run the full .NET suite and the web build. Run the parity run on a fresh live copy with the Migrate step running `migrate`, expecting 0 differences and an unchanged SHA-256 for the copy. Then run `/simplify` and the architecture-reviewer, tick the F02 acceptance criteria in the PRD in their own commit, and commit locally for the user's push and review.
