import { useEffect, useState } from 'react'

const DARK_SCHEME_QUERY = '(prefers-color-scheme: dark)'

export function useSystemColorScheme(): 'light' | 'dark' {
  const [isDark, setIsDark] = useState(() => window.matchMedia(DARK_SCHEME_QUERY).matches)

  useEffect(() => {
    const mediaQueryList = window.matchMedia(DARK_SCHEME_QUERY)
    const listener = (event: MediaQueryListEvent) => setIsDark(event.matches)
    mediaQueryList.addEventListener('change', listener)
    return () => mediaQueryList.removeEventListener('change', listener)
  }, [])

  return isDark ? 'dark' : 'light'
}
