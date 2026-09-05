import { fireEvent, screen } from '@testing-library/react'
import { render } from '../../test/renderWithFluent'
import { describe, expect, it, vi } from 'vitest'
import IncomeSection from '../IncomeSection'
import type { IncomeDto } from '../../api/types'

const INCOMES: IncomeDto[] = [
  {
    id: 'i1',
    date: '2026-07-05',
    incomeSourceId: '1',
    incomeSourceName: 'Gleison',
    grossValue: 3200,
    netValue: 2450,
    bankId: 'bank-barclays',
    bankName: 'Barclays',
    description: 'Salary',
    splitToReserve: false,
  },
  {
    id: 'i2',
    date: '2026-07-06',
    incomeSourceId: '3',
    incomeSourceName: 'Lottery',
    grossValue: null,
    netValue: 50,
    bankId: 'bank-chase',
    bankName: 'Chase',
    description: null,
    splitToReserve: false,
  },
]

describe('IncomeSection', () => {
  it('renders a row per income entry, including a placeholder for a null gross value', () => {
    render(<IncomeSection incomes={INCOMES} onEdit={vi.fn()} onDelete={vi.fn()} onNewIncome={vi.fn()} />)

    expect(screen.getByText('Gleison')).toBeInTheDocument()
    expect(screen.getByText('Lottery')).toBeInTheDocument()
    expect(screen.getByText('3,200.00')).toBeInTheDocument()
    expect(screen.getByText('2,450.00')).toBeInTheDocument()
    expect(screen.getByText('50.00')).toBeInTheDocument()
    expect(screen.getByText('—')).toBeInTheDocument()
    expect(screen.getByText('Salary')).toBeInTheDocument()
  })

  it('renders a blank Bank cell for a bank-less income', () => {
    const bankLessIncome: IncomeDto = {
      id: 'i3',
      date: '2026-07-07',
      incomeSourceId: '4',
      incomeSourceName: 'DividendoJuros',
      grossValue: null,
      netValue: 42.5,
      bankId: null,
      bankName: null,
      description: null,
      splitToReserve: false,
    }
    render(<IncomeSection incomes={[bankLessIncome]} onEdit={vi.fn()} onDelete={vi.fn()} onNewIncome={vi.fn()} />)

    // one dash for the null gross value, one for the null bank
    expect(screen.getAllByText('—')).toHaveLength(2)
  })

  it('calls onEdit with the clicked income entry', () => {
    const onEdit = vi.fn()
    render(<IncomeSection incomes={INCOMES} onEdit={onEdit} onDelete={vi.fn()} onNewIncome={vi.fn()} />)

    fireEvent.click(screen.getAllByRole('button', { name: 'Edit income' })[0])

    expect(onEdit).toHaveBeenCalledWith(INCOMES[0])
  })

  it('calls onDelete with the clicked income entry id', () => {
    const onDelete = vi.fn()
    render(<IncomeSection incomes={INCOMES} onEdit={vi.fn()} onDelete={onDelete} onNewIncome={vi.fn()} />)

    fireEvent.click(screen.getAllByRole('button', { name: 'Delete income' })[1])

    expect(onDelete).toHaveBeenCalledWith('i2')
  })

  it('calls onNewIncome when the New Income button is clicked', () => {
    const onNewIncome = vi.fn()
    render(<IncomeSection incomes={INCOMES} onEdit={vi.fn()} onDelete={vi.fn()} onNewIncome={onNewIncome} />)

    fireEvent.click(screen.getByRole('button', { name: 'New Income' }))

    expect(onNewIncome).toHaveBeenCalledOnce()
  })

  it('shows the split confirmation message when present', () => {
    render(
      <IncomeSection
        incomes={INCOMES}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        onNewIncome={vi.fn()}
        splitConfirmationMessage="Income saved and split to reserve"
      />,
    )

    expect(screen.getByText('Income saved and split to reserve')).toBeInTheDocument()
  })

  it('renders nothing extra when the split confirmation message is absent', () => {
    render(<IncomeSection incomes={INCOMES} onEdit={vi.fn()} onDelete={vi.fn()} onNewIncome={vi.fn()} />)

    expect(screen.queryByText('Income saved and split to reserve')).not.toBeInTheDocument()
  })

  it('sorts rows by Net when the Net column header is clicked', () => {
    render(<IncomeSection incomes={INCOMES} onEdit={vi.fn()} onDelete={vi.fn()} onNewIncome={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Net' }))

    const sourceCells = screen
      .getAllByRole('row')
      .slice(1)
      .map((row) => row.querySelectorAll('td')[1].textContent)
    expect(sourceCells).toEqual(['Lottery', 'Gleison'])

    fireEvent.click(screen.getByRole('button', { name: 'Net' }))

    const sourceCellsDescending = screen
      .getAllByRole('row')
      .slice(1)
      .map((row) => row.querySelectorAll('td')[1].textContent)
    expect(sourceCellsDescending).toEqual(['Gleison', 'Lottery'])
  })

  it('filters rows by Bank via the header checklist, and shows the empty message when nothing matches', () => {
    render(<IncomeSection incomes={INCOMES} onEdit={vi.fn()} onDelete={vi.fn()} onNewIncome={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Bank' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Chase' }))

    expect(screen.queryByText('Lottery')).not.toBeInTheDocument()
    expect(screen.getByText('Gleison')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('checkbox', { name: 'Barclays' }))

    expect(screen.getByText('No rows match the current filters')).toBeInTheDocument()
  })
})
