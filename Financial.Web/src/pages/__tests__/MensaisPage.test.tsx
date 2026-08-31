import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MensaisPage from '../MensaisPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { RecurringBillDto } from '../../api/types'

const {
  getMensaisBillsMock,
  createMensaisBillMock,
  updateMensaisBillMock,
  deleteMensaisBillMock,
  resetMensaisToUnsetMock,
} = vi.hoisted(() => ({
  getMensaisBillsMock: vi.fn<FinancialApiClient['getMensaisBills']>(),
  createMensaisBillMock: vi.fn<FinancialApiClient['createMensaisBill']>(),
  updateMensaisBillMock: vi.fn<FinancialApiClient['updateMensaisBill']>(),
  deleteMensaisBillMock: vi.fn<FinancialApiClient['deleteMensaisBill']>(),
  resetMensaisToUnsetMock: vi.fn<FinancialApiClient['resetMensaisToUnset']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getMensaisBills: getMensaisBillsMock,
    createMensaisBill: createMensaisBillMock,
    updateMensaisBill: updateMensaisBillMock,
    deleteMensaisBill: deleteMensaisBillMock,
    resetMensaisToUnset: resetMensaisToUnsetMock,
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

describe('MensaisPage', () => {
  beforeEach(() => {
    getMensaisBillsMock.mockReset()
    createMensaisBillMock.mockReset()
    updateMensaisBillMock.mockReset()
    deleteMensaisBillMock.mockReset()
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
    fireEvent.change(screen.getByLabelText('Description'), { target: { value: 'Aluguel' } })
    fireEvent.change(screen.getByLabelText('Due Day'), { target: { value: '5' } })
    fireEvent.change(screen.getByLabelText('Value'), { target: { value: '1000' } })

    getMensaisBillsMock.mockResolvedValue([
      ...BILLS,
      { ...BILLS[0], id: 'b3', description: 'Aluguel', dueDay: 5, value: 1000 },
    ])
    const addBillFormPanel = screen.getByText('Add Bill', { selector: 'p' }).closest('.mensais-page__form-panel') as HTMLElement
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

    const addBillFormPanel = screen.getByText('Add Bill', { selector: 'p' }).closest('.mensais-page__form-panel') as HTMLElement
    const fieldLabels = within(addBillFormPanel)
      .getAllByText(/^(Area|Description|Due Day|Value|Note)$/, { selector: 'label' })
      .map((label) => label.textContent)
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
