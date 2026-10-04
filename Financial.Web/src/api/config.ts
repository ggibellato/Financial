const rawUrl = import.meta.env.API_BASE_URL
if (!rawUrl) {
  throw new Error(
    'API_BASE_URL must be set (for example /api/v1/financial); an empty value makes API calls hit the SPA fallback and return HTML',
  )
}
export const API_BASE_URL = rawUrl.endsWith('/') ? rawUrl.slice(0, -1) : rawUrl
