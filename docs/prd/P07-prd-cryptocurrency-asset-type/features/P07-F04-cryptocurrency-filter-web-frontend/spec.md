## Technical Overview

**What:** Add "Cryptocurrency" (value `9`) as a new option in the Web Investment Tree's "Asset class" filter dropdown.

**Why:** F01 (merged) added `GlobalAssetClass.Cryptocurrency` (int value `9`) to the backend domain enum. The Web frontend's asset-class filter is a hardcoded literal array with no central TypeScript enum mirroring the backend — it simply needs the one new entry to let users filter the Investment Tree down to cryptocurrency holdings.

**Scope:**
- Included: one new entry in the `ASSET_CLASS_OPTIONS` array; a test case covering it.
- Excluded: any icon/color system (none exists for asset classes today — the only per-node visual cue is the unrelated active/inactive status icon); any shared TypeScript enum/type for `GlobalAssetClass` (none exists — the value is read ad hoc at runtime via `getMetaNumber(node.metadata, 'GlobalAssetClass')`, not modeled as a typed enum on the frontend, and introducing one now is out of scope for this single-entry addition).
- Consumes (per PRD): `GlobalAssetClass.Cryptocurrency` and its underlying integer value (`9`), provided by F01.

## Architecture Impact

**Affected components:**
- `Financial.Web/src/components/InvestmentTree.tsx` — the `ASSET_CLASS_OPTIONS` literal array and its consuming `<select>` dropdown (lines 9-18, 244-261)

No other component is affected — filtering logic (`String(assetClass) !== filterClass` at the `AssetNode`/`PortfolioNode`/`BrokerNode` levels) is entirely generic over the numeric value and requires no change to support a 9th option.

## Technical Decisions

| Decision | Chosen Approach | Alternative Considered | Trade-off |
|----------|----------------|----------------------|-----------|
| Where to add the new value | Append `{ value: 9, label: 'Cryptocurrency' }` as the last entry in the existing `ASSET_CLASS_OPTIONS` array, matching backend enum declaration order | Insert alphabetically among existing labels | Matches the existing array's ordering convention (declaration order, not alphabetical — e.g. "Real Estate" isn't alphabetically placed today) and the PRD's explicit instruction to add it "alongside the existing 8 options" |

## Component Overview

**Frontend:**

| File Path | New/Modified | Purpose | Key Responsibilities |
|-----------|--------------|---------|---------------------|
| `Financial.Web/src/components/InvestmentTree.tsx` | Modified | Investment Tree asset-class filter | Add `{ value: 9, label: 'Cryptocurrency' }` to `ASSET_CLASS_OPTIONS`; no other code path changes since filtering is generic over the numeric value |

## Testing Strategy

**Test File Structure:**

| Test File | Test Type | Target | Coverage Goal |
|-----------|-----------|--------|---------------|
| `Financial.Web/src/components/__tests__/InvestmentTree.test.tsx` | Unit/Component | `ASSET_CLASS_OPTIONS` dropdown + filtering | Cryptocurrency option renders and filters correctly |

**Test functions:**

| Test Function | Description | Assertions |
|---------------|-------------|------------|
| `asset class filter shows Cryptocurrency option` | Renders the tree, opens the "Asset class" dropdown | The `<select>` contains an `<option>` with value `"9"` and label `"Cryptocurrency"` |
| `asset class filter hides non-matching assets when Cryptocurrency selected` | Builds a tree with a mix of assets including one `makeAsset('BTC', true, 9)`, fires `fireEvent.change` on the filter with value `'9'` | Only the Cryptocurrency asset (BTC) remains visible; other assets are hidden, following the exact pattern of the existing "asset class filter hides non-matching assets" test |

**Acceptance criteria traceability (PRD Section 9, F04):**
- "The Web asset-class filter dropdown includes a 'Cryptocurrency' option with value 9" → `asset class filter shows Cryptocurrency option`
- "Selecting 'Cryptocurrency' filters the investment tree to show only assets with Class = Cryptocurrency" → `asset class filter hides non-matching assets when Cryptocurrency selected`
- "The 8 pre-existing filter options remain unchanged in label, value, and order" → verified by the existing, unmodified tests in `InvestmentTree.test.tsx` continuing to pass (no existing test assertions are changed by this feature)

**Cross-Feature Integration (PRD Section 9):** F04 has no Consumes/Provides relationships beyond F01 (already merged); no new integration criteria apply.
