import { act, renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useEntityForm } from '../useEntityForm'

describe('useEntityForm', () => {
  it('is dirty only while a value differs from the one it opened with', () => {
    const { result, rerender } = renderHook(({ name }) => useEntityForm({ name }), { initialProps: { name: 'XPI' } })
    expect(result.current.isDirty).toBe(false)

    rerender({ name: 'XPI 2' })
    expect(result.current.isDirty).toBe(true)

    rerender({ name: 'XPI' })
    expect(result.current.isDirty).toBe(false)
  })

  it('hides validation messages until validation is revealed', () => {
    const { result } = renderHook(() => useEntityForm({}))
    expect(result.current.shown('Name is required.')).toBe('')

    act(() => result.current.revealValidation())

    expect(result.current.shown('Name is required.')).toBe('Name is required.')
  })
})
