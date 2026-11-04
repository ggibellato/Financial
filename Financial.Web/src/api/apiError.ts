interface ApiErrorDetails {
  code?: string
  balance?: number
}

const OVERDRAFT_CONFIRMATION_CODE = 'overdraftConfirmationRequired'

export class ApiError extends Error {
  status: number
  code?: string
  balance?: number

  constructor(message: string, status: number, { code, balance }: ApiErrorDetails = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.balance = balance
  }
}

export const NETWORK_FAILURE_STATUS = 0

export function isNetworkFailure(err: unknown): boolean {
  return err instanceof ApiError && err.status === NETWORK_FAILURE_STATUS
}

export function isNotFound(err: unknown): boolean {
  return err instanceof ApiError && err.status === 404
}

export function isConflict(err: unknown): err is ApiError {
  return err instanceof ApiError && err.status === 409
}

export function overdraftBalance(err: ApiError): number | null {
  return err.code === OVERDRAFT_CONFIRMATION_CODE ? (err.balance ?? null) : null
}
