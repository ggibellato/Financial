import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { useMediaQuery } from '../useMediaQuery'
import { mockMatchMedia } from '../../test/mockMatchMedia'

const originalMatchMedia = window.matchMedia

describe('useMediaQuery', () => {
  afterEach(() => {
    window.matchMedia = originalMatchMedia
  })

  it('reflects the initial match state', () => {
    mockMatchMedia(true)

    const { result } = renderHook(() => useMediaQuery('(max-width: 599px)'))

    expect(result.current).toBe(true)
  })

  it('updates when the media query match state changes', () => {
    const { setMatches } = mockMatchMedia(false)

    const { result } = renderHook(() => useMediaQuery('(max-width: 599px)'))
    expect(result.current).toBe(false)

    act(() => setMatches(true))
    expect(result.current).toBe(true)
  })

  it('reads as no match where the browser has no matchMedia', () => {
    window.matchMedia = undefined as unknown as typeof window.matchMedia

    const { result } = renderHook(() => useMediaQuery('(max-width: 599px)'))

    expect(result.current).toBe(false)
  })

  it('follows a change of query', () => {
    window.matchMedia = vi.fn((query: string) => ({
      matches: query === '(min-width: 600px)',
      addEventListener: () => {},
      removeEventListener: () => {},
    })) as unknown as typeof window.matchMedia
    const { result, rerender } = renderHook(({ query }) => useMediaQuery(query), {
      initialProps: { query: '(max-width: 599px)' },
    })

    rerender({ query: '(min-width: 600px)' })

    expect(result.current).toBe(true)
  })

  it('stops listening once unmounted', () => {
    const addEventListener = vi.fn()
    const removeEventListener = vi.fn()
    window.matchMedia = vi.fn(() => ({ matches: false, addEventListener, removeEventListener })) as unknown as typeof window.matchMedia
    const { unmount } = renderHook(() => useMediaQuery('(max-width: 599px)'))

    unmount()

    expect(removeEventListener).toHaveBeenCalledWith(...addEventListener.mock.calls[0])
  })
})
