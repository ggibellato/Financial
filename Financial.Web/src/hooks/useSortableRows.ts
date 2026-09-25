import { useMemo, useState } from 'react'

export type SortDirection = 'ascending' | 'descending'

export interface SortState {
  columnKey: string
  direction: SortDirection
}

export type SortAccessor<T> = (row: T) => string | number | Date | null | undefined

/** Default sort for record-list grids whose primary column is `date` — newest first. */
export const DATE_DESC_SORT: SortState = { columnKey: 'date', direction: 'descending' }

export interface UseSortableRowsResult<T> {
  sortedRows: T[]
  sortState: SortState | null
  requestSort: (columnKey: string) => void
}

function compareValues(a: string | number | Date | null | undefined, b: string | number | Date | null | undefined): number {
  if (a == null && b == null) return 0
  if (a == null) return 1
  if (b == null) return -1

  if (a instanceof Date && b instanceof Date) {
    return a.getTime() - b.getTime()
  }

  if (typeof a === 'number' && typeof b === 'number') {
    return a - b
  }

  return String(a).localeCompare(String(b), undefined, { sensitivity: 'base' })
}

/**
 * Generic single-column sort: unsorted -> ascending -> descending -> unsorted.
 * Rows with a null/undefined accessor value always sort last, in both directions.
 *
 * `defaultSort` seeds the initial state (e.g. newest-first grids default to
 * `{ columnKey: 'date', direction: 'descending' }`) so the column header shows
 * the same active-sort indicator it would after the user clicked it.
 */
export function useSortableRows<T>(
  rows: T[],
  accessors: Record<string, SortAccessor<T>>,
  defaultSort?: SortState,
): UseSortableRowsResult<T> {
  const [sortState, setSortState] = useState<SortState | null>(defaultSort ?? null)

  const requestSort = (columnKey: string) => {
    setSortState((current) => {
      if (!current || current.columnKey !== columnKey) {
        return { columnKey, direction: 'ascending' }
      }
      if (current.direction === 'ascending') {
        return { columnKey, direction: 'descending' }
      }
      return null
    })
  }

  const sortedRows = useMemo(() => {
    if (!sortState) return rows

    const accessor = accessors[sortState.columnKey]
    if (!accessor) return rows

    const directionMultiplier = sortState.direction === 'ascending' ? 1 : -1

    return [...rows].sort((rowA, rowB) => {
      const valueA = accessor(rowA)
      const valueB = accessor(rowB)
      const comparison = compareValues(valueA, valueB)

      // Null-last ordering must hold in both directions, so only flip the sign
      // when neither side is null (compareValues already pinned null to +1/-1).
      return valueA == null || valueB == null ? comparison : comparison * directionMultiplier
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rows, sortState])

  return { sortedRows, sortState, requestSort }
}
