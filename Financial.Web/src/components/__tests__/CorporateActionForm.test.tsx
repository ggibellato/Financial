import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { render } from '../../test/renderWithFluent'
import type { AssetSearchOptionsData } from '../../hooks/useAssetSearchOptions'
import { formatN2 } from '../../utils/formatters'
import { BLANK_TARGET_ASSET_IDENTITY, BLANK_TARGET_ASSET_PICKER_VALUE } from '../targetAssetPickerValue'
import CorporateActionForm from '../CorporateActionForm'

const mockAssetSearchOptions: AssetSearchOptionsData = {
  options: [],
  isLoading: false,
  error: null,
  retry: vi.fn(),
}

vi.mock('../../hooks/useAssetSearchOptions', () => ({
  useAssetSearchOptions: () => mockAssetSearchOptions,
}))

const baseProps = {
  editingId: null,
  formEffectiveDate: '2026-07-25',
  formType: 'Split',
  formRatioNumerator: '',
  formRatioDenominator: '',
  formNote: '',
  formStep: 'fields' as const,
  formTargetAsset: BLANK_TARGET_ASSET_PICKER_VALUE,
  formExchangeRatio: '',
  formCashInLieu: '',
  sourceAssetName: 'KLBN4',
  sourceQuantity: 200,
  sourceCostBasis: 2000,
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
  onFieldChange: vi.fn(),
  onTargetAssetChange: vi.fn(),
  onAdvanceToConfirm: vi.fn(),
  onBackToFields: vi.fn(),
  onSave: vi.fn(),
  onCancel: vi.fn(),
}

describe('CorporateActionForm', () => {
  it('renders the create form title and confirm label, matching the New corporate action trigger', () => {
    render(<CorporateActionForm {...baseProps} />)

    expect(screen.getByRole('heading', { name: 'New corporate action' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add corporate action' })).toBeInTheDocument()
  })

  it('renders the edit form title and Save confirm label', () => {
    render(<CorporateActionForm {...baseProps} editingId="ca1" />)

    expect(screen.getByRole('heading', { name: 'Edit corporate action' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
  })

  it('shows only the Split field group: Effective Date, Type, ratio inputs, and Note', () => {
    render(<CorporateActionForm {...baseProps} />)

    expect(screen.getByLabelText(/^Effective Date/)).toBeInTheDocument()
    expect(screen.getByLabelText('Type')).toBeInTheDocument()
    expect(screen.getByLabelText(/^New units/)).toBeInTheDocument()
    expect(screen.getByLabelText(/^Old units/)).toBeInTheDocument()
    expect(screen.getByLabelText('Note')).toBeInTheDocument()
  })

  it('offers Split / Reverse Split and Merger in the type selector', () => {
    render(<CorporateActionForm {...baseProps} />)

    const options = screen.getByLabelText('Type').querySelectorAll('option')
    expect(Array.from(options).map((o) => o.textContent)).toEqual(['Split / Reverse Split', 'Merger'])
  })

  it('shows the inline ratio example text', () => {
    render(<CorporateActionForm {...baseProps} />)

    expect(screen.getByText(/2 new for 1 old is a 2-for-1 split/)).toBeInTheDocument()
  })

  it('marks Effective Date and the ratio inputs as required, leaving Note optional', () => {
    render(<CorporateActionForm {...baseProps} />)

    expect(screen.getByLabelText(/^Effective Date/)).toBeRequired()
    expect(screen.getByLabelText(/^New units/)).toBeRequired()
    expect(screen.getByLabelText(/^Old units/)).toBeRequired()
    expect(screen.getByLabelText('Note')).not.toBeRequired()
  })

  it('shows the ratio validation error as an accessible description on both New units and Old units fields', () => {
    render(<CorporateActionForm {...baseProps} saveErrorFields={{ formRatio: 'Enter a valid split ratio other than 1-for-1' }} />)

    expect(screen.getAllByText('Enter a valid split ratio other than 1-for-1')).toHaveLength(2)
    expect(screen.getByLabelText(/^New units/)).toHaveAccessibleDescription('Enter a valid split ratio other than 1-for-1')
    expect(screen.getByLabelText(/^Old units/)).toHaveAccessibleDescription('Enter a valid split ratio other than 1-for-1')
  })

  it('shows the general server error banner when no field claims it', () => {
    render(<CorporateActionForm {...baseProps} saveError="This holding has no open position to split." />)

    expect(screen.getByText('This holding has no open position to split.')).toBeInTheDocument()
  })

  it('does not show a duplicate general error banner when a field-level error already claims it', () => {
    render(
      <CorporateActionForm
        {...baseProps}
        saveError="Enter a valid split ratio other than 1-for-1"
        saveErrorFields={{ formRatio: 'Enter a valid split ratio other than 1-for-1' }}
      />,
    )

    expect(screen.getAllByText('Enter a valid split ratio other than 1-for-1')).toHaveLength(2)
  })

  it('shows Saving... and disables the confirm button while isSaving', () => {
    render(<CorporateActionForm {...baseProps} isSaving />)

    const button = screen.getByRole('button', { name: 'Saving...' })
    expect(button).toBeDisabled()
  })

  it('preserves entered values across a server-error re-render', () => {
    const { rerender } = render(<CorporateActionForm {...baseProps} formRatioNumerator="2" formRatioDenominator="1" formNote="from broker" />)

    rerender(<CorporateActionForm {...baseProps} formRatioNumerator="2" formRatioDenominator="1" formNote="from broker" saveError="Failed" />)

    expect(screen.getByLabelText(/^New units/)).toHaveValue(2)
    expect(screen.getByLabelText(/^Old units/)).toHaveValue(1)
    expect(screen.getByLabelText('Note')).toHaveValue('from broker')
  })

  it('calls onFieldChange with the field key when a ratio input changes', async () => {
    const onFieldChange = vi.fn()
    const user = userEvent.setup()
    render(<CorporateActionForm {...baseProps} onFieldChange={onFieldChange} />)

    await user.type(screen.getByLabelText(/^New units/), '2')

    expect(onFieldChange).toHaveBeenCalledWith('formRatioNumerator', expect.any(String))
  })

  it('calls onSave and onCancel', async () => {
    const onSave = vi.fn()
    const onCancel = vi.fn()
    const user = userEvent.setup()
    render(<CorporateActionForm {...baseProps} onSave={onSave} onCancel={onCancel} />)

    await user.click(screen.getByRole('button', { name: 'Add corporate action' }))
    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onSave).toHaveBeenCalledTimes(1)
    expect(onCancel).toHaveBeenCalledTimes(1)
  })

  it('shows only the Merger field group when Type is Merger, hiding the Split ratio fields', () => {
    render(<CorporateActionForm {...baseProps} formType="Merger" />)

    expect(screen.getByLabelText('Source Asset')).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: 'Target Asset' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Exchange Ratio/)).toBeInTheDocument()
    expect(screen.getByLabelText('Cash-in-Lieu Amount')).toBeInTheDocument()
    expect(screen.getByLabelText('Note')).toBeInTheDocument()
    expect(screen.queryByLabelText(/^New units/)).not.toBeInTheDocument()
    expect(screen.queryByLabelText(/^Old units/)).not.toBeInTheDocument()
  })

  it('clicking the primary button on the Merger fields step calls onAdvanceToConfirm, not onSave', async () => {
    const onAdvanceToConfirm = vi.fn()
    const onSave = vi.fn()
    const user = userEvent.setup()
    render(
      <CorporateActionForm
        {...baseProps}
        formType="Merger"
        onAdvanceToConfirm={onAdvanceToConfirm}
        onSave={onSave}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Continue' }))

    expect(onAdvanceToConfirm).toHaveBeenCalledTimes(1)
    expect(onSave).not.toHaveBeenCalled()
  })

  it('renders the confirm-step summary with computed units and cost basis, and wires Confirm & Save / Back', async () => {
    const onSave = vi.fn()
    const onBackToFields = vi.fn()
    const user = userEvent.setup()
    render(
      <CorporateActionForm
        {...baseProps}
        formType="Merger"
        formStep="confirm"
        formTargetAsset={{ assetName: 'Company B', identity: BLANK_TARGET_ASSET_IDENTITY }}
        formExchangeRatio="0.5"
        sourceAssetName="KLBN4"
        sourceQuantity={200}
        sourceCostBasis={2000}
        onSave={onSave}
        onBackToFields={onBackToFields}
      />,
    )

    expect(screen.getByText(new RegExp(`Your position in KLBN4 \\(${formatN2(200)} units\\)`))).toBeInTheDocument()
    expect(screen.getByText(`${formatN2(100)} units`)).toBeInTheDocument()
    expect(screen.getByText(formatN2(2000))).toBeInTheDocument()

    const confirmButton = screen.getByRole('button', { name: 'Confirm & Save' })
    await user.click(confirmButton)
    expect(onSave).toHaveBeenCalledTimes(1)

    await user.click(screen.getByRole('button', { name: 'Back' }))
    expect(onBackToFields).toHaveBeenCalledTimes(1)
  })

  it('shows Target Asset as read-only text instead of the picker when editing an existing merger', () => {
    render(
      <CorporateActionForm
        {...baseProps}
        editingId="ca2"
        formType="Merger"
        formTargetAsset={{ assetName: 'Company B', identity: BLANK_TARGET_ASSET_IDENTITY }}
      />,
    )

    expect(screen.getByLabelText('Target Asset')).toHaveValue('Company B')
    expect(screen.getByLabelText('Target Asset')).toBeDisabled()
    expect(screen.queryByRole('combobox', { name: 'Target Asset' })).not.toBeInTheDocument()
  })
})
