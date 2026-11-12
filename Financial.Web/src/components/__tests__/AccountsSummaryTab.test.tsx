import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountGroupSummaryDto, CategoryTotalDto, TitheSummaryDto } from '../../api/types'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import AccountsSummaryTab from '../AccountsSummaryTab'

const {
  getCategoryTotalsByMonthMock,
  getDefaultGroupSummaryMock,
  getTitheSummaryByMonthMock,
  updateTitheCarryForwardMock,
} = vi.hoisted(() => ({
  getCategoryTotalsByMonthMock: vi.fn<FinancialApiClient['getCategoryTotalsByMonth']>(),
  getDefaultGroupSummaryMock: vi.fn<FinancialApiClient['getDefaultGroupSummary']>(),
  getTitheSummaryByMonthMock: vi.fn<FinancialApiClient['getTitheSummaryByMonth']>(),
  updateTitheCarryForwardMock: vi.fn<FinancialApiClient['updateTitheCarryForward']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getCategoryTotalsByMonth: getCategoryTotalsByMonthMock,
    getDefaultGroupSummary: getDefaultGroupSummaryMock,
    getTitheSummaryByMonth: getTitheSummaryByMonthMock,
    updateTitheCarryForward: updateTitheCarryForwardMock,
  } as Partial<FinancialApiClient>,
}))

const AC_03 = '[AC P61-F01-accounts-overview-tabs-and-monthly-fold-03]'
const AC_07 = '[AC P61-F01-accounts-overview-tabs-and-monthly-fold-07]'

const CATEGORY_TOTALS: CategoryTotalDto[] = [
  { category: 'Groceries', totalValue: 412.37 },
  { category: 'Rent', totalValue: 2427.73 },
]

const SUMMARY: AccountGroupSummaryDto = {
  year: 2026,
  month: 10,
  members: [],
  totals: [{ currency: 'GBP', balance: 51.3, roundUpTotal: 0.6, cardOutstandingTotal: 100 }],
  categoryTotalsSum: 2840.1,
  incomeTotals: [
    { incomeSourceId: '1', name: 'Salary', netValue: 4100, grossValue: 5200 },
    { incomeSourceId: '2', name: 'Dividendo/Juros', netValue: 85.2, grossValue: null },
  ],
  totalIncoming: 4185.2,
}

const TITHE_SUMMARY: TitheSummaryDto = {
  calculatedTithe: 418.52,
  titheBalance: 368.52,
  carryForward: { amount: 50, included: true, fromYear: 2026, fromMonth: 9 },
}

function result() {
  return screen.getByRole('group', { name: 'Month result' })
}

describe('AccountsSummaryTab', () => {
  beforeEach(() => {
    getCategoryTotalsByMonthMock.mockReset().mockResolvedValue(CATEGORY_TOTALS)
    getDefaultGroupSummaryMock.mockReset().mockResolvedValue(SUMMARY)
    getTitheSummaryByMonthMock.mockReset().mockResolvedValue(TITHE_SUMMARY)
    updateTitheCarryForwardMock.mockReset()
  })

  it(`${AC_03} shows Category totals, Income by source with Total incoming, then the Result as a Surplus`, async () => {
    render(<AccountsSummaryTab year={2026} month={10} />)

    const categories = await screen.findByRole('table', { name: 'Category totals' })
    expect(within(categories).getByText('Groceries')).toBeInTheDocument()
    expect(screen.getByRole('group', { name: 'Totals for categories' })).toHaveTextContent('Total: 2,840.10')
    const income = screen.getByRole('table', { name: 'Income totals' })
    expect(within(income).getByText('Salary')).toBeInTheDocument()
    expect(within(income).getByText('Dividendo/Juros')).toBeInTheDocument()
    expect(screen.getByRole('group', { name: 'Totals for income' })).toHaveTextContent('Total incoming: 4,185.20')
    expect(result()).toHaveTextContent('Result: +1,345.10 Surplus')
    expect(income.compareDocumentPosition(result()) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(getDefaultGroupSummaryMock).toHaveBeenCalledWith(2026, 10)
  })

  it(`${AC_03} shows a Deficit when expenses exceed income`, async () => {
    getDefaultGroupSummaryMock.mockResolvedValue({ ...SUMMARY, totalIncoming: 2000 })

    render(<AccountsSummaryTab year={2026} month={10} />)

    await screen.findByRole('table', { name: 'Category totals' })
    expect(result()).toHaveTextContent('Result: -840.10 Deficit')
  })

  it(`${AC_03} shows no bank or card grid`, async () => {
    render(<AccountsSummaryTab year={2026} month={10} />)

    await screen.findByRole('table', { name: 'Category totals' })
    expect(screen.getAllByRole('table').map((table) => table.getAttribute('aria-label'))).toEqual(['Category totals', 'Income totals'])
    expect(screen.queryByText(/Bank balance:|Combined adjustment figure/)).not.toBeInTheDocument()
  })

  it(`${AC_03} keeps the tithe lines and the carry-forward checkbox, and the checkbox updates the month`, async () => {
    updateTitheCarryForwardMock.mockResolvedValue({ ...TITHE_SUMMARY, titheBalance: 418.52, carryForward: { amount: 50, included: false, fromYear: 2026, fromMonth: 9 } })
    render(<AccountsSummaryTab year={2026} month={10} />)

    const totals = await screen.findByRole('group', { name: 'Totals for income' })
    expect(totals).toHaveTextContent('Calculated tithe: 418.52')
    expect(totals).toHaveTextContent('Tithe balance: 368.52')
    fireEvent.click(screen.getByRole('checkbox', { name: /^50\.00 from/ }))

    await waitFor(() => expect(screen.getByRole('checkbox', { name: /^50\.00 from/ })).not.toBeChecked())
    expect(updateTitheCarryForwardMock).toHaveBeenCalledWith(2026, 10, { included: false })
    expect(totals).toHaveTextContent('Tithe balance: 418.52')
    expect(screen.getByRole('table', { name: 'Category totals' })).toBeInTheDocument()
  })

  it('shows a refused carry-forward toggle as an alert and keeps the grids', async () => {
    updateTitheCarryForwardMock.mockRejectedValue(new Error('No carry-forward is available for this month.'))
    render(<AccountsSummaryTab year={2026} month={10} />)

    fireEvent.click(await screen.findByRole('checkbox', { name: /^50\.00 from/ }))

    expect(await screen.findByRole('alert')).toHaveTextContent('No carry-forward is available for this month.')
    expect(screen.getByRole('table', { name: 'Category totals' })).toBeInTheDocument()
  })

  it(`${AC_07} shows one loading state and no grid while the summary is pending`, () => {
    getDefaultGroupSummaryMock.mockReturnValue(new Promise(() => {}))

    render(<AccountsSummaryTab year={2026} month={10} />)

    expect(screen.getAllByText('Loading…')).toHaveLength(1)
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it(`${AC_07} shows each section's empty message and a Balanced result for a month with nothing recorded`, async () => {
    getCategoryTotalsByMonthMock.mockResolvedValue([])
    getDefaultGroupSummaryMock.mockResolvedValue({ ...SUMMARY, categoryTotalsSum: 0, incomeTotals: [], totalIncoming: 0 })

    render(<AccountsSummaryTab year={2026} month={10} />)

    expect(await screen.findByText('No expenses recorded for this month.')).toBeInTheDocument()
    expect(screen.getByText('No income recorded for this month.')).toBeInTheDocument()
    expect(result()).toHaveTextContent('Result: 0.00 Balanced')
  })

  it(`${AC_07} shows an error with Try again and no partial grid, and Try again reloads`, async () => {
    getCategoryTotalsByMonthMock.mockRejectedValueOnce(new Error('boom'))

    render(<AccountsSummaryTab year={2026} month={10} />)

    expect(await screen.findByText('Unable to load the summary.')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('table', { name: 'Category totals' })).toBeInTheDocument()
    expect(getCategoryTotalsByMonthMock).toHaveBeenCalledTimes(2)
  })
})
