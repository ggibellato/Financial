import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { TransactionsData } from '../../hooks/useTransactions'
import type { TransactionDto } from '../../api/types'
import TransactionsTab from '../TransactionsTab'

const mockShowNewForm = vi.fn()
const mockShowEditForm = vi.fn()
const mockCancelForm = vi.fn()
const mockSetFormField = vi.fn()
const mockSaveForm = vi.fn()
const mockDeleteTransaction = vi.fn()
const mockRetry = vi.fn()

const TRANSACTION_BUY: TransactionDto = {
  id: 'aaa',
  date: '2024-03-15T00:00:00',
  type: 'Buy',
  quantity: 100,
  unitPrice: 4.2,
  fees: 0.5,
  totalPrice: 420.5,
}

const TRANSACTION_SELL: TransactionDto = {
  id: 'bbb',
  date: '2024-01-10T00:00:00',
  type: 'Sell',
  quantity: 50,
  unitPrice: 5.0,
  fees: 1.0,
  totalPrice: 251.0,
}

const DEFAULT_HOOK: TransactionsData = {
  asset: null,
  isLoading: false,
  error: null,
  retry: mockRetry,
  transactions: [],
  isFormVisible: false,
  editingId: null,
  formDate: '',
  formType: 'Buy',
  formQuantity: '',
  formUnitPrice: '',
  formFees: '',
  isSaving: false,
  saveError: null,
  deleteError: null,
  nodeType: 'Asset',
  showNewForm: mockShowNewForm,
  showEditForm: mockShowEditForm,
  cancelForm: mockCancelForm,
  setFormField: mockSetFormField,
  saveForm: mockSaveForm,
  deleteTransaction: mockDeleteTransaction,
}

let mockHookValue: TransactionsData = { ...DEFAULT_HOOK }

vi.mock('../../hooks/useTransactions', () => ({
  useTransactions: () => mockHookValue,
}))

function setMock(overrides: Partial<TransactionsData>) {
  mockHookValue = { ...DEFAULT_HOOK, ...overrides }
}

describe('TransactionsTab', () => {
  beforeEach(() => {
    mockShowNewForm.mockReset()
    mockShowEditForm.mockReset()
    mockCancelForm.mockReset()
    mockSetFormField.mockReset()
    mockSaveForm.mockReset()
    mockDeleteTransaction.mockReset()
    mockRetry.mockReset()
    mockHookValue = { ...DEFAULT_HOOK }
  })

  it('renders_loading_state', () => {
    setMock({ isLoading: true })
    render(<TransactionsTab />)
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_with_retry', () => {
    setMock({ error: 'Network error' })
    render(<TransactionsTab />)
    expect(screen.getByText('Network error')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('renders_placeholder_for_non_asset', () => {
    setMock({ nodeType: 'Portfolio' })
    render(<TransactionsTab />)
    expect(
      screen.getByText('Transactions are only available for individual assets'),
    ).toBeInTheDocument()
  })

  it('renders_table_with_correct_columns', () => {
    setMock({ transactions: [TRANSACTION_BUY] })
    render(<TransactionsTab />)
    expect(screen.getByText('Date')).toBeInTheDocument()
    expect(screen.getByText('Type')).toBeInTheDocument()
    expect(screen.getByText('Quantity')).toBeInTheDocument()
    expect(screen.getByText('Unit Price')).toBeInTheDocument()
    expect(screen.getByText('Fees')).toBeInTheDocument()
    expect(screen.getByText('Total')).toBeInTheDocument()
  })

  it('renders_date_in_dd_MM_yyyy_format', () => {
    setMock({ transactions: [TRANSACTION_BUY] })
    render(<TransactionsTab />)
    expect(screen.getByText('15/03/2024')).toBeInTheDocument()
  })

  it('renders_buy_type_in_green_bold', () => {
    setMock({ transactions: [TRANSACTION_BUY] })
    render(<TransactionsTab />)
    const typeCell = screen.getByText('Buy')
    expect(typeCell).toHaveClass('transactions-tab__type--buy')
  })

  it('renders_sell_type_in_red_bold', () => {
    setMock({ transactions: [TRANSACTION_SELL] })
    render(<TransactionsTab />)
    const typeCell = screen.getByText('Sell')
    expect(typeCell).toHaveClass('transactions-tab__type--sell')
  })

  it('renders_quantity_with_8_decimal_places', () => {
    setMock({ transactions: [TRANSACTION_BUY] })
    render(<TransactionsTab />)
    expect(screen.getByText('100.00000000')).toBeInTheDocument()
  })

  it('renders_total_in_bold', () => {
    setMock({ transactions: [TRANSACTION_BUY] })
    render(<TransactionsTab />)
    const totalCell = screen.getByText('420.50')
    expect(totalCell).toHaveClass('transactions-tab__total')
  })

  it('new_button_calls_show_new_form', () => {
    render(<TransactionsTab />)
    fireEvent.click(screen.getByRole('button', { name: 'New' }))
    expect(mockShowNewForm).toHaveBeenCalledTimes(1)
  })

  it('renders_form_when_form_visible', () => {
    setMock({ isFormVisible: true, editingId: null })
    render(<TransactionsTab />)
    expect(screen.getByText('New transaction')).toBeInTheDocument()
    expect(screen.getByLabelText('Date')).toBeInTheDocument()
    expect(screen.getByLabelText('Type')).toBeInTheDocument()
    expect(screen.getByLabelText('Quantity')).toBeInTheDocument()
    expect(screen.getByLabelText('Unit Price')).toBeInTheDocument()
    expect(screen.getByLabelText('Fees')).toBeInTheDocument()
  })

  it('renders_edit_transaction_title_when_editing', () => {
    setMock({ isFormVisible: true, editingId: 'aaa' })
    render(<TransactionsTab />)
    expect(screen.getByText('Edit transaction')).toBeInTheDocument()
  })

  it('save_button_disabled_while_saving', () => {
    setMock({ isFormVisible: true, isSaving: true })
    render(<TransactionsTab />)
    const saveBtn = screen.getByRole('button', { name: 'Saving...' })
    expect(saveBtn).toBeDisabled()
  })

  it('edit_icon_calls_show_edit_form', () => {
    setMock({ transactions: [TRANSACTION_BUY] })
    render(<TransactionsTab />)
    fireEvent.click(screen.getByRole('button', { name: 'Edit transaction' }))
    expect(mockShowEditForm).toHaveBeenCalledWith(TRANSACTION_BUY)
  })

  it('delete_icon_calls_delete_transaction', () => {
    setMock({ transactions: [TRANSACTION_BUY] })
    render(<TransactionsTab />)
    fireEvent.click(screen.getByRole('button', { name: 'Delete transaction' }))
    expect(mockDeleteTransaction).toHaveBeenCalledWith('aaa')
  })

  it('renders_save_error_below_form', () => {
    setMock({ isFormVisible: true, saveError: 'Failed to save' })
    render(<TransactionsTab />)
    expect(screen.getByText('Failed to save')).toBeInTheDocument()
  })

  it('renders_delete_error_below_table', () => {
    setMock({ deleteError: 'Failed to delete' })
    render(<TransactionsTab />)
    expect(screen.getByText('Failed to delete')).toBeInTheDocument()
  })

  it('empty_table_renders_no_rows', () => {
    setMock({ transactions: [] })
    render(<TransactionsTab />)
    const tbody = document.querySelector('tbody')
    expect(tbody?.children.length).toBe(0)
  })
})
