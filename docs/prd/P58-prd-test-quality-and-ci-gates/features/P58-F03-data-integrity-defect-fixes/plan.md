# Implementation Plan: Data-Integrity Defect Fixes

**Prerequisites:**
- .NET 10 SDK; no new NuGet packages (`System.Collections.Immutable` is part of the shared framework)
- A temp copy of `data/data-cashflow.json` and a copy of `Despesas.xlsx` for the PR3 manual check, never the live file
- Each stage below is one PR, branched from an up-to-date `main` (`git merge --ff-only origin/main` first). The three stages are independent and may merge in any order.

### Stage 1: Snapshot-Safe CashFlow Reads (PR1)

**1. Copy-on-write collections** - Change the CashFlow Domain item and id collections so that every mutation publishes a new immutable array and every enumeration walks the array that was current when it started. See spec §3 "Snapshot mechanism" and "Writer concurrency".

**2. Aggregate collection unification** - Move the four collections in `CashFlowData` that still wrap a plain list onto the shared collection type, keeping the aggregate's public properties and mutators unchanged.

**3. Repository concurrency proof** - Add the read-during-save scenarios described in spec §7 Fix 1 to the CashFlow Infrastructure tests, confirm they fail against the pre-change Domain code, then pass after it.

### Stage 2: Reserve Split Conservation (PR2)

**4. Split allocation rule** - Add the Domain rule that turns an ordered set of active buckets and a base amount into per-bucket amounts that sum to the percentage-implied target, with the residual on the largest bucket. See spec §3 "Split target" and "Tie-break".

**5. Split fan-out wiring** - Route the shared split fan-out in the reserve service through the new rule, so both the manual split and the income-linked split use it without changing their call sites.

**6. Literal split expectations** - Replace the income service split assertions that recompute amounts with production code by literal per-bucket values, and add the service-level thirds-split case from spec §7 Fix 2.

### Stage 3: Import Tool Guards (PR3)

**7. Command-line contract** - Introduce the argument parser for the required workbook and `--output` arguments and the optional mode flag, removing the hardcoded default paths. See spec §4 "Messages" for the exact refusal texts and exit codes.

**8. Live data file guard** - Add the repository-root-based live-file check and run it before any backup, raw migration or load touches the output file.

**9. Non-carried records disclosure** - Expose the category seed list's membership check, add the report that counts the record types a full rebuild will not carry over, and print it before the import writes; wrap the existing-file load so an unreadable file exits with the dedicated code.

**10. Tool documentation** - Update the README's CashFlowSpreadsheetImport section with the new usage, the refusal behaviour, the exit codes and the instruction to always target a temp copy.
