export interface SelectableBank {
  id: string
  isActive?: boolean
}

export function selectableBanks<T extends SelectableBank>(banks: T[], selectedId?: string): T[] {
  return banks.filter((bank) => bank.isActive !== false || bank.id === selectedId)
}
