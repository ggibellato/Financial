import { afterEach, describe, expect, it, vi } from 'vitest'
import type { TaxWorkbookDto, TaxWorkbookEntryDto } from '../../api/types'
import { buildTaxWorkbookCsv, buildTaxWorkbookCsvFilename, downloadCsv } from '../taxWorkbookCsv'

function buildWorkbook(overrides: Partial<TaxWorkbookDto> = {}): TaxWorkbookDto {
  return {
    jurisdiction: 'BR',
    taxYear: '2026',
    entries: [],
    categoryTotals: [],
    calculationStatus: null,
    ...overrides,
  }
}

describe('buildTaxWorkbookCsv', () => {
  it('produces exactly the 12 specified columns in order', () => {
    const csv = buildTaxWorkbookCsv(buildWorkbook())
    const header = csv.split('\r\n')[0]

    expect(header.split(',')).toEqual([
      'Date',
      'Jurisdiction',
      'TaxYear',
      'EventCategory',
      'Proceeds',
      'CostBasis',
      'GainLoss',
      'GrossAmount',
      'WithheldAmount',
      'NetAmount',
      'Currency',
      'EvidenceReference',
    ])
  })

  it('emits one row per entry, with the workbook\'s jurisdiction/taxYear/currency repeated on each', () => {
    const csv = buildTaxWorkbookCsv(
      buildWorkbook({
        jurisdiction: 'BR',
        taxYear: '2026',
        entries: [
          {
            id: '1',
            date: '2026-06-01',
            eventCategory: 'Dividend',
            proceeds: null,
            costBasis: null,
            gainLoss: null,
            grossAmount: 100,
            withheldAmount: 10,
            netAmount: 90,
            calculationStatus: 'Final',
            evidenceReference: 'ev-1',
            taxRuleLabel: 'Rule',
          },
          {
            id: '2',
            date: '2026-07-01',
            eventCategory: 'CapitalGain',
            proceeds: 500,
            costBasis: 400,
            gainLoss: 100,
            grossAmount: null,
            withheldAmount: null,
            netAmount: null,
            calculationStatus: 'Incomplete',
            evidenceReference: 'ev-2',
            taxRuleLabel: null,
          },
        ],
      }),
    )

    const rows = csv.split('\r\n').slice(1)
    expect(rows).toHaveLength(2)
    expect(rows[0]).toBe('2026-06-01,BR,2026,Dividend,,,,100,10,90,BRL,ev-1')
    expect(rows[1]).toBe('2026-07-01,BR,2026,CapitalGain,500,400,100,,,,BRL,ev-2')
  })

  it('derives GBP for UK and BRL for BR', () => {
    const ukCsv = buildTaxWorkbookCsv(
      buildWorkbook({
        jurisdiction: 'UK',
        taxYear: '2025/26',
        entries: [
          {
            id: '1',
            date: '2026-01-01',
            eventCategory: 'Dividend',
            proceeds: null,
            costBasis: null,
            gainLoss: null,
            grossAmount: 10,
            withheldAmount: 0,
            netAmount: 10,
            calculationStatus: 'Final',
            evidenceReference: 'ev-1',
            taxRuleLabel: null,
          },
        ],
      }),
    )

    expect(ukCsv.split('\r\n')[1]).toContain(',GBP,')
  })

  it('quotes a field containing a comma', () => {
    const csv = buildTaxWorkbookCsv(
      buildWorkbook({
        entries: [
          {
            id: '1',
            date: '2026-01-01',
            // No real field carries a comma today (EventCategory is a closed
            // enum), but the columns are free strings on the wire - guard against one anyway.
            eventCategory: 'Dividend, extra' as TaxWorkbookEntryDto['eventCategory'],
            proceeds: null,
            costBasis: null,
            gainLoss: null,
            grossAmount: 10,
            withheldAmount: 0,
            netAmount: 10,
            calculationStatus: 'Final',
            evidenceReference: 'ev-1',
            taxRuleLabel: null,
          },
        ],
      }),
    )

    expect(csv.split('\r\n')[1]).toContain('"Dividend, extra"')
  })

  it('doubles a quote inside a field', () => {
    const csv = buildTaxWorkbookCsv(
      buildWorkbook({
        jurisdiction: 'BR',
        entries: [
          {
            id: '1',
            date: '2026-01-01',
            eventCategory: 'Dividend "special"' as TaxWorkbookEntryDto['eventCategory'],
            proceeds: null,
            costBasis: null,
            gainLoss: null,
            grossAmount: 10,
            withheldAmount: 0,
            netAmount: 10,
            calculationStatus: 'Final',
            evidenceReference: 'ev-1',
            taxRuleLabel: null,
          },
        ],
      }),
    )

    expect(csv.split('\r\n')[1]).toContain(',"Dividend ""special""",')
  })
})

describe('buildTaxWorkbookCsvFilename', () => {
  it('replaces a UK tax year\'s slash with a dash', () => {
    expect(buildTaxWorkbookCsvFilename('UK', '2025/26')).toBe('tax-workbook-UK-2025-26.csv')
  })

  it('leaves a BR tax year unchanged', () => {
    expect(buildTaxWorkbookCsvFilename('BR', '2026')).toBe('tax-workbook-BR-2026.csv')
  })
})

describe('downloadCsv', () => {
  const { createObjectURL, revokeObjectURL } = URL

  afterEach(() => {
    Object.assign(URL, { createObjectURL, revokeObjectURL })
    vi.restoreAllMocks()
  })

  it('saves the text as a CSV file of the given name and leaves nothing behind', async () => {
    const createObjectURL = vi.fn<(blob: Blob) => string>(() => 'blob:csv')
    const revokeObjectURL = vi.fn()
    Object.assign(URL, { createObjectURL, revokeObjectURL })
    const clicks: { download: string; href: string; attached: boolean }[] = []
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      clicks.push({ download: this.download, href: this.href, attached: document.body.contains(this) })
    })

    downloadCsv('tax.csv', 'a,b')

    expect(clicks).toEqual([{ download: 'tax.csv', href: 'blob:csv', attached: true }])
    const blob = createObjectURL.mock.calls[0][0]
    expect(blob.type).toBe('text/csv;charset=utf-8;')
    expect(await blob.text()).toBe('a,b')
    expect(document.body.querySelector('a')).toBeNull()
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:csv')
  })
})
