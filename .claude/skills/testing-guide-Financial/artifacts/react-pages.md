> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# React Pages (`*Page.tsx` in `Financial.Web/src/pages/`)

## What to test

- **Data display**: after mock API resolves, expected labels and values appear in the DOM
- **Loading state**: if a loading indicator is rendered before the API resolves, verify its presence
- **Error state**: when the mock API rejects, the error message appears
- **User interactions**: form submission calls the API mock with the correct arguments
- **Navigation links**: links have the correct `href` attribute
- **State changes after mutation**: after add/update/delete, the updated content is visible

## Layer assignment

**Component test (unit-level for TypeScript)** — render the page in a `MemoryRouter`, mock the API client factory at the module boundary, assert user-visible output via `screen` queries.

## Setup pattern

```typescript
// Declare mocks BEFORE vi.mock() call (vi.mock is hoisted — see references/gotchas.md)
const getDataMock = vi.fn()
const submitMock = vi.fn()

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
    expect(screen.getByText('Expected Value')).toBeInTheDocument()
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

    fireEvent.change(screen.getByRole('spinbutton', { name: /quantity/i }), {
      target: { value: '5' },
    })
    fireEvent.click(screen.getByRole('button', { name: /save/i }))

    await waitFor(() => {
      expect(submitMock).toHaveBeenCalledWith(
        expect.objectContaining({ quantity: 5 })
      )
    })
  })

  it('navigation link points to correct route', async () => {
    getDataMock.mockResolvedValue(mockData)
    render(/* MemoryRouter */)

    await screen.findByText('Expected Label')

    const link = screen.getByRole('link', { name: /broker name/i })
    expect(link).toHaveAttribute('href', '/expected/path')
  })
})
```

**Mock data pattern** — use `satisfies` for type safety:
```typescript
const mockData = {
  name: 'BCIA11',
  brokerName: 'XPI',
  portfolioName: 'Default',
  // ...
} satisfies AssetDetailsDto
```

## When to skip

- Recharts chart rendering details (SVG structure, axis label positions)
- URL path matching behavior (router's responsibility — test the `href` attribute value instead)

## Examples from project

| Page | Key test scenarios |
|---|---|
| `AssetDetailPage` | Asset details displayed, transaction add/update/delete, credit add/remove, form field values |
| `BrokersPage` | Broker list rendered, links to broker detail pages have correct href |
| `BrokerDetailPage` | Broker info and portfolio list displayed |
| `CreditsPage` | Credits list for broker/portfolio/asset displayed |
| `CurrentValuesPage` | Asset value rows displayed with correct amounts |
| `DividendCheckPage` | Dividend check results displayed |
| `NavigationTreePage` | Tree nodes rendered, selection triggers navigation |
