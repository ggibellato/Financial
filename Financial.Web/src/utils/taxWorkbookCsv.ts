import type { TaxWorkbookDto } from '../api/types'

const CSV_COLUMNS = [
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
  'CalculationStatus',
  'EvidenceReference',
] as const

const CURRENCY_BY_JURISDICTION: Record<string, string> = {
  BR: 'BRL',
  UK: 'GBP',
}

function csvField(value: string | number | null | undefined): string {
  const text = value === null || value === undefined ? '' : String(value)
  return /[",\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text
}

export function buildTaxWorkbookCsv(workbook: TaxWorkbookDto): string {
  const currency = CURRENCY_BY_JURISDICTION[workbook.jurisdiction] ?? ''
  const rows = workbook.entries.map((entry) =>
    [
      entry.date,
      workbook.jurisdiction,
      workbook.taxYear,
      entry.eventCategory,
      entry.proceeds,
      entry.costBasis,
      entry.gainLoss,
      entry.grossAmount,
      entry.withheldAmount,
      entry.netAmount,
      currency,
      entry.calculationStatus,
      entry.evidenceReference,
    ]
      .map(csvField)
      .join(','),
  )

  return [CSV_COLUMNS.join(','), ...rows].join('\r\n')
}

export function buildTaxWorkbookCsvFilename(jurisdiction: string, taxYear: string): string {
  return `tax-workbook-${jurisdiction}-${taxYear.replace(/\//g, '-')}.csv`
}

export function downloadCsv(filename: string, csv: string): void {
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}
