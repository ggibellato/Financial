import type { PortfolioAssetSummaryItemDto } from '../api/types'

type PickSource = NonNullable<PortfolioAssetSummaryItemDto['pickSource']>

export const PICK_SOURCE_LABELS: Record<PickSource, string> = {
  Recommended: 'Recommended',
  MyChoice: 'My choice',
}

export const PICK_SOURCE_ORDER: readonly PickSource[] = ['Recommended', 'MyChoice']

export function pickSourceLabel(pickSource: PickSource | null | undefined): string | null {
  return pickSource ? PICK_SOURCE_LABELS[pickSource] : null
}
