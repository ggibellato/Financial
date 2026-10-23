export class ApiError extends Error {
  status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

export const NETWORK_FAILURE_STATUS = 0

export function isNetworkFailure(err: unknown): boolean {
  return err instanceof ApiError && err.status === NETWORK_FAILURE_STATUS
}
