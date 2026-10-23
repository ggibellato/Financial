import type { ViewedMonth } from '../utils/createFormDefaults'

export interface LockedBank {
  id: string
  name: string
}

export interface AccountFormSettings {
  lockedBank?: LockedBank
  keepOpenAfterSave?: boolean
  viewedMonth?: ViewedMonth
}
