export function allRowsTotalLabel(label: string, isFiltered: boolean | undefined): string {
  return isFiltered ? `${label} (all rows)` : label
}
