export const SEGMENT_CLASSES: readonly string[] = ['Equity', 'RealEstate']

export const MAX_SEGMENT_LENGTH = 40

export function segmentKey(segment: string): string {
  return segment.trim().toLocaleLowerCase()
}

export function distinctSegments(segments: readonly (string | null | undefined)[]): string[] {
  const byKey = new Map<string, string>()
  const named = segments.flatMap((s) => (s?.trim() ? [s.trim()] : [])).sort((a, b) => a.localeCompare(b))
  for (const segment of named) {
    if (!byKey.has(segmentKey(segment))) byKey.set(segmentKey(segment), segment)
  }
  return [...byKey.values()]
}
