export type Domain = 'investments' | 'cashflow'

const STORAGE_KEY = 'financial.selectedDomain'

export function getStoredDomain(): Domain | null {
  try {
    const value = sessionStorage.getItem(STORAGE_KEY)
    return value === 'investments' || value === 'cashflow' ? value : null
  } catch {
    return null
  }
}

export function setStoredDomain(domain: Domain): void {
  try {
    sessionStorage.setItem(STORAGE_KEY, domain)
  } catch {
    // sessionStorage unavailable (e.g. private browsing) - persistence is best-effort
  }
}
