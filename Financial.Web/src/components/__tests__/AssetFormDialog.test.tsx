import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import AssetFormDialog from '../AssetFormDialog'
import type { AssetAdminDto, BrokerDto, PortfolioDto } from '../../api/types'
import { duplicateNameRejection, expectDuplicateUnderName, saveFromTheSaveButton } from '../../test-utils/duplicateName'

const ACTIVE_BROKERS: BrokerDto[] = [
  { name: 'XPI', currency: 'BRL', status: 'Active', portfolioCount: 1, costBasisMethod: 'AverageCost' },
  { name: 'Avenue', currency: 'USD', status: 'Active', portfolioCount: 1, costBasisMethod: 'AverageCost' },
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
  valuationMethod: 'Unspecified',
  incomePolicy: 'Unknown',
  quantity: 100,
}

describe('AssetFormDialog', () => {
  it('renders in create mode with the first active broker and its portfolios selected', () => {
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={vi.fn()} onSubmit={vi.fn()} />,
    )

    expect(screen.getByRole('heading', { name: 'New asset' })).toBeInTheDocument()
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

    expect(screen.getByRole('heading', { name: 'Edit asset' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('BCIA11')
    expect(screen.getByLabelText('Broker')).toHaveValue('XPI')
    expect(screen.getByLabelText('Broker')).toHaveAttribute('readonly')
    expect(screen.getByLabelText('Portfolio')).toHaveValue('Default')
    expect(screen.getByLabelText('Portfolio')).toHaveAttribute('readonly')
  })

  it('shows the ISIN format message after Save and does not submit', async () => {
    const onSubmit = vi.fn()
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={vi.fn()} onSubmit={onSubmit} />,
    )
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'NEWASSET' } })
    fireEvent.change(screen.getByLabelText(/^Portfolio/), { target: { value: 'Default' } })

    fireEvent.change(screen.getByLabelText('ISIN'), { target: { value: 'NOT-AN-ISIN' } })
    expect(screen.queryByText(/^ISIN must be/)).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Add asset' }))

    expect(await screen.findByText(/^ISIN must be/)).toBeInTheDocument()
    await waitFor(() => expect(screen.getByLabelText('ISIN')).toHaveFocus())
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('a blank ISIN is valid (optional field)', () => {
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={vi.fn()} onSubmit={vi.fn()} />,
    )
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'NEWASSET' } })
    fireEvent.change(screen.getByLabelText(/^Portfolio/), { target: { value: 'Default' } })

    expect(screen.getByRole('button', { name: 'Add asset' })).not.toBeDisabled()
  })

  it('submits the selected broker/portfolio and trimmed identity fields', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={vi.fn()} onSubmit={onSubmit} />,
    )

    fireEvent.change(screen.getByLabelText(/^Portfolio/), { target: { value: 'Default' } })
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  NEWASSET  ' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add asset' }))

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({ brokerName: 'XPI', portfolioName: 'Default', name: 'NEWASSET' }),
      ),
    )
  })

  it('shows a duplicate name under Name and moves focus there', async () => {
    const message = 'Portfolio "Default" already holds an asset named "BCIA11".'
    const onSubmit = vi.fn().mockRejectedValue(duplicateNameRejection(message))
    render(<AssetFormDialog asset={EXISTING_ASSET} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={vi.fn()} onSubmit={onSubmit} />)

    saveFromTheSaveButton()

    await expectDuplicateUnderName(message)
  })

  it('focuses Broker, the first field, when creating', async () => {
    render(<AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    await waitFor(() => expect(screen.getByLabelText(/^Broker/)).toHaveFocus())
  })

  it('drops a duplicate-name message once a different portfolio under the same broker is chosen', async () => {
    const message = 'Portfolio "Default" already holds an asset named "BCIA11".'
    const onSubmit = vi.fn().mockRejectedValue(duplicateNameRejection(message))
    const portfolios: PortfolioDto[] = [...PORTFOLIOS, { name: 'Growth', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 0 }]
    render(<AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={portfolios} onCancel={vi.fn()} onSubmit={onSubmit} />)
    fireEvent.change(screen.getByLabelText(/^Portfolio/), { target: { value: 'Default' } })
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'BCIA11' } })
    saveFromTheSaveButton('Add asset')
    await expectDuplicateUnderName(message)

    fireEvent.change(screen.getByLabelText(/^Portfolio/), { target: { value: 'Growth' } })

    expect(screen.queryByText(message)).not.toBeInTheDocument()
  })

  it('drops a duplicate-name message once a different broker is chosen', async () => {
    const message = 'Portfolio "Default" already holds an asset named "BCIA11".'
    const onSubmit = vi.fn().mockRejectedValue(duplicateNameRejection(message))
    render(<AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} onCancel={vi.fn()} onSubmit={onSubmit} />)
    fireEvent.change(screen.getByLabelText(/^Portfolio/), { target: { value: 'Default' } })
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'BCIA11' } })
    saveFromTheSaveButton('Add asset')
    await expectDuplicateUnderName(message)

    fireEvent.change(screen.getByLabelText(/^Broker/), { target: { value: 'Avenue' } })

    expect(screen.queryByText(message)).not.toBeInTheDocument()
  })

  it('focuses Name, not the disabled Portfolio, when Save fails for a broker with no portfolios', async () => {
    const onSubmit = vi.fn()
    render(<AssetFormDialog asset={null} activeBrokers={[ACTIVE_BROKERS[0]]} portfolios={[]} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.click(screen.getByRole('button', { name: 'Add asset' }))

    expect(await screen.findByText('A portfolio is required.')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByLabelText(/^Name/)).toHaveFocus())
    expect(onSubmit).not.toHaveBeenCalled()
  })
})
