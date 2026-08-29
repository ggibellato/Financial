import { fireEvent, render, screen, within } from '@testing-library/react'
import React from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { PriceHistoryData } from '../../hooks/usePriceHistory'
import type { AssetPriceSnapshotDto } from '../../api/types'
import PriceHistoryTab from '../PriceHistoryTab'

const mockShowNewForm = vi.fn()
const mockShowEditForm = vi.fn()
const mockCancelForm = vi.fn()
const mockSetFormField = vi.fn()
const mockSaveForm = vi.fn()
const mockDeleteEntry = vi.fn()
const mockRetry = vi.fn()
const mockSetFilter = vi.fn()

vi.mock('recharts', () => ({
  LineChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="line-chart">{children}</div>
  ),
  Line: () => null,
  XAxis: () => null,
  YAxis: () => null,
  CartesianGrid: () => null,
  Tooltip: () => null,
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
}))

const MANUAL_ENTRY: AssetPriceSnapshotDto = {
  date: '2024-03-15T00:00:00',
  price: 120.5,
  isManual: true,
}

const AUTOMATIC_ENTRY: AssetPriceSnapshotDto = {
  date: '2024-01-10T00:00:00',
  price: 350.0,
  isManual: false,
}

const DEFAULT_HOOK: PriceHistoryData = {
  entries: [],
  filteredEntries: [],
  isLoading: false,
  error: null,
  retry: mockRetry,
  selectedFilter: 'last-12-months',
  setFilter: mockSetFilter,
  isFormVisible: false,
  editingDate: null,
  formDate: '',
  formPrice: '',
  isSaving: false,
  saveError: null,
  deleteError: null,
  showNewForm: mockShowNewForm,
  showEditForm: mockShowEditForm,
  cancelForm: mockCancelForm,
  setFormField: mockSetFormField,
  saveForm: mockSaveForm,
  deleteEntry: mockDeleteEntry,
}

let mockHookValue: PriceHistoryData = { ...DEFAULT_HOOK }

vi.mock('../../hooks/usePriceHistory', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../hooks/usePriceHistory')>()
  return {
    ...actual,
    usePriceHistory: () => mockHookValue,
  }
})

function setMock(overrides: Partial<PriceHistoryData>) {
  mockHookValue = { ...DEFAULT_HOOK, ...overrides }
}

describe('PriceHistoryTab', () => {
  beforeEach(() => {
  // Confirmation moved out of the data hooks and into their callers, so the stub belongs here
  // now. Default to accepting; the cancel path gets its own test.
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    mockShowNewForm.mockReset()
    mockShowEditForm.mockReset()
    mockCancelForm.mockReset()
    mockSetFormField.mockReset()
    mockSaveForm.mockReset()
    mockDeleteEntry.mockReset()
    mockRetry.mockReset()
    mockSetFilter.mockReset()
    mockHookValue = { ...DEFAULT_HOOK }
  })

  it('renders_loading_state', () => {
    setMock({ isLoading: true })
    render(<PriceHistoryTab />)
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_with_retry', () => {
    setMock({ error: 'Network error' })
    render(<PriceHistoryTab />)
    expect(screen.getByText('Network error')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('renders_table_and_chart', () => {
    setMock({ entries: [MANUAL_ENTRY], filteredEntries: [MANUAL_ENTRY] })
    render(<PriceHistoryTab />)
    expect(screen.getByRole('table')).toBeInTheDocument()
    expect(screen.getByTestId('responsive-container')).toBeInTheDocument()
  })

  it('renders_table_columns_date_price_source', () => {
    setMock({ entries: [MANUAL_ENTRY] })
    render(<PriceHistoryTab />)
    expect(screen.getByText('Date')).toBeInTheDocument()
    expect(screen.getByText('Price')).toBeInTheDocument()
    expect(screen.getByText('Source')).toBeInTheDocument()
  })

  it('renders_date_in_dd_MM_yyyy_format', () => {
    setMock({ entries: [MANUAL_ENTRY] })
    render(<PriceHistoryTab />)
    expect(screen.getByText('15/03/2024')).toBeInTheDocument()
  })

  it('renders_price_in_n2', () => {
    setMock({ entries: [MANUAL_ENTRY] })
    render(<PriceHistoryTab />)
    expect(screen.getByText('120.50')).toBeInTheDocument()
  })

  it('renders_manual_source_label', () => {
    setMock({ entries: [MANUAL_ENTRY] })
    render(<PriceHistoryTab />)
    const sourceCell = screen.getByText('Manual')
    expect(sourceCell).toHaveClass('price-history-tab__source--manual')
  })

  it('renders_automatic_source_label', () => {
    setMock({ entries: [AUTOMATIC_ENTRY] })
    render(<PriceHistoryTab />)
    const sourceCell = screen.getByText('Automatic')
    expect(sourceCell).toHaveClass('price-history-tab__source--automatic')
  })

  it('edit_and_delete_buttons_shown_for_manual_entry', () => {
    setMock({ entries: [MANUAL_ENTRY] })
    render(<PriceHistoryTab />)
    expect(screen.getByRole('button', { name: 'Edit price' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Delete price' })).toBeInTheDocument()
  })

  it('edit_and_delete_buttons_hidden_for_automatic_entry', () => {
    setMock({ entries: [AUTOMATIC_ENTRY] })
    render(<PriceHistoryTab />)
    expect(screen.queryByRole('button', { name: 'Edit price' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Delete price' })).not.toBeInTheDocument()
  })

  it('new_button_calls_show_new_form', () => {
    render(<PriceHistoryTab />)
    fireEvent.click(screen.getByRole('button', { name: 'New price' }))
    expect(mockShowNewForm).toHaveBeenCalledTimes(1)
  })

  it('renders_form_when_form_visible', () => {
    setMock({ isFormVisible: true, editingDate: null })
    render(<PriceHistoryTab />)
    expect(screen.getByLabelText('Date')).toBeInTheDocument()
    expect(screen.getByLabelText('Price')).toBeInTheDocument()
  })

  it('form_title_is_new_price_when_no_editing_date', () => {
    setMock({ isFormVisible: true, editingDate: null })
    render(<PriceHistoryTab />)
    expect(screen.getByText('New price', { selector: '.price-history-tab__form-title' })).toBeInTheDocument()
  })

  it('form_title_is_edit_price_when_editing_date_set', () => {
    setMock({ isFormVisible: true, editingDate: '2024-03-15' })
    render(<PriceHistoryTab />)
    expect(screen.getByText('Edit price')).toBeInTheDocument()
  })

  it('save_button_disabled_while_saving', () => {
    setMock({ isFormVisible: true, isSaving: true })
    render(<PriceHistoryTab />)
    const saveBtn = screen.getByRole('button', { name: 'Saving...' })
    expect(saveBtn).toBeDisabled()
  })

  it('edit_icon_calls_show_edit_form', () => {
    setMock({ entries: [MANUAL_ENTRY] })
    render(<PriceHistoryTab />)
    fireEvent.click(screen.getByRole('button', { name: 'Edit price' }))
    expect(mockShowEditForm).toHaveBeenCalledWith(MANUAL_ENTRY)
  })

  it('delete_icon_calls_delete_entry', () => {
    setMock({ entries: [MANUAL_ENTRY] })
    render(<PriceHistoryTab />)
    fireEvent.click(screen.getByRole('button', { name: 'Delete price' }))
    expect(mockDeleteEntry).toHaveBeenCalledWith('2024-03-15T00:00:00')
  })

  it('delete_icon_calls_delete_entry_only_after_the_user_confirms', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    setMock({ entries: [MANUAL_ENTRY] })
    render(<PriceHistoryTab />)
    fireEvent.click(screen.getByRole('button', { name: 'Delete price' }))
    expect(mockDeleteEntry).not.toHaveBeenCalled()
  })

  it('renders_save_error_below_form', () => {
    setMock({ isFormVisible: true, saveError: 'Failed' })
    render(<PriceHistoryTab />)
    expect(screen.getByText('Failed')).toBeInTheDocument()
  })

  it('renders_delete_error_below_table', () => {
    setMock({ deleteError: 'Failed to delete' })
    render(<PriceHistoryTab />)
    expect(screen.getByText('Failed to delete')).toBeInTheDocument()
  })

  it('renders_all_filter_buttons', () => {
    render(<PriceHistoryTab />)
    expect(screen.getByRole('button', { name: 'This month' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Last 3 months' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Last 6 months' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Last 12 months' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'YTD' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'All time' })).toBeInTheDocument()
  })

  it('active_filter_has_active_class', () => {
    setMock({ selectedFilter: 'last-12-months' })
    render(<PriceHistoryTab />)
    const btn = screen.getByRole('button', { name: 'Last 12 months' })
    expect(btn).toHaveClass('price-history-tab__filter-btn--active')
  })

  it('clicking_filter_calls_set_filter', () => {
    render(<PriceHistoryTab />)
    fireEvent.click(screen.getByRole('button', { name: 'Last 3 months' }))
    expect(mockSetFilter).toHaveBeenCalledWith('last-3-months')
  })

  it('empty_table_renders_no_rows', () => {
    setMock({ entries: [] })
    render(<PriceHistoryTab />)
    const rows = within(screen.getByRole('table')).getAllByRole('row')
    expect(rows).toHaveLength(1)
  })

  it('clicking_price_header_sorts_rows_ascending_then_descending', () => {
    setMock({ entries: [MANUAL_ENTRY, AUTOMATIC_ENTRY] })
    render(<PriceHistoryTab />)
    const table = screen.getByRole('table')

    fireEvent.click(screen.getByRole('button', { name: 'Price' }))
    let dataRows = within(table).getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('120.50')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('350.00')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Price' }))
    dataRows = within(table).getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('350.00')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('120.50')).toBeInTheDocument()
  })
})
