# Spec: F08. Spreadsheet Import Update

## 1. Technical Overview

**What:** Confirm and lock in, with regression tests, that `MonthlyExpenseSheetImporter` produces credit card expenses with `ChargeDate`/`InvoiceDate` correctly populated for every imported row.

**Why:** F01 gave `Expense.Create` the behavior this feature's PRD Capabilities describe (`ChargeDate = date` and `InvoiceDate` defaulting to the 1st of the charge month, for any expense with a non-null `CardTag`). `MonthlyExpenseSheetImporter` already constructs every expense via `Expense.Create(date, description, value, category, paymentSource, cardTag)` (no `invoiceDate` override) — the exact call shape that already gets this behavior automatically. **No production code change is required to satisfy F08's stated Capabilities**; verified by reading `MonthlyExpenseSheetImporter.cs` line 98, the only call site that constructs an `Expense` in the entire import path.

**Scope:**
- **Included:** Regression test coverage in `MonthlyExpenseSheetImporterTests.cs` that locks in `ChargeDate`/`InvoiceDate` correctness for imported credit card rows — this is F08's real, honest deliverable, since the production behavior itself already exists as a byproduct of F01. Per this project's "no over-engineering" convention, no code is added purely to have something to change; the gap this feature closes is test coverage, not behavior.
- **Excluded:** Any change to `MonthlyExpenseSheetImporter.cs` itself, since there is nothing incorrect to fix.

## 2. Architecture Impact

**Affected components:**

| Layer | Component | Change |
|---|---|---|
| Integrations Tests | `Tests/Financial.CashFlowSpreadsheetImport.Tests/SheetImporters/MonthlyExpenseSheetImporterTests.cs` | New regression tests per §7 |

**Data flow:** unchanged — `MonthlyExpenseSheetImporter.Import(...)` → `Expense.Create(date, description, value, category, paymentSource, cardTag)` → F01's existing `ChargeDate`/`InvoiceDate` defaulting logic.

## 3. Technical Decisions

| Decision | Chosen Approach | Alternative Considered | Trade-off |
|---|---|---|---|
| No code change to the importer | Confirmed via direct code inspection (`MonthlyExpenseSheetImporter.cs:98`) that it already calls `Expense.Create` with the exact signature that gets F01's `ChargeDate`/`InvoiceDate` defaults automatically | Add an explicit call passing `invoiceDate` through, or duplicate the defaulting logic in the importer | Either alternative would be redundant logic with no behavioral difference from what already happens — pure churn. The PRD's own Capabilities text ("mirroring `Expense.Create`'s default behavior from F01") already anticipates this: the importer was designed to inherit the behavior, not reimplement it. |

## 4. Component Overview

**Tests:**

| File Path | New/Modified | Purpose | Key Responsibilities |
|---|---|---|---|
| `Tests/Financial.CashFlowSpreadsheetImport.Tests/SheetImporters/MonthlyExpenseSheetImporterTests.cs` | Modified | Import regression tests | New tests asserting `ChargeDate`/`InvoiceDate` on imported credit card rows, per §7 |

## 5. API Contracts

None — this is a CLI import tool, not an HTTP endpoint.

## 6. Data Model

No schema change.

## 7. Testing Strategy

**Test File Structure:**

| Test File | Test Type | Target | Coverage Goal |
|---|---|---|---|
| `Tests/Financial.CashFlowSpreadsheetImport.Tests/SheetImporters/MonthlyExpenseSheetImporterTests.cs` | Unit | `MonthlyExpenseSheetImporter` | Every F08 acceptance criterion |

**Functions to add:**

| Test Function | Description | Assertions |
|---|---|---|
| `Import_CreditCardRow_SetsChargeDateEqualToImportedRowDate` | A row resolving to a card tag (fixed card section, blank payment source tag) | `expense.ChargeDate == expense.Date` (the imported row's date), non-null |
| `Import_CreditCardRow_DefaultsInvoiceDateToFirstOfChargeMonth` | Same row shape | `expense.InvoiceDate == new DateOnly(year, month, 1)` |
| `Import_BankExpenseRow_ChargeDateAndInvoiceDateAreNull` | A row with an explicit bank payment source tag (no card) | Both fields null, confirming the behavior is card-only |

**Acceptance criteria covered (PRD Section 9, F08):**
- Every newly imported credit card expense has `ChargeDate` equal to its imported row date — `Import_CreditCardRow_SetsChargeDateEqualToImportedRowDate`.
- Every newly imported credit card expense has `InvoiceDate` defaulted to the 1st of its charge date's month/year — `Import_CreditCardRow_DefaultsInvoiceDateToFirstOfChargeMonth`.
- Existing import failure/skip behavior for unresolvable card tags is unchanged — no code touched this path; the existing `Import_UnrecognizedCategory_SkipsExpenseAndFlagsRow`-style tests already cover it and remain green (verified in full-suite re-run, not duplicated here).
- A pre-import backup is created before the import writes any changes, matching existing behavior — unchanged code path (`Program.cs`'s `MigrationBackup.Create` call, untouched by this feature).

**Cross-Feature Integration criteria this feature satisfies:**
- "F01's field definitions are correctly applied by F08 to every newly imported credit card expense going forward" — covered by the two new tests above.

## Assumptions / Decisions Flagged for Review

1. This feature ships with zero production code changes — confirmed by direct inspection that F01 already made `MonthlyExpenseSheetImporter` correct. Flagging this explicitly since it's unusual for a PRD feature to require no implementation, only verification.
