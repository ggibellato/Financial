import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CurrentValuesPage from '../CurrentValuesPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetPriceDto, BrokerNodeDto, PortfolioReferenceDto } from '../../api/types'

const { getBrokersMock, getCurrentPriceMock, getAssetPriceFetchScopeMock } = vi.hoisted(() => ({
  getBrokersMock: vi.fn<FinancialApiClient['getBrokers']>(),
  getCurrentPriceMock: vi.fn<FinancialApiClient['getCurrentPrice']>(),
  getAssetPriceFetchScopeMock: vi.fn<FinancialApiClient['getAssetPriceFetchScope']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getBrokers: getBrokersMock,
    getCurrentPrice: getCurrentPriceMock,
    getAssetPriceFetchScope: getAssetPriceFetchScopeMock,
  } as Partial<FinancialApiClient>,
}))

const makeBroker = (
  portfolios: { name: string; assets: Partial<BrokerNodeDto['portfolios'][0]['assets'][0]>[] }[],
  brokerName = 'XPI',
  currency = 'BRL',
): BrokerNodeDto => ({
  name: brokerName,
  currency,
  portfolioCount: portfolios.length,
  totalAssets: portfolios.reduce((sum, p) => sum + p.assets.length, 0),
  portfolios: portfolios.map((p) => ({
    name: p.name,
    assetCount: p.assets.length,
    assets: p.assets.map((a) => ({
      name: a.name ?? 'ASSET',
      ticker: a.ticker ?? 'TICK',
      exchange: a.exchange ?? 'BVMF',
      country: 'BR',
      localTypeCode: 'FII',
      class: a.class ?? 'RealEstate',
      isin: 'BR000',
      quantity: 10,
      averagePrice: 100,
      positionType: a.positionType ?? 'Long',
      transactionCount: 0,
      creditCount: 0,
    })),
  })),
})

const makePrice = (ticker: string, price = 10.5): AssetPriceDto => ({
  exchange: 'BVMF',
  ticker,
  name: `${ticker} Name`,
  price,
  asOf: '2024-02-01T00:00:00Z',
  asOfDate: null,
  isManual: false,
  marketStatus: 'Current',
  source: 'Unknown',
})

describe('CurrentValuesPage', () => {
  const defaultScope: PortfolioReferenceDto[] = [
    { brokerName: 'XPI', portfolioName: 'Default' },
    { brokerName: 'XPI', portfolioName: 'Acoes' },
  ]

  beforeEach(() => {
    getBrokersMock.mockReset()
    getCurrentPriceMock.mockReset()
    getAssetPriceFetchScopeMock.mockReset()
    getAssetPriceFetchScopeMock.mockResolvedValue(defaultScope)
  })

  it('fetches prices only for assets in XPI/Default and XPI/Acoes', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([
        { name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] },
        { name: 'Acoes', assets: [{ name: 'KLBN4', ticker: 'KLBN4' }] },
        { name: 'Other', assets: [{ name: 'XXXX3', ticker: 'XXXX3' }] },
      ]),
    ])
    getCurrentPriceMock.mockResolvedValue(makePrice('BCIA11'))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))

    await waitFor(() => {
      expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'BCIA11', 'RealEstate', 'XPI', expect.any(String), expect.any(String), expect.any(String))
      expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'KLBN4', 'RealEstate', 'XPI', expect.any(String), expect.any(String), expect.any(String))
      expect(getCurrentPriceMock).not.toHaveBeenCalledWith('BVMF', 'XXXX3', 'RealEstate', 'XPI', expect.any(String), expect.any(String), expect.any(String))
    })
  })

  // Broker, portfolio and asset name together are what make the API record the fetched price
  // into Price history. Without them this screen took the lookup-only path and built no history.
  it('supplies the asset identity that enables recording', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([{ name: 'Acoes', assets: [{ name: 'KLBN4', ticker: 'KLBN4' }] }]),
    ])
    getCurrentPriceMock.mockResolvedValue(makePrice('KLBN4'))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))

    await waitFor(() =>
      expect(getCurrentPriceMock).toHaveBeenCalledWith(
        'BVMF',
        'KLBN4',
        'RealEstate',
        'XPI',
        'KLBN4',
        'Acoes',
        'KLBN4',
      ),
    )
  })

  it('does not render broker or portfolio filter controls', async () => {
    getBrokersMock.mockResolvedValue([makeBroker([{ name: 'Default', assets: [] }])])

    render(<CurrentValuesPage />)
    await screen.findByRole('button', { name: 'Check prices' })

    expect(screen.queryByRole('combobox')).not.toBeInTheDocument()
  })

  it('results table has Ticker, Name, Price columns and no As of column', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([{ name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] }]),
    ])
    getCurrentPriceMock.mockResolvedValue(makePrice('BCIA11'))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))

    await waitFor(() => expect(screen.queryByText(/Completed!/)).toBeInTheDocument())

    const headers = screen.getAllByRole('columnheader').map((th) => th.textContent)
    expect(headers).toContain('Ticker')
    expect(headers).toContain('Name')
    expect(headers).toContain('Price')
    expect(headers).not.toContain('As of')
  })

  it('Price cell shows formatted N2 value', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([{ name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] }]),
    ])
    getCurrentPriceMock.mockResolvedValue(makePrice('BCIA11', 85.5))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))

    expect(await screen.findByText('85.50')).toBeInTheDocument()
  })

  it('shows progress bar and initial progress text while fetching', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([{ name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] }]),
    ])
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))

    await waitFor(() => expect(screen.getByRole('progressbar')).toBeInTheDocument())
    expect(screen.getByText(/Fetching 0 of 1/)).toBeInTheDocument()
  })

  it('updates progress text after each individual fetch', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([
        { name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] },
        { name: 'Acoes', assets: [{ name: 'KLBN4', ticker: 'KLBN4' }] },
      ]),
    ])
    getCurrentPriceMock
      .mockResolvedValueOnce(makePrice('BCIA11'))
      .mockReturnValueOnce(new Promise(() => {}))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))

    await waitFor(() =>
      expect(screen.getByText('Fetching 1 of 2: BCIA11…')).toBeInTheDocument(),
    )
  })

  it('shows completion text after all fetches finish', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([{ name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] }]),
    ])
    getCurrentPriceMock.mockResolvedValue(makePrice('BCIA11'))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))

    expect(await screen.findByText('Completed! Loaded 1 assets.')).toBeInTheDocument()
    expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
  })

  it('Check prices button is disabled while running', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([{ name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] }]),
    ])
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))

    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Checking…' })).toBeDisabled(),
    )
  })

  it('shows dash in Price cell when individual asset fetch fails', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([
        {
          name: 'Default',
          assets: [
            { name: 'BCIA11', ticker: 'BCIA11' },
            { name: 'KLBN4', ticker: 'KLBN4' },
          ],
        },
      ]),
    ])
    getCurrentPriceMock
      .mockRejectedValueOnce(new Error('Network error'))
      .mockResolvedValueOnce(makePrice('KLBN4', 20.0))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))

    await waitFor(() => expect(screen.queryByText(/Completed!/)).toBeInTheDocument())

    const cells = screen.getAllByRole('cell')
    const priceCells = cells.filter((_, i) => i % 3 === 2)
    expect(priceCells[0]).toHaveTextContent('—')
    expect(priceCells[1]).toHaveTextContent('20.00')
  })

  it('sorts results by Price ascending when the Price header button is clicked', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([
        {
          name: 'Default',
          assets: [
            { name: 'BCIA11', ticker: 'BCIA11' },
            { name: 'KLBN4', ticker: 'KLBN4' },
          ],
        },
      ]),
    ])
    getCurrentPriceMock
      .mockResolvedValueOnce(makePrice('BCIA11', 85.5))
      .mockResolvedValueOnce(makePrice('KLBN4', 10.0))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))
    await waitFor(() => expect(screen.queryByText(/Completed!/)).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Price' }))

    const rows = screen.getAllByRole('row')
    expect(rows[1]).toHaveTextContent('KLBN4')
    expect(rows[2]).toHaveTextContent('BCIA11')
  })

  it('shows error state with Retry when broker tree fails to load', async () => {
    getBrokersMock.mockRejectedValue(new Error('Network error'))

    render(<CurrentValuesPage />)

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Check prices' })).not.toBeInTheDocument()
  })

  it('retries broker tree load on Retry click', async () => {
    getBrokersMock
      .mockRejectedValueOnce(new Error('Network error'))
      .mockResolvedValueOnce([makeBroker([{ name: 'Default', assets: [] }])])

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('button', { name: 'Check prices' })).toBeInTheDocument()
  })

  it('does not exclude flat or short assets from fetch scope', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([
        {
          name: 'Default',
          assets: [
            { name: 'BCIA11', ticker: 'BCIA11', positionType: 'Long' },
            { name: 'FLAT11', ticker: 'FLAT11', positionType: 'Flat' },
            { name: 'SHRT11', ticker: 'SHRT11', positionType: 'Short' },
          ],
        },
      ]),
    ])
    getCurrentPriceMock.mockResolvedValue(makePrice('BCIA11'))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))

    await waitFor(() => expect(screen.queryByText(/Completed!/)).toBeInTheDocument())

    expect(getCurrentPriceMock).toHaveBeenCalledTimes(3)
    expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'BCIA11', 'RealEstate', 'XPI', expect.any(String), expect.any(String), expect.any(String))
    expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'FLAT11', 'RealEstate', 'XPI', expect.any(String), expect.any(String), expect.any(String))
    expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'SHRT11', 'RealEstate', 'XPI', expect.any(String), expect.any(String), expect.any(String))
  })

  it('excludes assets with empty ticker or exchange', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([
        {
          name: 'Default',
          assets: [
            { name: 'NoTicker', ticker: '', exchange: 'BVMF' },
            { name: 'NOEX11', ticker: 'NOEX11', exchange: '' },
            { name: 'BCIA11', ticker: 'BCIA11', exchange: 'BVMF' },
          ],
        },
      ]),
    ])
    getCurrentPriceMock.mockResolvedValue(makePrice('BCIA11'))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))

    await waitFor(() => expect(screen.queryByText(/Completed!/)).toBeInTheDocument())

    expect(getCurrentPriceMock).toHaveBeenCalledTimes(1)
    expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'BCIA11', 'RealEstate', 'XPI', expect.any(String), expect.any(String), expect.any(String))
  })

  it('fetches Bitcoin under Coinbase/Cryptocurrency scope with assetClass and brokerName', async () => {
    getAssetPriceFetchScopeMock.mockResolvedValue([
      ...defaultScope,
      { brokerName: 'Coinbase', portfolioName: 'Cryptocurrency' },
    ])
    getBrokersMock.mockResolvedValue([
      makeBroker(
        [
          {
            name: 'Cryptocurrency',
            assets: [{ name: 'Bitcoin', ticker: 'BTC', exchange: '', class: 'Cryptocurrency' }],
          },
        ],
        'Coinbase',
        'GBP',
      ),
    ])
    getCurrentPriceMock.mockResolvedValue({
      exchange: '',
      ticker: 'BTC',
      name: 'Bitcoin',
      price: 48000,
      asOf: '2024-02-01T00:00:00Z',
      asOfDate: null,
      isManual: false,
      marketStatus: 'Current',
      source: 'Unknown',
    } satisfies AssetPriceDto)

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))

    await waitFor(() => {
      expect(getCurrentPriceMock).toHaveBeenCalledWith('', 'BTC', 'Cryptocurrency', 'Coinbase', expect.any(String), expect.any(String), expect.any(String))
    })
  })

  const checkPrices = async () => fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))
  const completed = () => screen.findByText(/Completed!/)
  const expectOnlyHeaderShown = () =>
    expect(screen.getByRole('heading', { name: 'Fetch current prices' }).closest('section')).toHaveTextContent(/^Fetch current pricesCheck prices$/)

  it('shows Loading while the brokers load, then no progress or results before a check', async () => {
    getBrokersMock.mockResolvedValue([makeBroker([{ name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] }])])

    render(<CurrentValuesPage />)

    expect(screen.getByText('Loading data…')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Check prices' })).not.toBeInTheDocument()
    await screen.findByRole('button', { name: 'Check prices' })
    expectOnlyHeaderShown()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('says the brokers could not be loaded when the load fails without a reason', async () => {
    getBrokersMock.mockRejectedValue('offline')

    render(<CurrentValuesPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Unable to load brokers.')
  })

  it('shows Loading again while a retry runs', async () => {
    getBrokersMock.mockRejectedValueOnce(new Error('Network error')).mockReturnValueOnce(new Promise(() => {}))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Try again' }))

    expect(screen.getByText('Loading data…')).toBeInTheDocument()
  })

  it('reads each portfolio in scope from its own broker', async () => {
    getAssetPriceFetchScopeMock.mockResolvedValue([{ brokerName: 'XPI', portfolioName: 'Default' }])
    getBrokersMock.mockResolvedValue([
      makeBroker([{ name: 'Default', assets: [{ name: 'OTHR3', ticker: 'OTHR3' }] }], 'Other'),
      makeBroker([{ name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] }]),
    ])
    getCurrentPriceMock.mockResolvedValue(makePrice('BCIA11'))

    render(<CurrentValuesPage />)
    await checkPrices()
    await completed()

    expect(getCurrentPriceMock).toHaveBeenCalledExactlyOnceWith('BVMF', 'BCIA11', 'RealEstate', 'XPI', 'BCIA11', 'Default', 'BCIA11')
  })

  it('does nothing when there are no assets to check', async () => {
    getBrokersMock.mockResolvedValue([makeBroker([{ name: 'Default', assets: [] }])])

    render(<CurrentValuesPage />)
    await checkPrices()

    expectOnlyHeaderShown()
    expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
    expect(getCurrentPriceMock).not.toHaveBeenCalled()
  })

  it('moves the progress bar by each finished asset', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([
        { name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] },
        { name: 'Acoes', assets: [{ name: 'KLBN4', ticker: 'KLBN4' }] },
      ]),
    ])
    getCurrentPriceMock.mockResolvedValueOnce(makePrice('BCIA11')).mockReturnValueOnce(new Promise(() => {}))

    render(<CurrentValuesPage />)
    await checkPrices()
    await screen.findByText('Fetching 1 of 2: BCIA11…')
    expect(screen.getByRole('progressbar')).toHaveAttribute('value', '50')
  })

  it('starts the progress bar from zero on a second check', async () => {
    getBrokersMock.mockResolvedValue([makeBroker([{ name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] }])])
    getCurrentPriceMock.mockResolvedValueOnce(makePrice('BCIA11')).mockReturnValueOnce(new Promise(() => {}))

    render(<CurrentValuesPage />)
    await checkPrices()
    await completed()
    await checkPrices()

    await waitFor(() => expect(screen.getByRole('progressbar')).toHaveAttribute('value', '0'))
  })

  it('names a result by the name the price lookup returns, else by the asset name', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([
        {
          name: 'Default',
          assets: [
            { name: 'Banco fund', ticker: 'BCIA11' },
            { name: 'Klabin', ticker: 'KLBN4' },
          ],
        },
      ]),
    ])
    getCurrentPriceMock.mockResolvedValueOnce(makePrice('BCIA11')).mockResolvedValueOnce({ ...makePrice('KLBN4'), name: '' })

    render(<CurrentValuesPage />)
    await checkPrices()
    await completed()

    const names = screen.getAllByRole('row').slice(1).map((row) => within(row).getAllByRole('cell')[1].textContent)
    expect(names).toEqual([expect.stringContaining('BCIA11 Name'), expect.stringContaining('Klabin')])
  })

  it('sorts the results by Ticker and by Name, marking the sorted header', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([
        {
          name: 'Default',
          assets: [
            { name: 'KLBN4', ticker: 'KLBN4' },
            { name: 'BCIA11', ticker: 'BCIA11' },
            { name: 'TAEE3', ticker: 'TAEE3' },
          ],
        },
      ]),
    ])
    const names: Record<string, string> = { KLBN4: 'Alpha paper', BCIA11: 'Zeta fund', TAEE3: 'Mid energy' }
    getCurrentPriceMock.mockImplementation(async (_exchange, ticker) => ({ ...makePrice(ticker), name: names[ticker] }))

    render(<CurrentValuesPage />)
    await checkPrices()
    await completed()
    const tickers = () => screen.getAllByRole('row').slice(1).map((row) => within(row).getAllByRole('cell')[0].textContent)
    const headerOf = (name: string) => screen.getByRole('button', { name }).closest('th')

    fireEvent.click(screen.getByRole('button', { name: 'Ticker' }))
    expect(tickers()).toEqual([expect.stringContaining('BCIA11'), expect.stringContaining('KLBN4'), expect.stringContaining('TAEE3')])
    expect(headerOf('Ticker')).toHaveAttribute('aria-sort', 'ascending')

    fireEvent.click(screen.getByRole('button', { name: 'Name' }))
    expect(tickers()).toEqual([expect.stringContaining('KLBN4'), expect.stringContaining('TAEE3'), expect.stringContaining('BCIA11')])
    expect(headerOf('Name')).toHaveAttribute('aria-sort', 'ascending')
    expect(headerOf('Ticker')).toHaveAttribute('aria-sort', 'none')

    fireEvent.click(screen.getByRole('button', { name: 'Price' }))
    expect(headerOf('Price')).toHaveAttribute('aria-sort', 'ascending')
  })

  it('keeps focus on a result as later results arrive and re-sort the rows', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([
        {
          name: 'Default',
          assets: [
            { name: 'ZZZ3', ticker: 'ZZZ3' },
            { name: 'AAA3', ticker: 'AAA3' },
          ],
        },
      ]),
    ])
    let resolveLast: (price: AssetPriceDto) => void = () => undefined
    getCurrentPriceMock.mockResolvedValueOnce(makePrice('ZZZ3')).mockReturnValueOnce(new Promise((resolve) => { resolveLast = resolve }))

    render(<CurrentValuesPage />)
    await checkPrices()
    fireEvent.click(await screen.findByRole('button', { name: 'Name' }))
    screen.getByText('ZZZ3 Name').focus()
    await act(async () => resolveLast(makePrice('AAA3')))

    expect(screen.getAllByRole('row')[1]).toHaveTextContent('AAA3')
    expect(document.activeElement).toHaveTextContent('ZZZ3 Name')
  })

  it('does not exclude Cryptocurrency assets with blank exchange', async () => {
    getAssetPriceFetchScopeMock.mockResolvedValue([{ brokerName: 'Coinbase', portfolioName: 'Cryptocurrency' }])
    getBrokersMock.mockResolvedValue([
      makeBroker(
        [
          {
            name: 'Cryptocurrency',
            assets: [{ name: 'Bitcoin', ticker: 'BTC', exchange: '', class: 'Cryptocurrency' }],
          },
        ],
        'Coinbase',
        'GBP',
      ),
    ])
    getCurrentPriceMock.mockResolvedValue({
      exchange: '',
      ticker: 'BTC',
      name: 'Bitcoin',
      price: 48000,
      asOf: '2024-02-01T00:00:00Z',
      asOfDate: null,
      isManual: false,
      marketStatus: 'Current',
      source: 'Unknown',
    } satisfies AssetPriceDto)

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check prices' }))

    await waitFor(() => expect(getCurrentPriceMock).toHaveBeenCalledTimes(1))
  })
})
