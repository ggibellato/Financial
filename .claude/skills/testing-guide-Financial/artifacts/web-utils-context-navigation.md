> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Web Utils, Context and Navigation (`Financial.Web/src/utils/*.ts`, `src/context/*.tsx`, `src/navigation/*.ts*`)

## What to test

- **Formatters** (`utils/formatters.ts`): `pad`, `formatN2`, `formatN8`, `formatPercent1`,
  `formatShortDate`, `parseMonthInputValue`, `currentYearMonth` (with `vi.useFakeTimers` /
  `vi.setSystemTime` for date-dependent helpers) — each branch and boundary (negative, zero,
  rounding half-way values, invalid input).
- **Period filtering** (`utils/periodFilter.ts`): inclusive boundaries, year rollover, empty
  range — mirrored by WPF `PeriodFilterHelper`.
- **Storage helpers** (`domainStorage.ts`, `sidebarStorage.ts`, `colourModeStorage.ts`):
  default when nothing stored, round-trip, corrupt value → default (never throws).
- **`confirmThenRun`**: runs on confirm, skips on cancel.
- **`createFormDefaults` / `expenseDefaults`**: today's date, last-used bank.
- **Contexts** (`SelectedNodeContext`, `ColourModeContext`): default value, setter, toggle,
  persistence to `localStorage`, `prefers-color-scheme` fallback (P43).
- **Navigation** (`navTree.ts`, `routes.tsx`): every sidebar destination has a declared route,
  every route is reachable from the sidebar, no duplicate paths (`routes.test.ts`); ids match
  WPF `NavTree` (parity).
- Negative: invalid dates, `NaN`, unknown colour mode string.

## Layer assignment

- **Unit only** — pure functions and providers rendered with a tiny probe component (see
  `ColourModeContext.test.tsx`'s `ModeDisplay` / `ModeSetter`). Storage is real jsdom storage,
  cleared in `afterEach`. No Integration/E2E of their own.

## Setup pattern

```ts
import { describe, expect, it } from 'vitest'
import { NAV_TREE } from '../navTree'
import { PAGE_ROUTES } from '../routes'

const navRoutes = NAV_TREE.flatMap((category) => [
  ...category.children.map((child) => child.route),
  ...(category.groups ?? []).flatMap((group) => group.children.map((child) => child.route)),
])
const declaredRoutes = PAGE_ROUTES.map((route) => `/${route.path}`)

describe('route and sidebar agreement', () => {
  it('every sidebar destination has a route declared for it', () => {
    const missing = navRoutes.filter((route) => !declaredRoutes.includes(route))

    expect(missing, `NAV_TREE lists these routes, but PAGE_ROUTES does not declare them: ${missing.join(', ')}`).toEqual([])
  })
})
```

(Verbatim from `Financial.Web/src/navigation/__tests__/routes.test.ts`.) For a context:

```tsx
function ModeDisplay({ testId = 'mode' }: { testId?: string }) {
  const { colourMode } = useColourMode()
  return <div data-testid={testId}>{colourMode}</div>
}
```

then `render(<ColourModeProvider><ModeDisplay /></ColourModeProvider>)` and
`localStorage.clear()` in `afterEach`.

## When to skip

- `theme/fluentTheme.ts` (token object), `styles/*.css`, `assets/` — no behaviour.
- `main.tsx` (excluded from coverage in `vite.config.ts`).
- `positionType.ts` if it is a lookup with no branch beyond the enum — one Theory-style
  `it.each`.

## Examples from project

- `Financial.Web/src/utils/__tests__/formatters.test.ts` — Unit; `Method_Scenario_Outcome`-style names.
- `Financial.Web/src/utils/__tests__/periodFilter.test.ts`, `priceHistoryChartData.test.ts` — Unit.
- `Financial.Web/src/context/__tests__/ColourModeContext.test.tsx`, `SelectedNodeContext.test.tsx` — Unit with probe components.
- `Financial.Web/src/navigation/__tests__/routes.test.ts` — Unit; sidebar ↔ router agreement.
