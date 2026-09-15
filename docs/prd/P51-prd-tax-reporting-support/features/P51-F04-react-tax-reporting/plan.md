# Implementation Plan: F04. React — Tax Reporting

**Prerequisites:**
- F01, F02, F03 merged (`TaxRulesController`, `TaxClassification`/backfill/live wiring,
  `TaxWorkbookController`) — all shipped.
- `Financial.Web/src/api/generated/openapi.ts` already carries every `Tax*` schema; regenerating it
  after Stage 1's `AssetDetailsDTO` change is still required (`npm run generate-api-types`).

### Stage 1: TaxProfile (Backend + Asset Detail)

**1. AssetDetailsDTO tax jurisdiction field** - Add the read-only, computed list of an asset's
distinct tax jurisdictions to the existing asset-detail response, populated from the asset's active
tax classifications. No new endpoint or persisted state.

**2. OpenAPI snapshot and frontend type regeneration** - Regenerate the backend OpenAPI snapshot and
the frontend's generated types so the new field is available to the React client.

**3. Asset detail view addition** - Show the asset's tax jurisdiction(s) in the existing asset summary
view, alongside the asset's other identifying fields, with an appropriate empty state when the asset
has no classification yet.

**PR boundary:** Stage 1 ships alone — 2 backend files, no frontend UI beyond one existing component
edit — well under the repo's 8-non-test-file limit (`docs/rules/design.md` §PR size). It also
completes F02's one outstanding acceptance criterion (`TaxProfile`), which F02's own spec explicitly
deferred here.

### Stage 2: Tax Page and CSV Export

**4. Workbook API client and types** - Add frontend type aliases and API client methods for F03's
already-shipped workbook endpoints (list selectable jurisdiction/tax-year pairs; fetch a workbook for
a selected pair).

**5. Tax page state hook** - Build the hook that loads the selectable options, tracks the current
jurisdiction/tax-year selection, fetches the corresponding workbook, and exposes independent
loading/error/empty states for the selector list and the workbook itself.

**6. CSV export utility** - Build a pure client-side function that turns a fetched workbook into the
13-column CSV format the PRD specifies, plus a small helper that triggers a browser download of the
result.

**7. Tax page UI** - Build the Tax page: jurisdiction and tax-year selectors, the workbook's entries
table, category totals, a visually distinct indicator for each calculation status (on entries and on
the aggregate), and an Export CSV action. Cover every state the PRD's F04 Experience block requires:
initial, loading, empty, success, disabled.

**8. Navigation** - Register the Tax page's route and add its sidebar entry under Investments.

**PR boundary:** Stage 2 ships alone — 8 non-test files (2 shared files reused from Stage 1's pattern
plus 6 new), at the repo's limit. It has no dependency on Stage 3.

### Stage 3: Admin Tax Rules Screen

**9. Tax rules API client and types** - Add frontend type aliases and API client methods for F01's
already-shipped tax-rule endpoints (list, create, update, delete).

**10. Tax rules list/CRUD state hook** - Build the hook managing the rules list and create/update/delete
operations, including surfacing a rejected delete's server message (a rule still backing a final
classification) without silently dropping the row.

**11. Tax rule form dialog** - Build the create/edit form: jurisdiction, event category, label,
description, and effective date range, with inline validation that mirrors the server's own
range-ordering and overlap checks so the common invalid case never round-trips, and a path for
surfacing a server-side rejection (overlap, or a blocked delete) inline.

**12. Admin Tax Rules page** - Build the list screen: table of existing rules, create/edit/delete
actions, an empty state prompting creation of the first rule, and inline server-error display for a
rejected delete naming the affected tax year(s).

**13. Navigation** - Register the Admin Tax Rules screen's route and add its sidebar entry under
Admin > Investment, alongside Assets, Brokers and Portfolios.

**PR boundary:** Stage 3 ships alone — 8 non-test files, at the repo's limit, independent of Stage 2's
files beyond the two shared, generic ones (`types.ts`, `financialApiClient.ts`) each stage extends
with its own additions.
