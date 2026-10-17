import { readStorage, writeStorage } from './safeStorage'

export type Domain = 'investments' | 'cashflow'

const STORAGE_KEY = 'financial.selectedDomain'

export function getStoredDomain(): Domain | null {
  const value = readStorage('session', STORAGE_KEY)
  return value === 'investments' || value === 'cashflow' ? value : null
}

export function setStoredDomain(domain: Domain): void {
  writeStorage('session', STORAGE_KEY, domain)
}
