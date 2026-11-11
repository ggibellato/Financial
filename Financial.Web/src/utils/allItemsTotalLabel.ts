export function allItemsTotalLabel(label: string, itemsNoun: string, isFiltered: boolean): string {
  return isFiltered ? `${label} (all ${itemsNoun})` : label
}
