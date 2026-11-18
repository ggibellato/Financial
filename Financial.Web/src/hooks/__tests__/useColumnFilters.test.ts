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

  it('clearFilters empties every column selection and restores all rows', () => {
    const { result } = renderHook(() => useColumnFilters(ROWS, ACCESSORS))
    act(() => result.current.toggleValue('category', 'Casa'))
    act(() => result.current.toggleValue('card', 'Amex'))

    act(() => result.current.clearFilters())

    expect(result.current.filteredRows).toEqual(ROWS)
    expect(result.current.isColumnFiltered('category')).toBe(false)
    expect(result.current.isColumnFiltered('card')).toBe(false)
    expect(result.current.selectedValues).toEqual({})
  })

  it('summarises whether any column filters the rows, with the shown and total counts', () => {
    const { result } = renderHook(() => useColumnFilters(ROWS, ACCESSORS))
    expect(result.current.filterSummary).toMatchObject({ isFiltered: false, shownCount: 3, totalCount: 3 })

    act(() => result.current.toggleValue('category', 'Casa'))

    expect(result.current.isFiltered).toBe(true)
    expect(result.current.filterSummary).toMatchObject({ isFiltered: true, shownCount: 2, totalCount: 3 })

    act(() => result.current.filterSummary.onClearFilters())

    expect(result.current.filterSummary).toMatchObject({ isFiltered: false, shownCount: 3, totalCount: 3 })
  })

  it('filters by a selection it is handed, even one naming a value the rows no longer have', () => {
    const selections = { category: new Set(['Mercado', 'Gone']) }
    const { result } = renderHook(() => useColumnFilters(ROWS, ACCESSORS, [selections, () => {}]))

    expect(result.current.filteredRows.map((r) => r.id)).toEqual(['e1', 'e3'])
    expect(result.current.isColumnFiltered('category')).toBe(true)
  })

  it('reads a column with no accessor as unfiltered, whatever it is handed', () => {
    const selections = { notes: new Set<string>() }
    const { result } = renderHook(() => useColumnFilters(ROWS, ACCESSORS, [selections, () => {}]))

    expect(result.current.isColumnFiltered('notes')).toBe(false)
  })

  describe('a text column', () => {
    interface Payment {
      id: string
      description: string | null
      category: string
    }

    const PAYMENTS: Payment[] = [
      { id: 'p1', description: 'Grocery Store', category: 'Mercado' },
      { id: 'p2', description: 'Online grocery', category: 'Casa' },
      { id: 'p3', description: 'Rent', category: 'Casa' },
      { id: 'p4', description: null, category: 'Mercado' },
    ]

    const TEXT_ACCESSORS = { description: (p: Payment) => p.description }

    function renderPayments() {
      return renderHook(() => useColumnFilters(PAYMENTS, { category: (p: Payment) => p.category }, undefined, TEXT_ACCESSORS))
    }

    it('keeps the rows whose text contains the search, ignoring case and surrounding spaces', () => {
      const { result } = renderPayments()

      act(() => result.current.setSearchText('description', '  GROCERY '))

      expect(result.current.filteredRows.map((p) => p.id)).toEqual(['p1', 'p2'])
      expect(result.current.searchText('description')).toBe('  GROCERY ')
      expect(result.current.isColumnFiltered('description')).toBe(true)
      expect(result.current.filterSummary).toMatchObject({ isFiltered: true, shownCount: 2, totalCount: 4 })
    })

    it('combines with the checklist columns, and blank search text filters nothing', () => {
      const { result } = renderPayments()

      act(() => result.current.setSearchText('description', 'grocery'))
      act(() => result.current.toggleValue('category', 'Mercado'))

      expect(result.current.filteredRows.map((p) => p.id)).toEqual(['p2'])

      act(() => result.current.setSearchText('description', '   '))

      expect(result.current.filteredRows.map((p) => p.id)).toEqual(['p2', 'p3'])
      expect(result.current.isColumnFiltered('description')).toBe(false)
    })

    it('clears the search with every other filter', () => {
      const { result } = renderPayments()
      act(() => result.current.setSearchText('description', 'rent'))
      act(() => result.current.toggleValue('category', 'Casa'))

      act(() => result.current.clearFilters())

      expect(result.current.filteredRows).toEqual(PAYMENTS)
      expect(result.current.searchText('description')).toBe('')
      expect(result.current.isFiltered).toBe(false)
    })
  })
})
