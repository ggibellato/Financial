# Implementation Plan: F03. Tax Year Workbook

**Prerequisites:**
- F01 (Tax Rules) — merged (#841, #842)
- F02 (Tax Profile and Classification) — merged (#843, #844, #845)

### Stage 1: Aggregation and API Surface

**1. Workbook DTOs** - Define the wire shapes for a selectable jurisdiction/tax-year option, a single workbook entry, a category total, and the assembled workbook itself.

**2. Workbook Aggregation Service** - Add the service that walks every asset's classified events, filters and groups them by jurisdiction, tax year and event category, resolves each entry's date and applicable rule label from its source record, and computes the workbook's overall calculation status from its entries.

**3. Dependency Injection Registration** - Register the new service in the Investment application layer's composition root.

**4. Tax Workbook Controller** - Expose the assembled workbook and the selectable jurisdiction/tax-year options over HTTP.

**PR boundary:** single PR, 8 non-test files — at the repo's limit but not over it. There is no
Domain-layer or live-wiring work to stage separately here, unlike F01/F02: this feature only reads
data those already-shipped features produced.
