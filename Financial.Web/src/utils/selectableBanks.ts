export interface SelectableBank {
  id: string
  isActive?: boolean
}

export function selectableBanks<T extends SelectableBank>(banks: T[], ...keptIds: (string | undefined)[]): T[] {
  return banks.filter((bank) => bank.isActive !== false || keptIds.includes(bank.id))
}
