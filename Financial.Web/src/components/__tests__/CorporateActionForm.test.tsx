import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { render } from '../../test/renderWithFluent'
import CorporateActionForm from '../CorporateActionForm'

const baseProps = {
  editingId: null,
  formEffectiveDate: '2026-07-25',
  formType: 'Split',
  formRatioNumerator: '',
  formRatioDenominator: '',
  formNote: '',
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
  onFieldChange: vi.fn(),
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

  it('offers only Split / Reverse Split in the type selector', () => {
    render(<CorporateActionForm {...baseProps} />)

    const options = screen.getByLabelText('Type').querySelectorAll('option')
    expect(Array.from(options).map((o) => o.textContent)).toEqual(['Split / Reverse Split'])
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
})
