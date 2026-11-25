import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import AssetFormDialog from '../AssetFormDialog'
import type { AssetAdminDto, BrokerDto, PortfolioDto } from '../../api/types'
import { duplicateNameRejection, expectDuplicateUnderName, saveFromTheSaveButton } from '../../test-utils/duplicateName'
import { optionNames } from '../../test-utils/optionNames'

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
  segment: 'Logistics',
  pickSource: null,
}

const SEGMENTS = ['Logistics', 'Paper']

const segmentOptions = () =>
  within(screen.getByRole('listbox')).getAllByRole('option').map((option) => option.textContent)

function renderEditing(asset: Partial<AssetAdminDto> = {}, onSubmit = vi.fn().mockResolvedValue(undefined)) {
  render(
    <AssetFormDialog
      asset={{ ...EXISTING_ASSET, ...asset }}
      activeBrokers={ACTIVE_BROKERS}
      portfolios={PORTFOLIOS}
      segments={SEGMENTS}
      onCancel={vi.fn()}
      onSubmit={onSubmit}
    />,
  )
  return onSubmit
}

describe('AssetFormDialog', () => {
  it('renders in create mode with the first active broker and its portfolios selected', () => {
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} segments={SEGMENTS} onCancel={vi.fn()} onSubmit={vi.fn()} />,
    )

    expect(screen.getByRole('heading', { name: 'New asset' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
    expect(screen.getByLabelText(/^Broker/)).toHaveValue('XPI')
    expect(optionNames(screen.getByLabelText(/^Portfolio/))).toEqual(['Select a portfolio', 'Default'])
  })

  it('changing the broker resets and rescopes the portfolio picker', () => {
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} segments={SEGMENTS} onCancel={vi.fn()} onSubmit={vi.fn()} />,
    )

    fireEvent.change(screen.getByLabelText(/^Broker/), { target: { value: 'Avenue' } })

    expect(optionNames(screen.getByLabelText(/^Portfolio/))).toEqual(['Select a portfolio', 'ISA'])
    expect(screen.getByLabelText(/^Portfolio/)).toHaveValue('')
  })

  it('renders in edit mode with read-only broker/portfolio and editable identity fields', () => {
    render(
      <AssetFormDialog
        asset={EXISTING_ASSET}
        activeBrokers={ACTIVE_BROKERS}
        portfolios={PORTFOLIOS} segments={SEGMENTS}
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
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} segments={SEGMENTS} onCancel={vi.fn()} onSubmit={onSubmit} />,
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
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} segments={SEGMENTS} onCancel={vi.fn()} onSubmit={vi.fn()} />,
    )
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'NEWASSET' } })
    fireEvent.change(screen.getByLabelText(/^Portfolio/), { target: { value: 'Default' } })

    expect(screen.getByRole('button', { name: 'Add asset' })).not.toBeDisabled()
  })

  it('submits the selected broker/portfolio and trimmed identity fields', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(
      <AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} segments={SEGMENTS} onCancel={vi.fn()} onSubmit={onSubmit} />,
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

  it('focuses Broker, the first field, when creating', async () => {
    render(<AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} segments={SEGMENTS} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    await waitFor(() => expect(screen.getByLabelText(/^Broker/)).toHaveFocus())
  })

  it('drops a duplicate-name message once a different portfolio under the same broker is chosen', async () => {
    const message = 'Portfolio "Default" already holds an asset named "BCIA11".'
    const onSubmit = vi.fn().mockRejectedValue(duplicateNameRejection(message))
    const portfolios: PortfolioDto[] = [...PORTFOLIOS, { name: 'Growth', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 0 }]
    render(<AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={portfolios} segments={SEGMENTS} onCancel={vi.fn()} onSubmit={onSubmit} />)
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
    render(<AssetFormDialog asset={null} activeBrokers={ACTIVE_BROKERS} portfolios={PORTFOLIOS} segments={SEGMENTS} onCancel={vi.fn()} onSubmit={onSubmit} />)
    fireEvent.change(screen.getByLabelText(/^Portfolio/), { target: { value: 'Default' } })
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'BCIA11' } })
    saveFromTheSaveButton('Add asset')
    await expectDuplicateUnderName(message)

    fireEvent.change(screen.getByLabelText(/^Broker/), { target: { value: 'Avenue' } })

    expect(screen.queryByText(message)).not.toBeInTheDocument()
  })

  it('focuses Name, not the disabled Portfolio, when Save fails for a broker with no portfolios', async () => {
    const onSubmit = vi.fn()
    render(<AssetFormDialog asset={null} activeBrokers={[ACTIVE_BROKERS[0]]} portfolios={[]} segments={SEGMENTS} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.click(screen.getByRole('button', { name: 'Add asset' }))

    expect(await screen.findByText('A portfolio is required.')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByLabelText(/^Name/)).toHaveFocus())
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('shows Segment only for Equity and RealEstate', () => {
    renderEditing()
    expect(screen.getByRole('combobox', { name: 'Segment' })).toHaveValue('Logistics')

    fireEvent.change(screen.getByLabelText('Class'), { target: { value: 'Equity' } })
    expect(screen.getByRole('combobox', { name: 'Segment' })).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Class'), { target: { value: 'Bond' } })
    expect(screen.queryByRole('combobox', { name: 'Segment' })).not.toBeInTheDocument()
  })

  it('lists the segments already used and keeps a new typed segment', async () => {
    const onSubmit = renderEditing({ segment: null })
    const segment = screen.getByRole('combobox', { name: 'Segment' })

    await userEvent.click(segment)
    expect(segmentOptions()).toEqual(['Logistics', 'Paper'])

    await userEvent.type(segment, '  Hybrid  ')
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ segment: 'Hybrid' })))
  })

  it('filters the segment suggestions by the typed text', async () => {
    renderEditing({ segment: null })

    await userEvent.type(screen.getByRole('combobox', { name: 'Segment' }), 'pap')

    expect(segmentOptions()).toEqual(['Paper'])
  })

  it('refuses a segment over 40 characters and moves focus to Segment', async () => {
    const onSubmit = renderEditing()
    const segment = screen.getByRole('combobox', { name: 'Segment' })
    fireEvent.change(segment, { target: { value: 'a'.repeat(41) } })

    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Segment must be 40 characters or fewer.')).toBeInTheDocument()
    await waitFor(() => expect(segment).toHaveFocus())
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('warns that the segment will be removed when Class changes to one without segments', () => {
    renderEditing()

    fireEvent.change(screen.getByLabelText(/^Class/), { target: { value: 'ETF' } })

    expect(screen.getByText('The segment will be removed when you save')).toBeInTheDocument()
  })

  it('offers Not set, Recommended and My choice as pick source and submits the choice', async () => {
    const onSubmit = renderEditing()
    const pickSource = screen.getByRole('combobox', { name: /^Pick source/ })
    expect(optionNames(pickSource)).toEqual(['Not set', 'Recommended', 'My choice'])
    expect(pickSource).toHaveValue('')

    fireEvent.change(pickSource, { target: { value: 'MyChoice' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ pickSource: 'MyChoice' })))
  })

  it('keeps the entered values and shows the server message when the save is refused', async () => {
    renderEditing({}, vi.fn().mockRejectedValue(new Error('Pick source must be Recommended or MyChoice.')))
    fireEvent.change(screen.getByRole('combobox', { name: /^Pick source/ }), { target: { value: 'Recommended' } })

    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Pick source must be Recommended or MyChoice.')).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: /^Pick source/ })).toHaveValue('Recommended')
  })
})
