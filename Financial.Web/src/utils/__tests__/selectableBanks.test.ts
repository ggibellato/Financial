import { describe, expect, it } from 'vitest'
import { selectableBanks } from '../selectableBanks'

const BARCLAYS = { id: 'bank-barclays', name: 'Barclays', isActive: true }
const CHASE = { id: 'bank-chase', name: 'Chase', isActive: false }
const MONZO = { id: 'bank-monzo', name: 'Monzo', isActive: true }

describe('selectableBanks', () => {
  it('[AC P60-F06-admin-accounts-page-05] hides inactive banks when nothing is selected', () => {
    expect(selectableBanks([BARCLAYS, CHASE, MONZO])).toEqual([BARCLAYS, MONZO])
  })

  it('[AC P60-F06-admin-accounts-page-05] keeps a selected inactive bank in its place', () => {
    expect(selectableBanks([BARCLAYS, CHASE, MONZO], 'bank-chase')).toEqual([BARCLAYS, CHASE, MONZO])
  })

  it('keeps every bank in the given order while all are active', () => {
    expect(selectableBanks([MONZO, BARCLAYS], 'bank-barclays')).toEqual([MONZO, BARCLAYS])
  })

  it('hides an inactive bank that is not the selected one', () => {
    expect(selectableBanks([BARCLAYS, CHASE], 'bank-barclays')).toEqual([BARCLAYS])
  })

  it('keeps options without an active flag', () => {
    const option = { id: 'acc-1', name: 'Pots' }
    expect(selectableBanks([option])).toEqual([option])
  })
})
