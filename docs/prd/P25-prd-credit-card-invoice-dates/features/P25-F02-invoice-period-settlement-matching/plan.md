# Implementation Plan: F02. Invoice-Period Settlement Matching

**Prerequisites:**
- F01 (Expense Payment-Date Domain Model Rework) merged to `main` — provides `ChargeDate`/`InvoiceDate`/`Settle`/`Unsettle`
- No new environment variables or configuration files

### Stage 1: Matching Key and Warning

**1. Re-key Statement Matching to InvoiceDate** - Change `CardStatementService.GetStatementExpenses`'s matching predicate from `ChargeDate.Year/Month` to `InvoiceDate.Year/Month`, so mark-paid and unmark-paid both operate on the invoice-period assignment rather than the charge-origination stand-in F01 used as a stability bridge. Reference the spec's §3 Technical Decisions for why this supersedes F01's temporary key.

**2. Zero-Match Warning** - Add a nullable `Warning` field to `CardStatementDTO` and populate it from `MarkStatementPaidAsync` whenever zero charges matched the statement's invoice period, per the spec's §5 API Contracts example.

### Stage 2: Regression and New Coverage

**3. Billing-Cutoff and Warning Test Coverage** - Add the billing-cutoff regression tests (mark-paid and unmark-paid) that construct a charge with an explicit `InvoiceDate` override differing from its `ChargeDate`'s month, and the zero-match/warning-present/warning-absent tests, per the spec's §7 Testing Strategy. Confirm every existing test in the file still passes unmodified, since their fixtures never diverge `InvoiceDate` from `ChargeDate`.

### Stage 3: Full Verification

**4. Full Solution Build and Test Pass** - Build and test every affected project to confirm the matching-key change and warning addition introduce no regressions anywhere else in the solution.
