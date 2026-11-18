import { useMemo, useState } from 'react'

export const NO_FILTER_MATCH_MESSAGE = 'No rows match the current filters.'

export type ColumnFilterAccessor<T> = (row: T) => string | string[] | null | undefined

export type TextFilterAccessor<T> = (row: T) => string | null | undefined

export type ColumnSelections = Record<string, Set<string>>

export type ColumnSelectionsState = [ColumnSelections, (change: (current: ColumnSelections) => ColumnSelections) => void]

export interface FilterSummary {
  isFiltered: boolean
  shownCount: number
  totalCount: number
  onClearFilters: () => void
}

export interface UseColumnFiltersResult<T> {
  filteredRows: T[]
  availableValues: Record<string, string[]>
  selectedValues: Record<string, Set<string> | undefined>
  toggleValue: (columnKey: string, value: string) => void
  toggleAll: (columnKey: string) => void
  isColumnFiltered: (columnKey: string) => boolean
  searchText: (columnKey: string) => string
  setSearchText: (columnKey: string, text: string) => void
  isFiltered: boolean
  clearFilters: () => void
  filterSummary: FilterSummary
}

function hasUncheckedValue(selected: Set<string> | undefined, available: string[] = []): boolean {
  return selected !== undefined && available.some((value) => !selected.has(value))
}

function searchTextIn(selected: Set<string> | undefined): string {
  return selected?.values().next().value ?? ''
}

function searchNeedle(text: string): string {
  return text.trim().toLocaleLowerCase()
}

function containsText(value: string | null | undefined, needle: string): boolean {
  return (value ?? '').toLocaleLowerCase().includes(needle)
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
 * is checked (OR within the column). A text column keeps its search text as its only selected
 * value, so one selections state (and one address update) covers both kinds of filter.
 */
export function useColumnFilters<T>(
  rows: T[],
  accessors: Record<string, ColumnFilterAccessor<T>>,
  selectionsState?: ColumnSelectionsState,
  textAccessors: Record<string, TextFilterAccessor<T>> = {},
): UseColumnFiltersResult<T> {
  const localSelections = useState<ColumnSelections>({})
  const [selections, setSelections] = selectionsState ?? localSelections

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

  const searchText = (columnKey: string): string => searchTextIn(selections[columnKey])

  const isColumnFiltered = (columnKey: string): boolean =>
    columnKey in textAccessors
      ? searchNeedle(searchText(columnKey)) !== ''
      : hasUncheckedValue(selections[columnKey], availableValues[columnKey])

  const setSearchText = (columnKey: string, text: string) => {
    setSelections((current) => {
      const next = { ...current }
      if (text) next[columnKey] = new Set([text])
      else delete next[columnKey]
      return next
    })
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
      const currentlyFiltered = hasUncheckedValue(selected, availableValues[columnKey])
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

  const clearFilters = () => setSelections(() => ({}))

  const filteredRows = useMemo(() => {
    const valuePredicates = Object.entries(accessors)
      .filter(([columnKey]) => isColumnFiltered(columnKey))
      .map(([columnKey, accessor]) => {
        const selected = selections[columnKey]
        return (row: T) => normalizeValues(accessor(row)).some((value) => selected.has(value))
      })
    const textPredicates = Object.entries(textAccessors)
      .filter(([columnKey]) => isColumnFiltered(columnKey))
      .map(([columnKey, accessor]) => {
        const needle = searchNeedle(searchText(columnKey))
        return (row: T) => containsText(accessor(row), needle)
      })
    const predicates = [...valuePredicates, ...textPredicates]
    return predicates.length === 0 ? rows : rows.filter((row) => predicates.every((matches) => matches(row)))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rows, selections, availableValues])

  const isFiltered = [...Object.keys(accessors), ...Object.keys(textAccessors)].some(isColumnFiltered)
  const filterSummary = { isFiltered, shownCount: filteredRows.length, totalCount: rows.length, onClearFilters: clearFilters }

  return {
    filteredRows,
    availableValues,
    selectedValues: selections,
    toggleValue,
    toggleAll,
    isColumnFiltered,
    searchText,
    setSearchText,
    isFiltered,
    clearFilters,
    filterSummary,
  }
}
