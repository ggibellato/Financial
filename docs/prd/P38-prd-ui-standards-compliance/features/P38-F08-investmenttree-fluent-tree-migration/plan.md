# Implementation Plan: F08. InvestmentTree → Fluent Tree Migration

**Prerequisites:**
- F01 merged (design tokens available — `var(--accent)` already declared).
- No new tools/libraries — `Tree`/`TreeItem`/`TreeItemLayout` are already part of the installed
  `@fluentui/react-components` package; no third-party drag library is added (see spec §3).

### Stage 1: Leaf Node — AssetNode

**1. Convert `AssetNode` to a Fluent `TreeItem`** - Replace the `<li draggable>` / custom `<button>`
structure with `TreeItem itemType="leaf"` wrapping `TreeItemLayout`, moving the existing drag-start
handlers onto the new element and the status-color dot into `TreeItemLayout`'s `iconBefore` slot,
preserving the click-to-select behavior via `useSelectedNode`.

### Stage 2: Branch Nodes — PortfolioNode and BrokerNode

**2. Convert `PortfolioNode` to a Fluent `TreeItem`** - Replace its custom chevron/row `<li>`
structure with `TreeItem itemType="branch"` (or `"leaf"` when it has no visible children after
filtering) wrapping `TreeItemLayout` and a nested `Tree` of `AssetNode`s, preserving the existing
`onDragOver`/`onDragLeave`/`onDrop` handlers and click-to-select.

**3. Convert `BrokerNode` to a Fluent `TreeItem`** - Same conversion, wrapping a nested `Tree` of
`PortfolioNode`s.

### Stage 3: Root Tree and Controlled Expand State

**4. Wire the root `Tree` with controlled expand state** - Replace the top-level `<ul>` with
`<Tree aria-label="Investments">`, add a single `openItems` state seeded with every broker's key once
tree data loads (matching brokers' previous expanded-by-default behavior, portfolios' previous
collapsed-by-default behavior), and wire `onOpenChange` to keep it updated.

### Stage 4: Token Compliance and Dead-Rule Cleanup

**5. Tokenize the selected-node color and remove dead CSS** - Replace
`InvestmentTree.css`'s hardcoded `#007acc` with `var(--accent)`, and remove any rule that only
existed for markup no longer present after the migration.

### Stage 5: Test Suite Alignment and Manual Verification

**6. Update every test querying the old DOM shape** - Rewrite the affected cases in
`InvestmentTree.test.tsx` (render/expand/select/status-icon/filter groups) for `role="treeitem"` and
the merged expand+select click behavior; leave the drag-and-drop assertions' shape intact, adjusting
only the element-lookup helper if the drop-target class's host element changes.

**7. Manually verify drag-and-drop end-to-end** - Per the PRD's own flagged highest-risk regression:
run the app, drag an asset onto a sibling portfolio, a different broker, and a refusing target, and
confirm the move/new-portfolio/refusal behavior is unchanged.
