import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
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

  it('shows the error/retry state regardless of the active tab', async () => {
    getCategoryTotalsForYearMock.mockRejectedValue(new Error('Network down'))

    render(<YearlySummaryPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Network down')).toBeInTheDocument()
  })

  it('defaults to the Category Totals tab on load', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    expect(screen.getByRole('cell', { name: 'Mercado' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Salary' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Investments' })).not.toBeInTheDocument()
  })

  it('marks Category Totals as the active tab button by default', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    expect(screen.getByRole('button', { name: 'Category Totals' })).toHaveClass('yearly-summary-page__tab--active')
    expect(screen.getByRole('button', { name: 'Investments' })).not.toHaveClass('yearly-summary-page__tab--active')
  })

  it('renders the combined table in the fixed row order', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    const rowLabels = screen
      .getAllByRole('row')
      .map((row) => row.querySelector('td')?.textContent ?? '')
      .filter((label) => label !== '')

    expect(rowLabels).toEqual([
      'Salary',
      'Salary after taxes',
      'Tax difference',
      'Dividendo/Juros',
      'Mercado',
      'Resultado (R-D-Inv)',
      'Total despesas',
    ])
  })

  it('renders Salary/Dividendo rows and category rows in the same table (no standalone Income Summary section)', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    expect(screen.queryByText('Income Summary')).not.toBeInTheDocument()

    const salaryCell = screen.getByRole('cell', { name: 'Salary' })
    const mercadoCell = screen.getByRole('cell', { name: 'Mercado' })
    expect(salaryCell.closest('table')).toBe(mercadoCell.closest('table'))
    expect(within(salaryCell.closest('tr')!).getByText('38,800.00')).toBeInTheDocument()
    const taxDifferenceRow = screen.getByRole('cell', { name: 'Tax difference' }).closest('tr')!
    expect(within(taxDifferenceRow).getByText('9,450.00')).toBeInTheDocument()
  })

  it('shows an Average column between the Dec and Yearly Total columns for every row', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    const headerCells = screen.getAllByRole('columnheader').map((th) => th.textContent)
    const decIndex = headerCells.indexOf('Dec')
    const averageIndex = headerCells.indexOf('Average')
    const yearlyTotalIndex = headerCells.indexOf('Yearly Total')
    expect(averageIndex).toBe(decIndex + 1)
    expect(yearlyTotalIndex).toBe(averageIndex + 1)

    // Mercado average = 1860 / 12 = 155.00
    const mercadoRow = screen.getByRole('cell', { name: 'Mercado' }).closest('tr')!
    expect(within(mercadoRow).getByText('155.00')).toBeInTheDocument()
  })

  it('computes Resultado and Total despesas from already-fetched data and renders them with emphasized styling', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    // Total despesas yearly total = sum of Mercado's monthly totals = 1,860.00
    const totalDespesasRow = screen.getByRole('cell', { name: 'Total despesas' }).closest('tr')!
    expect(totalDespesasRow).toHaveClass('yearly-summary-page__emphasized-row')
    expect(within(totalDespesasRow).getByText('1,860.00')).toBeInTheDocument()

    // Resultado yearly total = sum(salaryAfterTaxesMonthly)=29,600 + sum(dividendoJurosMonthly)=20 - totalDespesasYearlyTotal=1,860 + 0 (no Investimento category) = 27,760
    const resultadoRow = screen.getByRole('cell', { name: 'Resultado (R-D-Inv)' }).closest('tr')!
    expect(resultadoRow).toHaveClass('yearly-summary-page__emphasized-row')
    expect(within(resultadoRow).getByText('27,760.00')).toBeInTheDocument()
  })

  it('shows only the Investments tab content after clicking Investments', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))

    expect(screen.queryByRole('cell', { name: 'Salary' })).not.toBeInTheDocument()
    expect(screen.queryByRole('cell', { name: 'Mercado' })).not.toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Investments' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Investments' })).toHaveClass('yearly-summary-page__tab--active')
  })

  it('shows all 12 monthly balance values for each account', async () => {
    render(<YearlySummaryPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument())

    const chaseSaveRow = screen.getByRole('cell', { name: 'ChaseSave' }).closest('tr')!
    for (const value of INVESTMENT_DIFFS.accounts[0].monthlyValues) {
      expect(within(chaseSaveRow).getByText(value.toLocaleString(undefined, { minimumFractionDigits: 2 }))).toBeInTheDocument()
    }
  })

  it('marks liability accounts with a (-) suffix and asset accounts without one', async () => {
    render(<YearlySummaryPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByText('PlatinumVisa8003 (-)')).toBeInTheDocument())
    expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument()
  })

  it('renders a Total row matching the net position monthly values', async () => {
    render(<YearlySummaryPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'Total' })).toBeInTheDocument())

    const totalRow = screen.getByRole('cell', { name: 'Total' }).closest('tr')!
    expect(totalRow).toHaveClass('yearly-summary-page__emphasized-row')
    expect(within(totalRow).getByText('800.00')).toBeInTheDocument()
    expect(within(totalRow).getByText('1,350.00')).toBeInTheDocument()
  })

  it('renders a Month Result row with a blank January cell and the net position diffs for Feb-Dec', async () => {
    render(<YearlySummaryPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'Month Result' })).toBeInTheDocument())

    const monthResultRow = screen.getByRole('cell', { name: 'Month Result' }).closest('tr')!
    expect(monthResultRow).toHaveClass('yearly-summary-page__emphasized-row')
    const cells = within(monthResultRow).getAllByRole('cell')
    // cells[0] is the label; cells[1] is January, which must be blank
    expect(cells[1].textContent).toBe('')
    expect(within(monthResultRow).getAllByText('50.00').length).toBe(11)
  })

  it('shows Year Progress, Average Month Result, and Sum of Month Results, with Sum equal to Year Progress', async () => {
    render(<YearlySummaryPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByText('Year Progress')).toBeInTheDocument())

    const yearProgress = screen.getByText('Year Progress').closest('div')!
    expect(within(yearProgress).getByText('550.00')).toBeInTheDocument()

    const averageMonthResult = screen.getByText('Average Month Result').closest('div')!
    expect(within(averageMonthResult).getByText('50.00')).toBeInTheDocument()

    const sumOfMonthResults = screen.getByText('Sum of Month Results').closest('div')!
    expect(within(sumOfMonthResults).getByText('550.00')).toBeInTheDocument()
  })

  it('does not affect the Category Totals tab content when viewing Investments', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'Mercado' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Category Totals' }))

    expect(screen.getByRole('cell', { name: 'Mercado' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Salary' })).toBeInTheDocument()
  })

  it('re-scopes the combined table, including Resultado and Total despesas, when the year changes', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    expect(screen.getByRole('cell', { name: 'Mercado' })).toBeInTheDocument()

    const nextYearCategoryTotals: CategoryYearlyTotalDto[] = [
      { category: 'Carro', monthlyTotals: new Array(12).fill(50), yearlyTotal: 600 },
    ]
    getCategoryTotalsForYearMock.mockResolvedValue(nextYearCategoryTotals)

    fireEvent.change(screen.getByLabelText('Year'), { target: { value: '2027' } })

    await waitFor(() => expect(screen.getByRole('cell', { name: 'Carro' })).toBeInTheDocument())
    expect(screen.queryByRole('cell', { name: 'Mercado' })).not.toBeInTheDocument()

    // Total despesas yearly total now sums only Carro = 600.00
    const totalDespesasRow = screen.getByRole('cell', { name: 'Total despesas' }).closest('tr')!
    expect(within(totalDespesasRow).getByText('600.00')).toBeInTheDocument()
  })

  it('does not change the year picker value when switching tabs', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    const yearInput = screen.getByLabelText('Year') as HTMLInputElement
    const valueBefore = yearInput.value

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))

    expect(yearInput.value).toBe(valueBefore)
  })

  it('does not refetch data when switching tabs', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    const callCountBefore = getCategoryTotalsForYearMock.mock.calls.length

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    fireEvent.click(screen.getByRole('button', { name: 'Category Totals' }))

    expect(getCategoryTotalsForYearMock.mock.calls.length).toBe(callCountBefore)
  })

  it('keeps the active tab unchanged when the year value changes', async () => {
    render(<YearlySummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Investments' })).toBeInTheDocument())

    fireEvent.change(screen.getByLabelText('Year'), { target: { value: '2027' } })

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Investments' })).toBeInTheDocument())
    expect(screen.getByRole('button', { name: 'Investments' })).toHaveClass('yearly-summary-page__tab--active')
  })
})
