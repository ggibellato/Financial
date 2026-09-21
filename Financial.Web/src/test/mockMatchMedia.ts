import { vi } from 'vitest'

export function mockMatchMedia(initialMatches: boolean) {
  let matches = initialMatches
  let changeHandler: (() => void) | undefined

  const mediaQueryList = {
    get matches() {
      return matches
    },
    media: '',
    onchange: null,
    addEventListener: (event: string, handler: () => void) => {
      if (event === 'change') changeHandler = handler
    },
    removeEventListener: (event: string, handler: () => void) => {
      if (event === 'change' && changeHandler === handler) changeHandler = undefined
    },
    addListener: () => {},
    removeListener: () => {},
    dispatchEvent: () => false,
  } as unknown as MediaQueryList

  window.matchMedia = vi.fn().mockReturnValue(mediaQueryList)

  return {
    setMatches: (next: boolean) => {
      matches = next
      changeHandler?.()
    },
  }
}
