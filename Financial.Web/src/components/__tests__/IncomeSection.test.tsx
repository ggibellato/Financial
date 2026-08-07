import { fireEvent, render, screen } from '@testing-library/react'
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
})
