> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# React Pages (`*Page.tsx` in `Financial.Web/src/pages/`)

Examples: `MonthlyPage`, `AnnualSummaryPage`, `ControleMaePage`, `ReservaPage`, `ActiveInvestmentsPage`, `HistoricInvestmentsPage`, `CurrentValuesPage`, `InvestmentSnapshotsPage`, `MensaisPage`, `DividendCheckPage`, `RootRedirect`.

## What to test

- **Data display**: after the mocked API resolves, expected labels and values appear in the DOM
- **Loading state**: if a loading indicator is rendered before the API resolves, verify its presence
- **Error state**: when the mocked API rejects, the error message appears
- **User interactions**: form submission calls the API mock with the correct arguments
- **Navigation links**: links have the correct `href` attribute
- **State changes after mutation**: after add/update/delete, the updated content is visible
- Page-level branching (e.g., `RootRedirect` choosing a redirect target based on a condition)

## Layer assignment

**Component test (this project's unit-test equivalent)** via Vitest + React Testing Library + `jsdom`. Render the page in a `MemoryRouter`, mock the API client factory at the module boundary, assert user-visible output via `screen` queries. Not E2E — no real browser, no real HTTP.

## Setup pattern

```typescript
// Declare mocks BEFORE vi.mock() call (vi.mock is hoisted — see references/gotchas.md)
const getDataMock = vi.fn<FinancialApiClient['getRelevantData']>()
const submitMock = vi.fn<FinancialApiClient['submitData']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getRelevantData: getDataMock,
    submitData: submitMock,
    // include only methods this page actually calls
  }),
}))

describe('SomePage', () => {
  beforeEach(() => {
    getDataMock.mockReset()   // resets call count AND implementation
    submitMock.mockReset()
  })

  it('displays data after API resolves', async () => {
    getDataMock.mockResolvedValue(mockData satisfies SomeDto)

    render(
      <MemoryRouter initialEntries={['/path/to/page']}>
        <Routes>
          <Route path="/path/to/page" element={<SomePage />} />
        </Routes>
      </MemoryRouter>
    )

    // Use findBy* (async) for content that appears after API resolves
    expect(await screen.findByText('Expected Label')).toBeInTheDocument()
  })

  it('displays error when API rejects', async () => {
    getDataMock.mockRejectedValue(new Error('network error'))

    render(/* same MemoryRouter setup */)

    expect(await screen.findByText(/error/i)).toBeInTheDocument()
  })

  it('submits form with correct arguments', async () => {
    getDataMock.mockResolvedValue(mockData)
    submitMock.mockResolvedValue(updatedData)

    render(/* MemoryRouter */)
    await screen.findByText('Expected Label') // wait for page to load

    fireEvent.change(screen.getByRole('spinbutton', { name: /quantity/i }), { target: { value: '5' } })
    fireEvent.click(screen.getByRole('button', { name: /save/i }))

    await waitFor(() => {
      expect(submitMock).toHaveBeenCalledWith(expect.objectContaining({ quantity: 5 }))
    })
  })
})
```

**Mock data pattern** — use `satisfies` for type safety: `const mockData = { name: 'BCIA11', ... } satisfies AssetDetailsDto`.

## When to skip

- Recharts chart rendering details (SVG structure, axis label positions) — assert on data-derived text instead, or skip if the page just forwards data to an already-tested chart component
- URL path matching behavior (router's responsibility — test the `href` attribute value instead)

## Examples from project

| Page | Key test scenarios |
|---|---|
| `MonthlyPage` | Expense/income data displayed after resolve; loading/error states |
| `ControleMaePage` | Ledger entries displayed; currency conversion values shown |
| `ReservaPage` | Reserve bucket balances displayed per bucket |
| `DividendCheckPage` | Dividend check results displayed |
