const STORAGE_KEY = 'financial.colourMode'

export type ColourMode = 'light' | 'dark'

export function getStoredColourMode(): ColourMode {
  try {
    return localStorage.getItem(STORAGE_KEY) === 'dark' ? 'dark' : 'light'
  } catch {
    return 'light'
  }
}

export function setStoredColourMode(mode: ColourMode): void {
  try {
    localStorage.setItem(STORAGE_KEY, mode)
  } catch {
    // localStorage unavailable (e.g. private browsing) - persistence is best-effort
  }
}
