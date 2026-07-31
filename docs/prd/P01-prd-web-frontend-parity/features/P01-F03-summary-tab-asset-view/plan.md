# Implementation Plan: F03 — Summary Tab — Asset View

**Prerequisites:**
- F01 (App Navigation & Layout Restructure) — implemented
- F02 (Investment Tree & Split Panel Layout) — implemented
- `System.Text.Json` available in `Financial.Application` (used for `JsonStringEnumConverter`)

---

### Phase 1: Backend — Enum Serialization

**1. AssetDetailsDTO and AssetNodeDTO** — Add the `[JsonConverter(typeof(JsonStringEnumConverter))]` attribute to the `Class` and `Country` properties in both `AssetDetailsDTO.cs` and `AssetNodeDTO.cs`. After this change the API returns `"Equity"` instead of a numeric value for asset class, and `"BR"` instead of a numeric country code. See spec Section 5 for the full set of string values for each enum.

---

### Phase 2: Frontend — API Contract Alignment

**2. Update types.ts** — Change `class: number` to `class: string` and `country: number` to `country: string` in both `AssetDetailsDto` and `AssetNodeDto`. This aligns the TypeScript types with the updated API response contract defined in spec Section 5.

---

### Phase 3: Frontend — Hook and Component

**3. useAssetSummary hook** — Create `src/hooks/useAssetSummary.ts`. The hook reads the selected node from `SelectedNodeContext`, fetches asset details and the current price concurrently when a new asset is selected, exposes a refresh trigger for the price, and derives the four computed fields described in spec Section 4. It also exposes the `showCurrentSection` flag and separate loading/error states for asset and price fetches.

**4. AssetSummaryTab component** — Create `src/components/AssetSummaryTab.tsx` and `AssetSummaryTab.css`. The component consumes `useAssetSummary` and renders the two-column grid layout with all fields, the horizontal separators, the colour-coded monetary and percentage values, the Current section (shown or hidden per the hook flag), the Refresh button, and the Status error field. See spec Section 4 for the grid row order and colour rules.

**5. DetailPanel integration** — Modify `src/components/DetailPanel.tsx` to import `AssetSummaryTab` and render it in the `summary` tab branch when `selectedNode.nodeType === 'Asset'`. Broker and Portfolio node selections must continue to show the existing placeholder text, which F04 will replace.

---

### Phase 4: Tests

**6. Hook unit tests** — Write `src/hooks/useAssetSummary.test.ts` covering all hook states and formulas listed in spec Section 7: loading, success, computed value correctness, price-only error (status field), asset load failure, refresh trigger, node-change reset, and `showCurrentSection` flag logic.

**7. Component and integration tests** — Write `src/components/AssetSummaryTab.test.tsx` covering all rendering scenarios from spec Section 7. Extend `src/components/DetailPanel.test.tsx` with the three additional test functions that verify the summary tab routes to `AssetSummaryTab` for asset nodes and retains placeholders for broker and portfolio nodes.
