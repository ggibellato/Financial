import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import AssetFormDialog from '../AssetFormDialog'
import type { AssetAdminDto, BrokerDto, PortfolioDto } from '../../api/types'

const ACTIVE_BROKERS: BrokerDto[] = [
  { name: 'XPI', currency: 'BRL', status: 'Active', portfolioCount: 1 },
  { name: 'Avenue', currency: 'USD', status: 'Active', portfolioCount: 1 },
]

const PORTFOLIOS: PortfolioDto[] = [
  { name: 'Default', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 1 },
  { name: 'ISA', brokerName: 'Avenue', brokerStatus: 'Active', assetCount: 0 },
]

const EXISTING_ASSET: AssetAdminDto = {
  name: 'BCIA11',
  brokerName: 'XPI',
  portfolioName: 'Default',
  brokerStatus: 'Active',
  isin: 'BR0000000001',
  exchange: 'BVMF',
  ticker: 'BCIA11',
  country: 'BR',
  localTypeCode: 'FII',
  class: 'RealEstate',
  quantity: 100,
}

describe('AssetFormDialog', () => {
  it('renders in create mode with the first active broker and its portfolios selected', () => {
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={vi.fn()} onSubmit={vi.fn()} />,
    )

    expect(screen.getByRole('heading', { name: 'Create Asset' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
    expect(screen.getByLabelText(/^Broker/)).toHaveValue('XPI')
    const portfolioOptions = screen.getByLabelText(/^Portfolio/).querySelectorAll('option')
    expect(Array.from(portfolioOptions).map((o) => o.textContent)).toEqual(['Select a portfolio', 'Default'])
  })

  it('changing the broker resets and rescopes the portfolio picker', () => {
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={vi.fn()} onSubmit={vi.fn()} />,
    )

    fireEvent.change(screen.getByLabelText(/^Broker/), { target: { value: 'Avenue' } })

    const portfolioOptions = screen.getByLabelText(/^Portfolio/).querySelectorAll('option')
    expect(Array.from(portfolioOptions).map((o) => o.textContent)).toEqual(['Select a portfolio', 'ISA'])
    expect(screen.getByLabelText(/^Portfolio/)).toHaveValue('')
  })

  it('renders in edit mode with read-only broker/portfolio and editable identity fields', () => {
    render(
      <AssetFormDialog
        asset={EXISTING_ASSET}
        activeBrokers={ACTIVE_BROKERS}
        portfolios={PORTFOLIOS}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit Asset' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('BCIA11')
    expect(screen.getByLabelText('Broker')).toHaveValue('XPI')
    expect(screen.getByLabelText('Broker')).toBeDisabled()
    expect(screen.getByLabelText('Portfolio')).toHaveValue('Default')
    expect(screen.getByLabelText('Portfolio')).toBeDisabled()
  })

  it('disables Save and shows a validation message when the name is blank', () => {
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={vi.fn()} onSubmit={vi.fn()} />,
    )
    fireEvent.change(screen.getByLabelText(/^Portfolio/), { target: { value: 'Default' } })

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '   ' } })

    expect(screen.getByText('Name is required.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('disables Save and shows a validation message for an invalid ISIN format', () => {
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={vi.fn()} onSubmit={vi.fn()} />,
    )
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'NEWASSET' } })
    fireEvent.change(screen.getByLabelText(/^Portfolio/), { target: { value: 'Default' } })

    fireEvent.change(screen.getByLabelText('ISIN'), { target: { value: 'NOT-AN-ISIN' } })

    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('a blank ISIN is valid (optional field)', () => {
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={vi.fn()} onSubmit={vi.fn()} />,
    )
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'NEWASSET' } })
    fireEvent.change(screen.getByLabelText(/^Portfolio/), { target: { value: 'Default' } })

    expect(screen.getByRole('button', { name: 'Save' })).not.toBeDisabled()
  })

  it('submits the selected broker/portfolio and trimmed identity fields', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={vi.fn()} onSubmit={onSubmit} />,
    )

    fireEvent.change(screen.getByLabelText(/^Portfolio/), { target: { value: 'Default' } })
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  NEWASSET  ' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({ brokerName: 'XPI', portfolioName: 'Default', name: 'NEWASSET' }),
      ),
    )
  })

  it('shows a server error and re-enables Save when the submit rejects', async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error('Portfolio "Default" already has an asset named "BCIA11".'))
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={vi.fn()} onSubmit={onSubmit} />,
    )

    fireEvent.change(screen.getByLabelText(/^Portfolio/), { target: { value: 'Default' } })
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'BCIA11' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Portfolio "Default" already has an asset named "BCIA11".')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).not.toBeDisabled()
  })

  it('calls onCancel when Cancel is clicked', () => {
    const onCancel = vi.fn()
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={onCancel} onSubmit={vi.fn()} />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
  })
})
