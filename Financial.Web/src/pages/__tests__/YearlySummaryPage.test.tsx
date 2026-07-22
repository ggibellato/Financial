import { render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import YearlySummaryPage from '../YearlySummaryPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CategoryYearlyTotalDto, InvestmentDiffsYearlyDto } from '../../api/types'

const getCategoryTotalsForYearMock = vi.fn<FinancialApiClient['getCategoryTotalsForYear']>()
const getInvestmentDiffsForYearMock = vi.fn<FinancialApiClient['getInvestmentDiffsForYear']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getCategoryTotalsForYear: getCategoryTotalsForYearMock,
    getInvestmentDiffsForYear: getInvestmentDiffsForYearMock,
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

describe('YearlySummaryPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getCategoryTotalsForYearMock.mockResolvedValue(CATEGORY_TOTALS)
    getInvestmentDiffsForYearMock.mockResolvedValue(INVESTMENT_DIFFS)
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
})
