import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
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
})
