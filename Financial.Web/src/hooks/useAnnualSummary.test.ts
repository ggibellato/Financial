import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { CategoryAnnualAverageDto, CategoryTotalsAnnualDto, InvestmentAnnualResultDto } from '../api/types'
import { useAnnualSummary } from './useAnnualSummary'

const CURRENT_YEAR = new Date().getFullYear()

const getCategoryTotalsAnnualForYearMock = vi.fn<FinancialApiClient['getCategoryTotalsAnnualForYear']>()
const getInvestmentAnnualResultForYearMock = vi.fn<FinancialApiClient['getInvestmentAnnualResultForYear']>()
const getHistoricSummaryAverageFromYearMock = vi.fn<FinancialApiClient['getHistoricSummaryAverageFromYear']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getCategoryTotalsAnnualForYear: getCategoryTotalsAnnualForYearMock,
    getInvestmentAnnualResultForYear: getInvestmentAnnualResultForYearMock,
    getHistoricSummaryAverageFromYear: getHistoricSummaryAverageFromYearMock,
  }),
}))

const CATEGORY_TOTALS_ANNUAL: CategoryTotalsAnnualDto = {
  categoryTotals: [{ category: 'Mercado', monthlyTotals: new Array(12).fill(100), annualTotal: 1200, average: 100 }],
  incomeSummary: {
    salaryMonthly: new Array(12).fill(3200),
    salaryAnnualTotal: 38400,
    salaryAverage: 3200,
    salaryAfterTaxesMonthly: new Array(12).fill(2450),
    salaryAfterTaxesAnnualTotal: 29400,
    salaryAfterTaxesAverage: 2450,
    taxDifferenceMonthly: new Array(12).fill(750),
    taxDifferenceAnnualTotal: 9000,
    taxDifferenceAverage: 750,
    dividendoJurosMonthly: new Array(12).fill(15.5),
    dividendoJurosAnnualTotal: 186,
    dividendoJurosAverage: 15.5,
  },
  totalDespesasMonthly: new Array(12).fill(100),
  totalDespesasAnnualTotal: 1200,
  totalDespesasAverage: 100,
  resultadoMonthly: new Array(12).fill(2350),
  resultadoAnnualTotal: 28200,
  resultadoAverage: 2350,
}

const INVESTMENT_ANNUAL_RESULT: InvestmentAnnualResultDto = {
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

const HISTORIC_SUMMARY_AVERAGE: CategoryAnnualAverageDto[] = [
  {
    year: CURRENT_YEAR,
    annualAverages: [
      { category: 'Mercado', value: 100 },
      { category: 'Investimento', value: 500 },
    ],
  },
]

describe('useAnnualSummary', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getCategoryTotalsAnnualForYearMock.mockResolvedValue(CATEGORY_TOTALS_ANNUAL)
    getInvestmentAnnualResultForYearMock.mockResolvedValue(INVESTMENT_ANNUAL_RESULT)
    getHistoricSummaryAverageFromYearMock.mockResolvedValue(HISTORIC_SUMMARY_AVERAGE)
  })

  it('fetches category totals, investment annual result, and historic summary average for the current year on mount', async () => {
    const { result } = renderHook(() => useAnnualSummary())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getCategoryTotalsAnnualForYearMock).toHaveBeenCalledWith(CURRENT_YEAR)
    expect(getInvestmentAnnualResultForYearMock).toHaveBeenCalledWith(CURRENT_YEAR)
    expect(getHistoricSummaryAverageFromYearMock).toHaveBeenCalledWith(CURRENT_YEAR)
    expect(result.current.year).toBe(CURRENT_YEAR)
    expect(result.current.categoryTotals).toEqual(CATEGORY_TOTALS_ANNUAL.categoryTotals)
    expect(result.current.incomeSummary).toEqual(CATEGORY_TOTALS_ANNUAL.incomeSummary)
    expect(result.current.investmentAnnualResult).toEqual(INVESTMENT_ANNUAL_RESULT)
    expect(result.current.historicSummaryAverage).toEqual(HISTORIC_SUMMARY_AVERAGE)
  })

  it('issues exactly 3 requests per year load', async () => {
    const { result } = renderHook(() => useAnnualSummary())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getCategoryTotalsAnnualForYearMock).toHaveBeenCalledTimes(1)
    expect(getInvestmentAnnualResultForYearMock).toHaveBeenCalledTimes(1)
    expect(getHistoricSummaryAverageFromYearMock).toHaveBeenCalledTimes(1)
  })

  it('does not recompute total despesas or resultado client-side', async () => {
    // Values chosen so they would NOT match a client-side recomputation from categoryTotals/incomeSummary,
    // proving the hook reads them straight from the API response instead of deriving them.
    getCategoryTotalsAnnualForYearMock.mockResolvedValue({
      ...CATEGORY_TOTALS_ANNUAL,
      totalDespesasMonthly: new Array(12).fill(999),
      totalDespesasAnnualTotal: 11988,
      resultadoMonthly: new Array(12).fill(-42),
      resultadoAnnualTotal: -504,
    })

    const { result } = renderHook(() => useAnnualSummary())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.totalDespesasMonthly).toEqual(new Array(12).fill(999))
    expect(result.current.totalDespesasAnnualTotal).toBe(11988)
    expect(result.current.resultadoMonthly).toEqual(new Array(12).fill(-42))
    expect(result.current.resultadoAnnualTotal).toBe(-504)
  })

  it('re-fetches for a new year when the year changes', async () => {
    const { result } = renderHook(() => useAnnualSummary())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setYear(2020))

    await waitFor(() => expect(getCategoryTotalsAnnualForYearMock).toHaveBeenCalledWith(2020))
    await waitFor(() => expect(getInvestmentAnnualResultForYearMock).toHaveBeenCalledWith(2020))
    await waitFor(() => expect(getHistoricSummaryAverageFromYearMock).toHaveBeenCalledWith(2020))
  })

  it('surfaces a fetch error without crashing', async () => {
    getCategoryTotalsAnnualForYearMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useAnnualSummary())

    await waitFor(() => expect(result.current.error).toBe('Network down'))
  })

  it('re-fetches on retry', async () => {
    const { result } = renderHook(() => useAnnualSummary())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.retry())

    await waitFor(() => expect(getCategoryTotalsAnnualForYearMock).toHaveBeenCalledTimes(2))
  })
})
