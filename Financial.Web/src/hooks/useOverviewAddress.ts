import { useCallback } from 'react'
import { useSearchParams } from 'react-router-dom'
import { currentYearMonth, formatMonthInputValue, parseMonthInputValue } from '../utils/formatters'
import type { MonthSelection } from './useMonthSelection'

export interface OverviewAddress<TabId extends string> extends MonthSelection {
  tab: TabId
  setTab: (tab: TabId) => void
}

const PARAMS_KEPT_ON_MONTH_OR_TAB_CHANGE = ['month', 'tab'] as const

type KeptParam = (typeof PARAMS_KEPT_ON_MONTH_OR_TAB_CHANGE)[number]

export function useOverviewAddress<TabId extends string>(validTabs: readonly [TabId, ...TabId[]]): OverviewAddress<TabId> {
  const [searchParams, setSearchParams] = useSearchParams()
  const { year, month } = parseMonthInputValue(searchParams.get('month') ?? '') ?? currentYearMonth()
  const requestedTab = searchParams.get('tab')
  const tab = validTabs.find((id) => id === requestedTab) ?? validTabs[0]

  const setParam = useCallback(
    (name: KeptParam, value: string) =>
      setSearchParams(
        (params) => {
          const next = new URLSearchParams()
          for (const kept of PARAMS_KEPT_ON_MONTH_OR_TAB_CHANGE) {
            const keptValue = params.get(kept)
            if (keptValue !== null) next.set(kept, keptValue)
          }
          next.set(name, value)
          return next
        },
        { replace: true },
      ),
    [setSearchParams],
  )

  const setMonthInputValue = useCallback(
    (value: string) => {
      if (parseMonthInputValue(value)) setParam('month', value)
    },
    [setParam],
  )

  const setTab = useCallback((value: TabId) => setParam('tab', value), [setParam])

  return { year, month, monthInputValue: formatMonthInputValue(year, month), setMonthInputValue, tab, setTab }
}
