import { Toaster } from '@fluentui/react-components'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { ApiError } from '../../api/apiError'
import type { IncomeDto, IncomeSourceDto } from '../../api/types'
import { APP_TOASTER_ID } from '../../hooks/useSuccessToast'
import { account, potsAccount } from '../../test/account'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import { confirmDelete, requestDelete } from '../../test-utils/confirmDelete'
import { never } from '../../test-utils/deferred'
import { pinDate } from '../../test-utils/pinDate'
import IncomeTab from '../IncomeTab'

const { getIncomesByMonthMock, getIncomeSourcesMock, getAccountsMock, createIncomeMock, updateIncomeMock, deleteIncomeMock } = vi.hoisted(() => ({
  getIncomesByMonthMock: vi.fn<FinancialApiClient['getIncomesByMonth']>(),
  getIncomeSourcesMock: vi.fn<FinancialApiClient['getIncomeSources']>(),
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
  createIncomeMock: vi.fn<FinancialApiClient['createIncome']>(),
  updateIncomeMock: vi.fn<FinancialApiClient['updateIncome']>(),
  deleteIncomeMock: vi.fn<FinancialApiClient['deleteIncome']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getIncomesByMonth: getIncomesByMonthMock,
    getIncomeSources: getIncomeSourcesMock,
    getAccounts: getAccountsMock,
    createIncome: createIncomeMock,
    updateIncome: updateIncomeMock,
    deleteIncome: deleteIncomeMock,
  } as Partial<FinancialApiClient>,
}))

const AC = (n: string) => `[AC P61-F02-income-tab-and-bankless-income-${n}]`

const SOURCES: IncomeSourceDto[] = [
  { id: 'gleison', name: 'Gleison', isActive: true, group: 'Salary', potsAccountId: null, hasReferences: true },
  { id: 'ariana', name: 'Ariana', isActive: true, group: 'Salary', potsAccountId: 'pots', hasReferences: true },
  { id: 'dividends', name: 'DividendoJuros', isActive: true, group: 'DividendoJuros', potsAccountId: null, hasReferences: true },
]

const ACCOUNTS = [
  account({ id: 'barclays', name: 'Barclays' }),
  account({ id: 'card', name: 'Amex', capabilities: ['CanPayBills'] }),
  potsAccount({ id: 'pots', name: 'Reserva' }),
]

function income(fields: Partial<IncomeDto> & Pick<IncomeDto, 'id' | 'date'>): IncomeDto {
  return {
    incomeSourceId: 'dividends',
    incomeSourceName: 'DividendoJuros',
    grossValue: null,
    netValue: 0,
    bankId: null,
    bankName: null,
    description: null,
    potsAccountId: null,
    hasLinkedDeposit: false,
    fundingLines: [],
    ...fields,
  }
}

const SALARY = income({
  id: 'salary',
  date: '2026-10-28',
  incomeSourceId: 'gleison',
  incomeSourceName: 'Gleison',
  grossValue: 4500,
  netValue: 4100,
  bankId: 'barclays',
  bankName: 'Barclays',
})
const DIVIDENDS = income({ id: 'dividends-1', date: '2026-10-15', netValue: 85.2, description: 'XP dividends' })
const INTEREST = income({ id: 'interest', date: '2026-10-02', netValue: 0.1, description: 'Interest' })
const INCOMES = [DIVIDENDS, SALARY, INTEREST]

function renderTab() {
  return render(
    <>
      <IncomeTab year={2026} month={10} />
      <Toaster toasterId={APP_TOASTER_ID} />
    </>,
  )
}

async function grid() {
  const table = await screen.findByRole('table', { name: 'Incomes' })
  await waitFor(() => expect(within(table).queryByLabelText('Loading incomes')).not.toBeInTheDocument())
  return table
}

function bodyRows(table: HTMLElement) {
  return within(table).getAllByRole('row').slice(1)
}

function cellTexts(row: HTMLElement) {
  return within(row).getAllByRole('cell').slice(0, 6).map((cell) => cell.textContent)
}

function footer() {
  return screen.getByRole('group', { name: 'Totals for Incomes' })
}

function filterTo(label: string, value: string) {
  fireEvent.click(screen.getByRole('button', { name: `Filter by ${label}` }))
  fireEvent.click(screen.getByRole('checkbox', { name: '(All)' }))
  fireEvent.click(screen.getByRole('checkbox', { name: value }))
}

function discardPrompt() {
  return screen.findByRole('dialog', { name: 'Discard unsaved changes?', hidden: true })
}

async function openNewIncome() {
  await grid()
  fireEvent.click(screen.getByRole('button', { name: 'New income' }))
  return screen.findByRole('form', { name: 'New income' })
}

function field(form: HTMLElement, label: string) {
  return within(form).getByLabelText(new RegExp(`^${label}`))
}

function setField(form: HTMLElement, label: string, value: string) {
  fireEvent.change(field(form, label), { target: { value } })
}

describe('IncomeTab', () => {
  beforeEach(() => {
    pinDate('2026-10-20T12:00:00+01:00')
    sessionStorage.clear()
    getIncomesByMonthMock.mockReset().mockResolvedValue(INCOMES)
    getIncomeSourcesMock.mockReset().mockResolvedValue(SOURCES)
    getAccountsMock.mockReset().mockResolvedValue(ACCOUNTS)
    createIncomeMock.mockReset()
    updateIncomeMock.mockReset()
    deleteIncomeMock.mockReset()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it(`${AC('01')} lists every income newest first with No bank, a dash for no gross value, and a count with Total net`, async () => {
    renderTab()
    const table = await grid()

    expect(getIncomesByMonthMock).toHaveBeenCalledExactlyOnceWith(2026, 10)
    expect(bodyRows(table).map(cellTexts)).toEqual([
      ['Date:28/10/2026', 'Source:Gleison', 'Bank:Barclays', 'Description:', 'Gross:4,500.00', 'Net:4,100.00'],
      ['Date:15/10/2026', 'Source:DividendoJuros', 'Bank:No bank', 'Description:XP dividends', 'Gross:—', 'Net:85.20'],
      ['Date:02/10/2026', 'Source:DividendoJuros', 'Bank:No bank', 'Description:Interest', 'Gross:—', 'Net:0.10'],
    ])
    expect(footer()).toHaveTextContent('3 incomes · Total net 4,185.30')
  })

  it(`${AC('01')} filters by Bank and Source, showing the filtered count of all incomes`, async () => {
    renderTab()
    await grid()

    filterTo('Bank', 'No bank')
    expect(footer()).toHaveTextContent('2 of 3 incomes · Total net 85.30')

    filterTo('Source', 'Gleison')
    expect(screen.getByText('No rows match the current filters.')).toBeInTheDocument()
  })

  it(`${AC('01')} counts a single income as "1 income"`, async () => {
    getIncomesByMonthMock.mockResolvedValue([DIVIDENDS])
    renderTab()
    await grid()

    expect(footer()).toHaveTextContent('1 income · Total net 85.20')
  })

  it(`${AC('02')} starts New income on No bank and saves it with no bank, toasting and refreshing the list`, async () => {
    createIncomeMock.mockResolvedValue(income({ id: 'new', date: '2026-10-20', netValue: 42.1 }))
    renderTab()

    const form = await openNewIncome()
    expect(within(form).getByLabelText('Bank')).toHaveValue('')
    expect(within(form).getByRole('option', { name: '— No bank —' })).toBeInTheDocument()
    setField(form, 'Source', 'dividends')
    setField(form, 'Net value', '42.10')
    fireEvent.click(within(form).getByRole('button', { name: 'Add income' }))

    expect(await screen.findAllByText('Income added')).not.toHaveLength(0)
    expect(createIncomeMock).toHaveBeenCalledWith(expect.objectContaining({ bankId: null, netValue: 42.1, incomeSourceId: 'dividends' }))
    expect(screen.queryByRole('form', { name: 'New income' })).not.toBeInTheDocument()
    expect(getIncomesByMonthMock).toHaveBeenCalledTimes(2)
  })

  it(`${AC('03')} offers only active accounts that receive income and saves a bank income`, async () => {
    createIncomeMock.mockResolvedValue(SALARY)
    renderTab()

    const form = await openNewIncome()
    expect(within(within(form).getByLabelText('Bank')).getAllByRole('option').map((option) => option.textContent)).toEqual([
      '— No bank —',
      'Barclays',
    ])
    setField(form, 'Bank', 'barclays')
    setField(form, 'Source', 'gleison')
    setField(form, 'Net value', '4100')
    fireEvent.click(within(form).getByRole('button', { name: 'Add income' }))

    await waitFor(() => expect(createIncomeMock).toHaveBeenCalledWith(expect.objectContaining({ bankId: 'barclays' })))
  })

  it(`${AC('04')} edits a bank income to No bank`, async () => {
    updateIncomeMock.mockResolvedValue({ ...SALARY, bankId: null, bankName: null })
    renderTab()
    await grid()

    fireEvent.click(screen.getByRole('button', { name: 'Edit income 28/10/2026 Gleison' }))
    const form = await screen.findByRole('form', { name: 'Edit income' })
    expect(within(form).getByLabelText('Bank')).toHaveValue('barclays')
    setField(form, 'Bank', '')
    fireEvent.click(within(form).getByRole('button', { name: 'Save' }))

    expect(await screen.findAllByText('Income updated')).not.toHaveLength(0)
    expect(updateIncomeMock).toHaveBeenCalledWith('salary', expect.objectContaining({ bankId: null }))
  })

  it(`${AC('04')} keeps an inactive bank on an income being edited, and does not offer it for a new one`, async () => {
    getAccountsMock.mockResolvedValue([...ACCOUNTS, account({ id: 'old', name: 'Old bank', isActive: false })])
    getIncomesByMonthMock.mockResolvedValue([{ ...SALARY, bankId: 'old', bankName: 'Old bank' }])
    renderTab()
    await grid()

    fireEvent.click(screen.getByRole('button', { name: 'Edit income 28/10/2026 Gleison' }))
    const form = await screen.findByRole('form', { name: 'Edit income' })
    expect(field(form, 'Bank')).toHaveValue('old')
    expect(within(field(form, 'Bank')).getAllByRole('option').map((option) => option.textContent)).toEqual(['— No bank —', 'Barclays', 'Old bank'])
  })

  it(`${AC('04')} deletes an income after confirmation, toasting and refreshing the list`, async () => {
    deleteIncomeMock.mockResolvedValue()
    renderTab()

    const dialog = await requestDelete('income 15/10/2026 DividendoJuros, XP dividends', 'income')
    expect(dialog).toHaveTextContent('Delete this income? This cannot be undone.')
    fireEvent.click(within(dialog).getByRole('button', { name: 'Delete', hidden: true }))

    expect(await screen.findAllByText('Income deleted')).not.toHaveLength(0)
    expect(deleteIncomeMock).toHaveBeenCalledExactlyOnceWith('dividends-1')
    expect(getIncomesByMonthMock).toHaveBeenCalledTimes(2)
  })

  it(`${AC('04')} warns that a linked pots deposit is deleted with its income`, async () => {
    getIncomesByMonthMock.mockResolvedValue([{ ...SALARY, potsAccountId: 'pots', hasLinkedDeposit: true }])
    renderTab()

    const dialog = await requestDelete('income 28/10/2026 Gleison', 'income')

    expect(dialog).toHaveTextContent('Delete this income? Its Pots deposit is deleted with it. This cannot be undone.')
  })

  it(`${AC('05')} asks for a bank before splitting to pots and sends nothing`, async () => {
    renderTab()

    const form = await openNewIncome()
    setField(form, 'Source', 'ariana')
    setField(form, 'Net value', '1000')
    fireEvent.click(within(form).getByRole('button', { name: 'Add income' }))

    expect(await within(form).findByText('Choose the bank this income landed in.')).toBeInTheDocument()
    expect(createIncomeMock).not.toHaveBeenCalled()
  })

  it(`${AC('06')} shows a field message for a missing net value and keeps the form open`, async () => {
    renderTab()

    const form = await openNewIncome()
    fireEvent.click(within(form).getByRole('button', { name: 'Add income' }))

    expect(await within(form).findByText('Net value must be a non-negative number.')).toBeInTheDocument()
    expect(createIncomeMock).not.toHaveBeenCalled()
  })

  it(`${AC('06')} keeps the values and shows the server message when a save fails`, async () => {
    createIncomeMock.mockRejectedValue(new ApiError('Income source is required.', 400))
    renderTab()

    const form = await openNewIncome()
    setField(form, 'Net value', '10')
    fireEvent.click(within(form).getByRole('button', { name: 'Add income' }))

    expect(await within(form).findByText('Income source is required.')).toBeInTheDocument()
    expect(field(form, 'Net value')).toHaveValue(10)
  })

  it(`${AC('06')} closes the form, refreshes and warns when the edited income no longer exists`, async () => {
    updateIncomeMock.mockRejectedValue(new ApiError('Not found.', 404))
    renderTab()
    await grid()

    fireEvent.click(screen.getByRole('button', { name: 'Edit income 28/10/2026 Gleison' }))
    fireEvent.click(within(await screen.findByRole('form', { name: 'Edit income' })).getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('This income no longer exists; the list was refreshed.')).toBeInTheDocument()
    expect(screen.queryByRole('form', { name: 'Edit income' })).not.toBeInTheDocument()
    expect(getIncomesByMonthMock).toHaveBeenCalledTimes(2)

    fireEvent.click(screen.getByRole('button', { name: 'Dismiss warning' }))
    expect(screen.queryByText('This income no longer exists; the list was refreshed.')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'New income' })).toHaveFocus()
  })

  it(`${AC('06')} closes the dialog, refreshes and warns when the deleted income no longer exists`, async () => {
    deleteIncomeMock.mockRejectedValue(new ApiError('Not found.', 404))
    renderTab()

    await confirmDelete('income 15/10/2026 DividendoJuros, XP dividends', 'income')

    expect(await screen.findByText('This income no longer exists; the list was refreshed.')).toBeInTheDocument()
    expect(screen.queryByText('Income deleted')).not.toBeInTheDocument()
    expect(getIncomesByMonthMock).toHaveBeenCalledTimes(2)
  })

  it(`${AC('06')} keeps the dialog open with the message when a delete fails`, async () => {
    deleteIncomeMock.mockRejectedValue(new ApiError('Server unavailable.', 500))
    renderTab()

    await confirmDelete('income 15/10/2026 DividendoJuros, XP dividends', 'income')

    const dialog = await screen.findByRole('dialog', { name: 'Delete income', hidden: true })
    expect(await within(dialog).findByText('Server unavailable.')).toBeInTheDocument()
    expect(getIncomesByMonthMock).toHaveBeenCalledOnce()
  })

  it(`${AC('09')} shows a loading skeleton with New income enabled`, () => {
    getIncomesByMonthMock.mockReturnValue(never())
    renderTab()

    expect(screen.getByLabelText('Loading incomes')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'New income' })).toBeEnabled()
  })

  it(`${AC('09')} shows the empty month with New income`, async () => {
    getIncomesByMonthMock.mockResolvedValue([])
    renderTab()

    expect(await screen.findByText('No income in October 2026.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'New income' })).toBeInTheDocument()
    expect(screen.queryByRole('group', { name: 'Totals for Incomes' })).not.toBeInTheDocument()
  })

  it(`${AC('09')} shows the load error with Try again, which reloads and moves focus into the tab`, async () => {
    getIncomesByMonthMock.mockRejectedValueOnce('offline')
    renderTab()

    expect(await screen.findByText('Unable to load the incomes.')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    await grid()
    expect(getIncomesByMonthMock).toHaveBeenCalledTimes(2)
    await waitFor(() => expect(screen.getByRole('button', { name: 'New income' })).toHaveFocus())
  })

  it(`${AC('09')} names every row icon after its income and blocks the others while one deletes`, async () => {
    deleteIncomeMock.mockReturnValue(never())
    renderTab()

    await confirmDelete('income 15/10/2026 DividendoJuros, XP dividends', 'income')

    expect(await screen.findByRole('button', { name: 'Deleting income 15/10/2026 DividendoJuros, XP dividends' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Edit income 28/10/2026 Gleison' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Delete income 28/10/2026 Gleison' })).toHaveAccessibleDescription('Another delete is in progress.')
    expect(screen.getByRole('button', { name: 'Edit income 28/10/2026 Gleison' })).toBeDisabled()
  })

  it(`${AC('09')} blocks Delete with a reason and disables Edit while a save is in flight`, async () => {
    updateIncomeMock.mockReturnValue(never())
    renderTab()
    await grid()

    fireEvent.click(screen.getByRole('button', { name: 'Edit income 28/10/2026 Gleison' }))
    fireEvent.click(within(await screen.findByRole('form', { name: 'Edit income' })).getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('button', { name: 'Edit income 15/10/2026 DividendoJuros, XP dividends' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Delete income 15/10/2026 DividendoJuros, XP dividends' })).toHaveAccessibleDescription('Wait for the save to finish.')
  })

  it(`${AC('09')} moves focus to the first field on open and back to New income on Cancel`, async () => {
    renderTab()

    const form = await openNewIncome()
    await waitFor(() => expect(field(form, 'Date')).toHaveFocus())
    fireEvent.click(within(form).getByRole('button', { name: 'Cancel' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New income' })).toHaveFocus())
  })

  it('asks before discarding unsaved changes on Cancel, and closes at once with none', async () => {
    renderTab()

    const form = await openNewIncome()
    setField(form, 'Net value', '12')
    fireEvent.click(within(form).getByRole('button', { name: 'Cancel' }))
    fireEvent.click(await within(await discardPrompt()).findByRole('button', { name: 'Keep editing', hidden: true }))
    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Discard unsaved changes?', hidden: true })).not.toBeInTheDocument())
    expect(field(form, 'Net value')).toHaveValue(12)

    setField(form, 'Net value', '')
    fireEvent.click(within(form).getByRole('button', { name: 'Cancel' }))
    expect(screen.queryByRole('form', { name: 'New income' })).not.toBeInTheDocument()
  })

  it('asks before discarding unsaved changes when another income is opened', async () => {
    renderTab()

    const form = await openNewIncome()
    setField(form, 'Net value', '12')
    fireEvent.click(screen.getByRole('button', { name: 'Edit income 28/10/2026 Gleison' }))

    fireEvent.click(await within(await discardPrompt()).findByRole('button', { name: 'Discard', hidden: true }))

    expect(await screen.findByRole('form', { name: 'Edit income' })).toBeInTheDocument()
  })
})
