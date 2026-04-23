import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AssetDetailPage from '../AssetDetailPage'

const getAssetDetailsMock = vi.fn()
const addOperationMock = vi.fn()
const updateOperationMock = vi.fn()
const deleteOperationMock = vi.fn()
const updateCreditMock = vi.fn()
const deleteCreditMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: () => ({
    getAssetDetails: getAssetDetailsMock,
    addOperation: addOperationMock,
    updateOperation: updateOperationMock,
    deleteOperation: deleteOperationMock,
    updateCredit: updateCreditMock,
    deleteCredit: deleteCreditMock,
  }),
}))

describe('AssetDetailPage', () => {
  beforeEach(() => {
    getAssetDetailsMock.mockReset()
    addOperationMock.mockReset()
    updateOperationMock.mockReset()
    deleteOperationMock.mockReset()
    updateCreditMock.mockReset()
    deleteCreditMock.mockReset()
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

    fireEvent.click(await screen.findByRole('tab', { name: 'Operations' }))
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

  it('updates an operation', async () => {
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
      operations: [
        {
          id: 'op-1',
          date: '2024-01-05T00:00:00',
          type: 'Buy',
          quantity: 1,
          unitPrice: 10,
          fees: 0,
          totalPrice: 10,
        },
      ],
      credits: [],
    })
    updateOperationMock.mockResolvedValue({
      name: 'BCIA11',
      brokerName: 'XPI',
      portfolioName: 'Default',
      ticker: 'BCIA11',
      isin: 'TEST',
      exchange: 'BVMF',
      country: 0,
      localTypeCode: '',
      class: 0,
      quantity: 11,
      averagePrice: 101,
      isActive: true,
      totalBought: 1010,
      totalSold: 0,
      totalCredits: 11,
      operations: [
        {
          id: 'op-1',
          date: '2024-01-05T00:00:00',
          type: 'Buy',
          quantity: 2,
          unitPrice: 12,
          fees: 1,
          totalPrice: 25,
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

    fireEvent.click(await screen.findByRole('tab', { name: 'Operations' }))
    fireEvent.click(screen.getByRole('button', { name: 'Edit' }))
    fireEvent.change(screen.getByLabelText('Quantity'), { target: { value: '2' } })
    fireEvent.change(screen.getByLabelText('Unit price'), { target: { value: '12' } })
    fireEvent.change(screen.getByLabelText('Fees'), { target: { value: '1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Update operation' }))

    await waitFor(() => {
      expect(updateOperationMock).toHaveBeenCalledWith({
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

  it('deletes an operation after confirmation', async () => {
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
      operations: [
        {
          id: 'op-2',
          date: '2024-01-06T00:00:00',
          type: 'Sell',
          quantity: 1,
          unitPrice: 15,
          fees: 0,
          totalPrice: 15,
        },
      ],
      credits: [],
    })
    deleteOperationMock.mockResolvedValue({
      name: 'BCIA11',
      brokerName: 'XPI',
      portfolioName: 'Default',
      ticker: 'BCIA11',
      isin: 'TEST',
      exchange: 'BVMF',
      country: 0,
      localTypeCode: '',
      class: 0,
      quantity: 9,
      averagePrice: 100,
      isActive: true,
      totalBought: 1000,
      totalSold: 0,
      totalCredits: 11,
      operations: [],
      credits: [],
    })
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)

    render(
      <MemoryRouter initialEntries={['/assets/XPI/Default/BCIA11']}>
        <Routes>
          <Route path="/assets/:brokerName/:portfolioName/:assetName" element={<AssetDetailPage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'BCIA11' })).toBeInTheDocument()

    fireEvent.click(await screen.findByRole('tab', { name: 'Operations' }))
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }))

    await waitFor(() => {
      expect(deleteOperationMock).toHaveBeenCalledWith({
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
      credits: [
        {
          id: 'cr-1',
          date: '2024-02-01T00:00:00',
          type: 'Dividend',
          value: 5,
        },
      ],
    })
    updateCreditMock.mockResolvedValue({
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
      totalCredits: 12,
      operations: [],
      credits: [
        {
          id: 'cr-1',
          date: '2024-02-01T00:00:00',
          type: 'Rent',
          value: 7,
        },
      ],
    })

    render(
      <MemoryRouter initialEntries={['/assets/XPI/Default/BCIA11']}>
        <Routes>
          <Route path="/assets/:brokerName/:portfolioName/:assetName" element={<AssetDetailPage />} />
        </Routes>
      </MemoryRouter>,
    )

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
      credits: [
        {
          id: 'cr-2',
          date: '2024-02-03T00:00:00',
          type: 'Dividend',
          value: 4,
        },
      ],
    })
    deleteCreditMock.mockResolvedValue({
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
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)

    render(
      <MemoryRouter initialEntries={['/assets/XPI/Default/BCIA11']}>
        <Routes>
          <Route path="/assets/:brokerName/:portfolioName/:assetName" element={<AssetDetailPage />} />
        </Routes>
      </MemoryRouter>,
    )

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
