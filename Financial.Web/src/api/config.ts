const rawUrl = import.meta.env.API_BASE_URL ?? ''
export const API_BASE_URL = rawUrl.endsWith('/') ? rawUrl.slice(0, -1) : rawUrl
