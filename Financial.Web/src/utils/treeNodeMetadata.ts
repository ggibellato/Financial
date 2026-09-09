export function getMetaString(metadata: Record<string, unknown>, key: string): string {
  const value = metadata[key]
  return typeof value === 'string' ? value : ''
}

export function getMetaNumber(metadata: Record<string, unknown>, key: string): number {
  const value = metadata[key]
  return typeof value === 'number' ? value : -1
}
