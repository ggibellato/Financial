import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AnnualSummaryPage from '../AnnualSummaryPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CategoryAnnualAverageDto, CategoryTotalsAnnualDto, InvestmentAnnualResultDto } from '../../api/types'

const {
  getCategoryTotalsAnnualForYearMock,
  getInvestmentAnnualResultForYearMock,
  getHistoricSummaryAverageFromYearMock,
} = vi.hoisted(() => ({
  getCategoryTotalsAnnualForYearMock: vi.fn<FinancialApiClient['getCategoryTotalsAnnualForYear']>(),
  getInvestmentAnnualResultForYearMock: vi.fn<FinancialApiClient['getInvestmentAnnualResultForYear']>(),
  getHistoricSummaryAverageFromYearMock: vi.fn<FinancialApiClient['getHistoricSummaryAverageFromYear']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getCategoryTotalsAnnualForYear: getCategoryTotalsAnnualForYearMock,
    getInvestmentAnnualResultForYear: getInvestmentAnnualResultForYearMock,
    getHistoricSummaryAverageFromYear: getHistoricSummaryAverageFromYearMock,
  } as Partial<FinancialApiClient>,
}))

const CATEGORY_TOTALS_ANNUAL: CategoryTotalsAnnualDto = {
  categoryTotals: [
    {
      category: 'Mercado',
      monthlyTotals: [100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200, 210],
      annualTotal: 1860,
      average: 155,
    },
  ],
  incomeSummary: {
    salaryMonthly: [3200, 3200, 3200, 3200, 3200, 3200, 3200, 3200, 3200, 3200, 3200, 3600],
    salaryAnnualTotal: 38800,
    salaryAverage: 3233.33,
    salaryAfterTaxesMonthly: [2450, 2450, 2450, 2450, 2450, 2450, 2450, 2450, 2450, 2450, 2450, 2650],
    salaryAfterTaxesAnnualTotal: 29350,
    salaryAfterTaxesAverage: 2445.83,
    taxDifferenceMonthly: [750, 750, 750, 750, 750, 750, 750, 750, 750, 750, 750, 950],
    taxDifferenceAnnualTotal: 9450,
    taxDifferenceAverage: 787.5,
    dividendoJurosMonthly: [0, 0, 15.5, 0, 0, 0, 0, 0, 0, 0, 0, 4.5],
    dividendoJurosAnnualTotal: 20,
    dividendoJurosAverage: 1.67,
  },
  totalDespesasMonthly: [100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200, 210],
  totalDespesasAnnualTotal: 1860,
  totalDespesasAverage: 155,
  resultadoMonthly: [2350, 2340, 2330, 2320, 2310, 2300, 2290, 2280, 2270, 2260, 2250, 2440],
  resultadoAnnualTotal: 27740,
  resultadoAverage: 2311.67,
}

const INVESTMENT_ANNUAL_RESULT: InvestmentAnnualResultDto = {
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

const HISTORIC_SUMMARY_AVERAGE: CategoryAnnualAverageDto[] = [
  {
    year: 2026,
    annualAverages: [
      { category: 'Salary', value: 3200 },
      { category: 'Salary after taxes', value: 2450 },
      { category: 'Tax difference', value: 750 },
      { category: 'Dividendo/Juros', value: 15.5 },
      { category: 'Mercado', value: 155 },
      { category: 'Reserva', value: 0 },
      { category: 'Resultado (R-D-Inv)', value: 720 },
      { category: 'Total despesas', value: 155 },
    ],
  },
  {
    year: 2025,
    annualAverages: [
      { category: 'Salary', value: 3000 },
      { category: 'Salary after taxes', value: 2300 },
      { category: 'Tax difference', value: 700 },
      { category: 'Dividendo/Juros', value: 10 },
      { category: 'Mercado', value: 140 },
      { category: 'Reserva', value: 0 },
      { category: 'Resultado (R-D-Inv)', value: 650 },
      { category: 'Total despesas', value: 140 },
    ],
  },
]

describe('AnnualSummaryPage', () => {
  beforeEach(() => {
    getCategoryTotalsAnnualForYearMock.mockReset()
    getInvestmentAnnualResultForYearMock.mockReset()
    getHistoricSummaryAverageFromYearMock.mockReset()
    getCategoryTotalsAnnualForYearMock.mockResolvedValue(CATEGORY_TOTALS_ANNUAL)
    getInvestmentAnnualResultForYearMock.mockResolvedValue(INVESTMENT_ANNUAL_RESULT)
    getHistoricSummaryAverageFromYearMock.mockResolvedValue(HISTORIC_SUMMARY_AVERAGE)
  })

  it('shows a loading state before data arrives', () => {
    render(<AnnualSummaryPage />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error state with retry when the fetch fails', async () => {
    getCategoryTotalsAnnualForYearMock.mockRejectedValue(new Error('Network down'))

    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Network down')).toBeInTheDocument()
  })

  it('shows the error/retry state regardless of the active tab', async () => {
    getCategoryTotalsAnnualForYearMock.mockRejectedValue(new Error('Network down'))

    render(<AnnualSummaryPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Investments' }))

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
    expect(screen.getByRole('tab', { name: 'Category Totals' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Investments' })).toHaveAttribute('aria-selected', 'false')
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

  it('shows an Average column between the Dec and Annual Total columns, using the server-computed value', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    const headerCells = screen.getAllByRole('columnheader').map((th) => th.textContent)
    const decIndex = headerCells.indexOf('Dec')
    const averageIndex = headerCells.indexOf('Average')
    const annualTotalIndex = headerCells.indexOf('Annual Total')
    expect(averageIndex).toBe(decIndex + 1)
    expect(annualTotalIndex).toBe(averageIndex + 1)

    // Mercado's average (155.00) comes straight from the mocked API response, not a client
    // recomputation from monthlyTotals - proven by the fact this must match CATEGORY_TOTALS_ANNUAL's
    // explicit `average` field, not sum(monthlyTotals) / 12.
    const mercadoRow = screen.getByRole('cell', { name: 'Mercado' }).closest('tr')!
    expect(within(mercadoRow).getByText('155.00')).toBeInTheDocument()
  })

  it('renders Resultado and Total despesas using the server-computed values, with emphasized styling', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    // Total despesas annual total comes straight from the mocked API response, not a client recomputation.
    const totalDespesasRow = screen.getByRole('cell', { name: 'Total despesas' }).closest('tr')!
    expect(totalDespesasRow).toHaveClass('annual-summary-page__emphasized-row')
    expect(within(totalDespesasRow).getByText('1,860.00')).toBeInTheDocument()

    // Resultado annual total also comes straight from the mocked API response (corrected formula,
    // excludes Dividendo/Juros) - proves no client-side recomputation occurs.
    const resultadoRow = screen.getByRole('cell', { name: 'Resultado (R-D-Inv)' }).closest('tr')!
    expect(resultadoRow).toHaveClass('annual-summary-page__emphasized-row')
    expect(within(resultadoRow).getByText('27,740.00')).toBeInTheDocument()
  })

  it('shows only the Investments tab content after clicking Investments', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Investments' }))

    expect(screen.queryByRole('cell', { name: 'Salary' })).not.toBeInTheDocument()
    expect(screen.queryByRole('cell', { name: 'Mercado' })).not.toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Investments' })).toHaveAttribute('aria-selected', 'true')
  })

  it('shows all 12 monthly balance values for each account', async () => {
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('tab', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument())

    const chaseSaveRow = screen.getByRole('cell', { name: 'ChaseSave' }).closest('tr')!
    for (const value of INVESTMENT_ANNUAL_RESULT.accounts[0].monthlyValues) {
      expect(within(chaseSaveRow).getByText(value.toLocaleString(undefined, { minimumFractionDigits: 2 }))).toBeInTheDocument()
    }
  })

  it('marks liability accounts with a (-) suffix and asset accounts without one', async () => {
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('tab', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByText('PlatinumVisa8003 (-)')).toBeInTheDocument())
    expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument()
  })

  it('renders a Total row matching the net position monthly values', async () => {
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('tab', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'Total' })).toBeInTheDocument())

    const totalRow = screen.getByRole('cell', { name: 'Total' }).closest('tr')!
    expect(totalRow).toHaveClass('annual-summary-page__emphasized-row')
    expect(within(totalRow).getByText('800.00')).toBeInTheDocument()
    expect(within(totalRow).getByText('1,350.00')).toBeInTheDocument()
  })

  it('renders a real January Month Result value when prior-year data exists, plus the Feb-Dec diffs', async () => {
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('tab', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'Month Result' })).toBeInTheDocument())

    const monthResultRow = screen.getByRole('cell', { name: 'Month Result' }).closest('tr')!
    expect(monthResultRow).toHaveClass('annual-summary-page__emphasized-row')
    const cells = within(monthResultRow).getAllByRole('cell')
    // cells[0] is the label; cells[1] is January
    expect(cells[1].textContent).toBe('75.00')
    expect(within(monthResultRow).getAllByText('50.00').length).toBe(11)
  })

  it('renders a blank January Month Result cell when the API returns null for it', async () => {
    getInvestmentAnnualResultForYearMock.mockResolvedValue({
      ...INVESTMENT_ANNUAL_RESULT,
      netPosition: { ...INVESTMENT_ANNUAL_RESULT.netPosition, monthlyDiffs: [null, ...INVESTMENT_ANNUAL_RESULT.netPosition.monthlyDiffs.slice(1)] },
    })
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('tab', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'Month Result' })).toBeInTheDocument())

    const monthResultRow = screen.getByRole('cell', { name: 'Month Result' }).closest('tr')!
    const cells = within(monthResultRow).getAllByRole('cell')
    expect(cells[1].textContent).toBe('')
    expect(within(monthResultRow).getAllByText('50.00').length).toBe(11)
  })

  it('shows Year Progress, Average Month Result, and Sum of Month Results as returned by the API', async () => {
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('tab', { name: 'Investments' }))
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
    fireEvent.click(screen.getByRole('tab', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Category Totals' }))

    expect(screen.getByRole('cell', { name: 'Mercado' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Salary' })).toBeInTheDocument()
  })

  it('re-scopes the account table, Total row, Month Result row, and summary figures when the year changes on the Investments tab', async () => {
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('tab', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument())

    const nextYearInvestmentAnnualResult: InvestmentAnnualResultDto = {
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
    getInvestmentAnnualResultForYearMock.mockResolvedValue(nextYearInvestmentAnnualResult)

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

    const nextYearCategoryTotalsAnnual: CategoryTotalsAnnualDto = {
      ...CATEGORY_TOTALS_ANNUAL,
      categoryTotals: [{ category: 'Carro', monthlyTotals: new Array(12).fill(50), annualTotal: 600, average: 50 }],
      totalDespesasMonthly: new Array(12).fill(50),
      totalDespesasAnnualTotal: 600,
    }
    getCategoryTotalsAnnualForYearMock.mockResolvedValue(nextYearCategoryTotalsAnnual)

    fireEvent.change(screen.getByLabelText('Year'), { target: { value: '2027' } })

    await waitFor(() => expect(screen.getByRole('cell', { name: 'Carro' })).toBeInTheDocument())
    expect(screen.queryByRole('cell', { name: 'Mercado' })).not.toBeInTheDocument()

    // Total despesas annual total now reflects the new mocked response = 600.00
    const totalDespesasRow = screen.getByRole('cell', { name: 'Total despesas' }).closest('tr')!
    expect(within(totalDespesasRow).getByText('600.00')).toBeInTheDocument()
  })

  it('does not change the year picker value when switching tabs', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    const yearInput = screen.getByLabelText('Year') as HTMLInputElement
    const valueBefore = yearInput.value

    fireEvent.click(screen.getByRole('tab', { name: 'Investments' }))

    expect(yearInput.value).toBe(valueBefore)
  })

  it('does not refetch data when switching tabs', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    const callCountBefore = getCategoryTotalsAnnualForYearMock.mock.calls.length

    fireEvent.click(screen.getByRole('tab', { name: 'Investments' }))
    fireEvent.click(screen.getByRole('tab', { name: 'Category Totals' }))

    expect(getCategoryTotalsAnnualForYearMock.mock.calls.length).toBe(callCountBefore)
  })

  it('keeps the active tab unchanged when the year value changes', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument())

    fireEvent.change(screen.getByLabelText('Year'), { target: { value: '2027' } })

    await waitFor(() => expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument())
    expect(screen.getByRole('tab', { name: 'Investments' })).toHaveAttribute('aria-selected', 'true')
  })

  it('renders one column per year and the correct category values in the Historic Summary Average table', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Historic Summary Average' }))

    await waitFor(() => expect(screen.getByRole('columnheader', { name: '2025' })).toBeInTheDocument())

    const headerCells = screen.getAllByRole('columnheader').map((th) => th.textContent)
    expect(headerCells).toEqual(['Category', '2026', '2025'])

    const mercadoRow = screen.getByText('Mercado').closest('tr')!
    expect(within(mercadoRow).getByText('155.00')).toBeInTheDocument()
    expect(within(mercadoRow).getByText('140.00')).toBeInTheDocument()
  })

  it('renders a spacer row after Tax difference, Dividendo/Juros, and Reserva in the Historic Summary Average table', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Historic Summary Average' }))
    await waitFor(() => expect(screen.getByRole('columnheader', { name: '2025' })).toBeInTheDocument())

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
    fireEvent.click(screen.getByRole('tab', { name: 'Historic Summary Average' }))
    await waitFor(() => expect(screen.getByRole('columnheader', { name: '2025' })).toBeInTheDocument())

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
    fireEvent.click(screen.getByRole('tab', { name: 'Historic Summary Average' }))
    await waitFor(() => expect(screen.getByRole('columnheader', { name: '2025' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Category Totals' }))

    await waitFor(() => expect(screen.getByRole('cell', { name: 'Salary' })).toBeInTheDocument())
  })

  it('sorts the Category Totals data rows by Annual Total, without moving the fixed Salary/Resultado/Total despesas rows', async () => {
    getCategoryTotalsAnnualForYearMock.mockResolvedValue({
      ...CATEGORY_TOTALS_ANNUAL,
      categoryTotals: [
        { category: 'Mercado', monthlyTotals: new Array(12).fill(155), annualTotal: 1860, average: 155 },
        { category: 'Casa', monthlyTotals: new Array(12).fill(500), annualTotal: 6000, average: 500 },
      ],
    })
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'Mercado' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Annual Total' }))

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
      'Casa',
      'Resultado (R-D-Inv)',
      'Total despesas',
    ])
  })

  it('sorts the Investments accounts by a monthly column, without moving the fixed Total/Month Result rows', async () => {
    render(<AnnualSummaryPage />)

    fireEvent.click(screen.getByRole('tab', { name: 'Investments' }))
    await waitFor(() => expect(screen.getByRole('cell', { name: 'ChaseSave' })).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Jan' }))

    const rows = screen.getAllByRole('row')
    const dataRows = rows.slice(1, -2)
    expect(within(dataRows[0]).getByText('PlatinumVisa8003 (-)')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('ChaseSave')).toBeInTheDocument()

    const lastTwoRows = rows.slice(-2)
    expect(within(lastTwoRows[0]).getByRole('cell', { name: 'Total' })).toBeInTheDocument()
    expect(within(lastTwoRows[1]).getByRole('cell', { name: 'Month Result' })).toBeInTheDocument()
  })

  it('sorts every row of the Historic Summary Average table alphabetically by Category when its header is clicked', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Historic Summary Average' }))
    await waitFor(() => expect(screen.getByRole('columnheader', { name: '2025' })).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Category' }))

    const rowLabels = screen
      .getAllByRole('row')
      .slice(1)
      .map((row) => row.querySelector('td')?.textContent ?? '')
      .filter((label) => label !== '')

    expect(rowLabels).toEqual([...rowLabels].sort((a, b) => a.localeCompare(b)))
  })

  it('filters the Category Totals data rows by Category, without hiding the fixed Salary/Resultado/Total despesas rows', async () => {
    getCategoryTotalsAnnualForYearMock.mockResolvedValue({
      ...CATEGORY_TOTALS_ANNUAL,
      categoryTotals: [
        { category: 'Mercado', monthlyTotals: new Array(12).fill(155), annualTotal: 1860, average: 155 },
        { category: 'Casa', monthlyTotals: new Array(12).fill(500), annualTotal: 6000, average: 500 },
      ],
    })
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'Mercado' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Filter by Category' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Casa' }))

    expect(screen.queryByRole('cell', { name: 'Casa' })).not.toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Mercado' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Salary' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Resultado (R-D-Inv)' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Total despesas' })).toBeInTheDocument()
  })

  it('filters the Historic Summary Average table by Category', async () => {
    render(<AnnualSummaryPage />)

    await waitFor(() => expect(screen.getByText('Category Totals')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Historic Summary Average' }))
    await waitFor(() => expect(screen.getByRole('columnheader', { name: '2025' })).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Category' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Mercado' }))

    expect(screen.queryByRole('cell', { name: 'Mercado' })).not.toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Salary' })).toBeInTheDocument()
  })
})
