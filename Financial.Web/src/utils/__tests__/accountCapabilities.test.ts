import { describe, expect, it } from 'vitest'
import { account, cardAccount, potsAccount } from '../../test/account'
import { accountsWith } from '../accountCapabilities'

const RESERVA = potsAccount({ id: 'p1', name: 'Reserva' })
const HOLIDAYS = potsAccount({ id: 'p2', name: 'Holidays' })
const OLD_POTS = potsAccount({ id: 'p3', name: 'Old pots', isActive: false })
const ACCOUNTS = [RESERVA, account({ id: 'b1', name: 'Barclays' }), OLD_POTS, cardAccount({ id: 'c1', name: 'Amex' }), HOLIDAYS]

describe('accountsWith', () => {
  it('keeps the active accounts with the capability, sorted by name', () => {
    expect(accountsWith(ACCOUNTS, 'HasPots')).toEqual([HOLIDAYS, RESERVA])
  })

  it('also keeps an inactive account whose id is kept', () => {
    expect(accountsWith(ACCOUNTS, 'HasPots', null, 'p3')).toEqual([HOLIDAYS, OLD_POTS, RESERVA])
  })

  it('never keeps an account without the capability, even when its id is kept', () => {
    expect(accountsWith(ACCOUNTS, 'HasPots', 'b1')).toEqual([HOLIDAYS, RESERVA])
  })
})
