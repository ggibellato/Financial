import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import DividendCheckPage from '../DividendCheckPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { DividendHistoryItemDto, DividendSummaryDto } from '../../api/types'

const getDividendSummaryMock = vi.fn()
const getDividendHistoryMock = vi.fn()
const getWatchlistMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: () => ({
    getDividendSummary: getDividendSummaryMock,
    getDividendHistory: getDividendHistoryMock,
    getWatchlist: getWatchlistMock,
  } satisfies Partial<FinancialApiClient>),
}))

const baseSummary: DividendSummaryDto = {
  exchange: 'BVMF',
  ticker: 'KLBN4',
  name: 'Klabin SA',
  currentPrice: 10.5,
  priceAsOf: '2024-02-01T00:00:00Z',
  averageDividendLastFiveYears: 1.4,
  dividendYieldPercent: 13.33,
  priceMaxBuy: 20.0,
  discountPercent: 47.5,
  yearTotals: [{ year: 2023, total: 1.4 }],
}

const baseHistory: DividendHistoryItemDto[] = [
  { type: 'Dividend', date: '2024-02-01T00:00:00Z', value: 1.23 },
]

describe('DividendCheckPage', () => {
  beforeEach(() => {
    getDividendSummaryMock.mockReset()
    getDividendHistoryMock.mockReset()
    getWatchlistMock.mockResolvedValue([
      { group: 'Ja possuidas', name: 'KLBN4' },
      { group: 'Ja possuidas', name: 'TASA4' },
    ])
  })

  it('shows placeholder text before first check', () => {
    render(<DividendCheckPage />)
    expect(screen.getByText('Select a ticker and click Check')).toBeInTheDocument()
  })

  it('defaults to first watchlist item on load', async () => {
    render(<DividendCheckPage />)
    expect(await screen.findByDisplayValue('KLBN4')).toBeInTheDocument()
  })

  it('calls API with uppercased ticker and fixed BVMF exchange', async () => {
    getDividendSummaryMock.mockResolvedValue(baseSummary)
    getDividendHistoryMock.mockResolvedValue(baseHistory)

    render(<DividendCheckPage />)
    fireEvent.change(screen.getByLabelText('Ticker'), { target: { value: 'klbn4' } })
    fireEvent.click(screen.getByRole('button', { name: 'Check' }))

    await waitFor(() => {
      expect(getDividendSummaryMock).toHaveBeenCalledWith('KLBN4', 'BVMF')
      expect(getDividendHistoryMock).toHaveBeenCalledWith('KLBN4', 'BVMF')
    })
  })

  it('populates summary card after successful check', async () => {
    getDividendSummaryMock.mockResolvedValue(baseSummary)
    getDividendHistoryMock.mockResolvedValue(baseHistory)

    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    fireEvent.click(screen.getByRole('button', { name: 'Check' }))

    expect(await screen.findByText('KLBN4 - Klabin SA')).toBeInTheDocument()
    expect(screen.getByText(/Current price:/)).toBeInTheDocument()
    expect(screen.getByText(/Average Dividend:/)).toBeInTheDocument()
    expect(screen.getByText(/Price max buy:/)).toBeInTheDocument()
  })

  it('applies positive class when current price is below price max buy', async () => {
    getDividendSummaryMock.mockResolvedValue({ ...baseSummary, currentPrice: 10, priceMaxBuy: 20 })
    getDividendHistoryMock.mockResolvedValue([])

    const { container } = render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    fireEvent.click(screen.getByRole('button', { name: 'Check' }))

    await screen.findByText(/Price max buy:/)
    expect(container.querySelector('.summary-card__price-max--positive')).toBeInTheDocument()
    expect(container.querySelector('.summary-card__price-max--negative')).not.toBeInTheDocument()
  })

  it('applies negative class when current price is above price max buy', async () => {
    getDividendSummaryMock.mockResolvedValue({ ...baseSummary, currentPrice: 25, priceMaxBuy: 20 })
    getDividendHistoryMock.mockResolvedValue([])

    const { container } = render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    fireEvent.click(screen.getByRole('button', { name: 'Check' }))

    await screen.findByText(/Price max buy:/)
    expect(container.querySelector('.summary-card__price-max--negative')).toBeInTheDocument()
    expect(container.querySelector('.summary-card__price-max--positive')).not.toBeInTheDocument()
  })

  it('shows dividend history in date descending order', async () => {
    getDividendSummaryMock.mockResolvedValue(baseSummary)
    getDividendHistoryMock.mockResolvedValue([
      { type: 'Dividend', date: '2023-12-10T00:00:00Z', value: 0.87 },
      { type: 'Dividend', date: '2024-06-15T00:00:00Z', value: 1.23 },
    ])

    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    fireEvent.click(screen.getByRole('button', { name: 'Check' }))
    await screen.findByText('Dividend History')

    const tables = screen.getAllByRole('table')
    const historyRows = within(tables[0]).getAllByRole('row')
    expect(historyRows[1]).toHaveTextContent('15/06/2024')
    expect(historyRows[2]).toHaveTextContent('10/12/2023')
  })

  it('shows by year totals in year descending order', async () => {
    const summary = {
      ...baseSummary,
      yearTotals: [
        { year: 2022, total: 3 },
        { year: 2024, total: 5 },
      ],
    }
    getDividendSummaryMock.mockResolvedValue(summary)
    getDividendHistoryMock.mockResolvedValue(baseHistory)

    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    fireEvent.click(screen.getByRole('button', { name: 'Check' }))
    await screen.findByText('By Year')

    const tables = screen.getAllByRole('table')
    const yearRows = within(tables[1]).getAllByRole('row')
    expect(yearRows[1]).toHaveTextContent('2024')
    expect(yearRows[2]).toHaveTextContent('2022')
  })

  it('shows Checking... and disables button during fetch', async () => {
    getDividendSummaryMock.mockReturnValue(new Promise(() => {}))
    getDividendHistoryMock.mockReturnValue(new Promise(() => {}))

    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    fireEvent.click(screen.getByRole('button', { name: 'Check' }))

    expect(screen.getByRole('button', { name: 'Checking...' })).toBeDisabled()
  })

  it('clears results and shows error on check failure', async () => {
    getDividendSummaryMock.mockResolvedValueOnce(baseSummary)
    getDividendHistoryMock.mockResolvedValueOnce(baseHistory)

    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    fireEvent.click(screen.getByRole('button', { name: 'Check' }))
    await screen.findByText('KLBN4 - Klabin SA')

    getDividendSummaryMock.mockRejectedValue(new Error('API error'))
    getDividendHistoryMock.mockRejectedValue(new Error('API error'))
    fireEvent.click(screen.getByRole('button', { name: 'Check' }))
    await screen.findByRole('alert')

    expect(screen.queryByText('KLBN4 - Klabin SA')).not.toBeInTheDocument()
  })

  it('shows inline error with re-enabled Check button after failure', async () => {
    getDividendSummaryMock.mockRejectedValue(new Error('Network error'))
    getDividendHistoryMock.mockResolvedValue([])

    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    fireEvent.click(screen.getByRole('button', { name: 'Check' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Network error')
    expect(screen.getByRole('button', { name: 'Check' })).not.toBeDisabled()
  })

  it('sends freeform ticker not in watchlist to API', async () => {
    getDividendSummaryMock.mockResolvedValue({ ...baseSummary, ticker: 'CXSE3', name: 'CXSE3 SA' })
    getDividendHistoryMock.mockResolvedValue([])

    render(<DividendCheckPage />)
    fireEvent.change(screen.getByLabelText('Ticker'), { target: { value: 'CXSE3' } })
    fireEvent.click(screen.getByRole('button', { name: 'Check' }))

    await waitFor(() => {
      expect(getDividendSummaryMock).toHaveBeenCalledWith('CXSE3', 'BVMF')
    })
  })
})
