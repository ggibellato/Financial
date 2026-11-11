import { describe, expect, it } from 'vitest'
import type { CategoryAnnualAverageDto } from '../../api/types'
import { RESULTADO_LABEL, splitHistoricRows, TOTAL_DESPESAS_LABEL } from '../annualSummaryTotals'

const year = (y: CategoryAnnualAverageDto['year'], values: Record<string, number>): CategoryAnnualAverageDto =>
  ({ year: y, annualAverages: Object.entries(values).map(([category, value]) => ({ category, value })) }) as CategoryAnnualAverageDto

describe('splitHistoricRows', () => {
  it('keeps the totals out of the category rows and lists them Resultado first', () => {
    const years = [year(2026, { Casa: 10, [TOTAL_DESPESAS_LABEL]: 50, Mercado: 5, [RESULTADO_LABEL]: 7 })]

    const { categoryRows, totalRows } = splitHistoricRows(years)

    expect(categoryRows.map((row) => row.category)).toEqual(['Casa', 'Mercado'])
    expect(totalRows.map((row) => row.category)).toEqual([RESULTADO_LABEL, TOTAL_DESPESAS_LABEL])
  })

  it('looks up a category average per year, zero when the year has none', () => {
    const years = [year(2026, { Casa: 10 }), year(2025, { Mercado: 3 })]

    const { averageOf } = splitHistoricRows(years)

    expect(averageOf('Casa', 2026)).toBe(10)
    expect(averageOf('Casa', 2025)).toBe(0)
    expect(averageOf('Mercado', 2025)).toBe(3)
  })

  it('returns nothing when there are no years', () => {
    expect(splitHistoricRows([])).toMatchObject({ categoryRows: [], totalRows: [] })
  })
})
