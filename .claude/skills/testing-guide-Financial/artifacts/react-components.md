> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Shared Components (`*.tsx` in `Financial.Web/src/components/`)

## What to test

- **Prop-driven rendering**: component renders the correct content for each prop variant
- **Conditional output**: elements that appear only under certain prop combinations (e.g., a retry button that appears only when `onRetry` is provided)
- **Callback props**: interactive components invoke the provided callback when triggered
- **ARIA semantics**: if the component has accessibility-critical attributes, verify them

## Layer assignment

**Component test (unit)** — render in isolation with `render()` from Testing Library, no router needed unless the component uses `Link`. No API mocking needed — shared components receive all data via props.

## Setup pattern

```typescript
import { render, screen, fireEvent } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import { ComponentName } from '../ComponentName'

describe('ComponentName', () => {
  it('renders the provided message', () => {
    render(<ComponentName message="Something went wrong" />)

    expect(screen.getByText('Something went wrong')).toBeInTheDocument()
  })

  it('renders retry button when onRetry is provided', () => {
    render(<ComponentName message="Error" onRetry={vi.fn()} />)

    expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument()
  })

  it('does not render retry button when onRetry is not provided', () => {
    render(<ComponentName message="Error" />)

    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('calls onRetry when retry button is clicked', () => {
    const onRetry = vi.fn()
    render(<ComponentName message="Error" onRetry={onRetry} />)

    fireEvent.click(screen.getByRole('button', { name: /retry/i }))

    expect(onRetry).toHaveBeenCalledOnce()
  })
})
```

**`queryByRole` vs `getByRole`**: use `queryBy*` when asserting absence (it returns `null` instead of throwing). Use `getBy*` when the element is expected to be present.

## When to skip

- Components with no props and no conditional logic (static markup wrappers with no behavior)
- Purely visual components (spinners, dividers) with no interactive behavior or prop-driven content

## Examples from project

| Component | Key test scenarios |
|---|---|
| `ErrorState` | Error message displayed; retry button shown when `onRetry` provided, hidden otherwise; callback invoked on click |
| `LoadingState` | Loading indicator rendered (verify by role or text, not CSS class) |
| `NavigationTreePanel` | Tree nodes rendered from `items` prop; selection callback called with correct node when clicked |
