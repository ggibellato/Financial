import { act, renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useMonthSelection } from '../useMonthSelection'

describe('useMonthSelection', () => {
  it('starts on the given month and formats it as a month input value', () => {
    const { result } = renderHook(() => useMonthSelection({ year: 2026, month: 3 }))

    expect(result.current.year).toBe(2026)
    expect(result.current.month).toBe(3)
    expect(result.current.monthInputValue).toBe('2026-03')
  })

  it('reads a lazy initial month only once, at mount', () => {
    let calls = 0
    const { result, rerender } = renderHook(() =>
      useMonthSelection(() => {
        calls += 1
        return { year: 2025, month: 11 }
      }),
    )

    rerender()

    expect(calls).toBe(1)
    expect(result.current.monthInputValue).toBe('2025-11')
  })

  it('moves to the month typed into the month input', () => {
    const { result } = renderHook(() => useMonthSelection({ year: 2026, month: 3 }))

    act(() => result.current.setMonthInputValue('2024-12'))

    expect(result.current.year).toBe(2024)
    expect(result.current.month).toBe(12)
    expect(result.current.monthInputValue).toBe('2024-12')
  })

  it('keeps the current month when the input value is not a valid month', () => {
    const { result } = renderHook(() => useMonthSelection({ year: 2026, month: 3 }))

    act(() => result.current.setMonthInputValue('not-a-month'))

    expect(result.current.monthInputValue).toBe('2026-03')
  })

  it('keeps setMonthInputValue stable across month changes', () => {
    const { result } = renderHook(() => useMonthSelection({ year: 2026, month: 3 }))
    const initialSetter = result.current.setMonthInputValue

    act(() => result.current.setMonthInputValue('2026-04'))

    expect(result.current.setMonthInputValue).toBe(initialSetter)
  })
})
