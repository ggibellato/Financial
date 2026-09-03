import { describe, expect, it } from 'vitest'
import type { AssetPriceSnapshotDto, TransactionDto } from '../../api/types'
import { buildChartData } from '../priceHistoryChartData'

const MANUAL_ENTRY: AssetPriceSnapshotDto = { date: '2024-03-15T00:00:00', price: 120.5, isManual: true }
const AUTOMATIC_ENTRY: AssetPriceSnapshotDto = { date: '2024-01-10T00:00:00', price: 350.0, isManual: false }

const BUY_TRANSACTION: TransactionDto = {
  id: 'tx-buy',
  date: '2024-02-01T00:00:00',
  type: 'Buy',
  quantity: 10,
  unitPrice: 90,
  fees: 1,
  totalPrice: 901,
}

const SELL_TRANSACTION: TransactionDto = {
  id: 'tx-sell',
  date: '2024-02-10T00:00:00',
  type: 'Sell',
  quantity: 5,
  unitPrice: 130,
  fees: 1,
  totalPrice: 649,
}

describe('buildChartData', () => {
  it('combines_price_history_and_transactions_sorted_by_date_into_one_series', () => {
    const points = buildChartData([MANUAL_ENTRY, AUTOMATIC_ENTRY], [BUY_TRANSACTION, SELL_TRANSACTION])
    expect(points.map((p) => p.date)).toEqual(['10/01/2024', '01/02/2024', '10/02/2024', '15/03/2024'])
  })

  it('maps_buy_transactions_to_value_and_kind_using_unit_price', () => {
    const points = buildChartData([], [BUY_TRANSACTION])
    expect(points).toEqual([{ x: expect.any(Number), date: '01/02/2024', value: 90, kind: 'buy' }])
  })

  it('maps_sell_transactions_to_value_and_kind_using_unit_price', () => {
    const points = buildChartData([], [SELL_TRANSACTION])
    expect(points).toEqual([{ x: expect.any(Number), date: '10/02/2024', value: 130, kind: 'sell' }])
  })

  it('maps_manual_price_history_entries_to_value_and_kind', () => {
    const points = buildChartData([MANUAL_ENTRY], [])
    expect(points).toEqual([{ x: expect.any(Number), date: '15/03/2024', value: 120.5, kind: 'manual' }])
  })

  it('maps_automatic_price_history_entries_to_value_and_kind', () => {
    const points = buildChartData([AUTOMATIC_ENTRY], [])
    expect(points).toEqual([{ x: expect.any(Number), date: '10/01/2024', value: 350, kind: 'automatic' }])
  })
})
