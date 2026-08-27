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

  it('places rows with a null accessor value last in both ascending and descending order', () => {
    const { result } = renderHook(() => useSortableRows(ROWS, ACCESSORS))

    act(() => result.current.requestSort('value'))
    expect(result.current.sortedRows.at(-1)?.id).toBe('c')

    act(() => result.current.requestSort('value'))
    expect(result.current.sortedRows.at(-1)?.id).toBe('c')
  })
})
