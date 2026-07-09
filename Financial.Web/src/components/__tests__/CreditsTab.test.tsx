import { fireEvent, render, screen } from '@testing-library/react'
import React from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { CreditsData } from '../../hooks/useCredits'
import type { CreditDto } from '../../api/types'
import CreditsTab from '../CreditsTab'

const mockShowNewForm = vi.fn()
const mockShowEditForm = vi.fn()
const mockCancelForm = vi.fn()
const mockSetFormField = vi.fn()
const mockSaveForm = vi.fn()
const mockDeleteCredit = vi.fn()
const mockRetry = vi.fn()
const mockSetFilter = vi.fn()
const mockSetMode = vi.fn()
const mockSetChartType = vi.fn()

vi.mock('recharts', () => ({
  BarChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="bar-chart">{children}</div>
  ),
  Bar: () => null,
  LineChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="line-chart">{children}</div>
  ),
  Line: ({ name }: { name?: string }) => <div data-testid="line" data-name={name} />,
  XAxis: () => null,
  YAxis: () => null,
  CartesianGrid: () => null,
  Tooltip: () => null,
  Legend: () => null,
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
  LabelList: () => null,
}))

const CREDIT_DIVIDEND: CreditDto = {
  id: 'aaa',
  date: '2024-03-15T00:00:00',
  type: 'Dividend',
  value: 120.5,
}

const CREDIT_RENT: CreditDto = {
  id: 'bbb',
  date: '2024-01-10T00:00:00',
  type: 'Rent',
  value: 350.0,
}

const DEFAULT_HOOK: CreditsData = {
  credits: [],
  filteredCredits: [],
  chartData: [],
  creditTypes: [],
  isLoading: false,
  error: null,
  retry: mockRetry,
  selectedFilter: 'last-12-months',
  selectedMode: 'Stacked',
  selectedChartType: 'Bar',
  setFilter: mockSetFilter,
  setMode: mockSetMode,
  setChartType: mockSetChartType,
  isFormVisible: false,
  editingId: null,
  formDate: '',
  formType: 'Dividend',
  formValue: '',
  isSaving: false,
  saveError: null,
  deleteError: null,
  nodeType: 'Asset',
  showNewForm: mockShowNewForm,
  showEditForm: mockShowEditForm,
  cancelForm: mockCancelForm,
  setFormField: mockSetFormField,
  saveForm: mockSaveForm,
  deleteCredit: mockDeleteCredit,
}

let mockHookValue: CreditsData = { ...DEFAULT_HOOK }

vi.mock('../../hooks/useCredits', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../hooks/useCredits')>()
  return {
    ...actual,
    useCredits: () => mockHookValue,
  }
})

function setMock(overrides: Partial<CreditsData>) {
  mockHookValue = { ...DEFAULT_HOOK, ...overrides }
}

describe('CreditsTab', () => {
  beforeEach(() => {
    mockShowNewForm.mockReset()
    mockShowEditForm.mockReset()
    mockCancelForm.mockReset()
    mockSetFormField.mockReset()
    mockSaveForm.mockReset()
    mockDeleteCredit.mockReset()
    mockRetry.mockReset()
    mockSetFilter.mockReset()
    mockSetMode.mockReset()
    mockSetChartType.mockReset()
    mockHookValue = { ...DEFAULT_HOOK }
  })

  it('renders_loading_state', () => {
    setMock({ isLoading: true })
    render(<CreditsTab />)
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_with_retry', () => {
    setMock({ error: 'Network error' })
    render(<CreditsTab />)
    expect(screen.getByText('Network error')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('renders_chart_only_for_broker_node', () => {
    setMock({ nodeType: 'Broker' })
    render(<CreditsTab />)
    expect(screen.getByTestId('responsive-container')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'New' })).not.toBeInTheDocument()
  })

  it('renders_chart_only_for_portfolio_node', () => {
    setMock({ nodeType: 'Portfolio' })
    render(<CreditsTab />)
    expect(screen.getByTestId('responsive-container')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'New' })).not.toBeInTheDocument()
  })

  it('renders_table_and_chart_for_asset_node', () => {
    setMock({ nodeType: 'Asset', credits: [CREDIT_DIVIDEND] })
    render(<CreditsTab />)
    expect(screen.getByRole('table')).toBeInTheDocument()
    expect(screen.getByTestId('responsive-container')).toBeInTheDocument()
  })

  it('renders_table_columns_date_type_value', () => {
    setMock({ credits: [CREDIT_DIVIDEND] })
    render(<CreditsTab />)
    expect(screen.getByText('Date')).toBeInTheDocument()
    expect(screen.getByText('Type')).toBeInTheDocument()
    expect(screen.getByText('Value')).toBeInTheDocument()
  })

  it('renders_date_in_dd_MM_yyyy_format', () => {
    setMock({ credits: [CREDIT_DIVIDEND] })
    render(<CreditsTab />)
    expect(screen.getByText('15/03/2024')).toBeInTheDocument()
  })

  it('renders_dividend_type_with_dividend_class', () => {
    setMock({ credits: [CREDIT_DIVIDEND] })
    render(<CreditsTab />)
    const typeCell = screen.getByText('Dividend')
    expect(typeCell).toHaveClass('credits-tab__type--dividend')
  })

  it('renders_rent_type_with_rent_class', () => {
    setMock({ credits: [CREDIT_RENT] })
    render(<CreditsTab />)
    const typeCell = screen.getByText('Rent')
    expect(typeCell).toHaveClass('credits-tab__type--rent')
  })

  it('renders_value_in_n2_bold', () => {
    setMock({ credits: [CREDIT_DIVIDEND] })
    render(<CreditsTab />)
    const valueCell = screen.getByText('120.50')
    expect(valueCell).toHaveClass('credits-tab__value')
  })

  it('new_button_present_for_asset_only', () => {
    setMock({ nodeType: 'Asset' })
    render(<CreditsTab />)
    expect(screen.getByRole('button', { name: 'New' })).toBeInTheDocument()
  })

  it('new_button_not_present_for_broker', () => {
    setMock({ nodeType: 'Broker' })
    render(<CreditsTab />)
    expect(screen.queryByRole('button', { name: 'New' })).not.toBeInTheDocument()
  })

  it('new_button_calls_show_new_form', () => {
    render(<CreditsTab />)
    fireEvent.click(screen.getByRole('button', { name: 'New' }))
    expect(mockShowNewForm).toHaveBeenCalledTimes(1)
  })

  it('renders_form_when_form_visible', () => {
    setMock({ isFormVisible: true, editingId: null })
    render(<CreditsTab />)
    expect(screen.getByLabelText('Date')).toBeInTheDocument()
    expect(screen.getByLabelText('Type')).toBeInTheDocument()
    expect(screen.getByLabelText('Value')).toBeInTheDocument()
  })

  it('form_title_is_new_credit_when_no_editing_id', () => {
    setMock({ isFormVisible: true, editingId: null })
    render(<CreditsTab />)
    expect(screen.getByText('New credit')).toBeInTheDocument()
  })

  it('form_title_is_edit_credit_when_editing_id_set', () => {
    setMock({ isFormVisible: true, editingId: 'some-id' })
    render(<CreditsTab />)
    expect(screen.getByText('Edit credit')).toBeInTheDocument()
  })

  it('save_button_disabled_while_saving', () => {
    setMock({ isFormVisible: true, isSaving: true })
    render(<CreditsTab />)
    const saveBtn = screen.getByRole('button', { name: 'Saving...' })
    expect(saveBtn).toBeDisabled()
  })

  it('edit_icon_calls_show_edit_form', () => {
    setMock({ credits: [CREDIT_DIVIDEND] })
    render(<CreditsTab />)
    fireEvent.click(screen.getByRole('button', { name: 'Edit credit' }))
    expect(mockShowEditForm).toHaveBeenCalledWith(CREDIT_DIVIDEND)
  })

  it('delete_icon_calls_delete_credit', () => {
    setMock({ credits: [CREDIT_DIVIDEND] })
    render(<CreditsTab />)
    fireEvent.click(screen.getByRole('button', { name: 'Delete credit' }))
    expect(mockDeleteCredit).toHaveBeenCalledWith('aaa')
  })

  it('renders_save_error_below_form', () => {
    setMock({ isFormVisible: true, saveError: 'Failed' })
    render(<CreditsTab />)
    expect(screen.getByText('Failed')).toBeInTheDocument()
  })

  it('renders_delete_error_below_table', () => {
    setMock({ deleteError: 'Failed to delete' })
    render(<CreditsTab />)
    expect(screen.getByText('Failed to delete')).toBeInTheDocument()
  })

  it('renders_all_filter_buttons', () => {
    render(<CreditsTab />)
    expect(screen.getByRole('button', { name: 'This month' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Last 3 months' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Last 6 months' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Last 12 months' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'YTD' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'All time' })).toBeInTheDocument()
  })

  it('active_filter_has_active_class', () => {
    setMock({ selectedFilter: 'last-12-months' })
    render(<CreditsTab />)
    const btn = screen.getByRole('button', { name: 'Last 12 months' })
    expect(btn).toHaveClass('credits-tab__filter-btn--active')
  })

  it('clicking_filter_calls_set_filter', () => {
    render(<CreditsTab />)
    fireEvent.click(screen.getByRole('button', { name: 'Last 3 months' }))
    expect(mockSetFilter).toHaveBeenCalledWith('last-3-months')
  })

  it('renders_mode_toggles', () => {
    render(<CreditsTab />)
    expect(screen.getByRole('button', { name: 'Stacked' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Grouped' })).toBeInTheDocument()
  })

  it('active_mode_has_active_class', () => {
    setMock({ selectedMode: 'Stacked' })
    render(<CreditsTab />)
    const btn = screen.getByRole('button', { name: 'Stacked' })
    expect(btn).toHaveClass('credits-tab__mode-btn--active')
  })

  it('clicking_mode_calls_set_mode', () => {
    render(<CreditsTab />)
    fireEvent.click(screen.getByRole('button', { name: 'Grouped' }))
    expect(mockSetMode).toHaveBeenCalledWith('Grouped')
  })

  it('empty_table_renders_no_rows', () => {
    setMock({ credits: [] })
    render(<CreditsTab />)
    const tbody = document.querySelector('tbody')
    expect(tbody?.children.length).toBe(0)
  })

  it('renders_bar_line_toggle_defaulting_to_bar', () => {
    render(<CreditsTab />)
    expect(screen.getByRole('button', { name: 'Bar' })).toHaveClass('credits-tab__mode-btn--active')
    expect(screen.getByRole('button', { name: 'Line' })).not.toHaveClass('credits-tab__mode-btn--active')
  })

  it('clicking_line_toggle_calls_setChartType', () => {
    render(<CreditsTab />)
    fireEvent.click(screen.getByRole('button', { name: 'Line' }))
    expect(mockSetChartType).toHaveBeenCalledWith('Line')
  })

  it('renders_relabelled_toggle_rows', () => {
    render(<CreditsTab />)
    expect(screen.getByText('View:')).toBeInTheDocument()
    expect(screen.getByText('Group:')).toBeInTheDocument()
  })

  it('renders_bar_chart_unchanged_when_bar_selected', () => {
    setMock({ selectedChartType: 'Bar' })
    render(<CreditsTab />)
    expect(screen.getByTestId('bar-chart')).toBeInTheDocument()
    expect(screen.queryByTestId('line-chart')).not.toBeInTheDocument()
  })

  it('renders_single_total_line_when_grouped_and_line', () => {
    setMock({
      selectedChartType: 'Line',
      selectedMode: 'Grouped',
      creditTypes: ['Dividend', 'Rent'],
      chartData: [{ month: '03/2024', total: 150, byType: { Dividend: 100, Rent: 50 } }],
    })
    render(<CreditsTab />)
    const lines = screen.getAllByTestId('line')
    expect(lines).toHaveLength(1)
    expect(lines[0]).toHaveAttribute('data-name', 'Total')
  })

  it('renders_one_line_per_type_when_stacked_and_line', () => {
    setMock({
      selectedChartType: 'Line',
      selectedMode: 'Stacked',
      creditTypes: ['Dividend', 'Rent'],
      chartData: [{ month: '03/2024', total: 150, byType: { Dividend: 100, Rent: 50 } }],
    })
    render(<CreditsTab />)
    const lines = screen.getAllByTestId('line')
    expect(lines).toHaveLength(2)
    expect(lines.map((l) => l.getAttribute('data-name'))).toEqual(['Dividend', 'Rent'])
  })
})
