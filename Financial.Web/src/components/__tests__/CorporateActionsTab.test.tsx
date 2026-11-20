import { act, fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { CorporateActionsData } from '../../hooks/useCorporateActions'
import type { AssetDetailsDto, CorporateActionDto } from '../../api/types'
import { BLANK_TARGET_ASSET_PICKER_VALUE } from '../targetAssetPickerValue'
import { formatN2 } from '../../utils/formatters'
import { confirmDelete, requestDelete } from '../../test-utils/confirmDelete'
import CorporateActionsTab from '../CorporateActionsTab'
import { getErrorAlert } from '../../test-utils/errorAlert'

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
const SPLIT_NAME = 'corporate action Split KLBN4, 01/07/2026'

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
  deletingId: null,
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
  afterEach(() => {
    delete (Element.prototype as Partial<Element>).scrollIntoView
  })

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
  })

  it('shows a loading state', () => {
    setMock({ isLoading: true })
    render(<CorporateActionsTab />)
    expect(screen.getByText('Loading…')).toBeInTheDocument()
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

    fireEvent.click(screen.getByRole('button', { name: 'Edit corporate action Split KLBN4, 01/07/2026' }))

    expect(mockShowEditForm).toHaveBeenCalledWith(record)
  })

  it('deletes a corporate action only after the user confirms in the dialog', async () => {
    setMock({ corporateActions: [split({})] })
    render(<CorporateActionsTab />)

    const dialog = await requestDelete(SPLIT_NAME, 'corporate action')
    expect(dialog).toHaveAccessibleDescription(`Delete the ${SPLIT_NAME}? This cannot be undone.`)
    expect(mockDeleteCorporateAction).not.toHaveBeenCalled()
    fireEvent.click(within(dialog).getByRole('button', { name: 'Delete', hidden: true }))

    expect(mockDeleteCorporateAction).toHaveBeenCalledWith('ca1')
  })

  it('does not delete when the user cancels, and returns focus to the row delete', async () => {
    setMock({ corporateActions: [split({})] })
    render(<CorporateActionsTab />)

    const dialog = await requestDelete(SPLIT_NAME, 'corporate action')
    fireEvent.click(within(dialog).getByRole('button', { name: 'Cancel', hidden: true }))

    await waitFor(() => expect(screen.getByRole('button', { name: `Delete ${SPLIT_NAME}`, hidden: true })).toHaveFocus())
    expect(mockDeleteCorporateAction).not.toHaveBeenCalled()
  })

  it('shows the row being deleted as Deleting and unavailable', () => {
    setMock({ corporateActions: [split({})], deletingId: 'ca1' })
    render(<CorporateActionsTab />)

    expect(screen.getByRole('button', { name: `Deleting ${SPLIT_NAME}` })).toHaveAttribute('aria-disabled', 'true')
  })

  it('moves focus to New corporate action after the only row is deleted', async () => {
    setMock({ corporateActions: [split({})] })
    const { rerender } = render(<CorporateActionsTab />)
    await confirmDelete(SPLIT_NAME, 'corporate action')

    setMock({ corporateActions: [] })
    rerender(<CorporateActionsTab />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New corporate action' })).toHaveFocus())
  })

  it('announces a delete error and moves focus to it without hiding the history list', async () => {
    setMock({ corporateActions: [split({})], deleteError: 'Cannot delete: a later disposal depends on lots created by this split' })
    render(<CorporateActionsTab />)

    const alert = getErrorAlert()
    expect(alert).toHaveTextContent('Cannot delete: a later disposal depends on lots created by this split')
    await waitFor(() => expect(alert).toHaveFocus())
    expect(screen.getAllByRole('row')).toHaveLength(2)
  })

  it('renders a StatusBadge in the Tax status column for a Merger record with a calculation status, and "—" for a Split record', () => {
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

  it('shows the Resulting change text for a SpinOff Parent-role record', () => {
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

  it('shows the Resulting change text for a SpinOff New-role record', () => {
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

  it.each([
    ['Source', null, 'Position closed and converted'],
    ['Target', 20, `+${formatN2(20)} units received`],
    ['Target', null, 'Units received from merger'],
  ] as const)('describes the resulting change of a Merger %s record (converted quantity %s)', (role, convertedQuantity, expected) => {
    setMock({
      corporateActions: [split({ id: 'ca5', type: 'Merger', role, ratioFactor: null, convertedQuantity, linkedAssetName: 'TASA4' })],
    })

    render(<CorporateActionsTab />)

    expect(screen.getByText(expected)).toBeInTheDocument()
  })

  it('sorts the history by type when the Type header is clicked', () => {
    const splitRow = split({ id: 'ca1', effectiveDate: '2026-07-01T00:00:00' })
    const mergerRow = split({ id: 'ca2', type: 'Merger', role: 'Source', ratioFactor: null, effectiveDate: '2026-06-01T00:00:00' })
    setMock({ corporateActions: [splitRow, mergerRow] })

    render(<CorporateActionsTab />)
    const table = screen.getByRole('table')
    fireEvent.click(within(table).getByRole('button', { name: 'Type' }))

    const dataRows = within(table).getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('Merger')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('Split')).toBeInTheDocument()
    expect(within(table).getByRole('button', { name: 'Type' }).closest('th')).toHaveAttribute('aria-sort', 'ascending')
  })

  it('describes a split by its ratio, trimming only trailing zeros', () => {
    setMock({
      corporateActions: [
        split({ id: 'ca1', ratioFactor: 2 }),
        split({ id: 'ca2', ratioFactor: 1.5, effectiveDate: '2026-06-01T00:00:00' }),
        split({ id: 'ca3', ratioFactor: 10, effectiveDate: '2026-05-01T00:00:00' }),
      ],
    })

    render(<CorporateActionsTab />)

    expect(screen.getAllByRole('row').slice(1).map((row) => within(row).getAllByRole('cell')[3].textContent)).toEqual([
      'Resulting change:Quantity/average cost rescaled ×2',
      'Resulting change:Quantity/average cost rescaled ×1.5',
      'Resulting change:Quantity/average cost rescaled ×10',
    ])
  })

  it('names no resulting change for a merger or spin-off record without its role, and none from a ratio on a merger', () => {
    setMock({
      corporateActions: [
        split({ id: 'ca1', type: 'Merger', role: null, ratioFactor: null }),
        split({ id: 'ca2', type: 'SpinOff', role: null, ratioFactor: null, effectiveDate: '2026-06-01T00:00:00' }),
        split({ id: 'ca3', type: 'Merger', role: 'Source', ratioFactor: 2, effectiveDate: '2026-05-01T00:00:00' }),
      ],
    })

    render(<CorporateActionsTab />)

    const changes = screen.getAllByRole('row').slice(1).map((row) => within(row).getAllByRole('cell')[3])
    expect(within(changes[0]).getByRole('img', { name: 'None' })).toBeInTheDocument()
    expect(within(changes[1]).getByRole('img', { name: 'None' })).toBeInTheDocument()
    expect(changes[2]).toHaveTextContent('Position closed and converted')
  })

  it('says units were received from a spin-off when the quantity is not known', () => {
    setMock({ corporateActions: [split({ id: 'ca1', type: 'SpinOff', role: 'New', ratioFactor: null, convertedQuantity: null })] })

    render(<CorporateActionsTab />)

    expect(screen.getByText('Units received from spin-off')).toBeInTheDocument()
  })

  it('leaves the affected asset blank while the asset is not known', () => {
    setMock({ asset: null, corporateActions: [split({ id: 'ca1' })] })

    render(<CorporateActionsTab />)

    expect(within(screen.getAllByRole('row')[1]).getAllByRole('cell')[2]).toHaveTextContent(/^Affected\/linked asset:$/)
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
    expect(row).toHaveAttribute('tabindex', '-1')
    expect(document.querySelector('[data-corporate-action-id="ca1"]')).not.toHaveClass('corporate-actions-tab__row--highlighted')
    expect(Element.prototype.scrollIntoView).toHaveBeenCalledWith({ behavior: 'smooth', block: 'center' })
  })

  it('ignores a record asked for that is not loaded, whatever its id holds', () => {
    setMock({ corporateActions: [split({ id: 'ca1' })] })
    Element.prototype.scrollIntoView = vi.fn()

    render(<CorporateActionsTab focusRecordId={'not"loaded'} />)

    expect(document.body).toHaveFocus()
  })

  it('focuses the only record when it is the one asked for', () => {
    setMock({ corporateActions: [split({ id: 'ca1' })] })
    Element.prototype.scrollIntoView = vi.fn()

    render(<CorporateActionsTab focusRecordId="ca1" />)

    expect(document.querySelector('[data-corporate-action-id="ca1"] button')).toHaveFocus()
  })

  it('focuses the record asked for once the history finishes loading', () => {
    setMock({ isLoading: true })
    Element.prototype.scrollIntoView = vi.fn()
    const { rerender } = render(<CorporateActionsTab focusRecordId="ca2" />)

    setMock({ corporateActions: [split({ id: 'ca1' }), split({ id: 'ca2' })] })
    rerender(<CorporateActionsTab focusRecordId="ca2" />)

    expect(document.querySelector('[data-corporate-action-id="ca2"] button')).toHaveFocus()
  })

  describe('highlighting the record asked for', () => {
    beforeEach(() => {
      vi.useFakeTimers()
      Element.prototype.scrollIntoView = vi.fn()
      setMock({ corporateActions: [split({ id: 'ca1' }), split({ id: 'ca2' })] })
    })

    const highlighted = (id: string) => document.querySelector(`[data-corporate-action-id="${id}"]`)

    it('clears the highlight after two seconds', () => {
      render(<CorporateActionsTab focusRecordId="ca2" />)
      act(() => vi.advanceTimersByTime(1999))
      expect(highlighted('ca2')).toHaveClass('corporate-actions-tab__row--highlighted')

      act(() => vi.advanceTimersByTime(1))

      expect(highlighted('ca2')).not.toHaveClass('corporate-actions-tab__row--highlighted')
    })

    it('gives a record asked for next its own two seconds', () => {
      const { rerender } = render(<CorporateActionsTab focusRecordId="ca1" />)
      act(() => vi.advanceTimersByTime(1500))

      rerender(<CorporateActionsTab focusRecordId="ca2" />)
      act(() => vi.advanceTimersByTime(1000))

      expect(highlighted('ca2')).toHaveClass('corporate-actions-tab__row--highlighted')
    })
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
