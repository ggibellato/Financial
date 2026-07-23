import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MensaisPage from '../MensaisPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { RecurringBillDto } from '../../api/types'

const getMensaisBillsMock = vi.fn<FinancialApiClient['getMensaisBills']>()
const createMensaisBillMock = vi.fn<FinancialApiClient['createMensaisBill']>()
const updateMensaisBillMock = vi.fn<FinancialApiClient['updateMensaisBill']>()
const deleteMensaisBillMock = vi.fn<FinancialApiClient['deleteMensaisBill']>()
const resetMensaisToUnsetMock = vi.fn<FinancialApiClient['resetMensaisToUnset']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getMensaisBills: getMensaisBillsMock,
    createMensaisBill: createMensaisBillMock,
    updateMensaisBill: updateMensaisBillMock,
    deleteMensaisBill: deleteMensaisBillMock,
    resetMensaisToUnset: resetMensaisToUnsetMock,
  }),
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
    vi.clearAllMocks()
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

    await waitFor(() => expect(screen.getByText('Brasil')).toBeInTheDocument())
    expect(screen.getByText('UK')).toBeInTheDocument()
    expect(screen.getByText('INSS')).toBeInTheDocument()
    expect(screen.getByText('Council Tax')).toBeInTheDocument()
  })

  it('edits a row status/value via the toggled panel and saves, updating the displayed row', async () => {
    updateMensaisBillMock.mockResolvedValue({ ...BILLS[0], status: 'Paid', value: 900 })
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    const editButtons = screen.getAllByRole('button', { name: 'Edit' })
    fireEvent.click(editButtons[0])

    expect(screen.getByText('Edit Bill')).toBeInTheDocument()
    const valueInput = screen.getByDisplayValue('850')
    fireEvent.change(valueInput, { target: { value: '900' } })
    const statusSelect = screen.getByRole('combobox')
    fireEvent.change(statusSelect, { target: { value: 'Paid' } })

    getMensaisBillsMock.mockResolvedValue([{ ...BILLS[0], status: 'Paid', value: 900 }, BILLS[1]])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateMensaisBillMock).toHaveBeenCalledWith('b1', { status: 'Paid', value: 900 }),
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

    await waitFor(() => expect(screen.getByText('Brasil')).toBeInTheDocument())

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
    fireEvent.click(screen.getByRole('button', { name: 'Add' }))

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

  it('deletes a bill after confirming the prompt', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    deleteMensaisBillMock.mockResolvedValue(undefined)
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    getMensaisBillsMock.mockResolvedValue([BILLS[1]])
    fireEvent.click(screen.getAllByRole('button', { name: 'Delete' })[0])

    await waitFor(() => expect(deleteMensaisBillMock).toHaveBeenCalledWith('b1'))
    await waitFor(() => expect(screen.queryByText('INSS')).not.toBeInTheDocument())
  })

  it('does not delete when the confirmation prompt is declined', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    fireEvent.click(screen.getAllByRole('button', { name: 'Delete' })[0])

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
})
