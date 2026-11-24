import { describe, expect, it } from 'vitest'
import { savingsAccount } from '../../test/account'
import {
  cashbackDestinationAccounts,
  destinationChoice,
  destinationsSummary,
  roundUpDestinationAccounts,
  toMoneyDestination,
} from '../moneyDestination'

const SAVINGS_TERMS = savingsAccount({ id: 'x', name: 'x' }).savings!
const roundUpSaver = savingsAccount({ id: 'saver', name: 'Round-up saver', savings: { ...SAVINGS_TERMS, roundUpOnly: true } })
const chaseSave = savingsAccount({ id: 'save', name: 'Chase Save' })
const closedSaver = savingsAccount({ id: 'closed', name: 'Closed saver', isActive: false, savings: { ...SAVINGS_TERMS, roundUpOnly: true } })

describe('moneyDestination', () => {
  it('turns a destination into a picker choice and back', () => {
    expect(destinationChoice(null)).toBe('None')
    expect(destinationChoice({ kind: 'InvestedAutomatically', accountId: null, accountName: null })).toBe('InvestedAutomatically')
    expect(destinationChoice({ kind: 'Account', accountId: 'saver', accountName: 'Round-up saver' })).toBe('saver')
    expect(toMoneyDestination('saver')).toEqual({ kind: 'Account', accountId: 'saver', accountName: null })
    expect(toMoneyDestination('None')).toEqual({ kind: 'None', accountId: null, accountName: null })
  })

  it('[AC P66-F03-destinations-web-01] offers only active round-up-only savings for round-ups and other active savings for cashback', () => {
    const accounts = [roundUpSaver, chaseSave, closedSaver]

    expect(roundUpDestinationAccounts(accounts).map((account) => account.name)).toEqual(['Round-up saver'])
    expect(cashbackDestinationAccounts(accounts).map((account) => account.name)).toEqual(['Chase Save'])
  })

  it('[AC P66-F03-destinations-web-02] names the destinations other than None', () => {
    expect(
      destinationsSummary(
        { kind: 'Account', accountId: 'saver', accountName: 'Chase round-up saver' },
        { kind: 'InvestedAutomatically', accountId: null, accountName: null },
      ),
    ).toBe('Round-ups go to Chase round-up saver · Cashback is invested automatically')
    expect(destinationsSummary({ kind: 'InvestedAutomatically', accountId: null, accountName: null }, null)).toBe(
      'Round-ups are invested automatically',
    )
    expect(destinationsSummary({ kind: 'None', accountId: null, accountName: null }, null)).toBeNull()
  })
})
