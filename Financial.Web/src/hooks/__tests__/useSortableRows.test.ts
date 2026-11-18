import { act, renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useSortableRows } from '../useSortableRows'

interface Row {
  id: string
  name: string
  value: number | null
  date: Date | null
}

const ROWS: Row[] = [
  { id: 'b', name: 'Banana', value: 42.5, date: new Date('2026-03-01') },
  { id: 'a', name: 'apple', value: 9.99, date: new Date('2026-01-10') },
  { id: 'c', name: 'Cherry', value: null, date: null },
]

const ACCESSORS = {
  name: (row: Row) => row.name,
  value: (row: Row) => row.value,
  date: (row: Row) => row.date,
}

describe('useSortableRows', () => {
  it('returns rows unsorted when no sort has been requested', () => {
    const { result } = renderHook(() => useSortableRows(ROWS, ACCESSORS))

    expect(result.current.sortState).toBeNull()
    expect(result.current.sortedRows.map((row) => row.id)).toEqual(['b', 'a', 'c'])
  })

  it('advances unsorted -> ascending -> descending -> unsorted on repeated calls to the same column', () => {
    const { result } = renderHook(() => useSortableRows(ROWS, ACCESSORS))

    act(() => result.current.requestSort('value'))
    expect(result.current.sortState).toEqual({ columnKey: 'value', direction: 'ascending' })

    act(() => result.current.requestSort('value'))
    expect(result.current.sortState).toEqual({ columnKey: 'value', direction: 'descending' })

    act(() => result.current.requestSort('value'))
    expect(result.current.sortState).toBeNull()
    expect(result.current.sortedRows.map((row) => row.id)).toEqual(['b', 'a', 'c'])
  })

  it('resets the previous column when a different column is requested', () => {
    const { result } = renderHook(() => useSortableRows(ROWS, ACCESSORS))

    act(() => result.current.requestSort('value'))
    act(() => result.current.requestSort('name'))

    expect(result.current.sortState).toEqual({ columnKey: 'name', direction: 'ascending' })
  })

  it('sorts numeric accessor values numerically, not as strings', () => {
    const { result } = renderHook(() => useSortableRows(ROWS, ACCESSORS))

    act(() => result.current.requestSort('value'))

    expect(result.current.sortedRows.map((row) => row.id)).toEqual(['a', 'b', 'c'])
  })

  it('sorts date accessor values chronologically', () => {
    const { result } = renderHook(() => useSortableRows(ROWS, ACCESSORS))

    act(() => result.current.requestSort('date'))

    expect(result.current.sortedRows.map((row) => row.id)).toEqual(['a', 'b', 'c'])
  })

  it('sorts string accessor values case-insensitively', () => {
    const { result } = renderHook(() => useSortableRows(ROWS, ACCESSORS))

    act(() => result.current.requestSort('name'))

    expect(result.current.sortedRows.map((row) => row.id)).toEqual(['a', 'b', 'c'])
  })

  it('starts sorted by the given defaultSort, as if that column had been clicked', () => {
    const { result } = renderHook(() =>
      useSortableRows(ROWS, ACCESSORS, { columnKey: 'date', direction: 'descending' }),
    )

    expect(result.current.sortState).toEqual({ columnKey: 'date', direction: 'descending' })
    expect(result.current.sortedRows.map((row) => row.id)).toEqual(['b', 'a', 'c'])
  })

  it('still cycles from the defaultSort when its own column is clicked again', () => {
    const { result } = renderHook(() =>
      useSortableRows(ROWS, ACCESSORS, { columnKey: 'date', direction: 'descending' }),
    )

    act(() => result.current.requestSort('date'))
    expect(result.current.sortState).toBeNull()
    expect(result.current.sortedRows.map((row) => row.id)).toEqual(['b', 'a', 'c'])
  })

  it.each([
    ['value', 'ascending', ['y', 'x', 'n1', 'n2']],
    ['value', 'descending', ['x', 'y', 'n1', 'n2']],
    ['date', 'ascending', ['y', 'x', 'n1', 'n2']],
    ['date', 'descending', ['x', 'y', 'n1', 'n2']],
  ] as const)('sorts %s %s with the empty rows last, in their original order', (columnKey, direction, ids) => {
    const rows: Row[] = [
      { id: 'n1', name: 'N1', value: null, date: null },
      { id: 'x', name: 'X', value: 5, date: new Date('2026-02-01') },
      { id: 'n2', name: 'N2', value: null, date: null },
      { id: 'y', name: 'Y', value: 1, date: new Date('2026-01-01') },
    ]

    const { result } = renderHook(() => useSortableRows(rows, ACCESSORS, { columnKey, direction }))

    expect(result.current.sortedRows.map((row) => row.id)).toEqual(ids)
  })

  it('leaves the rows in their order for a column it cannot sort', () => {
    const { result } = renderHook(() => useSortableRows(ROWS, ACCESSORS))

    act(() => result.current.requestSort('notes'))

    expect(result.current.sortState).toEqual({ columnKey: 'notes', direction: 'ascending' })
    expect(result.current.sortedRows.map((row) => row.id)).toEqual(['b', 'a', 'c'])
  })
})
