> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# React Components (`Financial.Web/src/components/**/*.tsx`)

Forms (`ExpenseForm`, `TransferForm`, `*FormDialog`), grids (`BanksGrid`, `CardsGrid`,
`CategoryTotalsGrid`, `TotalsGrid`, `grid/ColumnFilterMenu`, `grid/SortableColumnHeader`),
sections/tabs (`ExpensesSection`, `IncomeSection`, `CreditsTab`, `PriceHistoryTab`), shell
pieces (`Sidebar`, `Breadcrumb`, `SplitPanel`, `SyncStatusBanner`, `PaymentDueBanner`,
`ColourModeToggleButton`), state components (`LoadingState`, `ErrorState`) and charts
(`BrokerBreakdownCharts`, recharts).

## What to test

- **Rendered semantics, by role/name**: `getByRole('alert')`, `getByRole('button', { name: 'Try again' })`,
  `getByRole('link', { name: 'Brokers' })`, `getByLabelText(/Due today.*urgent/)` — the
  accessible name is the assertion (`docs/ui/accessibility.md`: icon-only controls have names,
  status is not colour alone).
- **Each visual state** the component owns: nothing rendered when the data is null/empty
  (`no_banner_when_payments_is_empty_array`), loading, error with/without retry, saving
  (`shows Saving... and disables the button while isSaving`), disabled, validation error under
  the named field, general error banner when no field claims the error, unsaved-changes
  confirmation where the component owns it.
- **Interactions**: `userEvent` clicks/typing, keyboard operability
  (`close_button_is_keyboard_operable`: `await user.tab(); await user.keyboard('{Enter}')`),
  callbacks called with the right payload (`calls onSave and onCancel`).
- **Form rules mirrored in WPF**: required markers, field order, same-bank rejection,
  pre-filled defaults — these are the parity contract `Financial.App` must match.
- **Financial formatting**: right-aligned numbers, `formatN2`, signs, totals distinct from rows.
- Negative: invalid input shows a specific message; a rejected `onSave` promise keeps the
  entered values; an unknown status renders the neutral fallback.

## Layer assignment

- **Unit (component)** — RTL `render` (through `src/test/renderWithFluent.tsx` when the
  component uses `@fluentui/react-components`, which needs a `FluentProvider` to resolve
  styles and `useId` labels) with props/callbacks as `vi.fn()`. A component that consumes a
  hook may mock the hook module (`vi.mock('../../hooks/usePaymentsDue', …)`) so the component
  test controls the state matrix directly — the page test covers the real hook.
- **Integration (frontend)** at the page level (`react-pages.md`).
- **E2E**: only through the smoke journey (`../references/e2e-environment.md`); no
  component-level Playwright.
- Focus management (tabster) and layout cannot be trusted in jsdom — assert the handler
  fires and the accessible name exists; verify visual focus in the browser per
  `docs/rules/ui.md`.

## Setup pattern

From `Financial.Web/src/components/__tests__/PaymentDueBanner.test.tsx`:

```tsx
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { FluentProvider, webLightTheme } from '@fluentui/react-components'
import type { PaymentsDueData } from '../../hooks/usePaymentsDue'
import type { PaymentDueDto } from '../../api/types'
import PaymentDueBanner from '../PaymentDueBanner'

const dismissMock = vi.fn()

const mockHookValue: PaymentsDueData = {
  payments: null,
  dismiss: dismissMock,
}

vi.mock('../../hooks/usePaymentsDue', () => ({
  usePaymentsDue: () => mockHookValue,
}))

function setPayments(payments: PaymentDueDto[] | null) {
  mockHookValue.payments = payments
}

function renderBanner() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <PaymentDueBanner />
    </FluentProvider>,
  )
}

describe('PaymentDueBanner', () => {
  beforeEach(() => {
    dismissMock.mockReset()
    setPayments(null)
  })

  it('no_banner_when_payments_is_empty_array', () => {
    setPayments([])

    renderBanner()

    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})
```

Prefer `import { render, screen } from '../../test/renderWithFluent'` over an inline
`FluentProvider` for new tests — it uses the project theme (`financialLightTheme`). Query by
role/label; never `container.querySelector`.

## When to skip

- Pure presentational wrappers with no props branching (`SplitPanel` beyond its resize
  callback), CSS files, `assets/`.
- Recharts internals — assert the chart title/labels and the accessible summary text, not
  SVG paths (`ResizeObserver` is shimmed in `setupTests.ts`).
- Re-testing a hook's fetch logic inside every component that uses it.

## Examples from project

- `Financial.Web/src/components/__tests__/PaymentDueBanner.test.tsx` — Unit; state matrix, urgency tiers by `aria-label`, keyboard close.
- `Financial.Web/src/components/__tests__/TransferForm.test.tsx` — Unit; required markers, same-bank validation, backend error under the named field, saving state.
- `Financial.Web/src/components/__tests__/ErrorState.test.tsx`, `LoadingState.test.tsx` — Unit; `role="alert"`, retry button presence.
- `Financial.Web/src/components/__tests__/Sidebar.test.tsx` — Unit; `navigation` landmark name, collapse/expand buttons, links by name.
- `Financial.Web/src/components/grid/__tests__/ColumnFilterMenu.test.tsx`, `SortableColumnHeader.test.tsx` — Unit; P37 grid controls.
- `Financial.Web/src/components/__tests__/CardsGrid.test.tsx` — Unit; trailing fixed-width action column (#750).
