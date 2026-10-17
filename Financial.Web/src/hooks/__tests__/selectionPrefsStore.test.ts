import { describe, expect, it } from 'vitest'
import { createSelectionPrefsStore } from '../selectionPrefsStore'

interface Prefs {
  filter: string
  mode: string
}

const DEFAULTS: Prefs = { filter: '1Y', mode: 'Bar' }

describe('createSelectionPrefsStore', () => {
  it('restores the defaults for a selection that has no remembered prefs', () => {
    const store = createSelectionPrefsStore(DEFAULTS)

    expect(store.restore(new Map(), 'Asset|XP|Main|PETR4')).toEqual({ filter: '1Y', mode: 'Bar' })
  })

  it('restores the prefs remembered for that selection', () => {
    const store = createSelectionPrefsStore(DEFAULTS)
    const persistence = store.remember(new Map(), 'Broker|XP', { filter: 'All', mode: 'Line' })

    expect(store.restore(persistence, 'Broker|XP')).toEqual({ filter: 'All', mode: 'Line' })
    expect(store.restore(persistence, 'Broker|Trading212')).toEqual({ filter: '1Y', mode: 'Bar' })
  })

  it('remembers into a new map, leaving the previous one unchanged', () => {
    const store = createSelectionPrefsStore(DEFAULTS)
    const previous = new Map<string, Prefs>([['Broker|XP', { filter: '5Y', mode: 'Bar' }]])

    const next = store.remember(previous, 'Broker|XP', { filter: 'All', mode: 'Line' })

    expect(next).not.toBe(previous)
    expect(previous.get('Broker|XP')).toEqual({ filter: '5Y', mode: 'Bar' })
    expect(next.get('Broker|XP')).toEqual({ filter: 'All', mode: 'Line' })
  })
})
