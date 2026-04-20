import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AssetDetailPage from '../AssetDetailPage'

const getAssetDetailsMock = vi.fn()
const addOperationMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: () => ({
    getAssetDetails: getAssetDetailsMock,
    addOperation: addOperationMock,
  }),
}))

describe('AssetDetailPage', () => {
  beforeEach(() => {
    getAssetDetailsMock.mockReset()
    addOperationMock.mockReset()
  })

  it('renders asset details', async () => {
    getAssetDetailsMock.mockResolvedValue({
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
      operations: [],
      credits: [],
    })

    render(
      <MemoryRouter initialEntries={['/assets/XPI/Default/BCIA11']}>
        <Routes>
          <Route path="/assets/:brokerName/:portfolioName/:assetName" element={<AssetDetailPage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'BCIA11' })).toBeInTheDocument()
    expect(screen.getByText(/Ticker/)).toBeInTheDocument()
  })

  it('submits a new operation', async () => {
    getAssetDetailsMock.mockResolvedValue({
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
      operations: [],
      credits: [],
    })
    addOperationMock.mockResolvedValue({
      name: 'BCIA11',
      brokerName: 'XPI',
      portfolioName: 'Default',
      ticker: 'BCIA11',
      isin: 'TEST',
      exchange: 'BVMF',
      country: 0,
      localTypeCode: '',
      class: 0,
      quantity: 12,
      averagePrice: 102,
      isActive: true,
      totalBought: 1200,
      totalSold: 0,
      totalCredits: 11,
      operations: [
        {
          id: 'new-op',
          date: '2024-01-02T00:00:00',
          type: 'Buy',
          quantity: 2,
          unitPrice: 10,
          fees: 1,
          totalPrice: 21,
        },
      ],
      credits: [],
    })

    render(
      <MemoryRouter initialEntries={['/assets/XPI/Default/BCIA11']}>
        <Routes>
          <Route path="/assets/:brokerName/:portfolioName/:assetName" element={<AssetDetailPage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'BCIA11' })).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Date'), { target: { value: '2024-01-02' } })
    fireEvent.change(screen.getByLabelText('Type'), { target: { value: 'Buy' } })
    fireEvent.change(screen.getByLabelText('Quantity'), { target: { value: '2' } })
    fireEvent.change(screen.getByLabelText('Unit price'), { target: { value: '10' } })
    fireEvent.change(screen.getByLabelText('Fees'), { target: { value: '1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add operation' }))

    await waitFor(() => {
      expect(addOperationMock).toHaveBeenCalledWith({
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

  it('shows error state when request fails', async () => {
    getAssetDetailsMock.mockRejectedValue(new Error('Boom'))

    render(
      <MemoryRouter initialEntries={['/assets/XPI/Default/BCIA11']}>
        <Routes>
          <Route path="/assets/:brokerName/:portfolioName/:assetName" element={<AssetDetailPage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('alert')).toHaveTextContent('Boom')
  })
})
