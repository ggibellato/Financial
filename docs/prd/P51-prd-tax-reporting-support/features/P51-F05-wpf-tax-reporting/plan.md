# Implementation Plan: F05. WPF — Tax Reporting

**Prerequisites:**
- F01, F02, F03, F04 merged (`ITaxRuleService`, `ITaxWorkbookService`, `AssetDetailsDTO.TaxJurisdictions`) —
  all shipped and already registered in `Financial.App`'s DI container via `services.AddFinancialApplication()`.

### Stage 1: TaxProfile on Asset Detail

**1. Asset detail tax jurisdiction display** - Expose the asset's distinct tax jurisdictions (already
computed server-side via F04's `AssetDetailsDTO.TaxJurisdictions`) on the existing asset summary
ViewModel, and render it in the asset summary view next to the existing Country field, with an
appropriate empty display when the asset has no classification yet.

**PR boundary:** Stage 1 ships alone — 2 files, well under the repo's 8-non-test-file limit.

### Stage 2: Tax Page and CSV Export

**2. Workbook ViewModel** - Build the ViewModel that loads the selectable jurisdiction/tax-year
options, tracks the current selection (defaulting to the first option, resetting the tax year when
the jurisdiction changes), and loads the corresponding workbook, with independent loading/error
states for the options list and the workbook itself.

**3. Entry row display wrapper** - Build the per-entry display wrapper that resolves each of the 4
calculation statuses to the same colors and icons `Financial.Web`'s status badges render, for both
individual entries and the workbook's own aggregate status.

**4. Save-file dialog capability** - Add a native file-save capability to the existing dialog service
abstraction, the desktop equivalent of the browser download `Financial.Web`'s CSV export uses.

**5. Tax page view** - Build the Tax page: jurisdiction and tax-year selectors, the workbook's entries
grid, category totals, the status indicators, and an Export CSV action producing the same 13-column
file `Financial.Web` produces. Cover every state the PRD's F04 Experience block requires (this
feature's own Experience block mirrors it): initial, loading, empty, success, disabled.

**6. Navigation** - Register the Tax page's route and add its sidebar entry under Investments,
matching `Financial.Web`'s position in the same category.

**PR boundary:** Stage 2 ships alone — 9 non-test files, one over the repo's rule of thumb (the
mandatory 3-file navigation-registration trio this app always touches for a new top-level page, the
same shape as F04's own Stage 2 overage on the React side). No dependency on Stage 3.

### Stage 3: Admin Tax Rules Screen

**7. Tax rules list/CRUD ViewModel** - Build the ViewModel managing the rules list and
create/update/delete operations, including surfacing a rejected delete's server message (a rule
still backing a final classification) without dropping the row from the list, and wording the delete
confirmation around permanent deletion rather than deactivation.

**8. Tax rule form dialog** - Build the create/edit dialog: jurisdiction, event category (locked once
editing, since neither is updatable), label, description, and effective date range, with inline
validation mirroring the server's own range-ordering and overlap checks.

**9. Admin Tax Rules screen** - Build the list screen: table of existing rules, create/edit/delete
actions, an empty state prompting creation of the first rule, and inline error display for a rejected
delete naming the affected tax year(s).

**10. Navigation** - Register the Admin Tax Rules screen's route and add its sidebar entry under
Admin > Investment, alongside Assets, Brokers and Portfolios.

**PR boundary:** Stage 3 ships alone — 11 non-test files, the same unavoidable shape every existing
Admin CRUD screen in this app takes (view/viewmodel pair, form-dialog view/viewmodel pair, dialog
service, navigation trio) — not scope creep, flagged explicitly. Independent of Stage 2 beyond the
two files (`IDialogService`/`DialogService`) both stages extend with their own additions.
