export interface SelectOption {
  value: string
  label: string
}

export function optionalChoiceOptions<T extends string>(order: readonly T[], labels: Record<T, string>, noneLabel: string): SelectOption[] {
  return [{ value: '', label: noneLabel }, ...order.map((value) => ({ value, label: labels[value] }))]
}
