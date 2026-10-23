import { act, renderHook } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { useDiscardPrompt } from '../useDiscardPrompt'

describe('useDiscardPrompt', () => {
  it('runs the action at once when the form is clean', () => {
    const action = vi.fn()
    const { result } = renderHook(() => useDiscardPrompt(false))

    act(() => result.current.confirmDiscard(action))

    expect(action).toHaveBeenCalledOnce()
    expect(result.current.isPromptOpen).toBe(false)
  })

  it('opens the prompt instead of running the action when the form is dirty', () => {
    const action = vi.fn()
    const { result } = renderHook(() => useDiscardPrompt(true))

    act(() => result.current.confirmDiscard(action))

    expect(action).not.toHaveBeenCalled()
    expect(result.current.isPromptOpen).toBe(true)
  })

  it('drops the action when the user keeps editing', () => {
    const action = vi.fn()
    const { result } = renderHook(() => useDiscardPrompt(true))

    act(() => result.current.confirmDiscard(action))
    act(() => result.current.keepEditing())

    expect(action).not.toHaveBeenCalled()
    expect(result.current.isPromptOpen).toBe(false)
  })

  it('runs the action and closes the prompt when the user discards', () => {
    const action = vi.fn()
    const { result } = renderHook(() => useDiscardPrompt(true))

    act(() => result.current.confirmDiscard(action))
    act(() => result.current.discard())

    expect(action).toHaveBeenCalledOnce()
    expect(result.current.isPromptOpen).toBe(false)
  })
})
