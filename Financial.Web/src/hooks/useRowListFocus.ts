import { useEffect, useRef } from 'react'

export function useRowListFocus(rowCount: number, rowInputSelector: string) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const addButtonRef = useRef<HTMLButtonElement | null>(null)
  const previousRowCount = useRef(rowCount)

  useEffect(() => {
    if (rowCount > previousRowCount.current) {
      const inputs = containerRef.current?.querySelectorAll<HTMLElement>(rowInputSelector)
      inputs?.[inputs.length - 1]?.focus()
    } else if (rowCount < previousRowCount.current) {
      addButtonRef.current?.focus()
    }
    previousRowCount.current = rowCount
  }, [rowCount, rowInputSelector])

  return { containerRef, addButtonRef }
}
