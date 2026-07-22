import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MensaisPage from '../MensaisPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { RecurringBillInstanceDto } from '../../api/types'

const getMensaisInstancesMock = vi.fn<FinancialApiClient['getMensaisInstances']>()
const updateMensaisInstanceMock = vi.fn<FinancialApiClient['updateMensaisInstance']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getMensaisInstances: getMensaisInstancesMock,
    updateMensaisInstance: updateMensaisInstanceMock,
  }),
}))

const NOW = new Date()

const INSTANCES: RecurringBillInstanceDto[] = [
  {
    id: 'i1',
    templateId: 't1',
    year: NOW.getFullYear(),
    month: NOW.getMonth() + 1,
    dueDay: 10,
    description: 'INSS',
    area: 'Brasil',
    note: '',
    nitNumber: null,
    minimumWageValue: null,
    value: 850,
    status: 'Unset',
  },
  {
    id: 'i2',
    templateId: 't2',
    year: NOW.getFullYear(),
    month: NOW.getMonth() + 1,
    dueDay: 15,
    description: 'Council Tax',
    area: 'UK',
    note: '',
    nitNumber: null,
    minimumWageValue: null,
    value: 120,
    status: 'Unset',
  },
]

describe('MensaisPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getMensaisInstancesMock.mockResolvedValue(INSTANCES)
  })

  it('shows a loading state before data arrives', () => {
    render(<MensaisPage />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error state with retry when the fetch fails', async () => {
    getMensaisInstancesMock.mockRejectedValue(new Error('Network down'))

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
    updateMensaisInstanceMock.mockResolvedValue({ ...INSTANCES[0], status: 'Paid', value: 900 })
    render(<MensaisPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    const editButtons = screen.getAllByRole('button', { name: 'Edit' })
    fireEvent.click(editButtons[0])

    expect(screen.getByText('Edit Instance')).toBeInTheDocument()
    const valueInput = screen.getByDisplayValue('850')
    fireEvent.change(valueInput, { target: { value: '900' } })
    const statusSelect = screen.getByRole('combobox')
    fireEvent.change(statusSelect, { target: { value: 'Paid' } })

    getMensaisInstancesMock.mockResolvedValue([{ ...INSTANCES[0], status: 'Paid', value: 900 }, INSTANCES[1]])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateMensaisInstanceMock).toHaveBeenCalledWith('i1', { status: 'Paid', value: 900 }),
    )
    await waitFor(() => expect(screen.getByText('Paid')).toBeInTheDocument())
  })
})
