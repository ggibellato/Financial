import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { useConfirmedDelete } from '../useConfirmedDelete'
import { deferred } from '../../test-utils/deferred'

function renderDelete(remove: (target: string) => Promise<void>) {
  const onDeleted = vi.fn()
  const rendered = renderHook(() => useConfirmedDelete(remove, (target: string) => `Failed to delete ${target}`, onDeleted))
  return { ...rendered, onDeleted }
}

async function renderFailed(remove: (target: string) => Promise<void>) {
  const rendered = renderDelete(remove)
  act(() => rendered.result.current.request('rent'))
  act(() => rendered.result.current.confirm())
  await waitFor(() => expect(rendered.result.current.error).not.toBeNull())
  return rendered
}

describe('useConfirmedDelete', () => {
  it('deletes the requested target, closes and reports it', async () => {
    const { result, onDeleted } = renderDelete(() => Promise.resolve())

    act(() => result.current.request('rent'))
    await act(async () => result.current.confirm())

    expect(onDeleted).toHaveBeenCalledWith('rent')
    expect(result.current.isOpen).toBe(false)
    expect(result.current.isDeleting).toBe(false)
  })

  it('keeps the dialog open with the fallback message when the delete fails', async () => {
    const { result, onDeleted } = renderDelete(() => Promise.reject('nope'))

    act(() => result.current.request('rent'))
    act(() => result.current.confirm())

    await waitFor(() => expect(result.current.error).toBe('Failed to delete rent'))
    expect(result.current.isOpen).toBe(true)
    expect(onDeleted).not.toHaveBeenCalled()
  })

  it('clears the earlier failure when another delete is requested', async () => {
    const { result } = await renderFailed(() => Promise.reject('nope'))

    act(() => result.current.request('gym'))

    expect(result.current.error).toBeNull()
  })

  it('clears the earlier failure while the delete is tried again', async () => {
    const retry = deferred<void>()
    const remove = vi.fn<(target: string) => Promise<void>>().mockRejectedValueOnce('nope').mockReturnValueOnce(retry.promise)
    const { result } = await renderFailed(remove)

    act(() => result.current.confirm())

    expect(result.current.error).toBeNull()
    expect(result.current.isDeleting).toBe(true)
    await act(async () => retry.resolve())
  })

  it('cannot be cancelled while the delete is running', async () => {
    const removal = deferred<void>()
    const { result } = renderDelete(() => removal.promise)
    act(() => result.current.request('rent'))
    act(() => result.current.confirm())

    act(() => result.current.cancel())

    expect(result.current.isOpen).toBe(true)
    await act(async () => removal.resolve())
  })

  it('closes on cancel and deletes nothing', () => {
    const remove = vi.fn<(target: string) => Promise<void>>()
    const { result } = renderDelete(remove)
    act(() => result.current.request('rent'))

    act(() => result.current.cancel())
    act(() => result.current.confirm())

    expect(result.current.isOpen).toBe(false)
    expect(remove).not.toHaveBeenCalled()
  })
})
