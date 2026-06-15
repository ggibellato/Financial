const rawUrl = import.meta.env.API_BASE_URL
if (!rawUrl) throw new Error('API_BASE_URL is not set — copy .env.example to .env')

export const API_BASE_URL = rawUrl.endsWith('/') ? rawUrl.slice(0, -1) : rawUrl
