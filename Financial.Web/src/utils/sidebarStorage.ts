const STORAGE_KEY = 'financial.sidebarCollapsed'

export function getStoredSidebarCollapsed(): boolean {
  try {
    return localStorage.getItem(STORAGE_KEY) === 'true'
  } catch {
    return false
  }
}

export function setStoredSidebarCollapsed(collapsed: boolean): void {
  try {
    localStorage.setItem(STORAGE_KEY, String(collapsed))
  } catch {
    // localStorage unavailable (e.g. private browsing) - persistence is best-effort
  }
}
