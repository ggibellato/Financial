import { readStorage, writeStorage } from './safeStorage'

const STORAGE_KEY = 'financial.sidebarCollapsed'

export function getStoredSidebarCollapsed(): boolean {
  return readStorage('local', STORAGE_KEY) === 'true'
}

export function setStoredSidebarCollapsed(collapsed: boolean): void {
  writeStorage('local', STORAGE_KEY, String(collapsed))
}
