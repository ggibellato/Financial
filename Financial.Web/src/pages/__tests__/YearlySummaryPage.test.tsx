import { render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import YearlySummaryPage from '../YearlySummaryPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CategoryYearlyTotalDto, IncomeYearlySummaryDto, InvestmentDiffsYearlyDto } from '../../api/types'

const getCategoryTotalsForYearMock = vi.fn<FinancialApiClient['getCategoryTotalsForYear']>()
const getInvestmentDiffsForYearMock = vi.fn<FinancialApiClient['getInvestmentDiffsForYear']>()
const getIncomeSummaryForYearMock = vi.fn<FinancialApiClient['getIncomeSummaryForYear']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getCategoryTotalsForYear: getCategoryTotalsForYearMock,
    getInvestmentDiffsForYear: getInvestmentDiffsForYearMock,
    getIncomeSummaryForYear: getIncomeSummaryForYearMock,
  }),
}))

const CATEGORY_TOTALS: CategoryYearlyTotalDto[] = [
  {
    category: 'Mercado',
    monthlyTotals: [100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200, 210],
    yearlyTotal: 1860,
  },
]

const INVESTMENT_DIFFS: InvestmentDiffsYearlyDto = {
  accounts: [
    {
      account: 'ChaseSave',
      isLiability: false,
      monthlyValues: [1000, 1050, 1100, 1150, 1200, 1250, 1300, 1350, 1400, 1450, 1500, 1550],
      monthlyDiffs: [50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50],
    },
    {
      account: 'PlatinumVisa8003',
      isLiability: true,
      monthlyValues: [200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200],
      monthlyDiffs: [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
    },
  ],
  netPosition: {
    monthlyValues: [800, 850, 900, 950, 1000, 1050, 1100, 1150, 1200, 1250, 1300, 1350],
    monthlyDiffs: [50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50],
    fullYearNetChange: 550,
  },
}

const INCOME_SUMMARY: IncomeYearlySummaryDto = {
  salaryMonthly: [3200, 3200, 3200, 3200, 3200, 3200, 3200, 3200, 3200, 3200, 3200, 3600],
  salaryYearlyTotal: 38800,
  salaryAfterTaxesMonthly: [2450, 2450, 2450, 2450, 2450, 2450, 2450, 2450, 2450, 2450, 2450, 2650],
  salaryAfterTaxesYearlyTotal: 29350,
  taxDifferenceMonthly: [750, 750, 750, 750, 750, 750, 750, 750, 750, 750, 750, 950],
  taxDifferenceYearlyTotal: 9450,
  dividendoJurosMonthly: [0, 0, 15.5, 0, 0, 0, 0, 0, 0, 0, 0, 4.5],
  dividendoJurosYearlyTotal: 20,
}

describe('YearlySummaryPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getCategoryTotalsForYearMock.mockResolvedValue(CATEGORY_TOTALS)
    getInvestmentDiffsForYearMock.mockResolvedValue(INVESTMENT_DIFFS)
    getIncomeSummaryForYearMock.mockResolvedValue(INCOME_SUMMARY)
  })

  it('shows a loading state before data arrives', () => {
    render(<YearlySummaryPage />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error state with retry when the fetch fails', async () => {
    getCategoryTotalsForYearMock.mockRejectedValue(new Error('Network down'))

    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Network down')).toBeInTheDocument()
  })

  it('renders the category-totals table with monthly values and a yearly total column', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    expect(screen.getByRole('cell', { name: 'Mercado' })).toBeInTheDocument()
    expect(screen.getByText('1,860.00')).toBeInTheDocument()
  })

  it('renders the investment-diffs table with 11 monthly diff columns per account and the net position row', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Investment Diffs')).toBeInTheDocument())
    expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument()
    expect(screen.getByText('PlatinumVisa8003 (liability)')).toBeInTheDocument()
    expect(screen.getByText('Net Position')).toBeInTheDocument()
    expect(screen.getByText('550.00')).toBeInTheDocument()
  })

  it('renders the income-summary table with the header row and the four data rows', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Income Summary')).toBeInTheDocument())
    const incomeSection = screen.getByText('Income Summary').closest('section')!
    expect(within(incomeSection).getByText('Income')).toBeInTheDocument()
    expect(within(incomeSection).getByRole('cell', { name: 'Salary' })).toBeInTheDocument()
    expect(within(incomeSection).getByRole('cell', { name: 'Salary after taxes' })).toBeInTheDocument()
    expect(within(incomeSection).getByRole('cell', { name: 'Tax difference' })).toBeInTheDocument()
    expect(within(incomeSection).getByRole('cell', { name: 'Dividendo/Juros' })).toBeInTheDocument()
    expect(within(incomeSection).getByText('38,800.00')).toBeInTheDocument()
    expect(within(incomeSection).getByText('9,450.00')).toBeInTheDocument()
  })

  it('renders row 5 of the income-summary table blank, and shows no Lottery, tithe, or tithe-balance content anywhere', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Income Summary')).toBeInTheDocument())
    const incomeSection = screen.getByText('Income Summary').closest('section')!
    const rows = within(incomeSection).getAllByRole('row')
    const blankRow = rows[5]
    expect(blankRow.textContent).toBe('')

    expect(screen.queryByText(/Lottery/i)).not.toBeInTheDocument()
    expect(screen.queryByText(/Tithe/i)).not.toBeInTheDocument()
  })
})
