export const SEGMENT_CLASSES: readonly string[] = ['Equity', 'RealEstate']

export const MAX_SEGMENT_LENGTH = 40

export function segmentKey(segment: string): string {
  return segment.trim().toLocaleLowerCase()
}

export const NO_SEGMENT = 'No segment'

interface SegmentedItem {
  name: string
  segment: string | null
  value: number
}

export function totalsBySegment(items: readonly SegmentedItem[]): { name: string; value: number }[] {
  const byKey = new Map<string, { name: string; value: number }>()
  const byName = [...items].sort((a, b) => a.name.localeCompare(b.name))
  for (const item of byName) {
    const key = item.segment === null ? '' : segmentKey(item.segment)
    const total = byKey.get(key) ?? { name: item.segment ?? NO_SEGMENT, value: 0 }
    byKey.set(key, { ...total, value: total.value + item.value })
  }
  return [...byKey]
    .sort(([a], [b]) => (a === '' ? 1 : b === '' ? -1 : a.localeCompare(b)))
    .map(([, total]) => total)
}

export function distinctSegments(segments: readonly (string | null | undefined)[]): string[] {
  const byKey = new Map<string, string>()
  const named = segments.flatMap((s) => (s?.trim() ? [s.trim()] : [])).sort((a, b) => a.localeCompare(b))
  for (const segment of named) {
    if (!byKey.has(segmentKey(segment))) byKey.set(segmentKey(segment), segment)
  }
  return [...byKey.values()]
}
