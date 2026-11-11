import { useEffect, useRef, type FocusEvent, type RefObject } from 'react'

interface FocusedRowControl {
  element: HTMLElement
  body: HTMLElement
  bodyIndex: number
  rowIndex: number
  controlsAfter: number
}

const ROW_SELECTOR = 'tbody tr'
const CONTROL_SELECTOR = 'button, a[href], input, select, textarea'

function focusIsLost(): boolean {
  return document.activeElement === null || document.activeElement === document.body
}

function locate(region: HTMLElement, element: HTMLElement): FocusedRowControl | null {
  const row = element.closest<HTMLTableRowElement>(ROW_SELECTOR)
  const body = row?.parentElement
  if (!row || !body) return null
  const controls = Array.from(row.querySelectorAll(CONTROL_SELECTOR))
  const controlIndex = controls.indexOf(element)
  if (controlIndex < 0) return null
  return {
    element,
    body,
    bodyIndex: Array.from(region.querySelectorAll('tbody')).indexOf(body as HTMLTableSectionElement),
    rowIndex: Array.from(body.children).indexOf(row),
    controlsAfter: controls.length - 1 - controlIndex,
  }
}

function controlNear(region: HTMLElement, { body, bodyIndex, rowIndex, controlsAfter }: FocusedRowControl): HTMLElement | null {
  const rows = (body.isConnected ? body : region.querySelectorAll('tbody')[bodyIndex])?.children ?? []
  const row = rows[Math.min(rowIndex, rows.length - 1)]
  if (!row) return null
  const controls = row.querySelectorAll<HTMLElement>(CONTROL_SELECTOR)
  const control = controls[Math.max(controls.length - 1 - controlsAfter, 0)]
  return control && !control.matches(':disabled') ? control : null
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
    lastFocused.current = locate(event.currentTarget, event.target)
  }
}
