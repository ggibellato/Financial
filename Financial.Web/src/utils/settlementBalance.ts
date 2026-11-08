import type { AccountGroupMemberDto } from '../api/types'
import { formatCurrency, toPence } from './formatters'

type CurrencyBalances = AccountGroupMemberDto['currencyBalances']

function formatAmounts(balances: CurrencyBalances): string {
  return balances.map(({ balance, currency }) => formatCurrency(Math.abs(balance), currency)).join(' / ')
}

export function settlementCounterparty({ otherParticipant, name }: { otherParticipant: string | null; name: string }): string {
  return otherParticipant ?? name
}

export function settlementBalanceText(otherParticipant: string, currencyBalances: CurrencyBalances): string {
  const owedToYou = currencyBalances.filter(({ balance }) => toPence(balance) > 0)
  const owedByYou = currencyBalances.filter(({ balance }) => toPence(balance) < 0)
  const parts: string[] = []
  if (owedToYou.length > 0) parts.push(`${otherParticipant} owes you ${formatAmounts(owedToYou)}`)
  if (owedByYou.length > 0) parts.push(`You owe ${otherParticipant} ${formatAmounts(owedByYou)}`)
  return parts.length > 0 ? parts.join(' · ') : 'Settled'
}
