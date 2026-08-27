import { useMemo, useState } from 'react'

export type ColumnFilterAccessor<T> = (row: T) => string | string[] | null | undefined

export interface UseColumnFiltersResult<T> {
  filteredRows: T[]
  availableValues: Record<string, string[]>
  selectedValues: Record<string, Set<string> | undefined>
  toggleValue: (columnKey: string, value: string) => void
  toggleAll: (columnKey: string) => void
  isColumnFiltered: (columnKey: string) => boolean
}

function normalizeValues(value: string | string[] | null | undefined): string[] {
  if (value == null) return []
  return Array.isArray(value) ? value.filter((v): v is string => v != null) : [value]
}

/**
 * Per-column checklist filtering: each column's available values are always derived from the
 * full unfiltered `rows`, so a value never disappears just because another column's filter
 * currently hides its rows. Multiple filtered columns combine with AND; a row with more than one
 * value for a single column (e.g. a transfer touching two banks) is visible if ANY of its values
 * is checked (OR within the column).
 */
export function useColumnFilters<T>(
  rows: T[],
  accessors: Record<string, ColumnFilterAccessor<T>>,
): UseColumnFiltersResult<T> {
  const [selections, setSelections] = useState<Record<string, Set<string>>>({})

  const availableValues = useMemo(() => {
    const result: Record<string, string[]> = {}
    for (const [columnKey, accessor] of Object.entries(accessors)) {
      const values = new Set<string>()
      for (const row of rows) {
        for (const value of normalizeValues(accessor(row))) {
          values.add(value)
        }
      }
      result[columnKey] = [...values].sort((a, b) => a.localeCompare(b, undefined, { sensitivity: 'base' }))
    }
    return result
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rows])

  const isColumnFiltered = (columnKey: string): boolean => {
    const selected = selections[columnKey]
    if (selected === undefined) return false
    return selected.size < (availableValues[columnKey]?.length ?? 0)
  }

  const toggleValue = (columnKey: string, value: string) => {
    setSelections((current) => {
      const existing = current[columnKey] ?? new Set(availableValues[columnKey] ?? [])
      const next = new Set(existing)
      if (next.has(value)) {
        next.delete(value)
      } else {
        next.add(value)
      }
      return { ...current, [columnKey]: next }
    })
  }

  const toggleAll = (columnKey: string) => {
    setSelections((current) => {
      const selected = current[columnKey]
      const currentlyFiltered = selected !== undefined && selected.size < (availableValues[columnKey]?.length ?? 0)
      const next = { ...current }
      if (currentlyFiltered) {
        // Partially checked -> (All) checks everything, i.e. reverts to unfiltered.
        delete next[columnKey]
      } else {
        // Fully checked (or untouched) -> (All) unchecks everything.
        next[columnKey] = new Set()
      }
      return next
    })
  }

  const filteredRows = useMemo(() => {
    const activeColumns = Object.keys(accessors).filter((columnKey) => isColumnFiltered(columnKey))
    if (activeColumns.length === 0) {
      return rows
    }

    return rows.filter((row) =>
      activeColumns.every((columnKey) => {
        const selected = selections[columnKey]
        const rowValues = normalizeValues(accessors[columnKey](row))
        return selected != null && rowValues.some((value) => selected.has(value))
      }),
    )
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rows, selections, availableValues])

  return { filteredRows, availableValues, selectedValues: selections, toggleValue, toggleAll, isColumnFiltered }
}
