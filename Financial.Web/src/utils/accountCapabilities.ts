import type { AccountDto } from '../api/types'

export function accountsWith(accounts: AccountDto[], capability: string, ...keptIds: (string | null)[]): AccountDto[] {
  return accounts
    .filter((account) => account.capabilities.includes(capability) && (account.isActive || keptIds.includes(account.id)))
    .sort((a, b) => a.name.localeCompare(b.name))
}
