import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { useAsyncResource, type AsyncResourceOptions } from '../useAsyncResource'

function deferred<T>() {
  let resolve!: (value: T) => void
  const promise = new Promise<T>((res) => {
    resolve = res
  })
  return { promise, resolve }
}

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

  it('settles to idle when there is nothing to fetch even with initialLoading set', () => {
    const { result } = renderHook(() =>
      useAsyncResource<string>(() => null, [], 'Fallback message', { initialLoading: true }),
    )

    expect(result.current.isLoading).toBe(false)
  })

  it('reports loading before the first fetch settles when initialLoading is set', () => {
    const pending = deferred<string>()
    const { result } = renderResource(() => pending.promise, { initialLoading: true })

    expect(result.current.isLoading).toBe(true)
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

  it('ignores a refresh that returns after a newer dependency change', async () => {
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
      staleRefresh.resolve('refreshed-a')
      await refreshing
    })

    expect(result.current.data).toBe('value-b')
  })

  it('ignores a refresh failure that returns after a newer dependency change', async () => {
    let rejectRefresh!: (reason: unknown) => void
    const staleRefresh = new Promise<string>((_resolve, reject) => {
      rejectRefresh = reject
    })
    const fetcher = vi
      .fn<(key: string) => Promise<string>>()
      .mockResolvedValueOnce('value-a')
      .mockReturnValueOnce(staleRefresh)
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
      rejectRefresh(new Error('stale failure'))
      await refreshing
    })

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
})
