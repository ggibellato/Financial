import { act, fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import DividendCheckPage from '../DividendCheckPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { DividendHistoryItemDto, DividendSummaryDto } from '../../api/types'
import { never } from '../../test-utils/deferred'

const { getDividendSummaryMock, getDividendHistoryMock, getWatchlistMock } = vi.hoisted(() => ({
  getDividendSummaryMock: vi.fn<FinancialApiClient['getDividendSummary']>(),
  getDividendHistoryMock: vi.fn<FinancialApiClient['getDividendHistory']>(),
  getWatchlistMock: vi.fn<FinancialApiClient['getWatchlist']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getDividendSummary: getDividendSummaryMock,
    getDividendHistory: getDividendHistoryMock,
    getWatchlist: getWatchlistMock,
  } as Partial<FinancialApiClient>,
}))

const tickerField = () => screen.getByRole('combobox', { name: /Ticker/ })

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
  history: [],
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
    expect(screen.getByText('Choose a ticker, then Check dividends.')).toBeInTheDocument()
  })

  it('defaults to first watchlist item on load', async () => {
    render(<DividendCheckPage />)
    expect(await screen.findByDisplayValue('KLBN4')).toBeInTheDocument()
  })

  it('calls API with the trimmed, uppercased ticker and fixed BVMF exchange', async () => {
    getDividendSummaryMock.mockResolvedValue(baseSummary)
    getDividendHistoryMock.mockResolvedValue(baseHistory)

    render(<DividendCheckPage />)
    fireEvent.change(tickerField(), { target: { value: '  klbn4  ' } })
    fireEvent.click(screen.getByRole('button', { name: 'Check dividends' }))

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
    fireEvent.click(screen.getByRole('button', { name: 'Check dividends' }))

    expect(await screen.findByText('KLBN4 - Klabin SA')).toBeInTheDocument()
    expect(screen.getByText(/Current price:/)).toHaveTextContent('R$')
    expect(screen.getByText(/Average dividend:/)).toBeInTheDocument()
    expect(screen.getByText(/Price max buy:/)).toBeInTheDocument()
  })



  it('shows dividend history in date descending order', async () => {
    getDividendSummaryMock.mockResolvedValue(baseSummary)
    getDividendHistoryMock.mockResolvedValue([
      { type: 'Dividend', date: '2023-12-10T00:00:00Z', value: 0.87 },
      { type: 'Dividend', date: '2024-06-15T00:00:00Z', value: 1.23 },
    ])

    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    fireEvent.click(screen.getByRole('button', { name: 'Check dividends' }))
    await screen.findByText('Dividend history')

    const historyRows = within(screen.getByRole('table', { name: 'Dividend history' })).getAllByRole('row')
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
    fireEvent.click(screen.getByRole('button', { name: 'Check dividends' }))
    await screen.findByText('By year')

    const yearTable = screen.getByRole('table', { name: 'By year' })
    const yearRows = within(yearTable).getAllByRole('row')
    expect(yearRows[1]).toHaveTextContent('2024')
    expect(yearRows[2]).toHaveTextContent('2022')

    const yearHeader = within(yearTable).getByRole('button', { name: 'Year' })
    expect(yearHeader.closest('th')).toHaveAttribute('aria-sort', 'descending')
  })

  it('cycles the Date header through unsorted -> ascending -> descending, starting from the default descending sort', async () => {
    getDividendSummaryMock.mockResolvedValue(baseSummary)
    getDividendHistoryMock.mockResolvedValue([
      { type: 'Dividend', date: '2023-12-10T00:00:00Z', value: 0.87 },
      { type: 'Dividend', date: '2025-01-01T00:00:00Z', value: 2.5 },
      { type: 'Dividend', date: '2024-06-15T00:00:00Z', value: 1.23 },
    ])

    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    fireEvent.click(screen.getByRole('button', { name: 'Check dividends' }))
    await screen.findByText('Dividend history')

    const tables = screen.getAllByRole('table')
    const dateHeaderButton = within(tables[0]).getByRole('button', { name: 'Date' })
    const dateHeader = dateHeaderButton.closest('th')
    const readDates = () => within(tables[0]).getAllByRole('row').slice(1).map((row) => row.textContent)

    expect(dateHeader).toHaveAttribute('aria-sort', 'descending')
    expect(readDates()).toEqual([
      expect.stringContaining('01/01/2025'),
      expect.stringContaining('15/06/2024'),
      expect.stringContaining('10/12/2023'),
    ])

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'none')
    expect(readDates()).toEqual([
      expect.stringContaining('10/12/2023'),
      expect.stringContaining('01/01/2025'),
      expect.stringContaining('15/06/2024'),
    ])

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'ascending')
    expect(readDates()).toEqual([
      expect.stringContaining('10/12/2023'),
      expect.stringContaining('15/06/2024'),
      expect.stringContaining('01/01/2025'),
    ])

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'descending')
    expect(readDates()).toEqual([
      expect.stringContaining('01/01/2025'),
      expect.stringContaining('15/06/2024'),
      expect.stringContaining('10/12/2023'),
    ])
  })

  it('sorts by-year totals by year ascending when the Year header button is clicked', async () => {
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
    fireEvent.click(screen.getByRole('button', { name: 'Check dividends' }))
    await screen.findByText('By year')

    const tables = screen.getAllByRole('table')
    fireEvent.click(within(tables[1]).getByRole('button', { name: 'Year' }))

    const yearRows = within(tables[1]).getAllByRole('row')
    expect(yearRows[1]).toHaveTextContent('2022')
    expect(yearRows[2]).toHaveTextContent('2024')
  })

  it('shows Checking… and disables button during fetch', async () => {
    getDividendSummaryMock.mockReturnValue(never())
    getDividendHistoryMock.mockReturnValue(never())

    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    fireEvent.click(screen.getByRole('button', { name: 'Check dividends' }))

    expect(screen.getByRole('button', { name: 'Checking…' })).toHaveAttribute('aria-disabled', 'true')
    expect(screen.getByRole('status')).toHaveTextContent('Checking dividends…')
  })

  it('clears results and shows error on check failure', async () => {
    getDividendSummaryMock.mockResolvedValueOnce(baseSummary)
    getDividendHistoryMock.mockResolvedValueOnce(baseHistory)

    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    fireEvent.click(screen.getByRole('button', { name: 'Check dividends' }))
    await screen.findByText('KLBN4 - Klabin SA')

    getDividendSummaryMock.mockRejectedValue(new Error('API error'))
    getDividendHistoryMock.mockRejectedValue(new Error('API error'))
    fireEvent.click(screen.getByRole('button', { name: 'Check dividends' }))
    await screen.findByRole('alert')

    expect(screen.queryByText('KLBN4 - Klabin SA')).not.toBeInTheDocument()
  })

  it('shows inline error with re-enabled Check button after failure', async () => {
    getDividendSummaryMock.mockRejectedValue(new Error('Network error'))
    getDividendHistoryMock.mockResolvedValue([])

    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    fireEvent.click(screen.getByRole('button', { name: 'Check dividends' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Network error')
    expect(screen.getByRole('button', { name: 'Check dividends' })).not.toBeDisabled()
  })

  const check = () => fireEvent.click(screen.getByRole('button', { name: 'Check dividends' }))

  it('offers the watchlist tickers under their groups', async () => {
    getWatchlistMock.mockResolvedValue([
      { group: 'Ja possuidas', name: 'KLBN4' },
      { group: 'Outras', name: 'BBAS3' },
      { group: 'Ja possuidas', name: 'TASA4' },
    ])
    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')

    await userEvent.click(tickerField())

    const tickersOf = (group: string) =>
      within(screen.getByRole('group', { name: group })).getAllByRole('option').map((option) => option.textContent)
    expect(tickersOf('Ja possuidas')).toEqual(['KLBN4', 'TASA4'])
    expect(tickersOf('Outras')).toEqual(['BBAS3'])
  })

  it('says the watchlist is loading while it is on its way, and still accepts a typed ticker', () => {
    getWatchlistMock.mockReturnValue(never())
    render(<DividendCheckPage />)

    expect(tickerField()).toHaveAttribute('placeholder', 'Loading watchlist…')
    expect(tickerField()).toBeEnabled()
  })

  it('offers to reload the watchlist when it fails to load, keeping the ticker typeable', async () => {
    getWatchlistMock.mockRejectedValueOnce(new Error('Network error'))
    render(<DividendCheckPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network error')
    expect(tickerField()).toBeEnabled()

    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByDisplayValue('KLBN4')).toBeInTheDocument()
    expect(screen.queryByText('Network error')).not.toBeInTheDocument()
    await waitFor(() => expect(tickerField()).toHaveFocus())
  })

  it('checks the typed ticker when Enter is pressed in the field', async () => {
    getDividendSummaryMock.mockResolvedValue({ ...baseSummary, ticker: 'CXSE3', name: 'CXSE3 SA' })
    getDividendHistoryMock.mockResolvedValue([])
    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')

    await userEvent.clear(tickerField())
    await userEvent.type(tickerField(), 'CXSE3{Enter}')

    expect(await screen.findByText('CXSE3 - CXSE3 SA')).toBeInTheDocument()
    expect(getDividendSummaryMock).toHaveBeenCalledTimes(1)
  })

  it('completes a partial ticker on the first Enter and checks it on the second, leaving the list closed', async () => {
    getDividendSummaryMock.mockResolvedValue(baseSummary)
    getDividendHistoryMock.mockResolvedValue([])
    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')

    await userEvent.clear(tickerField())
    await userEvent.type(tickerField(), 'TAS{Enter}')
    expect(tickerField()).toHaveValue('TASA4')
    expect(getDividendSummaryMock).not.toHaveBeenCalled()

    await userEvent.keyboard('{Enter}')

    expect(getDividendSummaryMock).toHaveBeenCalledWith('TASA4', 'BVMF')
    expect(tickerField()).toHaveAttribute('aria-expanded', 'false')
  })

  it('keeps focus on the button and ignores another submit while a check runs', async () => {
    getDividendSummaryMock.mockReturnValue(never())
    getDividendHistoryMock.mockReturnValue(never())
    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')

    await userEvent.click(screen.getByRole('button', { name: 'Check dividends' }))
    await userEvent.click(screen.getByRole('button', { name: 'Checking…' }))

    expect(screen.getByRole('button', { name: 'Checking…' })).toHaveFocus()
    expect(getDividendSummaryMock).toHaveBeenCalledTimes(1)
  })

  it('keeps a ticker typed before the watchlist arrives', async () => {
    let resolveWatchlist: (items: { group: string; name: string }[]) => void = () => undefined
    getWatchlistMock.mockReturnValue(new Promise((resolve) => { resolveWatchlist = resolve }))
    render(<DividendCheckPage />)

    fireEvent.change(tickerField(), { target: { value: 'CXSE3' } })
    await act(async () => resolveWatchlist([{ group: 'Ja possuidas', name: 'KLBN4' }]))

    expect(tickerField()).toHaveValue('CXSE3')
  })

  it('loads an empty watchlist without failing, offering no ticker', async () => {
    getWatchlistMock.mockResolvedValue([])
    render(<DividendCheckPage />)

    await act(async () => undefined)

    expect(tickerField()).toHaveValue('')
    expect(tickerField()).toHaveAccessibleDescription('Your watchlist is empty. Type a ticker to check it.')
  })

  it.each(['', '   '])('asks for a ticker instead of checking %j', async (blank) => {
    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')

    fireEvent.change(tickerField(), { target: { value: blank } })
    check()

    expect(tickerField()).toHaveAccessibleDescription('Enter a ticker to check.')
    expect(tickerField()).toHaveFocus()
    expect(screen.queryByRole('button', { name: 'Try again' })).not.toBeInTheDocument()
    expect(getDividendSummaryMock).not.toHaveBeenCalled()

    fireEvent.change(tickerField(), { target: { value: 'K' } })

    expect(tickerField()).not.toHaveAccessibleDescription('Enter a ticker to check.')
  })

  it('clears the error when a later check succeeds', async () => {
    getDividendSummaryMock.mockRejectedValueOnce(new Error('Network error')).mockResolvedValueOnce(baseSummary)
    getDividendHistoryMock.mockResolvedValue(baseHistory)
    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    check()
    await screen.findByRole('alert')

    check()

    await screen.findByText('KLBN4 - Klabin SA')
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('says the dividend data could not be loaded when the check fails without a reason', async () => {
    getDividendSummaryMock.mockRejectedValue('offline')
    getDividendHistoryMock.mockResolvedValue([])
    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')

    check()

    expect(await screen.findByRole('alert')).toHaveTextContent('Unable to load dividend data.')
  })

  it('checks without reloading the page', async () => {
    getDividendSummaryMock.mockResolvedValue(baseSummary)
    getDividendHistoryMock.mockResolvedValue(baseHistory)
    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')

    expect(fireEvent.submit(screen.getByRole('form', { name: 'Dividend check' }))).toBe(false)
    expect(await screen.findByText('KLBN4 - Klabin SA')).toBeInTheDocument()
  })

  it('sorts the history by Type and by Value, and the year totals by Total, marking each header', async () => {
    getDividendSummaryMock.mockResolvedValue({
      ...baseSummary,
      yearTotals: [
        { year: 2024, total: 3 },
        { year: 2023, total: 5 },
        { year: 2022, total: 1 },
      ],
    })
    getDividendHistoryMock.mockResolvedValue([
      { type: 'JCP', date: '2025-01-01T00:00:00Z', value: 0.5 },
      { type: 'Dividend', date: '2024-06-15T00:00:00Z', value: 2.0 },
      { type: 'Rendimento', date: '2023-12-10T00:00:00Z', value: 1.0 },
    ])
    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')
    check()
    const history = await screen.findByRole('table', { name: 'Dividend history' })
    const years = screen.getByRole('table', { name: 'By year' })
    const column = (table: HTMLElement, index: number) =>
      within(table).getAllByRole('row').slice(1).map((row) => within(row).getAllByRole('cell')[index].textContent)
    const headerOf = (table: HTMLElement, name: string) => within(table).getByRole('button', { name }).closest('th')

    fireEvent.click(within(history).getByRole('button', { name: 'Type' }))
    expect(column(history, 0)).toEqual([expect.stringContaining('Dividend'), expect.stringContaining('JCP'), expect.stringContaining('Rendimento')])
    expect(headerOf(history, 'Type')).toHaveAttribute('aria-sort', 'ascending')

    fireEvent.click(within(history).getByRole('button', { name: 'Value' }))
    expect(column(history, 0)).toEqual([expect.stringContaining('JCP'), expect.stringContaining('Rendimento'), expect.stringContaining('Dividend')])
    expect(headerOf(history, 'Value')).toHaveAttribute('aria-sort', 'ascending')

    fireEvent.click(within(years).getByRole('button', { name: 'Total' }))
    expect(column(years, 0)).toEqual([expect.stringContaining('2022'), expect.stringContaining('2024'), expect.stringContaining('2023')])
    expect(headerOf(years, 'Total')).toHaveAttribute('aria-sort', 'ascending')
  })



  it.each([
    ['below', 10, 20, 'summary-card__price-max--positive', 'current price below max buy'],
    ['above', 25, 20, 'summary-card__price-max--negative', 'current price not below max buy'],
    ['equal to', 20, 20, 'summary-card__price-max--negative', 'current price not below max buy'],
    ['with no dividends to set', 10, null, undefined, 'no dividends to set a max buy'],
    ['missing against', 0, 20, undefined, 'no current price to compare'],
  ])('marks a current price %s the max buy', async (_, currentPrice, priceMaxBuy, colourClass, verdict) => {
    const discountPercent = currentPrice > 0 && priceMaxBuy ? baseSummary.discountPercent : null
    getDividendSummaryMock.mockResolvedValue({ ...baseSummary, currentPrice, priceMaxBuy, discountPercent })
    getDividendHistoryMock.mockResolvedValue([])
    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')

    check()

    const priceMaxText = await screen.findByText(/Price max buy:/)
    expect(priceMaxText.className).toBe(colourClass ?? '')
    expect(priceMaxText.textContent).toMatch(new RegExp(`— ${verdict}$`))
  })

  it('shows the max buy, discount and yield as not available when they are absent', async () => {
    getDividendSummaryMock.mockResolvedValue({ ...baseSummary, priceMaxBuy: null, discountPercent: null, dividendYieldPercent: null })
    getDividendHistoryMock.mockResolvedValue([])
    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')

    check()

    expect(await screen.findByRole('img', { name: 'Not set: no dividends' })).toHaveTextContent('—')
    expect(screen.getByRole('img', { name: 'Not available: no max buy or current price' })).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Not available: no current price' })).toBeInTheDocument()
  })

  it('says when there is no history and no annual totals', async () => {
    getDividendSummaryMock.mockResolvedValue({ ...baseSummary, yearTotals: [] })
    getDividendHistoryMock.mockResolvedValue([])
    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')

    check()

    expect(await screen.findByText('No dividend history found.')).toBeInTheDocument()
    expect(screen.getByText('No annual totals available.')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('drops the placeholder once a check has a result or an error', async () => {
    getDividendSummaryMock.mockResolvedValueOnce(baseSummary).mockRejectedValueOnce(new Error('Network error'))
    getDividendHistoryMock.mockResolvedValue(baseHistory)
    render(<DividendCheckPage />)
    await screen.findByDisplayValue('KLBN4')

    check()
    await screen.findByText('KLBN4 - Klabin SA')
    expect(screen.queryByText('Choose a ticker, then Check dividends.')).not.toBeInTheDocument()

    check()
    await screen.findByRole('alert')
    expect(screen.queryByText('Choose a ticker, then Check dividends.')).not.toBeInTheDocument()
  })

  it('sends freeform ticker not in watchlist to API', async () => {
    getDividendSummaryMock.mockResolvedValue({ ...baseSummary, ticker: 'CXSE3', name: 'CXSE3 SA' })
    getDividendHistoryMock.mockResolvedValue([])

    render(<DividendCheckPage />)
    fireEvent.change(tickerField(), { target: { value: 'CXSE3' } })
    fireEvent.click(screen.getByRole('button', { name: 'Check dividends' }))

    await waitFor(() => {
      expect(getDividendSummaryMock).toHaveBeenCalledWith('CXSE3', 'BVMF')
    })
  })
})
