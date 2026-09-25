import { fireEvent, render, screen, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { CorporateActionsData } from '../../hooks/useCorporateActions'
import type { AssetDetailsDto, CorporateActionDto } from '../../api/types'
import { BLANK_TARGET_ASSET_PICKER_VALUE } from '../targetAssetPickerValue'
import { formatN2 } from '../../utils/formatters'
import CorporateActionsTab from '../CorporateActionsTab'

const mockRetry = vi.fn()
const mockShowNewForm = vi.fn()
const mockShowEditForm = vi.fn()
const mockCancelForm = vi.fn()
const mockSetFormField = vi.fn()
const mockSetTargetAsset = vi.fn()
const mockSetNewAsset = vi.fn()
const mockAdvanceToConfirm = vi.fn()
const mockBackToFields = vi.fn()
const mockSaveForm = vi.fn()
const mockDeleteCorporateAction = vi.fn()

function split(overrides: Partial<CorporateActionDto>): CorporateActionDto {
  return {
    id: 'ca1',
    type: 'Split',
    effectiveDate: '2026-07-01T00:00:00',
    ratioFactor: 2,
    allocationPercentage: null,
    calculationStatus: null,
    carriedCostBasis: null,
    cashInLieu: null,
    convertedQuantity: null,
    correlationId: null,
    exchangeRatio: null,
    linkedAssetName: null,
    note: null,
    role: null,
    ...overrides,
  }
}

const ASSET: AssetDetailsDto = { name: 'KLBN4' } as AssetDetailsDto

const DEFAULT_HOOK: CorporateActionsData = {
  asset: ASSET,
  corporateActions: [],
  isLoading: false,
  error: null,
  retry: mockRetry,
  isFormVisible: false,
  editingId: null,
  formEffectiveDate: '',
  formType: 'Split',
  formRatioNumerator: '',
  formRatioDenominator: '',
  formNote: '',
  formStep: 'fields',
  formTargetAsset: BLANK_TARGET_ASSET_PICKER_VALUE,
  formExchangeRatio: '',
  formCashInLieu: '',
  formNewAsset: BLANK_TARGET_ASSET_PICKER_VALUE,
  formQuantityReceived: '',
  formAllocationPercentage: '',
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
  deleteError: null,
  targetAssetOptions: { options: [], isLoading: false, error: null, retry: vi.fn() },
  showNewForm: mockShowNewForm,
  showEditForm: mockShowEditForm,
  cancelForm: mockCancelForm,
  setFormField: mockSetFormField,
  setTargetAsset: mockSetTargetAsset,
  setNewAsset: mockSetNewAsset,
  advanceToConfirm: mockAdvanceToConfirm,
  backToFields: mockBackToFields,
  saveForm: mockSaveForm,
  deleteCorporateAction: mockDeleteCorporateAction,
}

let mockHookValue: CorporateActionsData = { ...DEFAULT_HOOK }

vi.mock('../../hooks/useCorporateActions', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../hooks/useCorporateActions')>()
  return {
    ...actual,
    useCorporateActions: () => mockHookValue,
  }
})

function setMock(overrides: Partial<CorporateActionsData>) {
  mockHookValue = { ...DEFAULT_HOOK, ...overrides }
}

describe('CorporateActionsTab', () => {
  beforeEach(() => {
    mockRetry.mockReset()
    mockShowNewForm.mockReset()
    mockShowEditForm.mockReset()
    mockCancelForm.mockReset()
    mockSetFormField.mockReset()
    mockSetTargetAsset.mockReset()
    mockSetNewAsset.mockReset()
    mockAdvanceToConfirm.mockReset()
    mockBackToFields.mockReset()
    mockSaveForm.mockReset()
    mockDeleteCorporateAction.mockReset()
    mockHookValue = { ...DEFAULT_HOOK }
    vi.spyOn(window, 'confirm').mockReturnValue(true)
  })

  it('shows a loading state', () => {
    setMock({ isLoading: true })
    render(<CorporateActionsTab />)
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error state with retry', () => {
    setMock({ error: 'Network down' })
    render(<CorporateActionsTab />)
    expect(screen.getByRole('alert')).toHaveTextContent('Network down')
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(mockRetry).toHaveBeenCalledTimes(1)
  })

  it('shows the empty state when no corporate actions have been recorded', () => {
    render(<CorporateActionsTab />)
    expect(
      screen.getByText('No corporate actions yet — nothing has changed this holding through a split, merger, or spin-off.'),
    ).toBeInTheDocument()
  })

  it('renders the New corporate action trigger, left-positioned, primary', () => {
    render(<CorporateActionsTab />)
    const trigger = screen.getByRole('button', { name: 'New corporate action' })
    expect(trigger).toBeInTheDocument()
    fireEvent.click(trigger)
    expect(mockShowNewForm).toHaveBeenCalledTimes(1)
  })

  it('renders every recorded corporate action for the asset in the history list', () => {
    const rowA = split({ id: 'ca1', effectiveDate: '2026-07-01T00:00:00', ratioFactor: 2 })
    const rowB = split({ id: 'ca2', effectiveDate: '2026-06-01T00:00:00', ratioFactor: 0.1 })
    setMock({ corporateActions: [rowA, rowB] })

    render(<CorporateActionsTab />)

    expect(screen.getAllByRole('row')).toHaveLength(3)
    expect(screen.getAllByText('KLBN4')).toHaveLength(2)
  })

  it('defaults to sorting by date descending, with the header showing the active sort', () => {
    const older = split({ id: 'ca1', effectiveDate: '2026-06-01T00:00:00' })
    const newer = split({ id: 'ca2', effectiveDate: '2026-07-01T00:00:00' })
    setMock({ corporateActions: [older, newer] })

    render(<CorporateActionsTab />)
    const table = screen.getByRole('table')
    const dateHeaderButton = within(table).getByRole('button', { name: 'Date' })
    const dateHeader = dateHeaderButton.closest('th')

    expect(dateHeader).toHaveAttribute('aria-sort', 'descending')
    let dataRows = within(table).getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('01/07/2026')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('01/06/2026')).toBeInTheDocument()

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'none')

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'ascending')
    dataRows = within(table).getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('01/06/2026')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('01/07/2026')).toBeInTheDocument()

    fireEvent.click(dateHeaderButton)
    expect(dateHeader).toHaveAttribute('aria-sort', 'descending')
  })

  it('shows the inline form when isFormVisible is true', () => {
    setMock({ isFormVisible: true })
    render(<CorporateActionsTab />)
    expect(screen.getByRole('heading', { name: 'New corporate action' })).toBeInTheDocument()
  })

  it('calls showEditForm with the record when Edit is clicked', () => {
    const record = split({})
    setMock({ corporateActions: [record] })
    render(<CorporateActionsTab />)

    fireEvent.click(screen.getByRole('button', { name: 'Edit corporate action' }))

    expect(mockShowEditForm).toHaveBeenCalledWith(record)
  })

  it('confirms before deleting a corporate action', () => {
    const record = split({})
    setMock({ corporateActions: [record] })
    render(<CorporateActionsTab />)

    fireEvent.click(screen.getByRole('button', { name: 'Delete corporate action' }))

    expect(window.confirm).toHaveBeenCalled()
    expect(mockDeleteCorporateAction).toHaveBeenCalledWith('ca1')
  })

  it('shows a delete error without hiding the history list', () => {
    const record = split({})
    setMock({ corporateActions: [record], deleteError: 'Cannot delete: a later disposal depends on lots created by this split' })
    render(<CorporateActionsTab />)

    expect(
      screen.getByText('Cannot delete: a later disposal depends on lots created by this split'),
    ).toBeInTheDocument()
    expect(screen.getAllByRole('row')).toHaveLength(2)
  })

  it('renders a StatusBadge in the Tax Status column for a Merger record with a calculation status, and "—" for a Split record', () => {
    const mergerRow = split({
      id: 'ca2',
      type: 'Merger',
      role: 'Source',
      calculationStatus: 'RequiresReview',
      ratioFactor: null,
    })
    const splitRow = split({ id: 'ca1', calculationStatus: null })
    setMock({ corporateActions: [mergerRow, splitRow] })

    render(<CorporateActionsTab />)

    expect(screen.getByText('Requires review')).toBeInTheDocument()
    expect(screen.getAllByText('—').length).toBeGreaterThan(0)
  })

  it('shows the Resulting Change text for a SpinOff Parent-role record', () => {
    const parentRow = split({
      id: 'ca3',
      type: 'SpinOff',
      role: 'Parent',
      calculationStatus: 'RequiresReview',
      ratioFactor: null,
      allocationPercentage: 25,
    })
    setMock({ corporateActions: [parentRow] })

    render(<CorporateActionsTab />)

    expect(screen.getByText('Cost basis reduced by spin-off')).toBeInTheDocument()
  })

  it('shows the Resulting Change text for a SpinOff New-role record', () => {
    const newRow = split({
      id: 'ca4',
      type: 'SpinOff',
      role: 'New',
      calculationStatus: 'RequiresReview',
      ratioFactor: null,
      convertedQuantity: 40,
    })
    setMock({ corporateActions: [newRow] })

    render(<CorporateActionsTab />)

    expect(screen.getByText(`+${formatN2(40)} units received`)).toBeInTheDocument()
  })

  it('focuses and highlights the row matching focusRecordId once the history has loaded', () => {
    const rowA = split({ id: 'ca1' })
    const rowB = split({ id: 'ca2' })
    setMock({ corporateActions: [rowA, rowB] })
    Element.prototype.scrollIntoView = vi.fn()

    render(<CorporateActionsTab focusRecordId="ca2" />)

    const row = document.querySelector('[data-corporate-action-id="ca2"]')
    expect(row?.querySelector('button')).toHaveFocus()
    expect(row).toHaveClass('corporate-actions-tab__row--highlighted')
  })

  it('does not crash or change focus when focusRecordId matches no loaded record', () => {
    const rowA = split({ id: 'ca1' })
    setMock({ corporateActions: [rowA] })
    Element.prototype.scrollIntoView = vi.fn()

    render(<CorporateActionsTab focusRecordId="does-not-exist" />)

    const row = document.querySelector('[data-corporate-action-id="ca1"]')
    expect(row).not.toHaveFocus()
    expect(document.body).toHaveFocus()
  })
})
