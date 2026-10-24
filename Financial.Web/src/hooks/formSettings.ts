import type { YearMonth } from './useMonthSelection'

export interface LockedBank {
  id: string
  name: string
}

export interface AccountFormSettings {
  lockedBank?: LockedBank
  keepOpenAfterSave?: boolean
  viewedMonth?: YearMonth
}
