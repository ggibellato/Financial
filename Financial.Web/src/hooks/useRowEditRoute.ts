import { useCallback, useEffect, useRef } from 'react'
import { useSearchParams } from 'react-router-dom'

const EDIT_PARAM = 'edit'

interface RowEditRoute<T> {
  rows: readonly T[]
  keyOf: (row: T) => string
  isLoaded: boolean
  isEditDisabled?: (row: T) => boolean
  open: (row: T) => void
}

export function useRowEditRoute<T>({ rows, keyOf, isLoaded, isEditDisabled, open }: RowEditRoute<T>) {
  const [searchParams, setSearchParams] = useSearchParams()
  const editedKey = searchParams.get(EDIT_PARAM)
  const handledKey = useRef<string | null>(null)
  const row = editedKey === null ? undefined : rows.find((candidate) => keyOf(candidate) === editedKey)

  const clear = useCallback(
    () =>
      setSearchParams(
        (params) => {
          const next = new URLSearchParams(params)
          next.delete(EDIT_PARAM)
          return next
        },
        { replace: true },
      ),
    [setSearchParams],
  )

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
