import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CurrentValuesPage from '../CurrentValuesPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetPriceDto, BrokerNodeDto } from '../../api/types'

const getBrokersMock = vi.fn()
const getCurrentPriceMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: () => ({
    getBrokers: getBrokersMock,
    getCurrentPrice: getCurrentPriceMock,
  } satisfies Partial<FinancialApiClient>),
}))

const makeBroker = (portfolios: { name: string; assets: Partial<BrokerNodeDto['portfolios'][0]['assets'][0]>[] }[]): BrokerNodeDto => ({
  name: 'XPI',
  currency: 'BRL',
  portfolioCount: portfolios.length,
  totalAssets: portfolios.reduce((sum, p) => sum + p.assets.length, 0),
  portfolios: portfolios.map((p) => ({
    name: p.name,
    assetCount: p.assets.length,
    activeAssetCount: p.assets.filter((a) => a.isActive !== false).length,
    assets: p.assets.map((a) => ({
      name: a.name ?? 'ASSET',
      ticker: a.ticker ?? 'TICK',
      exchange: a.exchange ?? 'BVMF',
      country: 'Brazil',
      localTypeCode: 'FII',
      class: 'RealEstateFund',
      isin: 'BR000',
      quantity: 10,
      averagePrice: 100,
      isActive: a.isActive ?? true,
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
})

describe('CurrentValuesPage', () => {
  beforeEach(() => {
    getBrokersMock.mockReset()
    getCurrentPriceMock.mockReset()
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
    fireEvent.click(await screen.findByRole('button', { name: 'Check Prices' }))

    await waitFor(() => {
      expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'BCIA11')
      expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'KLBN4')
      expect(getCurrentPriceMock).not.toHaveBeenCalledWith('BVMF', 'XXXX3')
    })
  })

  it('does not render broker or portfolio filter controls', async () => {
    getBrokersMock.mockResolvedValue([makeBroker([{ name: 'Default', assets: [] }])])

    render(<CurrentValuesPage />)
    await screen.findByRole('button', { name: 'Check Prices' })

    expect(screen.queryByRole('combobox')).not.toBeInTheDocument()
  })

  it('results table has Ticker, Name, Price columns and no As of column', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([{ name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] }]),
    ])
    getCurrentPriceMock.mockResolvedValue(makePrice('BCIA11'))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check Prices' }))

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
    fireEvent.click(await screen.findByRole('button', { name: 'Check Prices' }))

    expect(await screen.findByText('85.50')).toBeInTheDocument()
  })

  it('shows progress bar and initial progress text while fetching', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([{ name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] }]),
    ])
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check Prices' }))

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
    fireEvent.click(await screen.findByRole('button', { name: 'Check Prices' }))

    await waitFor(() =>
      expect(screen.getByText('Fetching 1 of 2: BCIA11...')).toBeInTheDocument(),
    )
  })

  it('shows completion text after all fetches finish', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([{ name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] }]),
    ])
    getCurrentPriceMock.mockResolvedValue(makePrice('BCIA11'))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check Prices' }))

    expect(await screen.findByText('Completed! Loaded 1 assets.')).toBeInTheDocument()
    expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
  })

  it('Check Prices button is disabled while running', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([{ name: 'Default', assets: [{ name: 'BCIA11', ticker: 'BCIA11' }] }]),
    ])
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check Prices' }))

    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Checking...' })).toBeDisabled(),
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
    fireEvent.click(await screen.findByRole('button', { name: 'Check Prices' }))

    await waitFor(() => expect(screen.queryByText(/Completed!/)).toBeInTheDocument())

    const cells = screen.getAllByRole('cell')
    const priceCells = cells.filter((_, i) => i % 3 === 2)
    expect(priceCells[0]).toHaveTextContent('—')
    expect(priceCells[1]).toHaveTextContent('20.00')
  })

  it('shows error state with Retry when broker tree fails to load', async () => {
    getBrokersMock.mockRejectedValue(new Error('Network error'))

    render(<CurrentValuesPage />)

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Check Prices' })).not.toBeInTheDocument()
  })

  it('retries broker tree load on Retry click', async () => {
    getBrokersMock
      .mockRejectedValueOnce(new Error('Network error'))
      .mockResolvedValueOnce([makeBroker([{ name: 'Default', assets: [] }])])

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('button', { name: 'Check Prices' })).toBeInTheDocument()
  })

  it('excludes inactive assets from fetch scope', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([
        {
          name: 'Default',
          assets: [
            { name: 'BCIA11', ticker: 'BCIA11', isActive: true },
            { name: 'INAC11', ticker: 'INAC11', isActive: false },
          ],
        },
      ]),
    ])
    getCurrentPriceMock.mockResolvedValue(makePrice('BCIA11'))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check Prices' }))

    await waitFor(() => expect(screen.queryByText(/Completed!/)).toBeInTheDocument())

    expect(getCurrentPriceMock).toHaveBeenCalledTimes(1)
    expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'BCIA11')
    expect(getCurrentPriceMock).not.toHaveBeenCalledWith('BVMF', 'INAC11')
  })

  it('excludes assets with empty ticker or exchange', async () => {
    getBrokersMock.mockResolvedValue([
      makeBroker([
        {
          name: 'Default',
          assets: [
            { name: 'NoTicker', ticker: '', exchange: 'BVMF' },
            { name: 'BCIA11', ticker: 'BCIA11', exchange: 'BVMF' },
          ],
        },
      ]),
    ])
    getCurrentPriceMock.mockResolvedValue(makePrice('BCIA11'))

    render(<CurrentValuesPage />)
    fireEvent.click(await screen.findByRole('button', { name: 'Check Prices' }))

    await waitFor(() => expect(screen.queryByText(/Completed!/)).toBeInTheDocument())

    expect(getCurrentPriceMock).toHaveBeenCalledTimes(1)
    expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'BCIA11')
  })
})
