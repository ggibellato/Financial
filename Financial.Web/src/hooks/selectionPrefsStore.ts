export interface SelectionPrefsStore<P> {
  restore: (persistence: Map<string, P>, key: string) => P
  remember: (persistence: Map<string, P>, key: string, prefs: P) => Map<string, P>
}

export function createSelectionPrefsStore<P>(defaults: P): SelectionPrefsStore<P> {
  return {
    restore: (persistence, key) => persistence.get(key) ?? defaults,
    remember: (persistence, key, prefs) => {
      const next = new Map(persistence)
      next.set(key, prefs)
      return next
    },
  }
}
