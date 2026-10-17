export type StorageArea = 'local' | 'session'

function resolveStorage(area: StorageArea): Storage {
  return area === 'local' ? localStorage : sessionStorage
}

export function readStorage(area: StorageArea, key: string): string | null {
  try {
    return resolveStorage(area).getItem(key)
  } catch {
    return null
  }
}

export function writeStorage(area: StorageArea, key: string, value: string): boolean {
  try {
    resolveStorage(area).setItem(key, value)
    return true
  } catch {
    return false
  }
}
