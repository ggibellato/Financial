export const DEFAULT_API_BASE_URL = 'https://localhost:7256/api/v1/financial'

export function resolveApiBaseUrl(baseUrl?: string): string {
  const candidate = baseUrl?.trim()
  const resolved = candidate && candidate.length > 0 ? candidate : DEFAULT_API_BASE_URL
  return resolved.endsWith('/') ? resolved.slice(0, -1) : resolved
}
