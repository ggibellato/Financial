import type { YearMonth } from './useMonthSelection'

export interface LockedBank {
  id: string
  name: string
}

export interface LockedCard {
  id: string
  name: string
}

export interface AccountFormSettings {
  lockedBank?: LockedBank
  lockedCard?: LockedCard
  keepOpenAfterSave?: boolean
  viewedMonth?: YearMonth
}
