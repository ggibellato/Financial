import { act, renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useColumnFilters } from '../useColumnFilters'

interface Row {
  id: string
  category: string
  card: string | null
}

const ROWS: Row[] = [
  { id: 'e1', category: 'Mercado', card: 'Amex' },
  { id: 'e2', category: 'Casa', card: 'Amex' },
  { id: 'e3', category: 'Mercado', card: null },
]

const ACCESSORS = {
  category: (r: Row) => r.category,
  card: (r: Row) => r.card,
}

interface TransferRow {
  id: string
  banks: [string, string] | [string]
}

const TRANSFER_ROWS: TransferRow[] = [
  { id: 't1', banks: ['Barclays', 'Chase'] },
  { id: 't2', banks: ['Trading212'] },
]

describe('useColumnFilters', () => {
  it('returns every row unfiltered by default', () => {
    const { result } = renderHook(() => useColumnFilters(ROWS, ACCESSORS))

    expect(result.current.filteredRows).toEqual(ROWS)
    expect(result.current.isColumnFiltered('category')).toBe(false)
  })

  it('computes available values sorted and deduped, excluding null', () => {
    const { result } = renderHook(() => useColumnFilters(ROWS, ACCESSORS))

    expect(result.current.availableValues.category).toEqual(['Casa', 'Mercado'])
    expect(result.current.availableValues.card).toEqual(['Amex'])
  })

  it('unchecking one value hides only rows with that value', () => {
    const { result } = renderHook(() => useColumnFilters(ROWS, ACCESSORS))

    act(() => result.current.toggleValue('category', 'Casa'))

    expect(result.current.filteredRows.map((r) => r.id)).toEqual(['e1', 'e3'])
    expect(result.current.isColumnFiltered('category')).toBe(true)
  })

  it('unchecking every value results in an empty filteredRows, not an automatic reset', () => {
    const { result } = renderHook(() => useColumnFilters(ROWS, ACCESSORS))

    act(() => result.current.toggleValue('category', 'Casa'))
    act(() => result.current.toggleValue('category', 'Mercado'))

    expect(result.current.filteredRows).toEqual([])
  })

  it('toggleAll on a filtered column reverts it to unfiltered', () => {
    const { result } = renderHook(() => useColumnFilters(ROWS, ACCESSORS))

    act(() => result.current.toggleValue('category', 'Casa'))
    act(() => result.current.toggleAll('category'))

    expect(result.current.isColumnFiltered('category')).toBe(false)
    expect(result.current.filteredRows).toEqual(ROWS)
  })

  it('toggleAll on an unfiltered column unchecks everything', () => {
    const { result } = renderHook(() => useColumnFilters(ROWS, ACCESSORS))

    act(() => result.current.toggleAll('category'))

    expect(result.current.filteredRows).toEqual([])
  })

  it("a column's available values are unaffected by another column's active filter", () => {
    const { result } = renderHook(() => useColumnFilters(ROWS, ACCESSORS))

    act(() => result.current.toggleValue('category', 'Casa'))

    expect(result.current.availableValues.card).toEqual(['Amex'])
  })

  it('two filtered columns combine with AND', () => {
    const rowsWithTwoCards: Row[] = [...ROWS, { id: 'e4', category: 'Mercado', card: 'Visa' }]
    const { result } = renderHook(() => useColumnFilters(rowsWithTwoCards, ACCESSORS))

    act(() => result.current.toggleValue('category', 'Casa'))
    act(() => result.current.toggleValue('card', 'Visa'))

    expect(result.current.filteredRows.map((r) => r.id)).toEqual(['e1'])
  })

  it('a row with multiple values for a column is visible if any of them is checked', () => {
    const { result } = renderHook(() =>
      useColumnFilters(TRANSFER_ROWS, { bank: (r: TransferRow) => r.banks }),
    )

    act(() => result.current.toggleValue('bank', 'Barclays'))
    act(() => result.current.toggleValue('bank', 'Trading212'))

    expect(result.current.filteredRows.map((r) => r.id)).toEqual(['t1'])
  })
})
