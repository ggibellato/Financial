import { useState } from 'react'

export function usePendingDelete<T>(deleteRow: (row: T) => void) {
  const [pendingRow, setPendingRow] = useState<T | null>(null)

  const confirm = () => {
    if (pendingRow === null) return
    deleteRow(pendingRow)
    setPendingRow(null)
  }

  return {
    pendingRow,
    request: (row: T) => setPendingRow(row),
    confirm,
    cancel: () => setPendingRow(null),
  }
}
