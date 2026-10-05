const API_PATH = '/api/v1/financial'
const SENTINEL_CATEGORY = 'E2E-TEST-DATA'
const READY_TIMEOUT_MS = 60_000
const POLL_INTERVAL_MS = 1_000

async function waitUntilHealthy(appUrl: string): Promise<void> {
  const deadline = Date.now() + READY_TIMEOUT_MS
  while (Date.now() < deadline) {
    const response = await fetch(`${appUrl}${API_PATH}/health`).catch(() => null)
    if (response?.ok) return
    await new Promise((resolve) => setTimeout(resolve, POLL_INTERVAL_MS))
  }
  throw new Error(`App not reachable at ${appUrl} within ${READY_TIMEOUT_MS / 1000} s`)
}

export default async function globalSetup(): Promise<void> {
  const appUrl = process.env.SMOKE_APP_URL?.replace(/\/$/, '')
  if (!appUrl) {
    throw new Error('SMOKE_APP_URL is required: point it at an app running on the test data, never the live one')
  }

  await waitUntilHealthy(appUrl)

  const response = await fetch(`${appUrl}${API_PATH}/categories`)
  const categories = (await response.json()) as { name: string }[]
  if (!categories.some((category) => category.name === SENTINEL_CATEGORY)) {
    throw new Error(`Refusing to run against non-test data: sentinel category ${SENTINEL_CATEGORY} not found at ${appUrl}`)
  }
}
