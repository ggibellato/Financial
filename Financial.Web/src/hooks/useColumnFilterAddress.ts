import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import type { ColumnSelections, ColumnSelectionsState } from './useColumnFilters'

function selectionsIn(params: URLSearchParams, columnKeys: readonly string[]): ColumnSelections {
  const selections: ColumnSelections = {}
  for (const key of columnKeys) {
    if (params.has(key)) selections[key] = new Set(params.getAll(key).filter(Boolean))
  }
  return selections
}

function withSelections(params: URLSearchParams, columnKeys: readonly string[], selections: ColumnSelections): URLSearchParams {
  const next = new URLSearchParams(params)
  for (const key of columnKeys) {
    next.delete(key)
    const selected = selections[key]
    if (selected === undefined) continue
    if (selected.size === 0) next.append(key, '')
    for (const value of [...selected].sort()) next.append(key, value)
  }
  return next
}

export function useColumnFilterAddress(columnKeys: readonly string[]): ColumnSelectionsState {
  const [searchParams, setSearchParams] = useSearchParams()
  const selections = useMemo(() => selectionsIn(searchParams, columnKeys), [searchParams, columnKeys])
  const setSelections = useCallback(
    (change: (current: ColumnSelections) => ColumnSelections) =>
      setSearchParams((params) => withSelections(params, columnKeys, change(selectionsIn(params, columnKeys))), { replace: true }),
    [setSearchParams, columnKeys],
  )
  return [selections, setSelections]
}
