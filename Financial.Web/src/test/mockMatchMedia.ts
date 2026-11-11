import { vi } from 'vitest'

export function mockMatchMedia(initialMatches: boolean) {
  let matches = initialMatches
  const changeHandlers = new Set<(event: MediaQueryListEvent) => void>()

  const mediaQueryList = {
    get matches() {
      return matches
    },
    media: '',
    onchange: null,
    addEventListener: (event: string, handler: (event: MediaQueryListEvent) => void) => {
      if (event === 'change') changeHandlers.add(handler)
    },
    removeEventListener: (event: string, handler: (event: MediaQueryListEvent) => void) => {
      if (event === 'change') changeHandlers.delete(handler)
    },
    addListener: () => {},
    removeListener: () => {},
    dispatchEvent: () => false,
  } as unknown as MediaQueryList

  window.matchMedia = vi.fn().mockReturnValue(mediaQueryList)

  return {
    setMatches: (next: boolean) => {
      matches = next
      changeHandlers.forEach((handler) => handler({ matches: next } as MediaQueryListEvent))
    },
  }
}
