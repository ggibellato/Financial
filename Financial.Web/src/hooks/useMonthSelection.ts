import { useCallback, useState } from 'react'
import { formatMonthInputValue, parseMonthInputValue } from '../utils/formatters'

export interface YearMonth {
  year: number
  month: number
}

export interface MonthSelection extends YearMonth {
  monthInputValue: string
  setMonthInputValue: (value: string) => void
}

export function useMonthSelection(initialMonth: YearMonth | (() => YearMonth)): MonthSelection {
  const [selected, setSelected] = useState<YearMonth>(initialMonth)

  const setMonthInputValue = useCallback((value: string) => {
    const parsed = parseMonthInputValue(value)
    if (!parsed) return
    setSelected(parsed)
  }, [])

  return {
    year: selected.year,
    month: selected.month,
    monthInputValue: formatMonthInputValue(selected.year, selected.month),
    setMonthInputValue,
  }
}
