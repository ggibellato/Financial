import { isFocusLost } from './focus'

const FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'

let retryRegion: HTMLElement | null = null

function stopAwaiting() {
  retryRegion = null
}

export function awaitRetryOutcome(region: HTMLElement | null) {
  retryRegion = region
  document.addEventListener('focusin', stopAwaiting, { once: true })
}

function isOutcomeOf(element: HTMLElement): boolean {
  return !!retryRegion?.isConnected && retryRegion.contains(element)
}

export function claimFocusForRetryOutcome(element: HTMLElement): boolean {
  const claims = isOutcomeOf(element) && isFocusLost()
  if (claims || !retryRegion?.isConnected) stopAwaiting()
  return claims
}

function focusTargetIn(region: HTMLElement): HTMLElement {
  const control = region.querySelector<HTMLElement>(FOCUSABLE)
  if (control) return control
  if (!region.hasAttribute('tabindex')) region.tabIndex = -1
  return region
}

function focusReloadedContent(region: HTMLElement) {
  if (!retryRegion) return
  queueMicrotask(() => {
    if (!region.isConnected || !claimFocusForRetryOutcome(region)) return
    focusTargetIn(region).focus()
  })
}

export function beginReload(): (region: HTMLElement) => void {
  const reloadsRetryOutcome = !!retryRegion?.isConnected
  return (region) => {
    if (reloadsRetryOutcome) focusReloadedContent(region)
  }
}
