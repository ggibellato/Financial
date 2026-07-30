> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# React Hooks & Context (`use*.ts` in `Financial.Web/src/hooks/`, `SelectedNodeContext`)

Examples: `useAggregatedSummary`, `useAnnualSummary`, `useAssetSummary`, `useBrokerBreakdown`, `useControleMae`, and the rest of `src/hooks/`; `SelectedNodeContext` in `src/context/`.

This is a real gap in the previous version of this guide — 12 hooks exist with their own test files and were never covered as their own artifact type.

## What to test

- Each branch over the discriminated input (this project's hooks commonly branch on `SelectedNode['nodeType']` — `'Broker'` vs `'Portfolio'` vs `'Asset'` — calling a different API client method per branch)
- Loading state while the mocked API call is pending
- Error state when the mocked API call rejects
- Data shape returned once resolved

## Layer assignment

Hook test via `renderHook` (React Testing Library) + Vitest. Same API-client mock boundary as pages/components — mock `financialApiClient` at the module level, never the underlying `fetch`.

## Setup pattern

```tsx
const getSummaryByBrokerMock = vi.fn<FinancialApiClient['getSummaryByBroker']>()
const getSummaryByPortfolioMock = vi.fn<FinancialApiClient['getSummaryByPortfolio']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getSummaryByBroker: getSummaryByBrokerMock,
    getSummaryByPortfolio: getSummaryByPortfolioMock,
  }),
}))

describe('useAggregatedSummary', () => {
  beforeEach(() => {
    getSummaryByBrokerMock.mockReset()
    getSummaryByPortfolioMock.mockReset()
  })

  it('calls getSummaryByBroker when the selected node is a Broker', async () => {
    getSummaryByBrokerMock.mockResolvedValue(SUMMARY_DTO)

    const { result } = renderHook(() => useAggregatedSummary(BROKER_NODE), {
      wrapper: createSelectedNodeWrapper(BROKER_NODE),
    })

    await waitFor(() => expect(result.current.loading).toBe(false))
    expect(result.current.data).toEqual(SUMMARY_DTO)
    expect(getSummaryByPortfolioMock).not.toHaveBeenCalled()
  })
})
```

`renderHook` does not wait for effects to settle on its own — always follow with `waitFor` (or `await screen.findBy...` if the hook is exercised through a component) before asserting on post-fetch state. Don't destructure `result.current` at the top of the test if you need to re-read it after a state update — re-read `result.current` fresh each time so you get the latest render.

## When to skip

- Don't re-test the API client's own request-building logic — that's `artifacts/api-client.md`'s job; the hook test only proves it calls the *right method for the right branch*
- A hook with no branching and no async boundary (a trivial `useState` wrapper) needs no dedicated test

## Examples from project

- `useAggregatedSummary.test.ts` — the canonical branching-over-`SelectedNode`-type example
- `SelectedNodeContext.test.tsx` — context provider test; same `renderHook`/RTL approach, testing that consumers receive the expected value and update on provider changes
