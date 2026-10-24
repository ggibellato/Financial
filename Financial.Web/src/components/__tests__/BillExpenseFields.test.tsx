import { describe, expect, it, vi } from 'vitest'
import { recurringBill } from '../../test/recurringBill'
import { optionNames } from '../../test-utils/optionNames'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import BillExpenseFields from '../BillExpenseFields'
import {
  billExpenseValuesFor,
  mapBillExpenseErrorToField,
  parseBillExpense,
  toBillPaymentExpense,
  validateBillExpense,
  type BillExpenseValues,
} from '../billExpenseValues'

const BILL = recurringBill({ id: 'bill-1', dueDay: 5, description: 'Council Tax', value: 120 })

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

describe('validateBillExpense', () => {
  it('names every invalid field, a missing date included', () => {
    expect(validateBillExpense({ date: '', description: ' ', value: '-1', bankId: '', categoryId: '' })).toEqual({
      date: 'Enter a payment date.',
      description: 'Enter a description.',
      value: 'Enter a value greater than 0.',
      bankId: 'Choose a bank.',
      categoryId: 'Choose a category.',
    })
    expect(validateBillExpense(VALID)).toEqual({})
  })
})

describe('toBillPaymentExpense', () => {
  it('sends the bank as the payment source', () => {
    expect(toBillPaymentExpense({ date: '2026-10-05', description: 'Council Tax', value: 120, bankId: 'bank-1', categoryId: 'cat-1' })).toEqual({
      date: '2026-10-05',
      description: 'Council Tax',
      value: 120,
      categoryId: 'cat-1',
      paymentSourceBankId: 'bank-1',
    })
  })
})

describe('mapBillExpenseErrorToField', () => {
  it.each([
    ['Date must not be in the future.', 'date'],
    ['Description must be 200 characters or fewer.', 'description'],
    ['Value must be greater than zero.', 'value'],
    ['Category is inactive.', 'categoryId'],
    ['Bank is inactive.', null],
  ])('maps "%s" to %s', (message, field) => {
    expect(mapBillExpenseErrorToField(message)).toBe(field)
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

  it('accepts an empty date, as the Mensais prompt always has', () => {
    expect(parseBillExpense({ ...VALID, date: '' })).not.toBeNull()
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

const BARCLAYS = { id: 'bank-1', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true, isActive: true }
const BANKS = [BARCLAYS, { ...BARCLAYS, id: 'bank-chase', name: 'Chase', isActive: false }]

describe('BillExpenseFields', () => {
  it('shows a locked bank as text instead of a bank dropdown', () => {
    render(
      <BillExpenseFields
        values={VALID}
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
    render(<BillExpenseFields values={VALID} categories={CATEGORIES} onFieldChange={onFieldChange} />)

    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '95' } })
    fireEvent.change(screen.getByLabelText(/^Category/), { target: { value: '' } })

    expect(onFieldChange).toHaveBeenCalledWith('value', '95')
    expect(onFieldChange).toHaveBeenCalledWith('categoryId', '')
  })

  it('[AC P60-F06-admin-accounts-page-05] leaves an inactive bank out of Bank', () => {
    render(<BillExpenseFields values={VALID} banks={BANKS} categories={CATEGORIES} onFieldChange={vi.fn()} />)

    expect(optionNames(screen.getByLabelText(/^Bank/))).toEqual(['Select bank…', 'Barclays'])
  })

  it('[AC P60-F06-admin-accounts-page-05] keeps an inactive bank that is already selected', () => {
    render(<BillExpenseFields values={{ ...VALID, bankId: 'bank-chase' }} banks={BANKS} categories={CATEGORIES} onFieldChange={vi.fn()} />)

    expect(screen.getByLabelText(/^Bank/)).toHaveValue('bank-chase')
  })
})
