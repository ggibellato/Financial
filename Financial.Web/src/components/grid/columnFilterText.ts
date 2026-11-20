function rowsNoun(count: number) {
  return count === 1 ? 'row' : 'rows'
}

export function filteredRowsMessage(shownCount: number, totalCount: number) {
  return `Filtered: ${shownCount} of ${totalCount} ${rowsNoun(totalCount)}`
}

export function clearedRowsMessage(count: number) {
  return `Filters cleared: showing all ${count} ${rowsNoun(count)}`
}

export const FILTER_APPLIED_DESCRIPTION = 'Filter applied'

export function filterTriggerLabel(label: string) {
  return `Filter by ${label}`
}

export function focusFilterTrigger(container: HTMLElement | null, label: string) {
  container?.querySelector<HTMLButtonElement>(`button[aria-label="${filterTriggerLabel(label)}"]`)?.focus()
}
