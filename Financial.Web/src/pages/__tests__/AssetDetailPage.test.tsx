import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AssetDetailPage from '../AssetDetailPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetDetailsDto } from '../../api/types'

const getAssetDetailsMock = vi.fn()
const addTransactionMock = vi.fn()
const updateTransactionMock = vi.fn()
const deleteTransactionMock = vi.fn()
const addCreditMock = vi.fn()
const updateCreditMock = vi.fn()
const deleteCreditMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): FinancialApiClient => ({
    getNavigationTree: vi.fn(),
    getBrokers: vi.fn(),
    getAssetDetails: getAssetDetailsMock,
    getCreditsByBroker: vi.fn(),
    getCreditsByPortfolio: vi.fn(),
    addTransaction: addTransactionMock,
    updateTransaction: updateTransactionMock,
    deleteTransaction: deleteTransactionMock,
    addCredit: addCreditMock,
    updateCredit: updateCreditMock,
    deleteCredit: deleteCreditMock,
    getDividendHistory: vi.fn(),
    getDividendSummary: vi.fn(),
    getCurrentPrice: vi.fn(),
  }),
}))

const baseAsset: AssetDetailsDto = {
  name: 'BCIA11',
  brokerName: 'XPI',
  portfolioName: 'Default',
  ticker: 'BCIA11',
  isin: 'TEST',
  exchange: 'BVMF',
  country: 0,
  localTypeCode: '',
  class: 0,
  quantity: 10,
  averagePrice: 100,
  isActive: true,
  totalBought: 1000,
  totalSold: 0,
  totalCredits: 11,
  transactions: [],
  credits: [],
}

const renderAssetDetail = () =>
  render(
    <MemoryRouter initialEntries={['/assets/XPI/Default/BCIA11']}>
      <Routes>
        <Route path="/assets/:brokerName/:portfolioName/:assetName" element={<AssetDetailPage />} />
      </Routes>
    </MemoryRouter>,
  )

describe('AssetDetailPage', () => {
  beforeEach(() => {
    getAssetDetailsMock.mockReset()
    addTransactionMock.mockReset()
    updateTransactionMock.mockReset()
    deleteTransactionMock.mockReset()
    addCreditMock.mockReset()
    updateCreditMock.mockReset()
    deleteCreditMock.mockReset()
  })

  it('renders asset details', async () => {
    getAssetDetailsMock.mockResolvedValue(baseAsset)

    renderAssetDetail()

    expect(await screen.findByRole('heading', { name: 'BCIA11' })).toBeInTheDocument()
    expect(screen.getByText(/Ticker/)).toBeInTheDocument()
  })

  it('renders credit chart controls', async () => {
    getAssetDetailsMock.mockResolvedValue({
      ...baseAsset,
      credits: [{ id: 'cr-0', date: '2024-02-01T00:00:00', type: 'Dividend', value: 5 }],
    } satisfies AssetDetailsDto)

    renderAssetDetail()

    expect(await screen.findByRole('heading', { name: 'BCIA11' })).toBeInTheDocument()

    fireEvent.click(await screen.findByRole('tab', { name: 'Credits' }))
    expect(screen.getByText('View:')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Stacked' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Grouped' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Last year' })).toBeInTheDocument()
  })

  it('submits a new transaction', async () => {
    getAssetDetailsMock.mockResolvedValue(baseAsset)
    addTransactionMock.mockResolvedValue({
      ...baseAsset,
      quantity: 12,
      averagePrice: 102,
      totalBought: 1200,
      transactions: [{ id: 'new-op', date: '2024-01-02T00:00:00', type: 'Buy', quantity: 2, unitPrice: 10, fees: 1, totalPrice: 21 }],
    } satisfies AssetDetailsDto)

    renderAssetDetail()

    expect(await screen.findByRole('heading', { name: 'BCIA11' })).toBeInTheDocument()

    fireEvent.click(await screen.findByRole('tab', { name: 'Transactions' }))
    fireEvent.change(screen.getByLabelText('Date'), { target: { value: '2024-01-02' } })
    fireEvent.change(screen.getByLabelText('Type'), { target: { value: 'Buy' } })
    fireEvent.change(screen.getByLabelText('Quantity'), { target: { value: '2' } })
    fireEvent.change(screen.getByLabelText('Unit price'), { target: { value: '10' } })
    fireEvent.change(screen.getByLabelText('Fees'), { target: { value: '1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add transaction' }))

    await waitFor(() => {
      expect(addTransactionMock).toHaveBeenCalledWith({
        brokerName: 'XPI',
        portfolioName: 'Default',
        assetName: 'BCIA11',
        date: '2024-01-02T00:00:00',
        type: 'Buy',
        quantity: 2,
        unitPrice: 10,
        fees: 1,
      })
    })
  })

  it('updates a transaction', async () => {
    getAssetDetailsMock.mockResolvedValue({
      ...baseAsset,
      transactions: [{ id: 'op-1', date: '2024-01-05T00:00:00', type: 'Buy', quantity: 1, unitPrice: 10, fees: 0, totalPrice: 10 }],
    } satisfies AssetDetailsDto)
    updateTransactionMock.mockResolvedValue({
      ...baseAsset,
      quantity: 11,
      averagePrice: 101,
      totalBought: 1010,
      transactions: [{ id: 'op-1', date: '2024-01-05T00:00:00', type: 'Buy', quantity: 2, unitPrice: 12, fees: 1, totalPrice: 25 }],
    } satisfies AssetDetailsDto)

    renderAssetDetail()

    expect(await screen.findByRole('heading', { name: 'BCIA11' })).toBeInTheDocument()

    fireEvent.click(await screen.findByRole('tab', { name: 'Transactions' }))
    fireEvent.click(screen.getByRole('button', { name: 'Edit' }))
    fireEvent.change(screen.getByLabelText('Quantity'), { target: { value: '2' } })
    fireEvent.change(screen.getByLabelText('Unit price'), { target: { value: '12' } })
    fireEvent.change(screen.getByLabelText('Fees'), { target: { value: '1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Update transaction' }))

    await waitFor(() => {
      expect(updateTransactionMock).toHaveBeenCalledWith({
        brokerName: 'XPI',
        portfolioName: 'Default',
        assetName: 'BCIA11',
        id: 'op-1',
        date: '2024-01-05T00:00:00',
        type: 'Buy',
        quantity: 2,
        unitPrice: 12,
        fees: 1,
      })
    })
  })

  it('deletes a transaction after confirmation', async () => {
    getAssetDetailsMock.mockResolvedValue({
      ...baseAsset,
      transactions: [{ id: 'op-2', date: '2024-01-06T00:00:00', type: 'Sell', quantity: 1, unitPrice: 15, fees: 0, totalPrice: 15 }],
    } satisfies AssetDetailsDto)
    deleteTransactionMock.mockResolvedValue({ ...baseAsset, quantity: 9 } satisfies AssetDetailsDto)
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)

    renderAssetDetail()

    expect(await screen.findByRole('heading', { name: 'BCIA11' })).toBeInTheDocument()

    fireEvent.click(await screen.findByRole('tab', { name: 'Transactions' }))
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }))

    await waitFor(() => {
      expect(deleteTransactionMock).toHaveBeenCalledWith({
        brokerName: 'XPI',
        portfolioName: 'Default',
        assetName: 'BCIA11',
        id: 'op-2',
      })
    })

    confirmSpy.mockRestore()
  })

  it('updates a credit', async () => {
    getAssetDetailsMock.mockResolvedValue({
      ...baseAsset,
      credits: [{ id: 'cr-1', date: '2024-02-01T00:00:00', type: 'Dividend', value: 5 }],
    } satisfies AssetDetailsDto)
    updateCreditMock.mockResolvedValue({
      ...baseAsset,
      totalCredits: 12,
      credits: [{ id: 'cr-1', date: '2024-02-01T00:00:00', type: 'Rent', value: 7 }],
    } satisfies AssetDetailsDto)

    renderAssetDetail()

    expect(await screen.findByRole('heading', { name: 'BCIA11' })).toBeInTheDocument()

    fireEvent.click(await screen.findByRole('tab', { name: 'Credits' }))
    fireEvent.click(screen.getByRole('button', { name: 'Edit' }))
    const creditForm = screen.getByRole('form', { name: 'Edit credit' })
    fireEvent.change(within(creditForm).getByLabelText('Type'), { target: { value: 'Rent' } })
    fireEvent.change(within(creditForm).getByLabelText('Value'), { target: { value: '7' } })
    fireEvent.click(screen.getByRole('button', { name: 'Update credit' }))

    await waitFor(() => {
      expect(updateCreditMock).toHaveBeenCalledWith({
        brokerName: 'XPI',
        portfolioName: 'Default',
        assetName: 'BCIA11',
        id: 'cr-1',
        date: '2024-02-01T00:00:00',
        type: 'Rent',
        value: 7,
      })
    })
  })

  it('deletes a credit after confirmation', async () => {
    getAssetDetailsMock.mockResolvedValue({
      ...baseAsset,
      credits: [{ id: 'cr-2', date: '2024-02-03T00:00:00', type: 'Dividend', value: 4 }],
    } satisfies AssetDetailsDto)
    deleteCreditMock.mockResolvedValue(baseAsset)
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)

    renderAssetDetail()

    expect(await screen.findByRole('heading', { name: 'BCIA11' })).toBeInTheDocument()

    fireEvent.click(await screen.findByRole('tab', { name: 'Credits' }))
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }))

    await waitFor(() => {
      expect(deleteCreditMock).toHaveBeenCalledWith({
        brokerName: 'XPI',
        portfolioName: 'Default',
        assetName: 'BCIA11',
        id: 'cr-2',
      })
    })

    confirmSpy.mockRestore()
  })

  it('shows error state when request fails', async () => {
    getAssetDetailsMock.mockRejectedValue(new Error('Boom'))

    renderAssetDetail()

    expect(await screen.findByRole('alert')).toHaveTextContent('Boom')
  })
})
