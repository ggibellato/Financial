> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# React Pages (`Financial.Web/src/pages/*Page.tsx`, `src/App.tsx`, `src/navigation/routes.tsx`)

A page composes real hooks, real components and the real `apiClient` module surface; the only
thing replaced is the API itself. This is the SPA's Integration layer and where Web features'
AC-tracing tests draw their setup from.

## What to test

- **Full state matrix** (`docs/rules/ui.md`, `docs/ui/review-checklist.md`): initial render,
  loading (`shows a loading state before data arrives`), empty, error with retry
  (`getBanksMock.mockRejectedValue(new Error('Network down'))` → `role="alert"` + "Try again"
  re-fetches), saving (submit disabled), success (list updated), disabled actions, unsaved
  changes (`closes an open create form when switching tabs away and back`).
- **Workflow semantics**: default tab, tab order, month/year scope re-fetching every grid
  (`re-scopes all 4 Summary grids when the month/year value changes`), no refetch on tab switch,
  a mutation on one tab reflected on another (`marking a statement paid from the Credit Card tab updates the Summary tab too`),
  confirmation before destructive actions (`does not unmark a paid statement when the user cancels the confirmation`).
- **Backend errors**: `ApiError` 409 (overdraft/duplicate) → message shown, form kept;
  404 → error state; 5xx → banner.
- **Routing/shell** (`App.test.tsx`, `RootRedirect.test.tsx`, `AppearancePage.test.tsx`):
  sidebar landmark, breadcrumb text, redirect for unknown paths, colour mode persisted in
  `localStorage`.
- **Accessibility at page level**: every action reachable by role/name; dialog focus return
  where the page owns the dialog.
- Negative criteria from the PRD (e.g. "if the endpoint fails the banner renders nothing") get
  their own tagged test (`../references/feature-traceability.md`).

## Layer assignment

- **Integration (frontend)**: real page + hooks + components + router (`MemoryRouter` with
  `initialEntries`) + context providers; `vi.mock('../../api/financialApiClient')` is the
  single fake — the backend is a separate deployable (`../references/mock-health-rules.md`).
  `sessionStorage`/`localStorage` are real jsdom storage, cleared in `afterEach`.
- **Unit**: not for pages — their logic lives in hooks/components.
- **E2E**: one critical journey per workflow in `Financial.Web/scripts/smoke-test.mjs`
  against the published API (`../references/e2e-environment.md`); today that is Historic
  Summary Average.

## Setup pattern

From `Financial.Web/src/pages/__tests__/MensaisPage.test.tsx`:

```tsx
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MensaisPage from '../MensaisPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BankDto, CategoryDto, RecurringBillDto } from '../../api/types'

const {
  getMensaisBillsMock,
  createMensaisBillMock,
  updateMensaisBillStatusMock,
  getBanksMock,
  getCategoriesMock,
} = vi.hoisted(() => ({
  getMensaisBillsMock: vi.fn<FinancialApiClient['getMensaisBills']>(),
  createMensaisBillMock: vi.fn<FinancialApiClient['createMensaisBill']>(),
  updateMensaisBillStatusMock: vi.fn<FinancialApiClient['updateMensaisBillStatus']>(),
  getBanksMock: vi.fn<FinancialApiClient['getBanks']>(),
  getCategoriesMock: vi.fn<FinancialApiClient['getCategories']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getMensaisBills: getMensaisBillsMock,
    createMensaisBill: createMensaisBillMock,
    updateMensaisBillStatus: updateMensaisBillStatusMock,
    getBanks: getBanksMock,
    getCategories: getCategoriesMock,
  } as Partial<FinancialApiClient>,
}))
```

(Trimmed from the real file, which mocks nine client methods.) Then per test:
`getMensaisBillsMock.mockResolvedValue(BILLS)`, `render(<MensaisPage />)` (through
`renderWithFluent` when Fluent components are on the page), `await screen.findByRole(...)`,
interact with `fireEvent`/`userEvent`, assert with `within(row)`. For routed pages wrap in
`<MemoryRouter initialEntries={['/cashflow/monthly']}>` as `App.test.tsx` does.

## When to skip

- `AdminEntityPlaceholderPage` beyond one render test.
- Duplicating every component-level state assertion at page level — the page test asserts the
  composition (the right component shows for the right state), the component test asserts its
  internals.
- `lazyPages.tsx` — `React.lazy` wiring; `routes.test.ts` covers agreement.

## Examples from project

- `Financial.Web/src/pages/__tests__/MonthlyPage.test.tsx` — Integration; the richest state/tab matrix in the suite.
- `Financial.Web/src/pages/__tests__/MensaisPage.test.tsx` — Integration; CRUD + status actions + UK expense prompt (P41).
- `Financial.Web/src/pages/__tests__/BanksPage.test.tsx` — Integration; error state with retry on load failure.
- `Financial.Web/src/__tests__/App.test.tsx` — Integration; shell, sidebar landmark, breadcrumb, storage cleanup.
- `Financial.Web/src/pages/__tests__/RootRedirect.test.tsx` — Integration; default route.
