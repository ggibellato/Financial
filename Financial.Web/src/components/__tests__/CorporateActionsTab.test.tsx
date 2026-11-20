import { act, fireEvent, screen, waitFor, within } from '../../test/renderWithFluent'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { ApiError } from '../../api/apiError'
import type { AssetDetailsDto, CorporateActionDto, SelectedNode } from '../../api/types'
import { formatN2 } from '../../utils/formatters'
import { confirmDelete, requestDelete } from '../../test-utils/confirmDelete'
import { deferred, never } from '../../test-utils/deferred'
import { getErrorAlert } from '../../test-utils/errorAlert'
import { renderWithSelectedNode } from '../../test-utils/renderWithSelectedNode'
import CorporateActionsTab from '../CorporateActionsTab'

const { getAssetDetailsMock, deleteCorporateActionMock, getAdminAssetsMock } = vi.hoisted(() => ({
  getAssetDetailsMock: vi.fn<FinancialApiClient['getAssetDetails']>(),
  deleteCorporateActionMock: vi.fn<FinancialApiClient['deleteCorporateAction']>(),
  getAdminAssetsMock: vi.fn<FinancialApiClient['getAdminAssets']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAssetDetails: getAssetDetailsMock,
    deleteCorporateAction: deleteCorporateActionMock,
    getAdminAssets: getAdminAssetsMock,
  } as Partial<FinancialApiClient>,
}))

const ASSET_NODE: SelectedNode = {
  nodeType: 'Asset',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  assetName: 'KLBN4',
  ticker: 'KLBN4',
  exchange: 'BVMF',
  positionType: 'Long',
}

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

const asset = (corporateActions: CorporateActionDto[]) => ({ name: 'KLBN4', corporateActions }) as AssetDetailsDto
const SPLIT_NAME = 'corporate action Split KLBN4, 01/07/2026'

function renderTab(focusRecordId?: string) {
  return renderWithSelectedNode(<CorporateActionsTab focusRecordId={focusRecordId} />, ASSET_NODE)
}

async function renderLoaded(corporateActions: CorporateActionDto[], focusRecordId?: string) {
  getAssetDetailsMock.mockResolvedValue(asset(corporateActions))
  const rendered = renderTab(focusRecordId)
  await act(async () => {})
  return rendered
}

const rowOf = (id: string) => document.querySelector(`[data-corporate-action-id="${id}"]`)

describe('CorporateActionsTab', () => {
  afterEach(() => {
    delete (Element.prototype as Partial<Element>).scrollIntoView
  })

  beforeEach(() => {
    getAssetDetailsMock.mockReset()
    deleteCorporateActionMock.mockReset()
    getAdminAssetsMock.mockReset().mockResolvedValue([])
  })

  it('shows a loading state', () => {
    getAssetDetailsMock.mockReturnValue(never())
    renderTab()
    expect(screen.getByText('Loading…')).toBeInTheDocument()
  })

  it('shows an error state, and retry loads the history', async () => {
    getAssetDetailsMock.mockRejectedValueOnce(new ApiError('Network down', 503)).mockResolvedValue(asset([split({})]))
    renderTab()

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('button', { name: `Delete ${SPLIT_NAME}` })).toBeInTheDocument()
  })

  it('shows the empty state when no corporate actions have been recorded', async () => {
    await renderLoaded([])
    expect(
      screen.getByText('No corporate actions yet — nothing has changed this holding through a split, merger, or spin-off.'),
    ).toBeInTheDocument()
  })

  it('opens the new corporate action form from the New corporate action trigger', async () => {
    await renderLoaded([])
    fireEvent.click(screen.getByRole('button', { name: 'New corporate action' }))
    expect(screen.getByRole('heading', { name: 'New corporate action' })).toBeInTheDocument()
  })

  it('renders every recorded corporate action for the asset in the history list', async () => {
    await renderLoaded([
      split({ id: 'ca1', effectiveDate: '2026-07-01T00:00:00', ratioFactor: 2 }),
      split({ id: 'ca2', effectiveDate: '2026-06-01T00:00:00', ratioFactor: 0.1 }),
    ])

    expect(screen.getAllByRole('row')).toHaveLength(3)
    expect(screen.getAllByText('KLBN4')).toHaveLength(2)
  })

  it('defaults to sorting by date descending, with the header showing the active sort', async () => {
    await renderLoaded([split({ id: 'ca1', effectiveDate: '2026-06-01T00:00:00' }), split({ id: 'ca2', effectiveDate: '2026-07-01T00:00:00' })])
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

  it('opens the record in the edit form when Edit is clicked', async () => {
    await renderLoaded([split({})])

    fireEvent.click(screen.getByRole('button', { name: 'Edit corporate action Split KLBN4, 01/07/2026' }))

    expect(screen.getByRole('heading', { name: 'Edit corporate action' })).toBeInTheDocument()
  })

  it('deletes a corporate action only after the user confirms in the dialog, then drops its row', async () => {
    deleteCorporateActionMock.mockResolvedValue(asset([]))
    await renderLoaded([split({})])

    const dialog = await requestDelete(SPLIT_NAME, 'corporate action')
    expect(dialog).toHaveAccessibleDescription(`Delete the ${SPLIT_NAME}? This cannot be undone.`)
    expect(deleteCorporateActionMock).not.toHaveBeenCalled()
    fireEvent.click(within(dialog).getByRole('button', { name: 'Delete', hidden: true }))

    expect(deleteCorporateActionMock).toHaveBeenCalledWith({ brokerName: 'XPI', portfolioName: 'Acoes', assetName: 'KLBN4', id: 'ca1' })
    await waitFor(() => expect(screen.queryByRole('button', { name: `Delete ${SPLIT_NAME}`, hidden: true })).not.toBeInTheDocument())
  })

  it('does not delete when the user cancels, and returns focus to the row delete', async () => {
    await renderLoaded([split({})])

    const dialog = await requestDelete(SPLIT_NAME, 'corporate action')
    fireEvent.click(within(dialog).getByRole('button', { name: 'Cancel', hidden: true }))

    await waitFor(() => expect(screen.getByRole('button', { name: `Delete ${SPLIT_NAME}`, hidden: true })).toHaveFocus())
    expect(deleteCorporateActionMock).not.toHaveBeenCalled()
  })

  it('shows the row being deleted as Deleting and unavailable', async () => {
    deleteCorporateActionMock.mockReturnValue(never())
    await renderLoaded([split({})])

    await confirmDelete(SPLIT_NAME, 'corporate action')

    expect(await screen.findByRole('button', { name: `Deleting ${SPLIT_NAME}`, hidden: true })).toHaveAttribute('aria-disabled', 'true')
  })

  it('moves focus to New corporate action after the only row is deleted', async () => {
    deleteCorporateActionMock.mockResolvedValue(asset([]))
    await renderLoaded([split({})])

    await confirmDelete(SPLIT_NAME, 'corporate action')

    await waitFor(() => expect(screen.getByRole('button', { name: 'New corporate action' })).toHaveFocus())
  })

  it('announces a delete error and moves focus to it without hiding the history list', async () => {
    deleteCorporateActionMock.mockRejectedValue(new ApiError('Cannot delete: a later disposal depends on lots created by this split', 409))
    await renderLoaded([split({})])

    await confirmDelete(SPLIT_NAME, 'corporate action')

    await waitFor(() => expect(getErrorAlert()).toHaveTextContent('Cannot delete: a later disposal depends on lots created by this split'))
    await waitFor(() => expect(getErrorAlert()).toHaveFocus())
    expect(screen.getAllByRole('row')).toHaveLength(2)
  })

  it('renders a StatusBadge in the Tax status column for a Merger record with a calculation status, and "—" for a Split record', async () => {
    await renderLoaded([
      split({ id: 'ca2', type: 'Merger', role: 'Source', calculationStatus: 'RequiresReview', ratioFactor: null }),
      split({ id: 'ca1', calculationStatus: null }),
    ])

    expect(screen.getByText('Requires review')).toBeInTheDocument()
    expect(screen.getAllByText('—').length).toBeGreaterThan(0)
  })

  it('shows the Resulting change text for a SpinOff Parent-role record', async () => {
    await renderLoaded([
      split({ id: 'ca3', type: 'SpinOff', role: 'Parent', calculationStatus: 'RequiresReview', ratioFactor: null, allocationPercentage: 25 }),
    ])

    expect(screen.getByText('Cost basis reduced by spin-off')).toBeInTheDocument()
  })

  it('shows the Resulting change text for a SpinOff New-role record', async () => {
    await renderLoaded([
      split({ id: 'ca4', type: 'SpinOff', role: 'New', calculationStatus: 'RequiresReview', ratioFactor: null, convertedQuantity: 40 }),
    ])

    expect(screen.getByText(`+${formatN2(40)} units received`)).toBeInTheDocument()
  })

  it.each([
    ['Source', null, 'Position closed and converted'],
    ['Target', 20, `+${formatN2(20)} units received`],
    ['Target', null, 'Units received from merger'],
  ] as const)('describes the resulting change of a Merger %s record (converted quantity %s)', async (role, convertedQuantity, expected) => {
    await renderLoaded([split({ id: 'ca5', type: 'Merger', role, ratioFactor: null, convertedQuantity, linkedAssetName: 'TASA4' })])

    expect(screen.getByText(expected)).toBeInTheDocument()
  })

  it('sorts the history by type when the Type header is clicked', async () => {
    await renderLoaded([
      split({ id: 'ca1', effectiveDate: '2026-07-01T00:00:00' }),
      split({ id: 'ca2', type: 'Merger', role: 'Source', ratioFactor: null, effectiveDate: '2026-06-01T00:00:00' }),
    ])
    const table = screen.getByRole('table')
    fireEvent.click(within(table).getByRole('button', { name: 'Type' }))

    const dataRows = within(table).getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('Merger')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('Split')).toBeInTheDocument()
    expect(within(table).getByRole('button', { name: 'Type' }).closest('th')).toHaveAttribute('aria-sort', 'ascending')
  })

  it('describes a split by its ratio, trimming only trailing zeros', async () => {
    await renderLoaded([
      split({ id: 'ca1', ratioFactor: 2 }),
      split({ id: 'ca2', ratioFactor: 1.5, effectiveDate: '2026-06-01T00:00:00' }),
      split({ id: 'ca3', ratioFactor: 10, effectiveDate: '2026-05-01T00:00:00' }),
    ])

    expect(screen.getAllByRole('row').slice(1).map((row) => within(row).getAllByRole('cell')[3].textContent)).toEqual([
      'Resulting change:Quantity/average cost rescaled ×2',
      'Resulting change:Quantity/average cost rescaled ×1.5',
      'Resulting change:Quantity/average cost rescaled ×10',
    ])
  })

  it('names no resulting change for a merger or spin-off record without its role, and none from a ratio on a merger', async () => {
    await renderLoaded([
      split({ id: 'ca1', type: 'Merger', role: null, ratioFactor: null }),
      split({ id: 'ca2', type: 'SpinOff', role: null, ratioFactor: null, effectiveDate: '2026-06-01T00:00:00' }),
      split({ id: 'ca3', type: 'Merger', role: 'Source', ratioFactor: 2, effectiveDate: '2026-05-01T00:00:00' }),
    ])

    const changes = screen.getAllByRole('row').slice(1).map((row) => within(row).getAllByRole('cell')[3])
    expect(within(changes[0]).getByRole('img', { name: 'None' })).toBeInTheDocument()
    expect(within(changes[1]).getByRole('img', { name: 'None' })).toBeInTheDocument()
    expect(changes[2]).toHaveTextContent('Position closed and converted')
  })

  it('says units were received from a spin-off when the quantity is not known', async () => {
    await renderLoaded([split({ id: 'ca1', type: 'SpinOff', role: 'New', ratioFactor: null, convertedQuantity: null })])

    expect(screen.getByText('Units received from spin-off')).toBeInTheDocument()
  })

  it('focuses and highlights the row matching focusRecordId once the history has loaded', async () => {
    Element.prototype.scrollIntoView = vi.fn()
    await renderLoaded([split({ id: 'ca1' }), split({ id: 'ca2' })], 'ca2')

    expect(rowOf('ca2')?.querySelector('button')).toHaveFocus()
    expect(rowOf('ca2')).toHaveClass('corporate-actions-tab__row--highlighted')
    expect(rowOf('ca2')).toHaveAttribute('tabindex', '-1')
    expect(rowOf('ca1')).not.toHaveClass('corporate-actions-tab__row--highlighted')
    expect(Element.prototype.scrollIntoView).toHaveBeenCalledWith({ behavior: 'smooth', block: 'center' })
  })

  it('ignores a record asked for that is not loaded, whatever its id holds', async () => {
    Element.prototype.scrollIntoView = vi.fn()
    await renderLoaded([split({ id: 'ca1' })], 'not"loaded')

    expect(document.body).toHaveFocus()
  })

  it('focuses the only record when it is the one asked for', async () => {
    Element.prototype.scrollIntoView = vi.fn()
    await renderLoaded([split({ id: 'ca1' })], 'ca1')

    expect(rowOf('ca1')?.querySelector('button')).toHaveFocus()
  })

  it('focuses the record asked for once the history finishes loading', async () => {
    Element.prototype.scrollIntoView = vi.fn()
    const pending = deferred<AssetDetailsDto>()
    getAssetDetailsMock.mockReturnValue(pending.promise)
    renderTab('ca2')
    expect(screen.getByText('Loading…')).toBeInTheDocument()

    await act(async () => pending.resolve(asset([split({ id: 'ca1' }), split({ id: 'ca2' })])))

    expect(rowOf('ca2')?.querySelector('button')).toHaveFocus()
  })

  describe('highlighting the record asked for', () => {
    beforeEach(() => {
      vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
      Element.prototype.scrollIntoView = vi.fn()
    })

    it('clears the highlight after two seconds', async () => {
      await renderLoaded([split({ id: 'ca1' }), split({ id: 'ca2' })], 'ca2')
      act(() => vi.advanceTimersByTime(1999))
      expect(rowOf('ca2')).toHaveClass('corporate-actions-tab__row--highlighted')

      act(() => vi.advanceTimersByTime(1))

      expect(rowOf('ca2')).not.toHaveClass('corporate-actions-tab__row--highlighted')
    })

    it('gives a record asked for next its own two seconds', async () => {
      const { rerender } = await renderLoaded([split({ id: 'ca1' }), split({ id: 'ca2' })], 'ca1')
      act(() => vi.advanceTimersByTime(1500))

      rerender(<CorporateActionsTab focusRecordId="ca2" />)
      act(() => vi.advanceTimersByTime(1000))

      expect(rowOf('ca2')).toHaveClass('corporate-actions-tab__row--highlighted')
    })
  })

  it('does not crash or change focus when focusRecordId matches no loaded record', async () => {
    Element.prototype.scrollIntoView = vi.fn()
    await renderLoaded([split({ id: 'ca1' })], 'does-not-exist')

    expect(rowOf('ca1')).not.toHaveFocus()
    expect(document.body).toHaveFocus()
  })
})
