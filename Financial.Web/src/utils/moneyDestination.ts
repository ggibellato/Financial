import type { AccountDto, MoneyDestinationDto } from '../api/types'
import { accountsWith } from './accountCapabilities'

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

const EARNS_INTEREST = 'EarnsInterest'

export function roundUpDestinationAccounts(accounts: AccountDto[], keptId: string): AccountDto[] {
  return accountsWith(accounts, EARNS_INTEREST, keptId).filter((account) => account.savings?.roundUpOnly === true || account.id === keptId)
}

export function cashbackDestinationAccounts(accounts: AccountDto[], keptId: string): AccountDto[] {
  return accountsWith(accounts, EARNS_INTEREST, keptId).filter((account) => account.savings?.roundUpOnly !== true || account.id === keptId)
}

interface MoneyWording {
  goesTo: string
  invested: string
  stopsGoingTo: string
  stopsBeingInvested: string
}

const ROUND_UPS: MoneyWording = {
  goesTo: 'Round-ups go to',
  invested: 'Round-ups are invested automatically',
  stopsGoingTo: 'Saving with round-up off stops sending round-ups to',
  stopsBeingInvested: 'Saving with round-up off stops investing them automatically.',
}

const CASHBACK: MoneyWording = {
  goesTo: 'Cashback goes to',
  invested: 'Cashback is invested automatically',
  stopsGoingTo: 'Saving with cashback off stops sending cashback to',
  stopsBeingInvested: 'Saving with cashback off stops investing it automatically.',
}

function switchOffHint(wording: MoneyWording, choice: string, accounts: readonly AccountDto[]): string | undefined {
  if (choice === NO_DESTINATION) return undefined
  if (choice === INVESTED_AUTOMATICALLY) return wording.stopsBeingInvested
  const name = accounts.find((account) => account.id === choice)?.name
  return name ? `${wording.stopsGoingTo} ${name}.` : undefined
}

export const roundUpSwitchOffHint = (choice: string, accounts: readonly AccountDto[]) => switchOffHint(ROUND_UPS, choice, accounts)

export const cashbackSwitchOffHint = (choice: string, accounts: readonly AccountDto[]) => switchOffHint(CASHBACK, choice, accounts)

export function roundUpSourceNames(accounts: readonly AccountDto[], accountId: string | undefined): string[] {
  return accounts
    .filter((account) => accountId !== undefined && account.roundUpDestination?.accountId === accountId)
    .map((account) => account.name)
}

function destinationPhrase(wording: MoneyWording, destination: MoneyDestinationDto | null | undefined): string | null {
  if (destination?.kind === INVESTED_AUTOMATICALLY) return wording.invested
  if (destination?.kind === ACCOUNT_DESTINATION && destination.accountName) return `${wording.goesTo} ${destination.accountName}`
  return null
}

export function destinationsSummary(
  roundUp: MoneyDestinationDto | null | undefined,
  cashback: MoneyDestinationDto | null | undefined,
): string | null {
  return [destinationPhrase(ROUND_UPS, roundUp), destinationPhrase(CASHBACK, cashback)].filter(Boolean).join(' · ') || null
}
