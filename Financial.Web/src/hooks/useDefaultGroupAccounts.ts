import { apiClient } from '../api/financialApiClient'
import type { AccountGroupMemberDto, CurrencyTotalDto } from '../api/types'
import { useAsyncResource } from './useAsyncResource'

export const DEFAULT_GROUP_LOAD_ERROR = 'Unable to load the accounts'

export interface DefaultGroupAccount extends AccountGroupMemberDto {
  isActive: boolean
}

export interface DefaultGroupAccounts {
  members: DefaultGroupAccount[]
  totals: CurrencyTotalDto[]
}

function loadDefaultGroupAccounts(year: number, month: number): Promise<DefaultGroupAccounts> {
  return Promise.all([apiClient.getDefaultGroupSummary(year, month), apiClient.getAccounts()]).then(([summary, accounts]) => {
    const inactiveIds = new Set(accounts.filter((account) => !account.isActive).map((account) => account.id))
    return {
      members: summary.members.map((member) => ({ ...member, isActive: !inactiveIds.has(member.accountId) })),
      totals: summary.totals,
    }
  })
}

export function useDefaultGroupAccounts(year: number, month: number) {
  return useAsyncResource(() => loadDefaultGroupAccounts(year, month), [year, month], DEFAULT_GROUP_LOAD_ERROR, {
    initialLoading: true,
  })
}
