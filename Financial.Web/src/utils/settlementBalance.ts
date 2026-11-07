import type { AccountGroupMemberDto } from '../api/types'
import { formatCurrency, toPence } from './formatters'

const SETTLED_TEXT = 'Settled'

type CurrencyBalances = AccountGroupMemberDto['currencyBalances']

function formatAmounts(balances: CurrencyBalances): string {
  return balances.map(({ balance, currency }) => formatCurrency(Math.abs(balance), currency)).join(' / ')
}

export function settlementBalanceText(otherParticipant: string, currencyBalances: CurrencyBalances): string {
  const owed = currencyBalances.filter(({ balance }) => toPence(balance) !== 0)
  const owedToYou = owed.filter(({ balance }) => balance > 0)
  const owedByYou = owed.filter(({ balance }) => balance < 0)
  const parts = [
    ...(owedToYou.length > 0 ? [`${otherParticipant} owes you ${formatAmounts(owedToYou)}`] : []),
    ...(owedByYou.length > 0 ? [`You owe ${otherParticipant} ${formatAmounts(owedByYou)}`] : []),
  ]
  return parts.length > 0 ? parts.join(' · ') : SETTLED_TEXT
}
