import type { AccountDto } from '../api/types'
import { selectableBanks } from './selectableBanks'

export function accountsWith(accounts: AccountDto[], capability: string, ...keptIds: (string | undefined)[]): AccountDto[] {
  return selectableBanks(
    accounts.filter((account) => account.capabilities.includes(capability)),
    ...keptIds,
  ).sort((a, b) => a.name.localeCompare(b.name))
}
