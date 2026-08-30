import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AssetsPage from '../AssetsPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetAdminDto, BrokerDto, PortfolioDto } from '../../api/types'

const {
  getAdminAssetsMock,
  createAssetMock,
  updateAssetMock,
  archiveAssetMock,
  getAdminBrokersMock,
  getAdminPortfoliosMock,
} = vi.hoisted(() => ({
  getAdminAssetsMock: vi.fn<FinancialApiClient['getAdminAssets']>(),
  createAssetMock: vi.fn<FinancialApiClient['createAsset']>(),
  updateAssetMock: vi.fn<FinancialApiClient['updateAsset']>(),
  archiveAssetMock: vi.fn<FinancialApiClient['archiveAsset']>(),
  getAdminBrokersMock: vi.fn<FinancialApiClient['getAdminBrokers']>(),
  getAdminPortfoliosMock: vi.fn<FinancialApiClient['getAdminPortfolios']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAdminAssets: getAdminAssetsMock,
    createAsset: createAssetMock,
    updateAsset: updateAssetMock,
    archiveAsset: archiveAssetMock,
    getAdminBrokers: getAdminBrokersMock,
    getAdminPortfolios: getAdminPortfoliosMock,
  } as Partial<FinancialApiClient>,
}))

const ASSETS: AssetAdminDto[] = [
  {
    name: 'BCIA11',
    brokerName: 'XPI',
    portfolioName: 'Default',
    brokerStatus: 'Active',
    isin: 'BR0000000001',
    exchange: 'BVMF',
    ticker: 'BCIA11T',
    country: 'BR',
    localTypeCode: 'FII',
    class: 'RealEstate',
    quantity: 100,
  },
  {
    name: 'CLOSEDASSET',
    brokerName: 'XPI',
    portfolioName: 'Uncategorized',
    brokerStatus: 'Historic',
    isin: '',
    exchange: '',
    ticker: 'CLOSEDT',
    country: 'Unknown',
    localTypeCode: '',
    class: 'Unknown',
    quantity: 0,
  },
]

const BROKERS: BrokerDto[] = [{ name: 'XPI', currency: 'BRL', status: 'Active', portfolioCount: 1 }]
const PORTFOLIOS: PortfolioDto[] = [{ name: 'Default', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 1 }]

describe('AssetsPage', () => {
  beforeEach(() => {
    getAdminAssetsMock.mockReset()
    createAssetMock.mockReset()
    updateAssetMock.mockReset()
    archiveAssetMock.mockReset()
    getAdminBrokersMock.mockReset()
    getAdminPortfoliosMock.mockReset()
    getAdminAssetsMock.mockResolvedValue(ASSETS)
    getAdminBrokersMock.mockResolvedValue(BROKERS)
    getAdminPortfoliosMock.mockResolvedValue(PORTFOLIOS)
  })

  it('renders the asset list once loaded', async () => {
    render(<AssetsPage />)

    await waitFor(() => expect(screen.getByText('BCIA11')).toBeInTheDocument())
    expect(screen.getByText('CLOSEDASSET')).toBeInTheDocument()
  })

  it('shows the empty state when there are no assets', async () => {
    getAdminAssetsMock.mockResolvedValue([])
    render(<AssetsPage />)

    expect(await screen.findByText('No assets yet — create one to get started.')).toBeInTheDocument()
  })

  it('shows an error state with retry on load failure', async () => {
    getAdminAssetsMock.mockRejectedValue(new Error('Network down'))
    render(<AssetsPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
  })

  it('creates an asset through the Create Asset dialog', async () => {
    createAssetMock.mockResolvedValue({ ...ASSETS[0], name: 'NEWASSET', quantity: 0 })
    render(<AssetsPage />)
    await waitFor(() => expect(screen.getByText('BCIA11')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Create Asset' }))
    fireEvent.change(screen.getByLabelText(/^Portfolio/), { target: { value: 'Default' } })
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'NEWASSET' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(createAssetMock).toHaveBeenCalledWith(
        expect.objectContaining({ brokerName: 'XPI', portfolioName: 'Default', name: 'NEWASSET' }),
      ),
    )
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Create Asset' })).not.toBeInTheDocument())
  })

  it('edits an asset through its row action', async () => {
    updateAssetMock.mockResolvedValue({ ...ASSETS[0], name: 'BCIA11B' })
    render(<AssetsPage />)
    await waitFor(() => expect(screen.getByText('BCIA11')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit BCIA11' }))
    expect(screen.getByRole('heading', { name: 'Edit Asset' })).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'BCIA11B' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(updateAssetMock).toHaveBeenCalledWith('XPI', 'Default', 'BCIA11', expect.objectContaining({ name: 'BCIA11B' })))
  })

  it('disables delete confirmation when the asset still holds a position', async () => {
    render(<AssetsPage />)
    await waitFor(() => expect(screen.getByText('BCIA11')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete BCIA11' }))

    expect(screen.getByText(/still holds a position of 100 and cannot be deleted/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    expect(confirmButtons[confirmButtons.length - 1]).toBeDisabled()
  })

  it('deletes (archives) an asset with zero quantity', async () => {
    archiveAssetMock.mockResolvedValue({} as never)
    render(<AssetsPage />)
    await waitFor(() => expect(screen.getByText('CLOSEDASSET')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete CLOSEDASSET' }))

    expect(screen.getByText(/will be archived into Historic Investments/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    fireEvent.click(confirmButtons[confirmButtons.length - 1])

    await waitFor(() =>
      expect(archiveAssetMock).toHaveBeenCalledWith({
        brokerName: 'XPI',
        sourcePortfolioName: 'Uncategorized',
        assetName: 'CLOSEDASSET',
        destinationPortfolioName: 'Uncategorized',
      }),
    )
  })
})
