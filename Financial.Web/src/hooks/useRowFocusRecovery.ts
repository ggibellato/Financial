import { useEffect, useRef, type FocusEvent, type RefObject } from 'react'
import { isFocusLost } from '../utils/focus'

interface FocusedRowControl {
  element: HTMLElement
  body: HTMLElement
  bodyIndex: number
  bodyCount: number
  rowIndex: number
  controlsAfter: number
}

const ROW_SELECTOR = 'tbody tr'
const CONTROL_SELECTOR = 'button, a[href], input, select, textarea'

function locate(region: HTMLElement, element: HTMLElement): FocusedRowControl | null {
  const row = element.closest<HTMLTableRowElement>(ROW_SELECTOR)
  const body = row?.parentElement
  if (!row || !body) return null
  const controls = Array.from(row.querySelectorAll(CONTROL_SELECTOR))
  const controlIndex = controls.indexOf(element)
  if (controlIndex < 0) return null
  const bodies = Array.from(region.querySelectorAll('tbody'))
  return {
    element,
    body,
    bodyIndex: bodies.indexOf(body as HTMLTableSectionElement),
    bodyCount: bodies.length,
    rowIndex: Array.from(body.children).indexOf(row),
    controlsAfter: controls.length - 1 - controlIndex,
  }
}

function sameBody(region: HTMLElement, { body, bodyIndex, bodyCount }: FocusedRowControl): Element | undefined {
  if (body.isConnected) return body
  const bodies = region.querySelectorAll('tbody')
  return bodies.length === bodyCount ? bodies[bodyIndex] : undefined
}

function controlNear(region: HTMLElement, focused: FocusedRowControl): HTMLElement | null {
  const { rowIndex, controlsAfter } = focused
  const rows = sameBody(region, focused)?.children ?? []
  const row = rows[Math.min(rowIndex, rows.length - 1)]
  if (!row) return null
  const controls = row.querySelectorAll<HTMLElement>(CONTROL_SELECTOR)
  const control = controls[Math.max(controls.length - 1 - controlsAfter, 0)]
  return control && !control.matches(':disabled') ? control : null
}

function watchForFocusLoss(onLost: () => void, onMoved: () => void): () => void {
  const observer = new MutationObserver(() => {
    if (!isFocusLost()) return
    stop()
    onLost()
  })
  const moved = () => {
    stop()
    onMoved()
  }
  const stop = () => {
    observer.disconnect()
    document.removeEventListener('focusin', moved, true)
  }
  observer.observe(document.body, { childList: true, subtree: true })
  document.addEventListener('focusin', moved, true)
  return stop
}

export function useRowFocusRecovery(
  regionRef: RefObject<HTMLElement | null>,
  fallbackRef: RefObject<HTMLElement | null>,
  isLoading: boolean,
) {
  const lastFocused = useRef<FocusedRowControl | null>(null)

  useEffect(() => {
    const focused = lastFocused.current
    if (isLoading || !focused || focused.element.isConnected) return
    const forget = () => {
      lastFocused.current = null
    }
    const recover = () => {
      forget()
      const region = regionRef.current
      const target = (region && controlNear(region, focused)) ?? fallbackRef.current
      target?.focus()
    }
    if (isFocusLost()) recover()
    else if (!regionRef.current?.contains(document.activeElement)) return watchForFocusLoss(recover, forget)
  })

  return (event: FocusEvent<HTMLElement>) => {
    if (!event.currentTarget.contains(event.target)) return
    lastFocused.current = locate(event.currentTarget, event.target)
  }
}
