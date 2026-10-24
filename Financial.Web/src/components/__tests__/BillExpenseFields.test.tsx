import { describe, expect, it, vi } from 'vitest'
import type { RecurringBillDto } from '../../api/types'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import BillExpenseFields from '../BillExpenseFields'
import { billExpenseValuesFor, parseBillExpense, type BillExpenseValues } from '../billExpenseValues'

const BILL: RecurringBillDto = {
  id: 'bill-1',
  dueDay: 5,
  description: 'Council Tax',
  value: 120,
  area: 'UK',
  note: '',
  status: 'Unset',
  minimumWageValue: null,
  nitNumber: null,
}

const VALID: BillExpenseValues = {
  date: '2026-10-05',
  description: ' Council Tax ',
  value: '120',
  bankId: 'bank-1',
  categoryId: 'cat-1',
}

const CATEGORIES = [{ id: 'cat-1', name: 'Home', active: true, isInvestment: false, isTithe: false, hasReferences: true }]

describe('billExpenseValuesFor', () => {
  it('pre-fills the description and value from the bill, with no category', () => {
    expect(billExpenseValuesFor(BILL, '2026-10-05', 'bank-1')).toEqual({
      date: '2026-10-05',
      description: 'Council Tax',
      value: '120',
      bankId: 'bank-1',
      categoryId: '',
    })
  })
})

describe('parseBillExpense', () => {
  it('returns the trimmed expense values when every field is valid', () => {
    expect(parseBillExpense(VALID)).toEqual({
      date: '2026-10-05',
      description: 'Council Tax',
      value: 120,
      bankId: 'bank-1',
      categoryId: 'cat-1',
    })
  })

  it.each([
    ['an empty description', { description: '  ' }],
    ['a zero value', { value: '0' }],
    ['a non-numeric value', { value: 'abc' }],
    ['no bank', { bankId: '' }],
    ['no category', { categoryId: '' }],
  ])('returns null for %s', (_, change) => {
    expect(parseBillExpense({ ...VALID, ...change })).toBeNull()
  })
})

describe('BillExpenseFields', () => {
  it('shows a locked bank as text instead of a bank dropdown', () => {
    render(
      <BillExpenseFields
        values={VALID}
        banks={[]}
        categories={CATEGORIES}
        lockedBank={{ id: 'bank-1', name: 'Barclays' }}
        onFieldChange={vi.fn()}
      />,
    )

    expect(screen.getByText('Barclays')).toBeInTheDocument()
    expect(screen.queryByRole('combobox', { name: /^Bank/ })).not.toBeInTheDocument()
  })

  it('reports each field change', () => {
    const onFieldChange = vi.fn()
    render(<BillExpenseFields values={VALID} banks={[]} categories={CATEGORIES} onFieldChange={onFieldChange} />)

    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '95' } })
    fireEvent.change(screen.getByLabelText(/^Category/), { target: { value: '' } })

    expect(onFieldChange).toHaveBeenCalledWith('value', '95')
    expect(onFieldChange).toHaveBeenCalledWith('categoryId', '')
  })
})
