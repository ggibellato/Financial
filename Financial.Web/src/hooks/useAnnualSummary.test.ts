import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { CategoryAnnualAverageDto, CategoryAnnualTotalDto, IncomeAnnualSummaryDto, InvestmentDiffsAnnualDto } from '../api/types'
import { useAnnualSummary } from './useAnnualSummary'

const CURRENT_YEAR = new Date().getFullYear()

const getCategoryTotalsForYearMock = vi.fn<FinancialApiClient['getCategoryTotalsForYear']>()
const getInvestmentDiffsForYearMock = vi.fn<FinancialApiClient['getInvestmentDiffsForYear']>()
const getIncomeSummaryForYearMock = vi.fn<FinancialApiClient['getIncomeSummaryForYear']>()
const getHistoricSummaryAverageFromYearMock = vi.fn<FinancialApiClient['getHistoricSummaryAverageFromYear']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getCategoryTotalsForYear: getCategoryTotalsForYearMock,
    getInvestmentDiffsForYear: getInvestmentDiffsForYearMock,
    getIncomeSummaryForYear: getIncomeSummaryForYearMock,
    getHistoricSummaryAverageFromYear: getHistoricSummaryAverageFromYearMock,
  }),
}))

const CATEGORY_TOTALS: CategoryAnnualTotalDto[] = [
  { category: 'Mercado', monthlyTotals: new Array(12).fill(100), annualTotal: 1200 },
]

const INVESTMENT_DIFFS: InvestmentDiffsAnnualDto = {
  accounts: [
    {
      account: 'ChaseSave',
      isLiability: false,
      monthlyValues: new Array(12).fill(1000),
      monthlyDiffs: new Array(12).fill(0),
    },
  ],
  netPosition: {
    monthlyValues: new Array(12).fill(1000),
    monthlyDiffs: new Array(12).fill(0),
    fullYearNetChange: 0,
    averageMonthResult: 0,
    sumOfMonthResults: 0,
  },
}

const INCOME_SUMMARY: IncomeAnnualSummaryDto = {
  salaryMonthly: new Array(12).fill(3200),
  salaryAnnualTotal: 38400,
  salaryAfterTaxesMonthly: new Array(12).fill(2450),
  salaryAfterTaxesAnnualTotal: 29400,
  taxDifferenceMonthly: new Array(12).fill(750),
  taxDifferenceAnnualTotal: 9000,
  dividendoJurosMonthly: new Array(12).fill(15.5),
  dividendoJurosAnnualTotal: 186,
}

const HISTORIC_SUMMARY_AVERAGE: CategoryAnnualAverageDto[] = [
  { 
    year: CURRENT_YEAR,
    annualAverages: [
      { category: 'Mercado', average: 100 },
      { category: 'Investimento', average: 500 },
    ],
   },
]

describe('useAnnualSummary', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getCategoryTotalsForYearMock.mockResolvedValue(CATEGORY_TOTALS)
    getInvestmentDiffsForYearMock.mockResolvedValue(INVESTMENT_DIFFS)
    getIncomeSummaryForYearMock.mockResolvedValue(INCOME_SUMMARY)
    getHistoricSummaryAverageFromYearMock.mockResolvedValue(HISTORIC_SUMMARY_AVERAGE)
  })

  it('fetches category totals, investment diffs, and income summary for the current year on mount', async () => {
    const { result } = renderHook(() => useAnnualSummary())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getCategoryTotalsForYearMock).toHaveBeenCalledWith(CURRENT_YEAR)
    expect(getInvestmentDiffsForYearMock).toHaveBeenCalledWith(CURRENT_YEAR)
    expect(getIncomeSummaryForYearMock).toHaveBeenCalledWith(CURRENT_YEAR)
    expect(getHistoricSummaryAverageFromYearMock).toHaveBeenCalledWith(CURRENT_YEAR)
    expect(result.current.year).toBe(CURRENT_YEAR)
    expect(result.current.categoryTotals).toEqual(CATEGORY_TOTALS)
    expect(result.current.investmentDiffs).toEqual(INVESTMENT_DIFFS)
    expect(result.current.incomeSummary).toEqual(INCOME_SUMMARY)
    expect(result.current.historicSummaryAverage).toEqual(HISTORIC_SUMMARY_AVERAGE)
  })

  it('re-fetches for a new year when the year changes', async () => {
    const { result } = renderHook(() => useAnnualSummary())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setYear(2020))

    await waitFor(() => expect(getCategoryTotalsForYearMock).toHaveBeenCalledWith(2020))
    await waitFor(() => expect(getInvestmentDiffsForYearMock).toHaveBeenCalledWith(2020))
    await waitFor(() => expect(getHistoricSummaryAverageFromYearMock).toHaveBeenCalledWith(2020))
  })

  it('surfaces a fetch error without crashing', async () => {
    getCategoryTotalsForYearMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useAnnualSummary())

    await waitFor(() => expect(result.current.error).toBe('Network down'))
  })

  it('re-fetches on retry', async () => {
    const { result } = renderHook(() => useAnnualSummary())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.retry())

    await waitFor(() => expect(getCategoryTotalsForYearMock).toHaveBeenCalledTimes(2))
  })

  it('computes total despesas as the sum of all category monthly totals', async () => {
    const multiCategoryTotals: CategoryAnnualTotalDto[] = [
      { category: 'Mercado', monthlyTotals: new Array(12).fill(100), annualTotal: 1200 },
      { category: 'Investimento', monthlyTotals: new Array(12).fill(500), annualTotal: 6000 },
      { category: 'Reserva', monthlyTotals: new Array(12).fill(50), annualTotal: 600 },
    ]
    getCategoryTotalsForYearMock.mockResolvedValue(multiCategoryTotals)

    const { result } = renderHook(() => useAnnualSummary())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.totalDespesasMonthly).toEqual(new Array(12).fill(650))
    expect(result.current.totalDespesasAnnualTotal).toBe(7800)
  })

  it('computes resultado as net income minus every category except Investimento', async () => {
    const multiCategoryTotals: CategoryAnnualTotalDto[] = [
      { category: 'Mercado', monthlyTotals: new Array(12).fill(100), annualTotal: 1200 },
      { category: 'Investimento', monthlyTotals: new Array(12).fill(500), annualTotal: 6000 },
      { category: 'Reserva', monthlyTotals: new Array(12).fill(50), annualTotal: 600 },
    ]
    getCategoryTotalsForYearMock.mockResolvedValue(multiCategoryTotals)

    const { result } = renderHook(() => useAnnualSummary())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    // resultado = salaryAfterTaxes (2450) + dividendoJuros (15.5) - totalDespesas (650) + investimento (500)
    expect(result.current.resultadoMonthly).toEqual(new Array(12).fill(2315.5))
    expect(result.current.resultadoAnnualTotal).toBeCloseTo(27786, 5)
  })
})
