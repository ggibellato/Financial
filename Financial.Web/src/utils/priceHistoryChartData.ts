import type { AssetPriceSnapshotDto, TransactionDto } from '../api/types'
import { formatShortDate } from './formatters'

export type ChartPointKind = 'automatic' | 'manual' | 'buy' | 'sell'

export interface ChartPoint {
  x: number
  date: string
  value: number
  kind: ChartPointKind
}

export function buildChartData(entries: AssetPriceSnapshotDto[], transactions: TransactionDto[]): ChartPoint[] {
  const historyPoints: ChartPoint[] = entries.map((entry) => ({
    x: new Date(entry.date).getTime(),
    date: formatShortDate(entry.date),
    value: entry.price,
    kind: entry.isManual ? 'manual' : 'automatic',
  }))
  const transactionPoints: ChartPoint[] = transactions.map((transaction) => ({
    x: new Date(transaction.date).getTime(),
    date: formatShortDate(transaction.date),
    value: transaction.unitPrice,
    kind: transaction.type === 'Buy' ? 'buy' : 'sell',
  }))
  return [...historyPoints, ...transactionPoints].sort((a, b) => a.x - b.x)
}
