# Implementation Plan: F07 — Shares Dividend Check Redesign

**Prerequisites:**
- F01 (App Navigation & Layout Restructure) implemented ✅
- No new npm packages required
- `DividendHistoryItemDto`, `DividendSummaryDto` types in `src/api/types.ts` are unchanged
- `getDividendHistory` and `getDividendSummary` methods in `src/api/financialApiClient.ts` are unchanged

---

### Stage 1: TickerCombobox Component

**1. TickerCombobox Component and Styles** - Create `src/components/TickerCombobox.tsx` and its paired `TickerCombobox.css`. The component receives a `groups` prop (array of `{ label, tickers }`), a `value` string, and an `onChange` callback. It renders a text input alongside a grouped dropdown overlay that opens on input interaction, supports Arrow Up/Down and Enter keyboard navigation across options, and closes on Escape or outside click. See the spec for the full interface shape and visual requirements.

**2. TickerCombobox Tests** - Create `src/components/__tests__/TickerCombobox.test.tsx` covering group label rendering, all 8 ticker options, default value display, option click callback, freeform typing callback, and keyboard/outside-click dismissal. Follow the `@testing-library/react` and `vi.fn()` patterns used in sibling component test files.

---

### Stage 2: DividendCheckPage Redesign

**3. DividendCheckPage Styles** - Create `src/pages/DividendCheckPage.css` with styles for the vertical four-line summary card, the blue colour class for the average dividend line, the green and red colour classes for the price max buy line, and the two-column table container with independently scrollable left (~2/3) and right (~1/3) regions.

**4. DividendCheckPage Rewrite** - Rewrite `src/pages/DividendCheckPage.tsx` to define the three-group watchlist constant (KLBN4 as default), replace the ticker and exchange inputs with `TickerCombobox`, hardcode `BVMF` in all API calls, apply the updated summary card structure and two-column table layout, sort Dividend History by date descending and By Year by year descending client-side, and clear summary and history state on error. See the spec for all field labels, colour rules, and formatting requirements.

**5. DividendCheckPage Tests Update** - Update `src/pages/__tests__/DividendCheckPage.test.tsx` to cover the new behaviour: KLBN4 default, BVMF exchange in API calls, green/red colour classes on price max buy, descending sort of both tables, results cleared on failure, and freeform ticker passthrough. Remove or update assertions that relied on the old exchange input and grid-style summary layout.
