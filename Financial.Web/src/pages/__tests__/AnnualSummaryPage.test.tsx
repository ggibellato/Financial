import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AnnualSummaryPage from '../AnnualSummaryPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CategoryAnnualAverageDto, CategoryAnnualTotalDto, IncomeAnnualSummaryDto, InvestmentDiffsAnnualDto } from '../../api/types'

const getCategoryTotalsForYearMock = vi.fn<FinancialApiClient['getCategoryTotalsForYear']>()
const getInvestmentDiffsForYearMock = vi.fn<FinancialApiClient['getInvestmentDiffsForYear']>()
const getIncomeSummaryForYearMock = vi.fn<FinancialApiClient['getIncomeSummaryForYear']>()
const getHistoricSummaryAverageFromYearMock = vi.fn<FinancialApiClient['getHistoricSummaryAverageFromYear']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getCategoryTotalsForYear: getCategoryTotalsForYearMock,
    getInvestmentDiffsForYear: getInvestmentDiffsForYearMock,
    getIncomeSummaryForYear: getIncomeSummaryForYearMock,
    getHistoricSummaryAverageFromYear: getHistoricSummaryAverageFromYearMock,
  }),
}))

const CATEGORY_TOTALS: CategoryAnnualTotalDto[] = [
  {
    category: 'Mercado',
    monthlyTotals: [100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200, 210],
    annualTotal: 1860,
  },
]

const INVESTMENT_DIFFS: InvestmentDiffsAnnualDto = {
  accounts: [
    {
      account: 'ChaseSave',
      isLiability: false,
      monthlyValues: [1000, 1050, 1100, 1150, 1200, 1250, 1300, 1350, 1400, 1450, 1500, 1550],
      monthlyDiffs: [75, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50],
    },
    {
      account: 'PlatinumVisa8003',
      isLiability: true,
      monthlyValues: [200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200, 200],
      monthlyDiffs: [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
    },
  ],
  netPosition: {
    monthlyValues: [800, 850, 900, 950, 1000, 1050, 1100, 1150, 1200, 1250, 1300, 1350],
    monthlyDiffs: [75, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50],
    fullYearNetChange: 550,
    averageMonthResult: 52.08,
    sumOfMonthResults: 625,
  },
}

const INCOME_SUMMARY: IncomeAnnualSummaryDto = {
  salaryMonthly: [3200, 3200, 3200, 3200, 3200, 3200, 3200, 3200, 3200, 3200, 3200, 3600],
  salaryAnnualTotal: 38800,
  salaryAfterTaxesMonthly: [2450, 2450, 2450, 2450, 2450, 2450, 2450, 2450, 2450, 2450, 2450, 2650],
  salaryAfterTaxesAnnualTotal: 29350,
  taxDifferenceMonthly: [750, 750, 750, 750, 750, 750, 750, 750, 750, 750, 750, 950],
  taxDifferenceAnnualTotal: 9450,
  dividendoJurosMonthly: [0, 0, 15.5, 0, 0, 0, 0, 0, 0, 0, 0, 4.5],
  dividendoJurosAnnualTotal: 20,
}

const HISTORIC_SUMMARY_AVERAGE: CategoryAnnualAverageDto[] = [
  {
    year: 2026,
    annualAverages: [
      { category: 'Salary', average: 3200 },
      { category: 'Salary after taxes', average: 2450 },
      { category: 'Tax difference', average: 750 },
      { category: 'Dividendo/Juros', average: 15.5 },
      { category: 'Mercado', average: 155 },
      { category: 'Reserva', average: 0 },
      { category: 'Resultado (R-D-Inv)', average: 720 },
      { category: 'Total despesas', average: 155 },
    ],
  },
  {
    year: 2025,
    annualAverages: [
      { category: 'Salary', average: 3000 },
      { category: 'Salary after taxes', average: 2300 },
      { category: 'Tax difference', average: 700 },
      { category: 'Dividendo/Juros', average: 10 },
      { category: 'Mercado', average: 140 },
      { category: 'Reserva', average: 0 },
      { category: 'Resultado (R-D-Inv)', average: 650 },
      { category: 'Total despesas', average: 140 },
    ],
  },
]

describe('AnnualSummaryPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getCategoryTotalsForYearMock.mockResolvedValue(CATEGORY_TOTALS)
    getInvestmentDiffsForYearMock.mockResolvedValue(INVESTMENT_DIFFS)
    getIncomeSummaryForYearMock.mockResolvedValue(INCOME_SUMMARY)
    getHistoricSummaryAverageFromYearMock.mockResolvedValue(HISTORIC_SUMMARY_AVERAGE)
  })

  it('shows a loading state before data arrives', () => {
    render(<AnnualSummaryPage />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error state with retry when the fetch fails', async () => {
    getCategoryTotalsForYearMock.mockRejectedValue(new Error('Network down'))

    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Network down')).toBeInTheDocument()
  })

  it('shows the error/retry state regardless of the active tab', async () => {
    getCategoryTotalsForYearMock.mockRejectedValue(new Error('Network down'))

    render(<AnnualSummaryPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Network down')).toBeInTheDocument()
  })

  it('defaults to the Category Totals tab on load', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    expect(screen.getByRole('cell', { name: 'Mercado' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Salary' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Investments' })).not.toBeInTheDocument()
  })

  it('marks Category Totals as the active tab button by default', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    expect(screen.getByRole('button', { name: 'Category Totals' })).toHaveClass('annual-summary-page__tab--active')
    expect(screen.getByRole('button', { name: 'Investments' })).not.toHaveClass('annual-summary-page__tab--active')
  })

  it('renders the combined table in the fixed row order', async () => {
    render(<AnnualSummaryPage />)

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
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    expect(screen.queryByText('Income Summary')).not.toBeInTheDocument()

    const salaryCell = screen.getByRole('cell', { name: 'Salary' })
    const mercadoCell = screen.getByRole('cell', { name: 'Mercado' })
    expect(salaryCell.closest('table')).toBe(mercadoCell.closest('table'))
    expect(within(salaryCell.closest('tr')!).getByText('38,800.00')).toBeInTheDocument()
    const taxDifferenceRow = screen.getByRole('cell', { name: 'Tax difference' }).closest('tr')!
    expect(within(taxDifferenceRow).getByText('9,450.00')).toBeInTheDocument()
  })

  it('shows an Average column between the Dec and Annual Total columns for every row', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    const headerCells = screen.getAllByRole('columnheader').map((th) => th.textContent)
    const decIndex = headerCells.indexOf('Dec')
    const averageIndex = headerCells.indexOf('Average')
    const annualTotalIndex = headerCells.indexOf('Annual Total')
    expect(averageIndex).toBe(decIndex + 1)
    expect(annualTotalIndex).toBe(averageIndex + 1)

    // A past (fully closed) year with all 12 months non-zero: Mercado average = 1860 / 12 = 155.00
    fireEvent.change(screen.getByLabelText('Year'), { target: { value: '2020' } })
    await waitFor(() => {
      const mercadoRow = screen.getByRole('cell', { name: 'Mercado' }).closest('tr')!
      expect(within(mercadoRow).getByText('155.00')).toBeInTheDocument()
    })
  })

  it('excludes zero-value months from the Average for a past year, for both category and income rows', async () => {
    getCategoryTotalsForYearMock.mockResolvedValue([
      { category: 'Mercado', monthlyTotals: [100, 100, 100, 100, 0, 0, 0, 0, 0, 0, 0, 0], annualTotal: 400 },
    ])
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.change(screen.getByLabelText('Year'), { target: { value: '2020' } })

    // Mercado: 400 / 4 non-zero months = 100.00 (not 400 / 12 = 33.33). The Average column is the
    // second-to-last cell (last is Annual Total); several monthly cells also happen to read "100.00"
    // so the column is located by position rather than by text.
    await waitFor(() => {
      const mercadoRow = screen.getByRole('cell', { name: 'Mercado' }).closest('tr')!
      const cells = within(mercadoRow).getAllByRole('cell')
      expect(cells[cells.length - 2].textContent).toBe('100.00')
    })

    // Dividendo/Juros (from the shared INCOME_SUMMARY fixture): [0,0,15.5,0,0,0,0,0,0,0,0,4.5]
    // sums to 20 over 2 non-zero months = 10.00 (not 20 / 12 = 1.67)
    const dividendoRow = screen.getByRole('cell', { name: 'Dividendo/Juros' }).closest('tr')!
    const dividendoCells = within(dividendoRow).getAllByRole('cell')
    expect(dividendoCells[dividendoCells.length - 2].textContent).toBe('10.00')
  })

  it('averages the current (open) year over only the completed months, ignoring the in-progress month and any zero completed months', async () => {
    const now = new Date()
    const currentYear = now.getFullYear()
    const monthsElapsed = now.getMonth()

    // First `monthsElapsed` entries are completed months (one deliberately 0 - still counted, unlike
    // a past year where a 0 month would be excluded); everything from index `monthsElapsed` onward
    // (the in-progress current month and any real future months) must be ignored entirely.
    const monthlyTotals: number[] = Array.from({ length: 12 }, (_, i) =>
      i >= monthsElapsed ? 9999 : i === 0 ? 0 : 100,
    )
    const expectedSum = monthlyTotals.slice(0, monthsElapsed).reduce((a, b) => a + b, 0)
    const expectedAverage = monthsElapsed === 0 ? 0 : expectedSum / monthsElapsed

    getCategoryTotalsForYearMock.mockResolvedValue([
      { category: 'Mercado', monthlyTotals, annualTotal: monthlyTotals.reduce((a, b) => a + b, 0) },
    ])
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.change(screen.getByLabelText('Year'), { target: { value: String(currentYear) } })

    await waitFor(() => {
      const mercadoRow = screen.getByRole('cell', { name: 'Mercado' }).closest('tr')!
      const cells = within(mercadoRow).getAllByRole('cell')
      expect(cells[cells.length - 2].textContent).toBe(expectedAverage.toFixed(2))
    })
  })

  it('computes Resultado and Total despesas from already-fetched data and renders them with emphasized styling', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    // Total despesas annual total = sum of Mercado's monthly totals = 1,860.00
    const totalDespesasRow = screen.getByRole('cell', { name: 'Total despesas' }).closest('tr')!
    expect(totalDespesasRow).toHaveClass('annual-summary-page__emphasized-row')
    expect(within(totalDespesasRow).getByText('1,860.00')).toBeInTheDocument()

    // Resultado annual total = sum(salaryAfterTaxesMonthly)=29,600 + sum(dividendoJurosMonthly)=20 - totalDespesasAnnualTotal=1,860 + 0 (no Investimento category) = 27,760
    const resultadoRow = screen.getByRole('cell', { name: 'Resultado (R-D-Inv)' }).closest('tr')!
    expect(resultadoRow).toHaveClass('annual-summary-page__emphasized-row')
    expect(within(resultadoRow).getByText('27,760.00')).toBeInTheDocument()
  })

  it('shows only the Investments tab content after clicking Investments', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))

    expect(screen.queryByRole('cell', { name: 'Salary' })).not.toBeInTheDocument()
    expect(screen.queryByRole('cell', { name: 'Mercado' })).not.toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Investments' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Investments' })).toHaveClass('annual-summary-page__tab--active')
  })

  it('shows all 12 monthly balance values for each account', async () => {
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument())

    const chaseSaveRow = screen.getByRole('cell', { name: 'ChaseSave' }).closest('tr')!
    for (const value of INVESTMENT_DIFFS.accounts[0].monthlyValues) {
      expect(within(chaseSaveRow).getByText(value.toLocaleString(undefined, { minimumFractionDigits: 2 }))).toBeInTheDocument()
    }
  })

  it('marks liability accounts with a (-) suffix and asset accounts without one', async () => {
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByText('PlatinumVisa8003 (-)')).toBeInTheDocument())
    expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument()
  })

  it('renders a Total row matching the net position monthly values', async () => {
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'Total' })).toBeInTheDocument())

    const totalRow = screen.getByRole('cell', { name: 'Total' }).closest('tr')!
    expect(totalRow).toHaveClass('annual-summary-page__emphasized-row')
    expect(within(totalRow).getByText('800.00')).toBeInTheDocument()
    expect(within(totalRow).getByText('1,350.00')).toBeInTheDocument()
  })

  it('renders a real January Month Result value when prior-year data exists, plus the Feb-Dec diffs', async () => {
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'Month Result' })).toBeInTheDocument())

    const monthResultRow = screen.getByRole('cell', { name: 'Month Result' }).closest('tr')!
    expect(monthResultRow).toHaveClass('annual-summary-page__emphasized-row')
    const cells = within(monthResultRow).getAllByRole('cell')
    // cells[0] is the label; cells[1] is January
    expect(cells[1].textContent).toBe('75.00')
    expect(within(monthResultRow).getAllByText('50.00').length).toBe(11)
  })

  it('renders a blank January Month Result cell when the API returns null for it', async () => {
    getInvestmentDiffsForYearMock.mockResolvedValue({
      ...INVESTMENT_DIFFS,
      netPosition: { ...INVESTMENT_DIFFS.netPosition, monthlyDiffs: [null, ...INVESTMENT_DIFFS.netPosition.monthlyDiffs.slice(1)] },
    })
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'Month Result' })).toBeInTheDocument())

    const monthResultRow = screen.getByRole('cell', { name: 'Month Result' }).closest('tr')!
    const cells = within(monthResultRow).getAllByRole('cell')
    expect(cells[1].textContent).toBe('')
    expect(within(monthResultRow).getAllByText('50.00').length).toBe(11)
  })

  it('shows Year Progress, Average Month Result, and Sum of Month Results as returned by the API', async () => {
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByText('Year Progress')).toBeInTheDocument())

    const yearProgress = screen.getByText('Year Progress').closest('div')!
    expect(within(yearProgress).getByText('550.00')).toBeInTheDocument()

    const averageMonthResult = screen.getByText('Average Month Result').closest('div')!
    expect(within(averageMonthResult).getByText('52.08')).toBeInTheDocument()

    const sumOfMonthResults = screen.getByText('Sum of Month Results').closest('div')!
    expect(within(sumOfMonthResults).getByText('625.00')).toBeInTheDocument()
  })

  it('does not affect the Category Totals tab content when viewing Investments', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'Mercado' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Category Totals' }))

    expect(screen.getByRole('cell', { name: 'Mercado' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Salary' })).toBeInTheDocument()
  })

  it('re-scopes the account table, Total row, Month Result row, and summary figures when the year changes on the Investments tab', async () => {
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument())

    const nextYearInvestmentDiffs: InvestmentDiffsAnnualDto = {
      accounts: [
        {
          account: 'ChipCashIsaGleison',
          isLiability: false,
          monthlyValues: new Array(12).fill(2000),
          monthlyDiffs: new Array(12).fill(0),
        },
      ],
      netPosition: {
        monthlyValues: new Array(12).fill(2000),
        monthlyDiffs: new Array(12).fill(0),
        fullYearNetChange: 0,
        averageMonthResult: 0,
        sumOfMonthResults: 0,
      },
    }
    getInvestmentDiffsForYearMock.mockResolvedValue(nextYearInvestmentDiffs)

    fireEvent.change(screen.getByLabelText('Year'), { target: { value: '2027' } })

    await waitFor(() => expect(screen.getByRole('cell', { name: 'ChipCashIsaGleison' })).toBeInTheDocument())
    expect(screen.queryByRole('cell', { name: 'ChaseSave' })).not.toBeInTheDocument()

    const totalRow = screen.getByRole('cell', { name: 'Total' }).closest('tr')!
    expect(within(totalRow).getAllByText('2,000.00').length).toBeGreaterThan(0)
    const yearProgress = screen.getByText('Year Progress').closest('div')!
    expect(within(yearProgress).getByText('0.00')).toBeInTheDocument()
  })

  it('re-scopes the combined table, including Resultado and Total despesas, when the year changes', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    expect(screen.getByRole('cell', { name: 'Mercado' })).toBeInTheDocument()

    const nextYearCategoryTotals: CategoryAnnualTotalDto[] = [
      { category: 'Carro', monthlyTotals: new Array(12).fill(50), annualTotal: 600 },
    ]
    getCategoryTotalsForYearMock.mockResolvedValue(nextYearCategoryTotals)

    fireEvent.change(screen.getByLabelText('Year'), { target: { value: '2027' } })

    await waitFor(() => expect(screen.getByRole('cell', { name: 'Carro' })).toBeInTheDocument())
    expect(screen.queryByRole('cell', { name: 'Mercado' })).not.toBeInTheDocument()

    // Total despesas annual total now sums only Carro = 600.00
    const totalDespesasRow = screen.getByRole('cell', { name: 'Total despesas' }).closest('tr')!
    expect(within(totalDespesasRow).getByText('600.00')).toBeInTheDocument()
  })

  it('does not change the year picker value when switching tabs', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    const yearInput = screen.getByLabelText('Year') as HTMLInputElement
    const valueBefore = yearInput.value

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))

    expect(yearInput.value).toBe(valueBefore)
  })

  it('does not refetch data when switching tabs', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    const callCountBefore = getCategoryTotalsForYearMock.mock.calls.length

    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    fireEvent.click(screen.getByRole('button', { name: 'Category Totals' }))

    expect(getCategoryTotalsForYearMock.mock.calls.length).toBe(callCountBefore)
  })

  it('keeps the active tab unchanged when the year value changes', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Investments' })).toBeInTheDocument())

    fireEvent.change(screen.getByLabelText('Year'), { target: { value: '2027' } })

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Investments' })).toBeInTheDocument())
    expect(screen.getByRole('button', { name: 'Investments' })).toHaveClass('annual-summary-page__tab--active')
  })

  it('renders one column per year and the correct category values in the Historic Summary Average table', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Historic Summary Average' }))

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Historic Summary Average' })).toBeInTheDocument())

    const headerCells = screen.getAllByRole('columnheader').map((th) => th.textContent)
    expect(headerCells).toEqual(['', '2026', '2025'])

    const mercadoRow = screen.getByText('Mercado').closest('tr')!
    expect(within(mercadoRow).getByText('155.00')).toBeInTheDocument()
    expect(within(mercadoRow).getByText('140.00')).toBeInTheDocument()
  })

  it('renders a spacer row after Tax difference, Dividendo/Juros, and Reserva in the Historic Summary Average table', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Historic Summary Average' }))
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Historic Summary Average' })).toBeInTheDocument())

    const rowLabels = screen
      .getAllByRole('row')
      .slice(1) // drop the header row
      .map((row) => row.querySelector('td')?.textContent ?? '')

    expect(rowLabels).toEqual([
      'Salary',
      'Salary after taxes',
      'Tax difference',
      '',
      'Dividendo/Juros',
      '',
      'Mercado',
      'Reserva',
      '',
      'Resultado (R-D-Inv)',
      'Total despesas',
    ])
  })

  it('renders Resultado and Total despesas rows bold and emphasized in the Historic Summary Average table', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Historic Summary Average' }))
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Historic Summary Average' })).toBeInTheDocument())

    const resultadoRow = screen.getByText('Resultado (R-D-Inv)').closest('tr')!
    expect(resultadoRow).toHaveClass('annual-summary-page__emphasized-row')
    expect(within(resultadoRow).getByText('720.00').closest('strong')).toBeInTheDocument()

    const totalDespesasRow = screen.getByText('Total despesas').closest('tr')!
    expect(totalDespesasRow).toHaveClass('annual-summary-page__emphasized-row')
    expect(within(totalDespesasRow).getByText('155.00').closest('strong')).toBeInTheDocument()
  })

  it('does not affect the Category Totals tab content when viewing Historic Summary Average', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Historic Summary Average' }))
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Historic Summary Average' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Category Totals' }))

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Category Totals' })).toBeInTheDocument())
  })
})
