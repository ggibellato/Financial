import type { PortfolioDto } from '../api/types'

export type ArcaBucket = NonNullable<PortfolioDto['arcaBucket']>

export const ARCA_BUCKET_LABELS: Record<ArcaBucket, string> = {
  Shares: 'A – Shares and businesses',
  RealEstate: 'R – Real estate',
  Cash: 'C – Cash and fixed income',
  Pension: 'Pension',
  ReserveFund: 'Reserve fund',
}

export const ARCA_BUCKET_ORDER: readonly ArcaBucket[] = ['Shares', 'RealEstate', 'Cash', 'Pension', 'ReserveFund']

export const NOT_IN_ARCA = 'Not in ARCA'

export function arcaBucketLabel(bucket: ArcaBucket | null): string {
  return bucket === null ? NOT_IN_ARCA : ARCA_BUCKET_LABELS[bucket]
}
