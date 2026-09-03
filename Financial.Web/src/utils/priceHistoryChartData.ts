import type { AssetPriceSnapshotDto, TransactionDto } from '../api/types'
import { formatShortDate } from './formatters'

export interface ChartPoint {
  x: number
  date: string
  price?: number
  isManual?: boolean
  buyPrice?: number
  sellPrice?: number
}

export function buildChartData(entries: AssetPriceSnapshotDto[], transactions: TransactionDto[]): ChartPoint[] {
  const historyPoints: ChartPoint[] = entries.map((entry) => ({
    x: new Date(entry.date).getTime(),
    date: formatShortDate(entry.date),
    price: entry.price,
    isManual: entry.isManual,
  }))
  const transactionPoints: ChartPoint[] = transactions.map((transaction) => ({
    x: new Date(transaction.date).getTime(),
    date: formatShortDate(transaction.date),
    buyPrice: transaction.type === 'Buy' ? transaction.unitPrice : undefined,
    sellPrice: transaction.type === 'Sell' ? transaction.unitPrice : undefined,
  }))
  return [...historyPoints, ...transactionPoints].sort((a, b) => a.x - b.x)
}
