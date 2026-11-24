import type { AccountDto, MoneyDestinationDto } from '../api/types'

export const NO_DESTINATION = 'None'
export const INVESTED_AUTOMATICALLY = 'InvestedAutomatically'
const ACCOUNT_DESTINATION = 'Account'

export function destinationChoice(destination: MoneyDestinationDto | null | undefined): string {
  if (!destination) return NO_DESTINATION
  return destination.kind === ACCOUNT_DESTINATION && destination.accountId ? destination.accountId : destination.kind
}

export function toMoneyDestination(choice: string): MoneyDestinationDto {
  return choice === NO_DESTINATION || choice === INVESTED_AUTOMATICALLY
    ? { kind: choice, accountId: null, accountName: null }
    : { kind: ACCOUNT_DESTINATION, accountId: choice, accountName: null }
}

export function roundUpDestinationAccounts(accounts: readonly AccountDto[]): AccountDto[] {
  return accounts.filter((account) => account.isActive && account.savings?.roundUpOnly === true)
}

export function cashbackDestinationAccounts(accounts: readonly AccountDto[]): AccountDto[] {
  return accounts.filter((account) => account.isActive && account.savings != null && !account.savings.roundUpOnly)
}

interface MoneyWording {
  goesTo: string
  invested: string
}

const ROUND_UPS: MoneyWording = { goesTo: 'Round-ups go to', invested: 'Round-ups are invested automatically' }
const CASHBACK: MoneyWording = { goesTo: 'Cashback goes to', invested: 'Cashback is invested automatically' }

function destinationPhrase(wording: MoneyWording, destination: MoneyDestinationDto | null | undefined): string | null {
  if (destination?.kind === INVESTED_AUTOMATICALLY) return wording.invested
  if (destination?.kind === ACCOUNT_DESTINATION && destination.accountName) return `${wording.goesTo} ${destination.accountName}`
  return null
}

export function destinationsSummary(
  roundUp: MoneyDestinationDto | null | undefined,
  cashback: MoneyDestinationDto | null | undefined,
): string | null {
  const phrases = [destinationPhrase(ROUND_UPS, roundUp), destinationPhrase(CASHBACK, cashback)].filter(
    (phrase): phrase is string => phrase !== null,
  )
  return phrases.length > 0 ? phrases.join(' · ') : null
}
