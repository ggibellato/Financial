import { afterEach, describe, expect, it } from 'vitest'
import { awaitRetryOutcome, beginReload, claimFocusForRetryOutcome } from '../retryOutcomeFocus'

function mountRegion(html = ''): HTMLElement {
  const region = document.createElement('div')
  region.innerHTML = html
  document.body.appendChild(region)
  return region
}

async function reloadInto(region: HTMLElement, reloaded = region) {
  const finishReload = beginReload()
  finishReload(reloaded)
  await Promise.resolve()
}

describe('retry outcome focus', () => {
  afterEach(() => {
    ;(document.activeElement as HTMLElement | null)?.blur()
    document.body.innerHTML = ''
  })

  it('focuses the first control of the content reloaded by the retry', async () => {
    const region = mountRegion('<p>Loaded</p><button>Edit</button>')
    awaitRetryOutcome(region)

    await reloadInto(region)

    expect(document.activeElement).toBe(region.querySelector('button'))
  })

  it('makes the reloaded region itself focusable when it holds no control', async () => {
    const region = mountRegion('<p>Loaded</p>')
    awaitRetryOutcome(region)

    await reloadInto(region)

    expect(document.activeElement).toBe(region)
    expect(region.getAttribute('tabindex')).toBe('-1')
  })

  it('keeps a tabindex the region already has', async () => {
    const region = mountRegion('<p>Loaded</p>')
    region.tabIndex = 0
    awaitRetryOutcome(region)

    await reloadInto(region)

    expect(document.activeElement).toBe(region)
    expect(region.getAttribute('tabindex')).toBe('0')
  })

  it('does not focus content outside the region awaiting the retry', async () => {
    const region = mountRegion('<p>Loading</p>')
    const elsewhere = mountRegion('<button>Other</button>')
    awaitRetryOutcome(region)

    await reloadInto(region, elsewhere)

    expect(document.activeElement).toBe(document.body)
  })

  it('lets only one outcome claim the focus', () => {
    const region = mountRegion('<button>Edit</button>')
    const button = region.querySelector('button') as HTMLElement
    awaitRetryOutcome(region)

    expect(claimFocusForRetryOutcome(button)).toBe(true)
    expect(claimFocusForRetryOutcome(button)).toBe(false)
  })
})
