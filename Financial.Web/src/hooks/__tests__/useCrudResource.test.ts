import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useCrudResource, type CrudResourceOptions, type RefreshMode } from '../useCrudResource'

interface Item {
  id: string
}

const ITEMS: Item[] = [{ id: 'a' }, { id: 'b' }]
const NO_ITEMS: Item[] = []

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

describe('useCrudResource', () => {
  const load = vi.fn<() => Promise<Item[]>>()

  beforeEach(() => {
    load.mockReset()
    load.mockResolvedValue(ITEMS)
  })

  function renderCrud(refreshMode?: RefreshMode) {
    const options: CrudResourceOptions<Item[]> = {
      load,
      initialData: NO_ITEMS,
      loadErrorMessage: 'Unable to load items',
      refreshMode,
    }
    return renderHook(() => useCrudResource(options))
  }

  async function renderLoaded(refreshMode?: RefreshMode) {
    const rendered = renderCrud(refreshMode)
    await waitFor(() => expect(rendered.result.current.isLoading).toBe(false))
    return rendered
  }

  it('starts loading with the initial data and loads once on mount', async () => {
    const { result } = renderCrud()

    expect(result.current.isLoading).toBe(true)
    expect(result.current.data).toEqual([])
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(load).toHaveBeenCalledTimes(1)
    expect(result.current.data).toEqual(ITEMS)
    expect(result.current.error).toBeNull()
  })

  it('uses the fallback message when the load rejects with a non-Error value', async () => {
    load.mockRejectedValue('boom')
    const { result } = renderCrud()

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Unable to load items')
  })

  it('surfaces the load error message', async () => {
    load.mockRejectedValue(new Error('Network down'))
    const { result } = renderCrud()

    await waitFor(() => expect(result.current.error).toBe('Network down'))
  })

  it('retry reloads and clears a previous error', async () => {
    load.mockRejectedValueOnce(new Error('Network down'))
    const { result } = renderCrud()
    await waitFor(() => expect(result.current.error).toBe('Network down'))

    act(() => result.current.retry())

    await waitFor(() => expect(result.current.data).toEqual(ITEMS))
    expect(result.current.error).toBeNull()
    expect(load).toHaveBeenCalledTimes(2)
  })

  it('keeps the current data visible while a reload is in flight', async () => {
    const { result } = await renderLoaded()
    const pendingLoad = deferred<Item[]>()
    load.mockReturnValueOnce(pendingLoad.promise)

    act(() => result.current.retry())

    await waitFor(() => expect(result.current.isLoading).toBe(true))
    expect(result.current.data).toEqual(ITEMS)
    await act(async () => pendingLoad.resolve([{ id: 'c' }]))
    expect(result.current.data).toEqual([{ id: 'c' }])
  })

  it('mutateAndRefresh returns the mutation result and reloads through the loading state by default', async () => {
    const { result } = await renderLoaded()
    const pendingLoad = deferred<Item[]>()
    load.mockReturnValueOnce(pendingLoad.promise)

    let returned: Item | undefined
    await act(async () => {
      returned = await result.current.mutateAndRefresh(() => Promise.resolve({ id: 'new' }))
    })

    expect(returned).toEqual({ id: 'new' })
    expect(result.current.isLoading).toBe(true)
    expect(load).toHaveBeenCalledTimes(2)
    await act(async () => pendingLoad.resolve(ITEMS))
    expect(result.current.isLoading).toBe(false)
  })

  it('mutateAndRefresh in silent mode awaits the reload without flipping isLoading', async () => {
    const { result } = await renderLoaded('silent')
    load.mockResolvedValueOnce([{ id: 'c' }])

    await act(async () => {
      await result.current.mutateAndRefresh(() => Promise.resolve('ok'))
    })

    expect(result.current.isLoading).toBe(false)
    expect(result.current.data).toEqual([{ id: 'c' }])
    expect(load).toHaveBeenCalledTimes(2)
  })

  it('mutateAndRefresh rejects with the mutation error and does not reload', async () => {
    const { result } = await renderLoaded()

    await expect(result.current.mutateAndRefresh(() => Promise.reject(new Error('Duplicate')))).rejects.toThrow(
      'Duplicate',
    )
    expect(load).toHaveBeenCalledTimes(1)
  })

  it('refresh in silent mode reloads without flipping isLoading', async () => {
    const { result } = await renderLoaded('silent')
    const pendingLoad = deferred<Item[]>()
    load.mockReturnValueOnce(pendingLoad.promise)

    act(() => {
      void result.current.refresh()
    })

    expect(result.current.isLoading).toBe(false)
    await act(async () => pendingLoad.resolve([{ id: 'c' }]))
    expect(result.current.data).toEqual([{ id: 'c' }])
  })

  it('runPendingMutation tracks the pending id and reloads on success', async () => {
    const { result } = await renderLoaded()
    const mutation = deferred<void>()

    act(() => result.current.runPendingMutation('a', () => mutation.promise, 'Failed to delete item'))

    expect(result.current.pendingId).toBe('a')
    expect(result.current.pendingError).toBeNull()
    await act(async () => mutation.resolve())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(result.current.pendingId).toBeNull()
    expect(load).toHaveBeenCalledTimes(2)
  })

  it('runPendingMutation surfaces the error, clears the pending id and does not reload', async () => {
    const { result } = await renderLoaded()

    act(() =>
      result.current.runPendingMutation('a', () => Promise.reject(new Error('Still referenced')), 'Failed to delete item'),
    )

    await waitFor(() => expect(result.current.pendingError).toBe('Still referenced'))
    expect(result.current.pendingId).toBeNull()
    expect(load).toHaveBeenCalledTimes(1)
  })

  it('runPendingMutation calls onSuccess only when the mutation succeeds', async () => {
    const { result } = await renderLoaded()
    const onSuccess = vi.fn()

    act(() => result.current.runPendingMutation('a', () => Promise.reject(new Error('Still referenced')), 'Failed to delete item', onSuccess))
    await waitFor(() => expect(result.current.pendingError).toBe('Still referenced'))
    expect(onSuccess).not.toHaveBeenCalled()

    act(() => result.current.runPendingMutation('b', () => Promise.resolve(), 'Failed to delete item', onSuccess))
    await waitFor(() => expect(onSuccess).toHaveBeenCalledOnce())
  })

  it('runPendingMutation uses the fallback message for a non-Error rejection and clears it on the next attempt', async () => {
    const { result } = await renderLoaded()

    act(() => result.current.runPendingMutation('a', () => Promise.reject('nope'), 'Failed to delete item'))
    await waitFor(() => expect(result.current.pendingError).toBe('Failed to delete item'))

    const mutation = deferred<void>()
    act(() => result.current.runPendingMutation('b', () => mutation.promise, 'Failed to delete item'))

    expect(result.current.pendingId).toBe('b')
    expect(result.current.pendingError).toBeNull()
    await act(async () => mutation.resolve())
  })
})
