> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# React Components (`*.tsx` in `Financial.Web/src/components/`)

Examples: `BanksGrid`, `CardsGrid`, `CategoryTotalsGrid`, `IncomingGrid`, `ExpenseForm`, `IncomeForm`, `DetailPanel`, `ErrorState`, `LoadingState`, `SplitPanel`, `TickerCombobox`, `InvestmentTree`, `BrokerBreakdownCharts`, `TransactionsTab`, `CreditsTab`, `AggregatedSummaryTab`, `AssetSummaryTab`, `PortfolioSummaryTab`, `CashFlowLayout`, `InvestmentsLayout`.

## What to test

- **Prop-driven rendering**: component renders the correct content for each prop variant
- **Conditional output**: elements that appear only under certain prop combinations (e.g., a retry button that appears only when `onRetry` is provided)
- **Callback props**: interactive components invoke the provided callback when triggered
- **ARIA semantics**: if the component has accessibility-critical attributes, verify them

## Layer assignment

**Component test (unit)** — render in isolation with `render()` from Testing Library, no router needed unless the component uses `Link`. No API mocking needed — shared components receive all data via props, not via direct API calls.

## Setup pattern

```typescript
import { render, screen, fireEvent } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import { ErrorState } from '../ErrorState'

describe('ErrorState', () => {
  it('renders the provided message', () => {
    render(<ErrorState message="Something went wrong" />)

    expect(screen.getByText('Something went wrong')).toBeInTheDocument()
  })

  it('renders retry button when onRetry is provided', () => {
    render(<ErrorState message="Error" onRetry={vi.fn()} />)

    expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument()
  })

  it('does not render retry button when onRetry is not provided', () => {
    render(<ErrorState message="Error" />)

    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('calls onRetry when retry button is clicked', () => {
    const onRetry = vi.fn()
    render(<ErrorState message="Error" onRetry={onRetry} />)

    fireEvent.click(screen.getByRole('button', { name: /retry/i }))

    expect(onRetry).toHaveBeenCalledOnce()
  })
})
```

**`queryByRole` vs `getByRole`**: use `queryBy*` when asserting absence (returns `null` instead of throwing). Use `getBy*`/`findBy*` when the element is expected to be present.

## When to skip

- Components with no props and no conditional logic (static markup wrappers with no behavior)
- Purely visual components (spinners, dividers) with no interactive behavior or prop-driven content
- Passing a prop through to a child with no transformation — that's a wiring/mirror test

## Examples from project

| Component | Key test scenarios |
|---|---|
| `ErrorState` | Error message displayed; retry button shown when `onRetry` provided, hidden otherwise |
| `LoadingState` | Loading indicator rendered (by role or text, not CSS class) |
| `TickerCombobox` | Filters options as the user types; calls `onSelect` with correct ticker |
| `CategoryTotalsGrid` | One row per category with its total; empty state when no rows |
