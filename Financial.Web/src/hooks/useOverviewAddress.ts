import { useCallback } from 'react'
import { useSearchParams } from 'react-router-dom'
import { currentYearMonth, formatMonthInputValue, parseMonthInputValue } from '../utils/formatters'
import type { MonthSelection } from './useMonthSelection'

export function useOverviewAddress(): MonthSelection {
  const [searchParams, setSearchParams] = useSearchParams()
  const { year, month } = parseMonthInputValue(searchParams.get('month') ?? '') ?? currentYearMonth()

  const setMonthInputValue = useCallback(
    (value: string) => {
      if (!parseMonthInputValue(value)) return
      setSearchParams(
        (params) => {
          const next = new URLSearchParams(params)
          next.set('month', value)
          return next
        },
        { replace: true },
      )
    },
    [setSearchParams],
  )

  return { year, month, monthInputValue: formatMonthInputValue(year, month), setMonthInputValue }
}
