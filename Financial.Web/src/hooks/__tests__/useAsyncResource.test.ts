import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { useAsyncResource, useSelectionGuard, type AsyncResourceOptions } from '../useAsyncResource'
import { deferred, never } from '../../test-utils/deferred'

function renderResource(fetcher: (key: string) => Promise<string> | null, options?: AsyncResourceOptions) {
  return renderHook(({ key }) => useAsyncResource<string>(() => fetcher(key), [key], 'Fallback message', options), {
    initialProps: { key: 'a' },
  })
}

describe('useAsyncResource', () => {
  it('starts idle by default and loads the fetched data', async () => {
    const { result } = renderResource((key) => Promise.resolve(`value-${key}`))

    await waitFor(() => expect(result.current.data).toBe('value-a'))
    expect(result.current.isLoading).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it.each([
    ['is not loading', undefined, false],
    ['reports loading', { initialLoading: true }, true],
  ])('%s on the first render with options %o', (_, options, loading) => {
    const seen: boolean[] = []

    renderHook(() => {
      seen.push(useAsyncResource<string>(() => never(), [], 'Fallback message', options).isLoading)
    })

    expect(seen[0]).toBe(loading)
  })

  it('ignores a response that arrives after the selection was cleared', async () => {
    const slow = deferred<string>()
    const { result, rerender } = renderResource((key) => (key === 'a' ? slow.promise : null))

    rerender({ key: '' })
    await act(async () => slow.resolve('value-a'))

    expect(result.current.data).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })

  it('settles to idle when there is nothing to fetch even with initialLoading set', () => {
    const { result } = renderHook(() =>
      useAsyncResource<string>(() => null, [], 'Fallback message', { initialLoading: true }),
    )

    expect(result.current.isLoading).toBe(false)
  })

  it('clears the previous data while a dependency change reloads by default', async () => {
    const next = deferred<string>()
    const fetcher = vi.fn((key: string) => (key === 'a' ? Promise.resolve('value-a') : next.promise))
    const { result, rerender } = renderResource(fetcher)
    await waitFor(() => expect(result.current.data).toBe('value-a'))

    rerender({ key: 'b' })

    expect(result.current.isLoading).toBe(true)
    expect(result.current.data).toBeNull()
  })

  it('keeps the previous data while a dependency change reloads when keepDataWhileLoading is set', async () => {
    const next = deferred<string>()
    const fetcher = vi.fn((key: string) => (key === 'a' ? Promise.resolve('value-a') : next.promise))
    const { result, rerender } = renderResource(fetcher, { keepDataWhileLoading: true })
    await waitFor(() => expect(result.current.data).toBe('value-a'))

    rerender({ key: 'b' })

    expect(result.current.isLoading).toBe(true)
    expect(result.current.data).toBe('value-a')

    await act(async () => next.resolve('value-b'))
    expect(result.current.data).toBe('value-b')
  })

  it('ignores a response that arrives after a newer dependency change', async () => {
    const slow = deferred<string>()
    const fast = deferred<string>()
    const { result, rerender } = renderResource((key) => (key === 'a' ? slow.promise : fast.promise))

    rerender({ key: 'b' })
    await act(async () => fast.resolve('value-b'))
    await act(async () => slow.resolve('value-a'))

    expect(result.current.data).toBe('value-b')
    expect(result.current.isLoading).toBe(false)
  })

  it('ignores a failure that arrives after a newer dependency change', async () => {
    let rejectSlow!: (reason: unknown) => void
    const slow = new Promise<string>((_resolve, reject) => {
      rejectSlow = reject
    })
    const { result, rerender } = renderResource((key) => (key === 'a' ? slow : Promise.resolve('value-b')))

    rerender({ key: 'b' })
    await waitFor(() => expect(result.current.data).toBe('value-b'))
    await act(async () => rejectSlow(new Error('stale failure')))

    expect(result.current.error).toBeNull()
  })

  it.each([
    ['response', (stale: ReturnType<typeof deferred<string>>) => stale.resolve('refreshed-a')],
    ['failure', (stale: ReturnType<typeof deferred<string>>) => stale.reject(new Error('stale failure'))],
  ])('ignores a refresh %s that returns after a newer dependency change', async (_outcome, settle) => {
    const staleRefresh = deferred<string>()
    const fetcher = vi
      .fn<(key: string) => Promise<string>>()
      .mockResolvedValueOnce('value-a')
      .mockReturnValueOnce(staleRefresh.promise)
      .mockResolvedValueOnce('value-b')
    const { result, rerender } = renderResource(fetcher)
    await waitFor(() => expect(result.current.data).toBe('value-a'))

    let refreshing!: Promise<void>
    act(() => {
      refreshing = result.current.refresh()
    })
    rerender({ key: 'b' })
    await waitFor(() => expect(result.current.data).toBe('value-b'))
    await act(async () => {
      settle(staleRefresh)
      await refreshing
    })

    expect(result.current.data).toBe('value-b')
    expect(result.current.error).toBeNull()
  })

  it('refresh re-fetches without flipping isLoading', async () => {
    const fetcher = vi.fn<(key: string) => Promise<string>>().mockResolvedValueOnce('first')
    const { result } = renderResource(fetcher)
    await waitFor(() => expect(result.current.data).toBe('first'))

    const next = deferred<string>()
    fetcher.mockReturnValueOnce(next.promise)
    let refreshing!: Promise<void>
    act(() => {
      refreshing = result.current.refresh()
    })

    expect(result.current.isLoading).toBe(false)
    expect(result.current.data).toBe('first')

    await act(async () => {
      next.resolve('second')
      await refreshing
    })
    expect(result.current.data).toBe('second')
    expect(fetcher).toHaveBeenCalledTimes(2)
  })

  it('refresh surfaces a failure as the error, keeping the previous data', async () => {
    const fetcher = vi
      .fn<(key: string) => Promise<string>>()
      .mockResolvedValueOnce('first')
      .mockRejectedValueOnce('not an error')
    const { result } = renderResource(fetcher)
    await waitFor(() => expect(result.current.data).toBe('first'))

    await act(async () => {
      await result.current.refresh()
    })

    expect(result.current.error).toBe('Fallback message')
    expect(result.current.data).toBe('first')
  })

  it('refresh clears a previous error once it succeeds', async () => {
    const fetcher = vi
      .fn<(key: string) => Promise<string>>()
      .mockResolvedValueOnce('first')
      .mockRejectedValueOnce('not an error')
      .mockResolvedValueOnce('second')
    const { result } = renderResource(fetcher)
    await waitFor(() => expect(result.current.data).toBe('first'))
    await act(async () => {
      await result.current.refresh()
    })
    expect(result.current.error).toBe('Fallback message')

    await act(async () => {
      await result.current.refresh()
    })

    expect(result.current.error).toBeNull()
    expect(result.current.data).toBe('second')
  })

  it('refresh does nothing when the fetcher has nothing to fetch', async () => {
    const { result } = renderHook(() => useAsyncResource<string>(() => null, [], 'Fallback message'))

    await act(async () => {
      await result.current.refresh()
    })

    expect(result.current.data).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })

  it('setData replaces the data from the current value without touching loading or error', async () => {
    const { result } = renderResource(() => Promise.resolve('value'))
    await waitFor(() => expect(result.current.data).toBe('value'))

    act(() => result.current.setData((current) => `${current}-updated`))

    expect(result.current.data).toBe('value-updated')
    expect(result.current.isLoading).toBe(false)
    expect(result.current.error).toBeNull()
  })

  describe('useSelectionGuard', () => {
    it('dispatches before any selection has begun', () => {
      const dispatch = vi.fn()
      const { result } = renderHook(() => useSelectionGuard<string>(dispatch))

      expect(result.current.dispatchForCurrentSelection()('loaded')).toBe(true)
      expect(dispatch).toHaveBeenCalledWith('loaded')
    })

    it('drops what an earlier selection dispatches once a newer one begins', () => {
      const dispatch = vi.fn()
      const { result } = renderHook(() => useSelectionGuard<string>(dispatch))
      const earlier = result.current.beginSelection()
      const newer = result.current.beginSelection()

      expect(earlier('stale')).toBe(false)
      expect(newer('fresh')).toBe(true)
      expect(dispatch.mock.calls).toEqual([['fresh']])
    })
  })
})
