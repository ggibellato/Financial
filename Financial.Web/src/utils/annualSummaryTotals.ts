import type { CategoryAnnualAverageDto } from '../api/types'

export const RESULTADO_LABEL = 'Resultado (R-D-Inv)'
export const TOTAL_DESPESAS_LABEL = 'Total despesas'

const TOTAL_CATEGORIES = [RESULTADO_LABEL, TOTAL_DESPESAS_LABEL]

export type HistoricYear = CategoryAnnualAverageDto
export type HistoricAverageOf = (category: string, year: HistoricYear['year']) => number

export function splitHistoricRows(years: HistoricYear[]) {
  const averages = new Map(years.map((y) => [y.year, new Map(y.annualAverages.map((d) => [d.category, d.value]))]))
  const rows = years[0]?.annualAverages ?? []
  const averageOf: HistoricAverageOf = (category, year) => averages.get(year)?.get(category) ?? 0
  return {
    categoryRows: rows.filter((row) => !TOTAL_CATEGORIES.includes(row.category)),
    totalRows: TOTAL_CATEGORIES.flatMap((category) => rows.filter((row) => row.category === category)),
    averageOf,
  }
}
