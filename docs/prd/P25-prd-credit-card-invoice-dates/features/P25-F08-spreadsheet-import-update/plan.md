# Implementation Plan: F08. Spreadsheet Import Update

**Prerequisites:**
- F01 (Expense Payment-Date Domain Model Rework) merged to `main` — already gives `Expense.Create` (called by the importer) its `ChargeDate`/`InvoiceDate` defaulting behavior
- No new environment variables or configuration files

### Stage 1: Regression Coverage

**1. Import ChargeDate/InvoiceDate Regression Tests** - Add the tests described in the spec's §7 Testing Strategy, locking in that a freshly imported credit card row gets `ChargeDate`/`InvoiceDate` populated correctly, and that a bank row's remain null. No production code changes are needed, per the spec's §3 Technical Decisions.

### Stage 2: Full Verification

**2. Full Solution Build and Test Pass** - Build and test every affected project to confirm the new coverage passes and nothing else regressed.
