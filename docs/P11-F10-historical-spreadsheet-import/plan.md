# Implementation Plan: F10. Historical Spreadsheet Import

**Prerequisites:**
- F02-F08's Domain entities, `ICashFlowRepository`/`CashFlowJsonRepository`, and `CashFlowSerializerAdapter` already exist
- New NuGet dependency: `ClosedXML`
- The real `Despesas.xlsx` file (at `C:\Users\ggibe\Downloads\Despesas.xlsx`) is available locally for both format reconnaissance and the final manual verification run

### Stage 1: Project Scaffolding and Column Resolution

**1. Console project** - Create the new `Integrations/CashFlowSpreadsheetImport` console project referencing `Financial.CashFlow.Domain`, `Financial.CashFlow.Infrastructure`, `Financial.Shared.Infrastructure`, and `ClosedXML`; add it to `Financial.slnx`.

**2. Column resolver and raw-label fallback** - Add the shared column-identification helper (category-vs-description by value cardinality, tolerant of the header-label swap) and the wrapper type that preserves an unresolved category/payment-source/card label for the error report.

**3. Import report** - Add the accumulator that collects per-sheet and per-row outcomes and renders the final summary.

**4. Unit tests** - Add tests for the column resolver against synthetic 2017-shaped and 2019+-shaped header rows, and for the report accumulator.

### Stage 2: Monthly Expense Tabs

**5. Monthly expense sheet importer** - Add the parser for one `MonYYYY` tab: category/description column resolution, the E-column payment-source tag, and `Expense` creation (including `Category.Reserva` rows as ordinary expenses).

**6. Sheet enumeration** - Add the logic in `Program.cs` that selects exactly the 115 in-scope `MonYYYY` tab names (February 2017 through 2026) and excludes every full-Portuguese-name or pre-2017 tab.

**7. Unit tests** - Add tests covering an 11-column (2017-shaped) and a 17-column (2026-shaped) synthetic sheet, the payment-source tag, and an unrecognized category falling back to `RawLabelFallback` and being reported rather than dropped.

### Stage 3: Reservas, Mensais, and Controle Mae Sheets

**8. Reservas sheet importer** - Add the parser producing one `ReserveMovement` per populated bucket column per row.

**9. Mensais sheet importer** - Add the parser producing one `RecurringBillTemplate` per Brasil/UK row (with NIT/minimum-wage carried through where present) plus exactly one `RecurringBillInstance` for the month stamped at the top of the sheet.

**10. Controle mae sheet importer** - Add the parser that extracts a date from each row's free-text description (skipping and reporting rows where none can be confidently found) and preserves the sheet's own recorded BRL/GBP values without any live FX call.

**11. Unit tests** - Add tests for all three importers covering the scenarios above, including the single-vs-multi-bucket Reservas row cases and the Controle mae date-extraction-fails case.

### Stage 4: Resumo Validation, Investment Snapshots, and Final Assembly

**12. Resumo validation reader and investment snapshot import** - Add the reader that both writes `InvestmentSnapshot` entities from each `Resumo{Year}` sheet's 11-account rows/columns and computes the values needed to compare against F09's own yearly totals/diffs after import.

**13. Program.cs assembly** - Wire every importer together: build a fresh `CashFlowData`, run all sheet importers, run the Resumo validation pass, serialize to `data-cashflow.json`, print the final report.

**14. Unit tests** - Add a test for the report's final rendering listing every sheet's outcome.

### Stage 5: Verification

**15. Full-suite validation** - Build the entire solution and run every test project, confirming no regression to Investments or the existing CashFlow features from F01-F09.

**16. Manual verification against the real workbook** - Run the importer against the real local `Despesas.xlsx`, confirm every one of the 115 monthly tabs is either imported or reported with a specific error (none silently skipped), confirm no `Julho 2014`-`Janeiro 2017` tab was read, spot-check a handful of known figures (a specific month's category total, a Reservas bucket balance, the current Mensais snapshot) against the live spreadsheet, review the Resumo validation report for any unexpected discrepancies, then re-run the import from scratch and confirm the second run's `data-cashflow.json` matches the first.
