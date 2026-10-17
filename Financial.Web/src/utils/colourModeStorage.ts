import { readStorage, writeStorage } from './safeStorage'

const STORAGE_KEY = 'financial.colourMode'

export type ColourMode = 'light' | 'dark'

export function getStoredColourMode(): ColourMode {
  return readStorage('local', STORAGE_KEY) === 'dark' ? 'dark' : 'light'
}

export function setStoredColourMode(mode: ColourMode): void {
  writeStorage('local', STORAGE_KEY, mode)
}
