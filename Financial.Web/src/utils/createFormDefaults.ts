const STORAGE_PREFIX = 'financial.createFormDefault.'

export function getStoredDefault(key: string): string | null {
  try {
    return sessionStorage.getItem(STORAGE_PREFIX + key)
  } catch {
    return null
  }
}

export function setStoredDefault(key: string, value: string): void {
  try {
    sessionStorage.setItem(STORAGE_PREFIX + key, value)
  } catch {
    // sessionStorage unavailable (e.g. private browsing) - persistence is best-effort
  }
}
