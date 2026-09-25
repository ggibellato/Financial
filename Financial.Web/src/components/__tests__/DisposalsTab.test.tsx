import { fireEvent, render, screen, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { DisposalsData } from '../../hooks/useDisposals'
import type { DisposalRecordDto } from '../../api/types'
import DisposalsTab from '../DisposalsTab'

const mockRetry = vi.fn()
const mockSetTaxYear = vi.fn()
const mockToggleExpanded = vi.fn()

function disposal(overrides: Partial<DisposalRecordDto>): DisposalRecordDto {
  return {
    id: 'd1',
    transactionId: 't1',
    date: '2025-11-03T00:00:00',
    method: 'AverageCost',
    lotsConsumed: [{ sourceTransactionId: null, quantity: 5, unitCost: 100 }],
    quantityDisposed: 5,
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

const DEFAULT_HOOK: DisposalsData = {
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

let mockHookValue: DisposalsData = { ...DEFAULT_HOOK }

vi.mock('../../hooks/useDisposals', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../hooks/useDisposals')>()
  return {
    ...actual,
    useDisposals: () => mockHookValue,
  }
})

function setMock(overrides: Partial<DisposalsData>) {
  mockHookValue = { ...DEFAULT_HOOK, ...overrides }
}

describe('DisposalsTab', () => {
  beforeEach(() => {
    mockRetry.mockReset()
    mockSetTaxYear.mockReset()
    mockToggleExpanded.mockReset()
    mockHookValue = { ...DEFAULT_HOOK }
  })

  it('shows a loading state', () => {
    setMock({ isLoading: true })
    render(<DisposalsTab />)
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error state with retry', () => {
    setMock({ error: 'Network down' })
    render(<DisposalsTab />)
    expect(screen.getByRole('alert')).toHaveTextContent('Network down')
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(mockRetry).toHaveBeenCalledTimes(1)
  })

  it('shows the initial empty message when nothing has ever been sold', () => {
    setMock({ chains: [], filteredChains: [] })
    render(<DisposalsTab />)
    expect(screen.getByText(/nothing has been sold from this holding/)).toBeInTheDocument()
  })

  it('lists active disposals with their fields', () => {
    const chain = { active: disposal({}), history: [] }
    setMock({ chains: [chain], filteredChains: [chain], taxYearOptions: ['2025/26'] })
    render(<DisposalsTab />)

    expect(screen.getByText('Average Cost')).toBeInTheDocument()
    expect(screen.getByText('600.00')).toBeInTheDocument()
    expect(screen.getByText('500.00')).toBeInTheDocument()
    expect(screen.getByText('100.00')).toBeInTheDocument()
    expect(within(document.querySelector('tbody')!).getByText('2025/26')).toBeInTheDocument()
  })

  it('shows the tax-year filter and calls setTaxYear on selection', () => {
    const chain = { active: disposal({}), history: [] }
    setMock({ chains: [chain], filteredChains: [chain], taxYearOptions: ['2025/26', '2024/25'] })
    render(<DisposalsTab />)

    fireEvent.click(screen.getByRole('tab', { name: '2024/25' }))
    expect(mockSetTaxYear).toHaveBeenCalledWith('2024/25')
  })

  it('shows an explicit empty message when the selected tax year has no disposals', () => {
    setMock({
      chains: [{ active: disposal({}), history: [] }],
      filteredChains: [],
      taxYearOptions: ['2025/26'],
      selectedTaxYear: '2024/25',
    })
    render(<DisposalsTab />)
    expect(screen.getByText('No disposals in 2024/25.')).toBeInTheDocument()
  })

  it('does not show an expand toggle for a disposal with no superseded history', () => {
    const chain = { active: disposal({ id: 'd1' }), history: [] }
    setMock({ chains: [chain], filteredChains: [chain], taxYearOptions: ['2025/26'] })
    render(<DisposalsTab />)
    expect(screen.queryByRole('button', { name: /audit trail/ })).not.toBeInTheDocument()
  })

  it('defaults to sorting by date descending, with the header showing the active sort', () => {
    const older = { active: disposal({ id: 'd1', transactionId: 't1', date: '2025-01-05T00:00:00' }), history: [] }
    const newer = { active: disposal({ id: 'd2', transactionId: 't2', date: '2025-11-03T00:00:00' }), history: [] }
    setMock({
      chains: [older, newer],
      filteredChains: [older, newer],
      taxYearOptions: ['2025/26'],
    })
    render(<DisposalsTab />)
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

  it('expands and collapses the audit trail for a disposal with superseded history', () => {
    const predecessor = disposal({
      id: 'd0',
      method: 'AverageCost',
      proceeds: 550,
      costBasis: 500,
      gainLoss: 50,
      createdAt: '2025-10-01T09:00:00Z',
    })
    const active = disposal({ id: 'd1' })
    const chain = { active, history: [predecessor] }
    setMock({ chains: [chain], filteredChains: [chain], taxYearOptions: ['2025/26'] })

    const { rerender } = render(<DisposalsTab />)
    const toggle = screen.getByRole('button', { name: /Show audit trail/ })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')

    fireEvent.click(toggle)
    expect(mockToggleExpanded).toHaveBeenCalledWith('d1')

    setMock({ chains: [chain], filteredChains: [chain], taxYearOptions: ['2025/26'], expandedIds: new Set(['d1']) })
    rerender(<DisposalsTab />)

    expect(screen.getByRole('button', { name: /Hide audit trail/ })).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText(/Audit trail for the/)).toBeInTheDocument()
    expect(screen.getByText('50.00')).toBeInTheDocument()
  })
})
