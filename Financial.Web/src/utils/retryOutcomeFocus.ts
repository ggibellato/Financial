import { isFocusLost } from './focus'

const FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'

let isAwaitingRetryOutcome = false

function stopAwaiting() {
  isAwaitingRetryOutcome = false
}

export function awaitRetryOutcome() {
  isAwaitingRetryOutcome = true
  document.addEventListener('focusin', stopAwaiting, { once: true })
}

export function claimFocusForRetryOutcome(): boolean {
  const claims = isAwaitingRetryOutcome && isFocusLost()
  stopAwaiting()
  return claims
}

export function focusReloadedContent(region: HTMLElement) {
  if (!isAwaitingRetryOutcome) return
  queueMicrotask(() => {
    if (!region.isConnected || !claimFocusForRetryOutcome()) return
    region.querySelector<HTMLElement>(FOCUSABLE)?.focus()
  })
}
