import { fireEvent, render, screen, waitFor } from '../../test/renderWithFluent'
import { describe, expect, it, vi } from 'vitest'
import PortfolioFormDialog from '../PortfolioFormDialog'
import type { BrokerDto } from '../../api/types'
import { duplicateNameRejection, expectDuplicateUnderName, saveFromTheSaveButton } from '../../test-utils/duplicateName'
import { optionNames } from '../../test-utils/optionNames'

const ACTIVE_BROKERS: BrokerDto[] = [
  { name: 'XPI', currency: 'BRL', status: 'Active', portfolioCount: 1, costBasisMethod: 'AverageCost' },
  { name: 'Avenue', currency: 'USD', status: 'Active', portfolioCount: 0, costBasisMethod: 'AverageCost' },
]

describe('PortfolioFormDialog', () => {
  it('renders in create mode with an empty name and the first active broker selected', () => {
    render(<PortfolioFormDialog portfolio={null} activeBrokers={ACTIVE_BROKERS} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'New portfolio' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
    expect(screen.getByLabelText(/^Broker/)).toHaveValue('XPI')
  })

  it('lists only the given active brokers in the picker', () => {
    render(<PortfolioFormDialog portfolio={null} activeBrokers={ACTIVE_BROKERS} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(optionNames(screen.getByLabelText(/^Broker/))).toEqual(['XPI', 'Avenue'])
  })

  it('renders in edit mode pre-filled with a read-only broker and editable name', () => {
    render(
      <PortfolioFormDialog
        portfolio={{ name: 'Default', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 2, arcaBucket: null }}
        activeBrokers={ACTIVE_BROKERS}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit portfolio' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('Default')
    expect(screen.getByLabelText(/^Broker/)).toHaveValue('XPI')
    expect(screen.getByLabelText(/^Broker/)).toHaveAttribute('readonly')
  })

  it('submits the selected broker and trimmed name', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<PortfolioFormDialog portfolio={null} activeBrokers={ACTIVE_BROKERS} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Broker/), { target: { value: 'Avenue' } })
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  Growth  ' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add portfolio' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Avenue', 'Growth', null))
  })

  it('drops a duplicate-name message once a different broker is chosen', async () => {
    const message = 'Broker "XPI" already has a portfolio named "Default".'
    const onSubmit = vi.fn().mockRejectedValue(duplicateNameRejection(message))
    render(<PortfolioFormDialog portfolio={null} activeBrokers={ACTIVE_BROKERS} onCancel={vi.fn()} onSubmit={onSubmit} />)
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Default' } })
    saveFromTheSaveButton('Add portfolio')
    await expectDuplicateUnderName(message)

    fireEvent.change(screen.getByLabelText(/^Broker/), { target: { value: 'Avenue' } })

    expect(screen.queryByText(message)).not.toBeInTheDocument()
  })

  it('offers Not in ARCA and the five ARCA buckets and submits the chosen one', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(
      <PortfolioFormDialog
        portfolio={{ name: 'FII', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 2, arcaBucket: 'RealEstate' }}
        activeBrokers={ACTIVE_BROKERS}
        onCancel={vi.fn()}
        onSubmit={onSubmit}
      />,
    )
    const bucket = screen.getByRole('combobox', { name: /^ARCA bucket/ })
    expect(optionNames(bucket)).toEqual([
      'Not in ARCA',
      'A – Shares and businesses',
      'R – Real estate',
      'C – Cash and fixed income',
      'Pension',
      'Reserve fund',
    ])
    expect(bucket).toHaveValue('RealEstate')

    fireEvent.change(bucket, { target: { value: 'ReserveFund' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('XPI', 'FII', 'ReserveFund'))
  })

  it('keeps the chosen bucket and shows the server message when the save is refused', async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error('ARCA bucket must be one of Shares, RealEstate, Cash, Pension, ReserveFund or empty.'))
    render(<PortfolioFormDialog portfolio={null} activeBrokers={ACTIVE_BROKERS} onCancel={vi.fn()} onSubmit={onSubmit} />)
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'FII' } })
    fireEvent.change(screen.getByRole('combobox', { name: /^ARCA bucket/ }), { target: { value: 'Cash' } })

    fireEvent.click(screen.getByRole('button', { name: 'Add portfolio' }))

    expect(await screen.findByText('ARCA bucket must be one of Shares, RealEstate, Cash, Pension, ReserveFund or empty.')).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: /^ARCA bucket/ })).toHaveValue('Cash')
  })

  it('sends no bucket when Not in ARCA is chosen for a portfolio that had one', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(
      <PortfolioFormDialog
        portfolio={{ name: 'FII', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 2, arcaBucket: 'RealEstate' }}
        activeBrokers={ACTIVE_BROKERS}
        onCancel={vi.fn()}
        onSubmit={onSubmit}
      />,
    )

    fireEvent.change(screen.getByRole('combobox', { name: /^ARCA bucket/ }), { target: { value: '' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('XPI', 'FII', null))
  })
})
