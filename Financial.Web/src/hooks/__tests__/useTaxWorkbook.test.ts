import { renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { TaxWorkbookDto, TaxWorkbookOptionDto } from '../../api/types'
import { useTaxWorkbook } from '../useTaxWorkbook'

const { getTaxWorkbookOptionsMock, getTaxWorkbookMock } = vi.hoisted(() => ({
  getTaxWorkbookOptionsMock: vi.fn<FinancialApiClient['getTaxWorkbookOptions']>(),
  getTaxWorkbookMock: vi.fn<FinancialApiClient['getTaxWorkbook']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getTaxWorkbookOptions: getTaxWorkbookOptionsMock,
    getTaxWorkbook: getTaxWorkbookMock,
  } as Partial<FinancialApiClient>,
}))

const { downloadCsvMock } = vi.hoisted(() => ({
  downloadCsvMock: vi.fn(),
}))

vi.mock('../../utils/taxWorkbookCsv', () => ({
  buildTaxWorkbookCsv: () => 'csv-content',
  buildTaxWorkbookCsvFilename: (jurisdiction: string, taxYear: string) => `tax-workbook-${jurisdiction}-${taxYear}.csv`,
  downloadCsv: downloadCsvMock,
}))

const OPTIONS: TaxWorkbookOptionDto[] = [
  { jurisdiction: 'BR', taxYear: '2025' },
  { jurisdiction: 'BR', taxYear: '2026' },
  { jurisdiction: 'UK', taxYear: '2025/26' },
]

function buildWorkbook(overrides: Partial<TaxWorkbookDto> = {}): TaxWorkbookDto {
  return {
    jurisdiction: 'BR',
    taxYear: '2025',
    entries: [],
    categoryTotals: [],
    calculationStatus: null,
    ...overrides,
  }
}

describe('useTaxWorkbook', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('loads options and defaults selection to the first one, then fetches its workbook', async () => {
    getTaxWorkbookOptionsMock.mockResolvedValue(OPTIONS)
    getTaxWorkbookMock.mockResolvedValue(buildWorkbook())

    const { result } = renderHook(() => useTaxWorkbook())

    expect(result.current.isLoadingOptions).toBe(true)

    await waitFor(() => expect(result.current.isLoadingOptions).toBe(false))
    expect(result.current.jurisdiction).toBe('BR')
    expect(result.current.taxYear).toBe('2025')
    expect(result.current.jurisdictions).toEqual(['BR', 'UK'])
    expect(result.current.taxYearsForJurisdiction).toEqual(['2025', '2026'])

    await waitFor(() => expect(getTaxWorkbookMock).toHaveBeenCalledWith('BR', '2025'))
    await waitFor(() => expect(result.current.workbook).not.toBeNull())
  })

  it('selecting a jurisdiction resets the tax year to that jurisdiction\'s first option', async () => {
    getTaxWorkbookOptionsMock.mockResolvedValue(OPTIONS)
    getTaxWorkbookMock.mockResolvedValue(buildWorkbook())

    const { result } = renderHook(() => useTaxWorkbook())
    await waitFor(() => expect(result.current.isLoadingOptions).toBe(false))

    result.current.selectJurisdiction('UK')

    await waitFor(() => expect(result.current.jurisdiction).toBe('UK'))
    expect(result.current.taxYear).toBe('2025/26')
    await waitFor(() => expect(getTaxWorkbookMock).toHaveBeenCalledWith('UK', '2025/26'))
  })

  it('selecting a tax year keeps the current jurisdiction and refetches', async () => {
    getTaxWorkbookOptionsMock.mockResolvedValue(OPTIONS)
    getTaxWorkbookMock.mockResolvedValue(buildWorkbook())

    const { result } = renderHook(() => useTaxWorkbook())
    await waitFor(() => expect(result.current.isLoadingOptions).toBe(false))

    result.current.selectTaxYear('2026')

    await waitFor(() => expect(result.current.taxYear).toBe('2026'))
    expect(result.current.jurisdiction).toBe('BR')
    await waitFor(() => expect(getTaxWorkbookMock).toHaveBeenCalledWith('BR', '2026'))
  })

  it('surfaces an options load error and retries on demand', async () => {
    getTaxWorkbookOptionsMock.mockRejectedValueOnce(new Error('boom'))
    getTaxWorkbookOptionsMock.mockResolvedValueOnce(OPTIONS)
    getTaxWorkbookMock.mockResolvedValue(buildWorkbook())

    const { result } = renderHook(() => useTaxWorkbook())

    await waitFor(() => expect(result.current.optionsError).toBe('boom'))

    result.current.retryOptions()

    await waitFor(() => expect(result.current.optionsError).toBeNull())
    expect(result.current.jurisdictions).toEqual(['BR', 'UK'])
  })

  it('surfaces a workbook load error and retries on demand', async () => {
    getTaxWorkbookOptionsMock.mockResolvedValue(OPTIONS)
    getTaxWorkbookMock.mockRejectedValueOnce(new Error('workbook failed'))
    getTaxWorkbookMock.mockResolvedValueOnce(buildWorkbook())

    const { result } = renderHook(() => useTaxWorkbook())

    await waitFor(() => expect(result.current.workbookError).toBe('workbook failed'))

    result.current.retryWorkbook()

    await waitFor(() => expect(result.current.workbookError).toBeNull())
    expect(result.current.workbook).not.toBeNull()
  })

  it('with no options at all, never attempts a workbook fetch', async () => {
    getTaxWorkbookOptionsMock.mockResolvedValue([])

    const { result } = renderHook(() => useTaxWorkbook())

    await waitFor(() => expect(result.current.isLoadingOptions).toBe(false))
    expect(result.current.jurisdictions).toEqual([])
    expect(result.current.jurisdiction).toBeNull()
    expect(getTaxWorkbookMock).not.toHaveBeenCalled()
  })

  it('canExportCsv is false for an empty workbook and true once entries exist', async () => {
    getTaxWorkbookOptionsMock.mockResolvedValue(OPTIONS)
    getTaxWorkbookMock.mockResolvedValue(buildWorkbook({ entries: [] }))

    const { result } = renderHook(() => useTaxWorkbook())
    await waitFor(() => expect(result.current.workbook).not.toBeNull())

    expect(result.current.canExportCsv).toBe(false)

    result.current.exportCsv()
    expect(downloadCsvMock).not.toHaveBeenCalled()
  })

  it('exportCsv downloads the CSV when the workbook has entries', async () => {
    getTaxWorkbookOptionsMock.mockResolvedValue(OPTIONS)
    getTaxWorkbookMock.mockResolvedValue(
      buildWorkbook({
        entries: [
          {
            id: '1',
            date: '2025-06-01',
            eventCategory: 'Dividend',
            proceeds: null,
            costBasis: null,
            gainLoss: null,
            grossAmount: 100,
            withheldAmount: 10,
            netAmount: 90,
            calculationStatus: 'Final',
            evidenceReference: 'evidence-1',
            taxRuleLabel: 'BR dividend rule',
          },
        ],
      }),
    )

    const { result } = renderHook(() => useTaxWorkbook())
    await waitFor(() => expect(result.current.canExportCsv).toBe(true))

    result.current.exportCsv()

    expect(downloadCsvMock).toHaveBeenCalledWith('tax-workbook-BR-2025.csv', 'csv-content')
  })
})
