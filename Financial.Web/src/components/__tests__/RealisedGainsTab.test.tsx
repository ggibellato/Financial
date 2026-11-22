import { fireEvent, render, screen, within } from '../../test/renderWithFluent'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { RealisedGainsData } from '../../hooks/useRealisedGains'
import type { RealisedGainDto } from '../../api/types'
import RealisedGainsTab from '../RealisedGainsTab'

const mockRetry = vi.fn()
const mockSetTaxYear = vi.fn()
const mockToggleExpanded = vi.fn()

function realisedGain(overrides: Partial<RealisedGainDto>): RealisedGainDto {
  return {
    id: 'g1',
    transactionId: 't1',
    date: '2025-11-03T00:00:00',
    method: 'AverageCost',
    lotsConsumed: [{ sourceTransactionId: null, quantity: 5, unitCost: 100 }],
    quantitySold: 5,
    proceeds: 600,
    costBasis: 500,
    gainLoss: 100,
    currency: 'GBP',
    taxYear: '2025/26',
    status: 'Active',
    supersededByRecordId: null,
    createdAt: '2025-11-03T09:00:00Z',
    ...overrides,
  }
}

const DEFAULT_HOOK: RealisedGainsData = {
  chains: [],
  filteredChains: [],
  taxYearOptions: [],
  isLoading: false,
  error: null,
  retry: mockRetry,
  selectedTaxYear: 'All years',
  setTaxYear: mockSetTaxYear,
  expandedIds: new Set(),
  toggleExpanded: mockToggleExpanded,
}

let mockHookValue: RealisedGainsData = { ...DEFAULT_HOOK }

vi.mock('../../hooks/useRealisedGains', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../hooks/useRealisedGains')>()
  return {
    ...actual,
    useRealisedGains: () => mockHookValue,
  }
})

function setMock(overrides: Partial<RealisedGainsData>) {
  mockHookValue = { ...DEFAULT_HOOK, ...overrides }
}

describe('RealisedGainsTab', () => {
  beforeEach(() => {
    mockRetry.mockReset()
    mockSetTaxYear.mockReset()
    mockToggleExpanded.mockReset()
    mockHookValue = { ...DEFAULT_HOOK }
  })

  it('shows a loading state', () => {
    setMock({ isLoading: true })
    render(<RealisedGainsTab />)
    expect(screen.getByText('Loading…')).toBeInTheDocument()
  })

  it('shows an error state with retry', () => {
    setMock({ error: 'Network down' })
    render(<RealisedGainsTab />)
    expect(screen.getByRole('alert')).toHaveTextContent('Network down')
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(mockRetry).toHaveBeenCalledTimes(1)
  })

  it('shows the initial empty message when nothing has ever been sold', () => {
    setMock({ chains: [], filteredChains: [] })
    render(<RealisedGainsTab />)
    expect(screen.getByText(/nothing has been sold from this holding/)).toBeInTheDocument()
  })

  it('[AC P50-F04-react-disposals-and-cost-basis-01] lists active realised gains with their fields', () => {
    const chain = { active: realisedGain({}), history: [] }
    setMock({ chains: [chain], filteredChains: [chain], taxYearOptions: ['2025/26'] })
    render(<RealisedGainsTab />)

    expect(screen.getByText('03/11/2025')).toBeInTheDocument()
    expect(screen.getByText('5.00000000')).toBeInTheDocument()
    expect(screen.getByText('Average cost')).toBeInTheDocument()
    expect(screen.getByText('600.00')).toBeInTheDocument()
    expect(screen.getByText('500.00')).toBeInTheDocument()
    expect(screen.getByText('100.00')).toBeInTheDocument()
    expect(within(document.querySelector('tbody')!).getByText('2025/26')).toBeInTheDocument()
  })

  it('shows the tax-year filter and calls setTaxYear on selection', () => {
    const chain = { active: realisedGain({}), history: [] }
    setMock({ chains: [chain], filteredChains: [chain], taxYearOptions: ['2025/26', '2024/25'] })
    render(<RealisedGainsTab />)

    fireEvent.click(screen.getByRole('tab', { name: '2024/25' }))
    expect(mockSetTaxYear).toHaveBeenCalledWith('2024/25')
  })

  it('[AC P50-F04-react-disposals-and-cost-basis-02] shows an explicit empty message when the selected tax year has no realised gains', () => {
    setMock({
      chains: [{ active: realisedGain({}), history: [] }],
      filteredChains: [],
      taxYearOptions: ['2025/26'],
      selectedTaxYear: '2024/25',
    })
    render(<RealisedGainsTab />)
    expect(screen.getByText('No realised gains in 2024/25.')).toBeInTheDocument()
  })

  it('does not show an expand toggle for a realised gain with no superseded history', () => {
    const chain = { active: realisedGain({ id: 'g1' }), history: [] }
    setMock({ chains: [chain], filteredChains: [chain], taxYearOptions: ['2025/26'] })
    render(<RealisedGainsTab />)
    expect(screen.queryByRole('button', { name: /audit trail/ })).not.toBeInTheDocument()
  })

  it('[AC P50-F04-react-disposals-and-cost-basis-01] defaults to sorting by date descending, with the header showing the active sort', () => {
    const older = { active: realisedGain({ id: 'g1', transactionId: 't1', date: '2025-01-05T00:00:00' }), history: [] }
    const newer = { active: realisedGain({ id: 'g2', transactionId: 't2', date: '2025-11-03T00:00:00' }), history: [] }
    setMock({
      chains: [older, newer],
      filteredChains: [older, newer],
      taxYearOptions: ['2025/26'],
    })
    render(<RealisedGainsTab />)
    const table = screen.getByRole('table')
    const dateHeaderButton = within(table).getByRole('button', { name: 'Date' })
    const dateHeader = dateHeaderButton.closest('th')

    expect(dateHeader).toHaveAttribute('aria-sort', 'descending')
    let dataRows = within(table).getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('03/11/2025')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('05/01/2025')).toBeInTheDocument()

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'none')

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'ascending')
    dataRows = within(table).getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('05/01/2025')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('03/11/2025')).toBeInTheDocument()

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'descending')
  })

  it('[AC P50-F04-react-disposals-and-cost-basis-03] expands and collapses the audit trail for a realised gain with superseded history', () => {
    const predecessor = realisedGain({
      id: 'g0',
      method: 'AverageCost',
      proceeds: 550,
      costBasis: 500,
      gainLoss: 50,
      createdAt: '2025-10-01T09:00:00Z',
    })
    const active = realisedGain({ id: 'g1' })
    const chain = { active, history: [predecessor] }
    setMock({ chains: [chain], filteredChains: [chain], taxYearOptions: ['2025/26'] })

    const { rerender } = render(<RealisedGainsTab />)
    const toggle = screen.getByRole('button', { name: /Show audit trail/ })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')

    fireEvent.click(toggle)
    expect(mockToggleExpanded).toHaveBeenCalledWith('g1')

    setMock({ chains: [chain], filteredChains: [chain], taxYearOptions: ['2025/26'], expandedIds: new Set(['g1']) })
    rerender(<RealisedGainsTab />)

    expect(screen.getByRole('button', { name: /Hide audit trail/ })).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText(/Audit trail for the/)).toBeInTheDocument()
    expect(screen.getByText('50.00')).toBeInTheDocument()
    const history = screen.getByRole('table', { name: 'Audit trail for the 03/11/2025 sale' })
    const panelId = screen.getByRole('button', { name: /Hide audit trail/ }).getAttribute('aria-controls')!
    expect(document.getElementById(panelId)).toContainElement(history)
  })

  it('shows no audit trail for a realised gain marked expanded that has no history', () => {
    const chain = { active: realisedGain({ id: 'g1' }), history: [] }
    setMock({ chains: [chain], filteredChains: [chain], taxYearOptions: ['2025/26'], expandedIds: new Set(['g1']) })

    render(<RealisedGainsTab />)

    expect(screen.queryByText(/Audit trail for the/)).not.toBeInTheDocument()
  })

  it('colours a gain green and a loss red, in the sale and its audit trail', () => {
    const gain = { active: realisedGain({ id: 'g1', gainLoss: 100 }), history: [realisedGain({ id: 'g0', gainLoss: -40 })] }
    const loss = { active: realisedGain({ id: 'g2', transactionId: 't2', date: '2025-10-01T00:00:00', gainLoss: -25 }), history: [] }
    setMock({ chains: [gain, loss], filteredChains: [gain, loss], taxYearOptions: ['2025/26'], expandedIds: new Set(['g1']) })

    render(<RealisedGainsTab />)

    expect(screen.getByText('100.00').closest('td')).toHaveClass('realised-gains-tab__value--green')
    expect(screen.getByText('-40.00').closest('td')).toHaveClass('realised-gains-tab__value--red')
    expect(screen.getByText('-25.00').closest('td')).toHaveClass('realised-gains-tab__value--red')
  })

  it.each(['Quantity', 'Method', 'Proceeds', 'Cost basis', 'Gain/loss', 'Tax year'])('marks the %s header when it sorts the sales', (name) => {
    const chain = { active: realisedGain({}), history: [] }
    setMock({ chains: [chain], filteredChains: [chain], taxYearOptions: ['2025/26'] })
    render(<RealisedGainsTab />)

    fireEvent.click(screen.getByRole('button', { name }))

    expect(screen.getByRole('button', { name }).closest('th')).toHaveAttribute('aria-sort', 'ascending')
  })
})
