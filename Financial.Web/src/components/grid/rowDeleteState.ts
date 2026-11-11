export interface RowDeleteState {
  isDeleting: boolean
  isOtherDeleting: boolean
}

export function rowDeleteState(deletingKey: string | null, rowKey: string): RowDeleteState {
  return { isDeleting: deletingKey === rowKey, isOtherDeleting: deletingKey !== null && deletingKey !== rowKey }
}
