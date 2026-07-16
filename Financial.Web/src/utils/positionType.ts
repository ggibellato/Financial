import type { PositionType } from '../api/types'

export const POSITION_TYPE_STATUS_CLASS: Record<PositionType, string> = {
  Long: 'long',
  Flat: 'flat',
  Short: 'short',
}
