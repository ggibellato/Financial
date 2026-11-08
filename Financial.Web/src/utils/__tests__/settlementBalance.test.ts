import { describe, expect, it } from 'vitest'
import { settlementBalanceText, settlementCounterparty } from '../settlementBalance'

describe('settlementCounterparty', () => {
  it('names the other participant', () => {
    expect(settlementCounterparty({ otherParticipant: 'Samuel', name: 'Son account' })).toBe('Samuel')
  })

  it('falls back to the account name without one', () => {
    expect(settlementCounterparty({ otherParticipant: null, name: 'Mãe' })).toBe('Mãe')
  })
})

describe('settlementBalanceText', () => {
  it('says the other participant owes you every positive currency, joined with " / "', () => {
    expect(
      settlementBalanceText('Mãe', [
        { currency: 'BRL', balance: 167.8768 },
        { currency: 'GBP', balance: 7.94214099 },
      ]),
    ).toBe('Mãe owes you R$167.88 / £7.94')
  })

  it('says you owe the other participant when the balance is negative', () => {
    expect(settlementBalanceText('Mãe', [{ currency: 'GBP', balance: -15 }])).toBe('You owe Mãe £15.00')
  })

  it('joins mixed directions with " · "', () => {
    expect(
      settlementBalanceText('Mãe', [
        { currency: 'BRL', balance: -10 },
        { currency: 'GBP', balance: 2.5 },
      ]),
    ).toBe('Mãe owes you £2.50 · You owe Mãe R$10.00')
  })

  it('leaves out a currency whose balance is zero to the penny', () => {
    expect(
      settlementBalanceText('Mãe', [
        { currency: 'BRL', balance: 0.004 },
        { currency: 'GBP', balance: 7.94 },
      ]),
    ).toBe('Mãe owes you £7.94')
  })

  it('reads "Settled" when every currency is zero', () => {
    expect(
      settlementBalanceText('Mãe', [
        { currency: 'BRL', balance: 0 },
        { currency: 'GBP', balance: -0.001 },
      ]),
    ).toBe('Settled')
  })

  it('reads a one-currency account', () => {
    expect(settlementBalanceText('Samuel', [{ currency: 'GBP', balance: 20 }])).toBe('Samuel owes you £20.00')
  })

  it('reads "Settled" for an account with no balances', () => {
    expect(settlementBalanceText('Samuel', [])).toBe('Settled')
  })
})
