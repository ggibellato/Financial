import { act, renderHook } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { usePendingDelete } from '../usePendingDelete'

interface Row {
  id: string
}

describe('usePendingDelete', () => {
  it('holds the requested row until it is confirmed, then deletes it once and clears', () => {
    const deleteRow = vi.fn()
    const { result } = renderHook(() => usePendingDelete<Row>(deleteRow))

    act(() => result.current.request({ id: 'a' }))
    expect(result.current.pendingRow).toEqual({ id: 'a' })
    expect(deleteRow).not.toHaveBeenCalled()

    act(() => result.current.confirm())

    expect(deleteRow).toHaveBeenCalledExactlyOnceWith({ id: 'a' })
    expect(result.current.pendingRow).toBeNull()
  })

  it('deletes nothing when cancelled, or confirmed with no row pending', () => {
    const deleteRow = vi.fn()
    const { result } = renderHook(() => usePendingDelete<Row>(deleteRow))

    act(() => result.current.request({ id: 'a' }))
    act(() => result.current.cancel())
    act(() => result.current.confirm())

    expect(result.current.pendingRow).toBeNull()
    expect(deleteRow).not.toHaveBeenCalled()
  })
})
