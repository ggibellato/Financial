import { act, renderHook } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { useDialogSubmit } from '../useDialogSubmit'

describe('useDialogSubmit', () => {
  it('starts idle with no error', () => {
    const { result } = renderHook(() => useDialogSubmit('The bank could not be saved.'))

    expect(result.current.isSaving).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('reports saving while the save is pending and stays saving once it resolves', async () => {
    let resolveSave: () => void = () => {}
    const save = vi.fn(() => new Promise<void>((resolve) => { resolveSave = resolve }))
    const { result } = renderHook(() => useDialogSubmit('The bank could not be saved.'))

    let pending: Promise<void> = Promise.resolve()
    act(() => { pending = result.current.submit(save) })

    expect(result.current.isSaving).toBe(true)

    await act(async () => {
      resolveSave()
      await pending
    })

    expect(save).toHaveBeenCalledTimes(1)
    expect(result.current.isSaving).toBe(true)
    expect(result.current.error).toBeNull()
  })

  it('shows the rejection message and re-enables saving when the save throws an Error', async () => {
    const { result } = renderHook(() => useDialogSubmit('The bank could not be saved.'))

    await act(() => result.current.submit(() => Promise.reject(new Error('A bank named "Barclays" already exists.'))))

    expect(result.current.error).toBe('A bank named "Barclays" already exists.')
    expect(result.current.isSaving).toBe(false)
  })

  it('falls back to the given message when the rejection is not an Error', async () => {
    const { result } = renderHook(() => useDialogSubmit('The bank could not be saved.'))

    await act(() => result.current.submit(() => Promise.reject('network down')))

    expect(result.current.error).toBe('The bank could not be saved.')
  })

  it('clears the previous error when a new save starts', async () => {
    const { result } = renderHook(() => useDialogSubmit('The bank could not be saved.'))
    await act(() => result.current.submit(() => Promise.reject(new Error('Server unavailable.'))))

    act(() => { void result.current.submit(() => new Promise(() => {})) })

    expect(result.current.error).toBeNull()
    expect(result.current.isSaving).toBe(true)
  })
})
