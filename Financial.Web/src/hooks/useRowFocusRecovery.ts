import { useEffect, useRef, type FocusEvent, type RefObject } from 'react'

interface FocusedRowControl {
  element: HTMLElement
  rowIndex: number
  controlIndex: number
}

const ROW_SELECTOR = 'tbody tr'
const CONTROL_SELECTOR = 'button, a[href], input, select, textarea'

function focusIsLost(): boolean {
  return document.activeElement === null || document.activeElement === document.body
}

function locate(element: HTMLElement): FocusedRowControl | null {
  const row = element.closest<HTMLTableRowElement>(ROW_SELECTOR)
  if (!row?.parentElement) return null
  const rowIndex = Array.from(row.parentElement.children).indexOf(row)
  const controlIndex = Array.from(row.querySelectorAll(CONTROL_SELECTOR)).indexOf(element)
  return controlIndex < 0 ? null : { element, rowIndex, controlIndex }
}

function controlNear(region: HTMLElement, { rowIndex, controlIndex }: FocusedRowControl): HTMLElement | null {
  const rows = region.querySelectorAll(ROW_SELECTOR)
  const row = rows[Math.min(rowIndex, rows.length - 1)]
  if (!row) return null
  const controls = row.querySelectorAll<HTMLElement>(CONTROL_SELECTOR)
  return controls[Math.min(controlIndex, controls.length - 1)] ?? null
}

export function useRowFocusRecovery(
  regionRef: RefObject<HTMLElement | null>,
  fallbackRef: RefObject<HTMLElement | null>,
  isLoading: boolean,
) {
  const lastFocused = useRef<FocusedRowControl | null>(null)

  useEffect(() => {
    const focused = lastFocused.current
    if (isLoading || !focused || focused.element.isConnected || !focusIsLost()) return
    lastFocused.current = null
    const region = regionRef.current
    const target = (region && controlNear(region, focused)) ?? fallbackRef.current
    target?.focus()
  })

  return (event: FocusEvent<HTMLElement>) => {
    if (!event.currentTarget.contains(event.target)) return
    lastFocused.current = locate(event.target)
  }
}
