import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MensaisPage from '../MensaisPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BankDto, CategoryDto, RecurringBillDto } from '../../api/types'

const {
  getMensaisBillsMock,
  createMensaisBillMock,
  updateMensaisBillMock,
  updateMensaisBillStatusMock,
  deleteMensaisBillMock,
  resetMensaisToUnsetMock,
  getBanksMock,
  getCategoriesMock,
  createExpenseMock,
} = vi.hoisted(() => ({
  getMensaisBillsMock: vi.fn<FinancialApiClient['getMensaisBills']>(),
  createMensaisBillMock: vi.fn<FinancialApiClient['createMensaisBill']>(),
  updateMensaisBillMock: vi.fn<FinancialApiClient['updateMensaisBill']>(),
  updateMensaisBillStatusMock: vi.fn<FinancialApiClient['updateMensaisBillStatus']>(),
  deleteMensaisBillMock: vi.fn<FinancialApiClient['deleteMensaisBill']>(),
  resetMensaisToUnsetMock: vi.fn<FinancialApiClient['resetMensaisToUnset']>(),
  getBanksMock: vi.fn<FinancialApiClient['getBanks']>(),
  getCategoriesMock: vi.fn<FinancialApiClient['getCategories']>(),
  createExpenseMock: vi.fn<FinancialApiClient['createExpense']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getMensaisBills: getMensaisBillsMock,
    createMensaisBill: createMensaisBillMock,
    updateMensaisBill: updateMensaisBillMock,
    updateMensaisBillStatus: updateMensaisBillStatusMock,
    deleteMensaisBill: deleteMensaisBillMock,
    resetMensaisToUnset: resetMensaisToUnsetMock,
    getBanks: getBanksMock,
    getCategories: getCategoriesMock,
    createExpense: createExpenseMock,
  } as Partial<FinancialApiClient>,
}))

const BILLS: RecurringBillDto[] = [
  {
    id: 'b1',
    dueDay: 10,
    description: 'INSS',
    area: 'Brasil',
    note: 'Paga via boleto',
    nitNumber: null,
    minimumWageValue: null,
    value: 850,
    status: 'Unset',
  },
  {
    id: 'b2',
    dueDay: 15,
    description: 'Council Tax',
    area: 'UK',
    note: 'Direct debit',
    nitNumber: null,
    minimumWageValue: null,
    value: 120,
    status: 'Unset',
  },
]

const BANKS: BankDto[] = [
  {
    id: 'bank-1',
    name: 'Barclays',
    roundUpEnabled: false,
    openingBalance: 0,
    openingBalanceDate: '2026-01-01',
    hasReferences: false,
  },
]

const CATEGORIES: CategoryDto[] = [
  { id: 'cat-1', name: 'Bills', active: true, isInvestment: false, isTithe: false, hasReferences: false },
]

describe('MensaisPage', () => {
  beforeEach(() => {
    getMensaisBillsMock.mockReset()
    createMensaisBillMock.mockReset()
    updateMensaisBillMock.mockReset()
    updateMensaisBillStatusMock.mockReset()
    deleteMensaisBillMock.mockReset()
    getBanksMock.mockReset()
    getCategoriesMock.mockReset()
    createExpenseMock.mockReset()
    getBanksMock.mockResolvedValue(BANKS)
    getCategoriesMock.mockResolvedValue(CATEGORIES)
    resetMensaisToUnsetMock.mockReset()
    getMensaisBillsMock.mockResolvedValue(BILLS)
  })

  it('shows a loading state before data arrives', () => {
    render(<MensaisPage />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error state with retry when the fetch fails', async () => {
    getMensaisBillsMock.mockRejectedValue(new Error('Network down'))

    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Network down')).toBeInTheDocument()
  })

  it('renders Brasil and UK as two separate grouped sections', async () => {
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())
    // Rendered in source order: Brasil's table first, UK's second.
    const tables = screen.getAllByRole('table')
    expect(tables).toHaveLength(2)
    expect(within(tables[0]).getByText('INSS')).toBeInTheDocument()
    expect(within(tables[0]).getByText('NIT')).toBeInTheDocument()
    expect(within(tables[1]).getByText('Council Tax')).toBeInTheDocument()
    expect(within(tables[1]).queryByText('NIT')).not.toBeInTheDocument()
  })

  it('edits a row status/value via the toggled panel and saves, updating the displayed row', async () => {
    updateMensaisBillMock.mockResolvedValue({ ...BILLS[0], status: 'Paid', value: 900 })
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    const editButtons = screen.getAllByRole('button', { name: 'Edit bill' })
    fireEvent.click(editButtons[0])

    expect(screen.getByText('Edit Bill')).toBeInTheDocument()
    const valueInput = screen.getByDisplayValue('850')
    fireEvent.change(valueInput, { target: { value: '900' } })
    const statusSelect = screen.getByRole('combobox')
    fireEvent.change(statusSelect, { target: { value: 'Paid' } })

    getMensaisBillsMock.mockResolvedValue([{ ...BILLS[0], status: 'Paid', value: 900 }, BILLS[1]])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateMensaisBillMock).toHaveBeenCalledWith('b1', {
        dueDay: 10,
        description: 'INSS',
        value: 900,
        area: 'Brasil',
        note: 'Paga via boleto',
        nitNumber: null,
        minimumWageValue: null,
        status: 'Paid',
      }),
    )
    await waitFor(() => expect(screen.getByText('Paid')).toBeInTheDocument())
  })

  it('changes a bill status via the inline status menu, without opening the edit drawer', async () => {
    updateMensaisBillStatusMock.mockResolvedValue({ ...BILLS[0], status: 'Paid' })
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    const statusButtons = screen.getAllByRole('button', { name: /^Status: Unset/ })
    fireEvent.click(statusButtons[0])
    fireEvent.click(await screen.findByRole('menuitem', { name: 'Paid' }))

    await waitFor(() => expect(updateMensaisBillStatusMock).toHaveBeenCalledWith('b1', { status: 'Paid' }))
    await waitFor(() => expect(screen.getByRole('button', { name: /^Status: Paid/ })).toBeInTheDocument())
    expect(screen.queryByText('Edit Bill')).not.toBeInTheDocument()
  })

  it('marking a UK bill Paid opens the expense prompt instead of calling the status API', async () => {
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('Council Tax')).toBeInTheDocument())

    const statusButtons = screen.getAllByRole('button', { name: /^Status: Unset/ })
    fireEvent.click(statusButtons[1])
    fireEvent.click(await screen.findByRole('menuitem', { name: 'Paid' }))

    expect(await screen.findByRole('heading', { name: 'Generate expense for this payment?' })).toBeInTheDocument()
    expect(updateMensaisBillStatusMock).not.toHaveBeenCalled()
  })

  it('marking a Brasil bill Paid never opens the expense prompt', async () => {
    updateMensaisBillStatusMock.mockResolvedValue({ ...BILLS[0], status: 'Paid' })
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    const statusButtons = screen.getAllByRole('button', { name: /^Status: Unset/ })
    fireEvent.click(statusButtons[0])
    fireEvent.click(await screen.findByRole('menuitem', { name: 'Paid' }))

    await waitFor(() => expect(updateMensaisBillStatusMock).toHaveBeenCalledWith('b1', { status: 'Paid' }))
    expect(screen.queryByRole('heading', { name: 'Generate expense for this payment?' })).not.toBeInTheDocument()
  })

  it('confirming the expense prompt creates the expense, marks the bill Paid, and closes the prompt', async () => {
    createExpenseMock.mockResolvedValue({
      id: 'exp-1', date: '2026-09-01', description: 'Council Tax', value: 120,
      categoryId: 'cat-1', categoryName: 'Bills', paymentSourceBankId: 'bank-1', paymentSourceBankName: 'Barclays',
      creditCardId: null, creditCardName: null, chargeDate: null, invoiceDate: null,
      paymentStatus: 'ImmediatePayment', roundUpAmount: null, suggestedRoundUpAmount: null, countsAsTithe: true,
    })
    updateMensaisBillStatusMock.mockResolvedValue({ ...BILLS[1], status: 'Paid' })
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('Council Tax')).toBeInTheDocument())
    const statusButtons = screen.getAllByRole('button', { name: /^Status: Unset/ })
    fireEvent.click(statusButtons[1])
    fireEvent.click(await screen.findByRole('menuitem', { name: 'Paid' }))
    await screen.findByRole('heading', { name: 'Generate expense for this payment?' })

    fireEvent.change(screen.getByLabelText(/^Bank/), { target: { value: 'bank-1' } })
    fireEvent.change(screen.getByLabelText(/^Category/), { target: { value: 'cat-1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Confirm' }))

    await waitFor(() => expect(createExpenseMock).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(updateMensaisBillStatusMock).toHaveBeenCalledWith('b2', { status: 'Paid' }))
    await waitFor(() =>
      expect(screen.queryByRole('heading', { name: 'Generate expense for this payment?' })).not.toBeInTheDocument(),
    )
  })

  it('canceling the expense prompt makes no API calls', async () => {
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('Council Tax')).toBeInTheDocument())
    const statusButtons = screen.getAllByRole('button', { name: /^Status: Unset/ })
    fireEvent.click(statusButtons[1])
    fireEvent.click(await screen.findByRole('menuitem', { name: 'Paid' }))
    await screen.findByRole('heading', { name: 'Generate expense for this payment?' })

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(screen.queryByRole('heading', { name: 'Generate expense for this payment?' })).not.toBeInTheDocument()
    expect(createExpenseMock).not.toHaveBeenCalled()
    expect(updateMensaisBillStatusMock).not.toHaveBeenCalled()
  })

  it('shows an error and leaves the status unchanged when the status update fails', async () => {
    updateMensaisBillStatusMock.mockRejectedValue(new Error('Status is not recognized.'))
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    const statusButtons = screen.getAllByRole('button', { name: /^Status: Unset/ })
    fireEvent.click(statusButtons[0])
    fireEvent.click(await screen.findByRole('menuitem', { name: 'Paid' }))

    await waitFor(() => expect(screen.getByText('Status is not recognized.')).toBeInTheDocument())
    expect(screen.getAllByRole('button', { name: /^Status: Unset/ })).toHaveLength(2)
  })

  it('shows each bill\'s Note in both the Brasil and UK grids', async () => {
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())
    expect(screen.getAllByText('Note')).toHaveLength(2)
    expect(screen.getByText('Paga via boleto')).toBeInTheDocument()
    expect(screen.getByText('Direct debit')).toBeInTheDocument()
  })

  it('shows NIT and Min. Wage columns only in the Brasil section', async () => {
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    expect(screen.getAllByText('NIT')).toHaveLength(1)
    expect(screen.getAllByText('Min. Wage')).toHaveLength(1)
  })

  it('adds a new bill via the Add Bill form', async () => {
    createMensaisBillMock.mockResolvedValue({
      id: 'b3',
      dueDay: 5,
      description: 'Aluguel',
      value: 1000,
      area: 'Brasil',
      note: '',
      nitNumber: null,
      minimumWageValue: null,
      status: 'Unset',
    })
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Add Bill' }))
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Aluguel' } })
    fireEvent.change(screen.getByLabelText(/^Due Day/), { target: { value: '5' } })
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '1000' } })

    getMensaisBillsMock.mockResolvedValue([
      ...BILLS,
      { ...BILLS[0], id: 'b3', description: 'Aluguel', dueDay: 5, value: 1000 },
    ])
    const addBillFormPanel = screen.getByRole('heading', { name: 'Add Bill' }).closest('div') as HTMLElement
    fireEvent.click(within(addBillFormPanel).getByRole('button', { name: 'Add Bill' }))

    await waitFor(() =>
      expect(createMensaisBillMock).toHaveBeenCalledWith({
        dueDay: 5,
        description: 'Aluguel',
        value: 1000,
        area: 'Brasil',
        note: '',
      }),
    )
    await waitFor(() => expect(screen.getByText('Aluguel')).toBeInTheDocument())
  })

  it('lists the Add Bill fields in Area, Description, Due Day, Value, Note order', async () => {
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Add Bill' }))

    const addBillFormPanel = screen.getByRole('heading', { name: 'Add Bill' }).closest('div') as HTMLElement
    const fieldLabels = within(addBillFormPanel)
      .getAllByText(/^(Area|Description|Due Day|Value|Note)\*?$/, { selector: 'label' })
      .map((label) => label.textContent?.replace(/\*$/, ''))
    expect(fieldLabels).toEqual(['Area', 'Description', 'Due Day', 'Value', 'Note'])
  })

  it('deletes a bill after confirming the prompt', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    deleteMensaisBillMock.mockResolvedValue(undefined)
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    getMensaisBillsMock.mockResolvedValue([BILLS[1]])
    fireEvent.click(screen.getAllByRole('button', { name: 'Delete bill' })[0])

    await waitFor(() => expect(deleteMensaisBillMock).toHaveBeenCalledWith('b1'))
    await waitFor(() => expect(screen.queryByText('INSS')).not.toBeInTheDocument())
  })

  it('does not delete when the confirmation prompt is declined', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    fireEvent.click(screen.getAllByRole('button', { name: 'Delete bill' })[0])

    expect(deleteMensaisBillMock).not.toHaveBeenCalled()
  })

  it('resets all bills to Unset after confirming the prompt', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    resetMensaisToUnsetMock.mockResolvedValue([
      { ...BILLS[0], status: 'Unset' },
      { ...BILLS[1], status: 'Unset' },
    ])
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Reset All to Unset' }))

    await waitFor(() => expect(resetMensaisToUnsetMock).toHaveBeenCalledTimes(1))
  })

  it('does not reset when the confirmation prompt is declined', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Reset All to Unset' }))

    expect(resetMensaisToUnsetMock).not.toHaveBeenCalled()
  })

  it('sorts the Brasil and UK grids independently by clicking their Description column headers', async () => {
    const bills: RecurringBillDto[] = [
      { id: 'b1', dueDay: 10, description: 'INSS', area: 'Brasil', note: '', nitNumber: null, minimumWageValue: null, value: 850, status: 'Unset' },
      { id: 'b2', dueDay: 5, description: 'Aluguel', area: 'Brasil', note: '', nitNumber: null, minimumWageValue: null, value: 1200, status: 'Unset' },
      { id: 'b3', dueDay: 15, description: 'Council Tax', area: 'UK', note: '', nitNumber: null, minimumWageValue: null, value: 120, status: 'Unset' },
      { id: 'b4', dueDay: 1, description: 'Broadband', area: 'UK', note: '', nitNumber: null, minimumWageValue: null, value: 40, status: 'Unset' },
    ]
    getMensaisBillsMock.mockResolvedValue(bills)

    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    const tables = screen.getAllByRole('table')
    const [brasilDescriptionHeader, ukDescriptionHeader] = screen.getAllByRole('button', { name: 'Description' })

    fireEvent.click(brasilDescriptionHeader)

    let brasilRows = within(tables[0]).getAllByRole('row').slice(1)
    expect(brasilRows.map((r) => r.textContent)).toEqual([
      expect.stringContaining('Aluguel'),
      expect.stringContaining('INSS'),
    ])
    let ukRows = within(tables[1]).getAllByRole('row').slice(1)
    expect(ukRows.map((r) => r.textContent)).toEqual([
      expect.stringContaining('Council Tax'),
      expect.stringContaining('Broadband'),
    ])

    fireEvent.click(ukDescriptionHeader)

    ukRows = within(tables[1]).getAllByRole('row').slice(1)
    expect(ukRows.map((r) => r.textContent)).toEqual([
      expect.stringContaining('Broadband'),
      expect.stringContaining('Council Tax'),
    ])
    brasilRows = within(tables[0]).getAllByRole('row').slice(1)
    expect(brasilRows.map((r) => r.textContent)).toEqual([
      expect.stringContaining('Aluguel'),
      expect.stringContaining('INSS'),
    ])
  })
})
