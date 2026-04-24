import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import DividendCheckPage from '../DividendCheckPage'

const getDividendSummaryMock = vi.fn()
const getDividendHistoryMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: () => ({
    getDividendSummary: getDividendSummaryMock,
    getDividendHistory: getDividendHistoryMock,
  }),
}))

describe('DividendCheckPage', () => {
  beforeEach(() => {
    getDividendSummaryMock.mockReset()
    getDividendHistoryMock.mockReset()
  })

  it('loads summary and history on check', async () => {
    getDividendSummaryMock.mockResolvedValue({
      exchange: 'BVMF',
      ticker: 'BCIA11',
      name: 'Sample Asset',
      currentPrice: 10.5,
      priceAsOf: '2024-02-01T00:00:00Z',
      averageDividendLastFiveYears: 4,
      priceMaxBuy: 66.67,
      discountPercent: 20,
      yearTotals: [{ year: 2023, total: 4 }],
    })
    getDividendHistoryMock.mockResolvedValue([
      {
        type: 'Dividend',
        date: '2024-02-01T00:00:00Z',
        value: 1.23,
      },
    ])

    render(<DividendCheckPage />)

    fireEvent.change(screen.getByLabelText('Ticker'), { target: { value: 'bcia11' } })
    fireEvent.change(screen.getByLabelText('Exchange'), { target: { value: 'bvmf' } })
    fireEvent.click(screen.getByRole('button', { name: 'Check' }))

    expect(await screen.findByText('BCIA11 - Sample Asset')).toBeInTheDocument()
    expect(screen.getByText('Dividend')).toBeInTheDocument()

    await waitFor(() => {
      expect(getDividendSummaryMock).toHaveBeenCalledWith('BCIA11', 'BVMF')
      expect(getDividendHistoryMock).toHaveBeenCalledWith('BCIA11', 'BVMF')
    })
  })

  it('shows error when request fails', async () => {
    getDividendSummaryMock.mockRejectedValue(new Error('Boom'))
    getDividendHistoryMock.mockResolvedValue([])

    render(<DividendCheckPage />)

    fireEvent.change(screen.getByLabelText('Ticker'), { target: { value: 'BCIA11' } })
    fireEvent.click(screen.getByRole('button', { name: 'Check' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Boom')
  })
})
