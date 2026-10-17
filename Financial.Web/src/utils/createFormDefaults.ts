import { readStorage, writeStorage } from './safeStorage'

const STORAGE_PREFIX = 'financial.createFormDefault.'

export function getStoredDefault(key: string): string | null {
  return readStorage('session', STORAGE_PREFIX + key)
}

export function setStoredDefault(key: string, value: string): void {
  writeStorage('session', STORAGE_PREFIX + key, value)
}
