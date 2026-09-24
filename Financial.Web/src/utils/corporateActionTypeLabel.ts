const TYPE_LABELS: Record<string, string> = {
  Split: 'Split',
  Merger: 'Merger',
  SpinOff: 'Spin-off',
}

export function corporateActionTypeLabel(type: string): string {
  return TYPE_LABELS[type] ?? type
}
