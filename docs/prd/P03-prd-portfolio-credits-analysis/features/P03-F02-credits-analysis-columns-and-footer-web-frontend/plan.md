# Implementation Plan: P03-F02 — Credits Analysis Columns and Footer — Web Frontend

**Prerequisites:**
- F01 fully implemented and endpoint returning all 7 credit-analysis fields (confirmed)
- Node.js + npm available; `npm run dev` and `npm test` operational in `Financial.Web/`
- TypeScript build validation: `npx tsc -b --noEmit` must pass after each phase

---

### Phase 1: Type and Formatter Foundation

**1. Extend `PortfolioAssetSummaryItemDto`** — Add the 7 new fields returned by F01's endpoint to the TypeScript interface in `api/types.ts`. See the spec API Contracts section for the exact field names and types. After this step, run `npx tsc -b --noEmit` to confirm the type change compiles cleanly; the existing test fixture will produce a TypeScript error that signals Phase 2 is needed.

**2. Add `formatCreditMonth` helper** — Add a file-scoped function in `PortfolioSummaryTab.tsx` that parses a `"YYYY-MM"` string and returns `"MMM YYYY"` format (e.g., `"Jun 2026"`) using `'en-GB'` locale. See the spec Technical Decisions section for the locale rationale. This helper will be used by the new cells in Phase 2.

---

### Phase 2: Table Columns and Footer Panel

**3. Extend the table headers and `AssetRow` cells** — Add 5 new `<th>` elements after XIRR in `<thead>`: Last Month Credits, Last Credit Month, Last Month %, Est. Annual Credits, Est. Annual %. Add the corresponding 5 `<td>` cells in `AssetRow`, applying the `portfolio-summary__credits-separator` CSS class to the first header and first cell. Apply "—" fallbacks for null values per the spec API Contracts table. The separator class will be defined in Phase 3.

**4. Add the footer `<div>` panel** — Render a new `<div>` as a sibling to `.portfolio-summary__table-section` inside the `.portfolio-summary` flex container, positioned after it. The footer appears once `items` is non-null and non-empty. Include the 5 aggregate items (Total Invested, Total Credits, Current Value, Credits [Mon YYYY], Est. Annual Credits) with labels and values. Implement the three-state logic for Current Value (see spec Technical Decisions) and compute the "Credits [Mon YYYY]" label from `new Date()` at render time. See spec Section 7 test list for the exact null/sum behaviour expected for Est. Annual Credits.

---

### Phase 3: Styles and Tests

**5. Add CSS for separator and footer** — In `PortfolioSummaryTab.css`, define `.portfolio-summary__credits-separator` with a 3 px solid left border using `var(--accent)` and appropriate left padding to offset the border. Add `.portfolio-summary__footer` as a flex container with `var(--code-bg)` background, padding, and gap between items. Add `.portfolio-summary__footer-item` for label/value pairs and `.portfolio-summary__footer-footnote` for the asterisk footnote text. Run `npm run dev` and visually confirm the separator and footer render correctly before proceeding.

**6. Update test fixture and add column tests** — In `PortfolioSummaryTab.test.tsx`, extend `ITEM_1` (and any other item fixtures) with all 7 new DTO fields to resolve TypeScript errors. Add the 11 column-rendering test cases listed in spec Section 7 (column headers, formatted values, null/"—" fallbacks, separator class). Run `npm test` to confirm all pass.

**7. Add footer tests** — Add the remaining 10 footer test cases from spec Section 7: total aggregates, dynamic credits label (using `vi.setSystemTime`), three Current Value states, and the DOM structure assertion that no `<tfoot>` is present. Confirm all 22 existing tests still pass alongside the new ones.
