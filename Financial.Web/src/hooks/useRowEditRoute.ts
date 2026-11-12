import { useEffect, useRef } from 'react'

interface RowEditRoute<T> {
  editedKey: string | null
  rows: readonly T[]
  keyOf: (row: T) => string
  isLoaded: boolean
  isEditDisabled?: (row: T) => boolean
  open: (row: T) => void
  clear: () => void
}

export function useRowEditRoute<T>({ editedKey, rows, keyOf, isLoaded, isEditDisabled, open, clear }: RowEditRoute<T>) {
  const handledKey = useRef<string | null>(null)
  const row = editedKey === null ? undefined : rows.find((candidate) => keyOf(candidate) === editedKey)

  useEffect(() => {
    if (editedKey === null) {
      handledKey.current = null
      return
    }
    if (!isLoaded || !row || handledKey.current === editedKey || isEditDisabled?.(row)) return
    handledKey.current = editedKey
    clear()
    open(row)
  })

  return { isNotFound: editedKey !== null && isLoaded && !row, dismiss: clear }
}
