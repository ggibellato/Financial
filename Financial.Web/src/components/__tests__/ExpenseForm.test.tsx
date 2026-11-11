import { fireEvent, screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { render } from '../../test/renderWithFluent'
import ExpenseForm from '../ExpenseForm'
import type { BankOption, CategoryDto } from '../../api/types'
import { optionNames } from '../../test-utils/optionNames'
import { todayIsoDate } from '../../utils/formatters'
import { getErrorAlert } from '../../test-utils/errorAlert'

const BANKS: BankOption[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, isActive: true },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, isActive: true },
]

const CATEGORIES: CategoryDto[] = [
  { id: 'category-mercado', name: 'Mercado', active: true, isInvestment: false, isTithe: false, hasReferences: false },
  { id: 'category-casa', name: 'Casa', active: true, isInvestment: false, isTithe: false, hasReferences: false },
  { id: 'category-dizimo', name: 'Dizimo', active: true, isInvestment: false, isTithe: true, hasReferences: false },
]

const INACTIVE_BANK: BankOption = { ...BANKS[0], id: 'bank-chase', name: 'Chase', isActive: false }

const baseProps = {
  isEditing: false,
  date: '',
  description: '',
  value: '',
  categoryId: 'category-mercado',
  paymentSource: 'bank-barclays',
  creditCardId: '',
  creditCardName: '',
  invoiceDate: '',
  invoiceMonthMovedFrom: null,
  roundUpAmount: '',
  countsAsTithe: true,
  paymentMode: 'bank' as const,
  banks: BANKS,
  categories: CATEGORIES,
  isSettled: false,
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
  onFieldChange: vi.fn(),
  onSave: vi.fn(),
  onCancel: vi.fn(),
}

describe('ExpenseForm', () => {
  it('renders the create form with empty date/description/value fields', () => {
    render(<ExpenseForm {...baseProps} />)

    expect(screen.getByText('New expense')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Date/)).toHaveValue('')
    expect(screen.getByLabelText(/^Description/)).toHaveValue('')
    expect(screen.getByLabelText(/^Value/)).toHaveValue(null)
    expect(screen.getByRole('button', { name: 'Add expense' })).toBeInTheDocument()
  })

  it('shows the settlement note and hides payment fields when settled', () => {
    render(
      <ExpenseForm
        {...baseProps}
        isEditing
        isSettled
        paymentSource="bank-trading212"
        creditCardId="card-baamex"
        creditCardName="BaAmex"
      />,
    )

    expect(screen.getByText(/locked while its card statement is paid/)).toBeInTheDocument()
    expect(screen.queryByLabelText('Payment source')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Card')).not.toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('says a charge with no bank was paid through its card statement', () => {
    render(<ExpenseForm {...baseProps} isEditing isSettled paymentMode="card" paymentSource="" creditCardId="card-baamex" creditCardName="BaAmex" />)

    expect(
      screen.getByText(
        "Paid through BaAmex's statement. Date, value, card and invoice month are locked while the statement is paid — unmark the statement to change them.",
      ),
    ).toBeInTheDocument()
    expect(screen.queryByText(/Paid by/)).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Card')).not.toBeInTheDocument()
  })

  it('shows the bank picker and no toggle in bank mode', () => {
    render(<ExpenseForm {...baseProps} paymentMode="bank" />)
    expect(screen.getByLabelText(/^Payment source/)).toBeInTheDocument()
    expect(screen.queryByLabelText(/^Card/)).not.toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it("shows the charge's card as read-only text and no toggle in card mode", () => {
    render(<ExpenseForm {...baseProps} paymentMode="card" creditCardId="card-baamex" creditCardName="BaAmex" />)
    expect(screen.queryByLabelText(/^Payment source/)).not.toBeInTheDocument()
    expect(screen.getByText('BaAmex')).toBeInTheDocument()
    expect(screen.queryByRole('combobox', { name: /^Card/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('lists exactly the categories passed in via the categories prop, by name', () => {
    render(<ExpenseForm {...baseProps} />)

    const categorySelect = screen.getByLabelText(/^Category/)
    expect(within(categorySelect).getByRole('option', { name: 'Mercado' })).toBeInTheDocument()
    expect(within(categorySelect).getByRole('option', { name: 'Casa' })).toBeInTheDocument()
    expect(within(categorySelect).getAllByRole('option')).toHaveLength(CATEGORIES.length)
  })

  it('submits the selected category id, not its name', () => {
    const onFieldChange = vi.fn()
    render(<ExpenseForm {...baseProps} onFieldChange={onFieldChange} />)

    fireEvent.change(screen.getByLabelText(/^Category/), { target: { value: 'category-casa' } })

    expect(onFieldChange).toHaveBeenCalledWith('categoryId', 'category-casa')
  })

  it('shows an editable invoice month field pre-filled from the date when card mode is selected', () => {
    render(<ExpenseForm {...baseProps} paymentMode="card" date="2026-07-15" />)

    const invoiceField = screen.getByLabelText('Invoice month')
    expect(invoiceField).toBeInTheDocument()
    expect(invoiceField).not.toBeDisabled()
    expect(invoiceField).toHaveValue('2026-07')
  })

  it('persists a changed invoice month while unpaid', () => {
    const onFieldChange = vi.fn()
    render(<ExpenseForm {...baseProps} paymentMode="card" date="2026-07-15" onFieldChange={onFieldChange} />)

    fireEvent.change(screen.getByLabelText('Invoice month'), { target: { value: '2026-08' } })

    expect(onFieldChange).toHaveBeenCalledWith('invoiceDate', '2026-08')
  })

  it('shows the invoice month field read-only, still focusable, once settled', () => {
    render(
      <ExpenseForm
        {...baseProps}
        isEditing
        isSettled
        paymentSource="bank-trading212"
        creditCardId="card-baamex"
        creditCardName="BaAmex"
        invoiceDate="2026-07"
      />,
    )

    const invoiceField = screen.getByLabelText('Invoice month')
    expect(invoiceField).toHaveAttribute('readonly')
    expect(invoiceField).toBeEnabled()
    expect(invoiceField).toHaveValue('2026-07')
  })

  it('locks date and value on a settled charge while description and category stay editable', () => {
    render(
      <ExpenseForm
        {...baseProps}
        isEditing
        isSettled
        paymentSource="bank-trading212"
        creditCardId="card-baamex"
        creditCardName="BaAmex"
      />,
    )

    expect(screen.getByLabelText(/^Date/)).toHaveAttribute('readonly')
    expect(screen.getByLabelText(/^Value/)).toHaveAttribute('readonly')
    expect(screen.getAllByText('Locked: its card statement is paid')).toHaveLength(2)
    expect(screen.getByLabelText(/^Description/)).toBeEnabled()
    expect(screen.getByLabelText(/^Category/)).toBeEnabled()
  })

  it('keeps date and value editable on an unsettled charge', () => {
    render(<ExpenseForm {...baseProps} isEditing paymentMode="card" creditCardId="card-baamex" />)

    expect(screen.getByLabelText(/^Date/)).not.toHaveAttribute('readonly')
    expect(screen.getByLabelText(/^Value/)).not.toHaveAttribute('readonly')
  })

  it("defaults the invoice month to the date's month", () => {
    render(<ExpenseForm {...baseProps} paymentMode="card" date="2026-07-15" creditCardId="card-baamex" />)

    expect(screen.getByLabelText('Invoice month')).toHaveValue('2026-07')
  })

  it('hides the invoice month field in bank mode', () => {
    render(<ExpenseForm {...baseProps} paymentMode="bank" />)

    expect(screen.queryByLabelText('Invoice month')).not.toBeInTheDocument()
  })

  it('shows the round-up field only for a round-up-enabled bank in bank mode', () => {
    const { rerender } = render(<ExpenseForm {...baseProps} paymentMode="bank" paymentSource="bank-barclays" />)
    expect(screen.queryByLabelText('Round-up')).not.toBeInTheDocument()

    rerender(<ExpenseForm {...baseProps} paymentMode="bank" paymentSource="bank-trading212" />)
    expect(screen.getByLabelText('Round-up')).toBeInTheDocument()

    rerender(<ExpenseForm {...baseProps} paymentMode="card" paymentSource="bank-trading212" />)
    expect(screen.queryByLabelText('Round-up')).not.toBeInTheDocument()
  })

  it('hides the counts-toward-tithe checkbox for a non-tithe category', () => {
    render(<ExpenseForm {...baseProps} categoryId="category-mercado" />)

    expect(screen.queryByLabelText('Counts toward tithe')).not.toBeInTheDocument()
  })

  it('shows the counts-toward-tithe checkbox, checked by default, for the tithe category', () => {
    render(<ExpenseForm {...baseProps} categoryId="category-dizimo" countsAsTithe />)

    expect(screen.getByLabelText('Counts toward tithe')).toBeChecked()
  })

  it('reports unchecking the counts-toward-tithe checkbox', () => {
    const onFieldChange = vi.fn()
    render(
      <ExpenseForm {...baseProps} categoryId="category-dizimo" countsAsTithe onFieldChange={onFieldChange} />,
    )

    fireEvent.click(screen.getByLabelText('Counts toward tithe'))

    expect(onFieldChange).toHaveBeenCalledWith('countsAsTithe', 'false')
  })

  it('reports checking the counts-toward-tithe checkbox', () => {
    const onFieldChange = vi.fn()
    render(
      <ExpenseForm
        {...baseProps}
        categoryId="category-dizimo"
        countsAsTithe={false}
        onFieldChange={onFieldChange}
      />,
    )

    fireEvent.click(screen.getByLabelText('Counts toward tithe'))

    expect(onFieldChange).toHaveBeenCalledWith('countsAsTithe', 'true')
  })

  it('shows each bank-mode field its own validation message', () => {
    render(
      <ExpenseForm
        {...baseProps}
        paymentSource="bank-trading212"
        saveErrorFields={{
          date: 'Date is required.',
          description: 'Description is required.',
          paymentSource: 'Choose a payment source',
          value: 'Value is required',
          roundUpAmount: 'Round-up is too large',
        }}
      />,
    )

    for (const message of [
      'Date is required.',
      'Description is required.',
      'Choose a payment source',
      'Value is required',
      'Round-up is too large',
    ]) {
      expect(screen.getByText(message)).toBeInTheDocument()
    }
  })

  it('shows the card field its validation message in card mode', () => {
    render(<ExpenseForm {...baseProps} paymentMode="card" saveErrorFields={{ creditCardId: 'Choose a card' }} />)

    expect(screen.getByText('Choose a card')).toBeInTheDocument()
  })

  it('shows the save error below the form only when no field carries its own error', () => {
    const { rerender } = render(<ExpenseForm {...baseProps} saveError="Could not save." />)
    expect(screen.getByText('Could not save.')).toBeInTheDocument()

    rerender(<ExpenseForm {...baseProps} saveError="Could not save." saveErrorFields={{ value: 'Value is required' }} />)
    expect(screen.queryByText('Could not save.')).not.toBeInTheDocument()
  })

  it('labels the primary button by state: add, save when editing, saving while in flight', () => {
    const { rerender } = render(<ExpenseForm {...baseProps} />)
    expect(screen.getByRole('button', { name: 'Add expense' })).toBeEnabled()

    rerender(<ExpenseForm {...baseProps} isEditing />)
    expect(screen.getByText('Edit expense')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeEnabled()

    rerender(<ExpenseForm {...baseProps} isEditing isSaving />)
    expect(screen.getByRole('button', { name: 'Saving…' })).toHaveAttribute('aria-disabled', 'true')
  })

  it('names the raw payment source and card id in the settlement note when no name is known', () => {
    render(
      <ExpenseForm
        {...baseProps}
        isEditing
        isSettled
        paymentSource="bank-unknown"
        creditCardId="card-unknown"
        creditCardName=""
      />,
    )

    expect(screen.getByText(/Paid by bank-unknown via card card-unknown/)).toBeInTheDocument()
  })

  it('calls onSave and onCancel', () => {
    const onSave = vi.fn()
    const onCancel = vi.fn()
    render(<ExpenseForm {...baseProps} onSave={onSave} onCancel={onCancel} />)

    fireEvent.click(screen.getByRole('button', { name: 'Add expense' }))
    expect(onSave).toHaveBeenCalled()

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onCancel).toHaveBeenCalled()
  })
})

describe('ExpenseForm with a locked bank', () => {
  it('shows the locked bank as read-only text instead of the payment source picker', () => {
    render(<ExpenseForm {...baseProps} paymentSource="bank-trading212" lockedBank={{ id: 'bank-trading212', name: 'Trading212' }} />)

    expect(screen.getByText('Payment source')).toBeInTheDocument()
    expect(screen.getByText('Trading212')).toBeInTheDocument()
    expect(screen.queryByRole('combobox', { name: 'Payment source' })).not.toBeInTheDocument()
    expect(screen.queryByRole('textbox', { name: 'Payment source' })).not.toBeInTheDocument()
    expect(screen.getByLabelText('Round-up')).toBeInTheDocument()
  })

  it('saves when the form is submitted with Enter', () => {
    const onSave = vi.fn()
    render(<ExpenseForm {...baseProps} onSave={onSave} />)

    fireEvent.submit(screen.getByLabelText(/^Description/).closest('form') as HTMLFormElement)

    expect(onSave).toHaveBeenCalledOnce()
    expect(screen.getByRole('button', { name: 'Add expense' })).toHaveAttribute('type', 'submit')
    expect(screen.getByRole('button', { name: 'Cancel' })).toHaveAttribute('type', 'button')
  })

  it('shows a save error above the fields', () => {
    render(<ExpenseForm {...baseProps} saveError="Could not save. Check the connection and try again." />)

    const message = screen.getByText('Could not save. Check the connection and try again.')
    const dateField = screen.getByLabelText(/^Date/)
    expect(message.compareDocumentPosition(dateField) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it('[AC P60-F06-admin-accounts-page-05] leaves an inactive bank out of Payment source on a new expense', () => {
    render(<ExpenseForm {...baseProps} banks={[...BANKS, INACTIVE_BANK]} />)

    expect(optionNames(screen.getByLabelText(/^Payment source/))).toEqual(['Barclays', 'Trading212'])
  })

  it('[AC P60-F06-admin-accounts-page-05] keeps the inactive bank of an expense being edited', () => {
    render(<ExpenseForm {...baseProps} isEditing paymentSource="bank-chase" banks={[...BANKS, INACTIVE_BANK]} />)

    expect(screen.getByLabelText(/^Payment source/)).toHaveValue('bank-chase')
    expect(optionNames(screen.getByLabelText(/^Payment source/))).toEqual(['Barclays', 'Trading212', 'Chase'])
  })

  it("[AC P60-F06-admin-accounts-page-05] keeps the edited expense's inactive bank after another bank is picked", () => {
    const props = { ...baseProps, isEditing: true, banks: [...BANKS, INACTIVE_BANK] }
    const { rerender } = render(<ExpenseForm {...props} paymentSource="bank-chase" />)

    rerender(<ExpenseForm {...props} paymentSource="bank-barclays" />)

    expect(optionNames(screen.getByLabelText(/^Payment source/))).toContain('Chase')
  })
})

describe('ExpenseForm with a locked card', () => {
  const cardProps = { ...baseProps, paymentMode: 'card' as const, paymentSource: '', creditCardId: 'card-baamex' }

  it('shows the locked card as read-only text and names the record a charge', () => {
    render(<ExpenseForm {...cardProps} date="2026-10-14" lockedCard={{ id: 'card-baamex', name: 'BaAmex' }} />)

    expect(screen.getByRole('heading', { name: 'New charge' })).toBeInTheDocument()
    expect(screen.getByText('BaAmex')).toBeInTheDocument()
    expect(screen.queryByRole('combobox', { name: /^Card/ })).not.toBeInTheDocument()
    expect(screen.queryByLabelText(/^Payment source/)).not.toBeInTheDocument()
    expect(screen.getByLabelText('Statement month')).toHaveValue('2026-10')
    expect(screen.getByRole('button', { name: 'Add charge' })).toBeInTheDocument()
  })

  it('titles an edit Edit charge and keeps the statement month editable', () => {
    render(<ExpenseForm {...cardProps} isEditing invoiceDate="2026-11" lockedCard={{ id: 'card-baamex', name: 'BaAmex' }} />)

    expect(screen.getByRole('heading', { name: 'Edit charge' })).toBeInTheDocument()
    expect(screen.getByLabelText('Statement month')).toBeEnabled()
  })

  it('describes the statement month field with the month a date change moved it from', () => {
    render(
      <ExpenseForm {...cardProps} isEditing date="2026-10-05" invoiceDate="2026-10" invoiceMonthMovedFrom="2026-11" lockedCard={{ id: 'card-baamex', name: 'BaAmex' }} />,
    )

    const hint = 'Statement month moved to October 2026 with the date (was November 2026).'
    expect(screen.getByLabelText('Statement month')).toHaveAccessibleDescription(hint)
    expect(screen.getAllByRole('status').map((status) => status.textContent)).toContain(hint)
  })

  it('shows no statement month hint while the date has not moved it', () => {
    render(<ExpenseForm {...cardProps} isEditing date="2026-10-05" invoiceDate="2026-10" lockedCard={{ id: 'card-baamex', name: 'BaAmex' }} />)

    expect(screen.getByLabelText('Statement month')).not.toHaveAccessibleDescription()
  })

  describe('in refund mode', () => {
    const refundProps = { ...cardProps, isRefund: true, lockedCard: { id: 'card-baamex', name: 'BaAmex' } }

    it('titles the form New refund, labels the amount and limits the date to today', () => {
      render(<ExpenseForm {...refundProps} date="2026-10-14" />)

      expect(screen.getByRole('heading', { name: 'New refund' })).toBeInTheDocument()
      expect(screen.getByLabelText(/^Amount/)).toBeInTheDocument()
      expect(screen.queryByLabelText(/^Value/)).not.toBeInTheDocument()
      expect(screen.getByLabelText(/^Date/)).toHaveAttribute('max', todayIsoDate())
      expect(screen.getByLabelText('Statement month')).toHaveValue('2026-10')
      expect(screen.getByRole('button', { name: 'Add refund' })).toBeInTheDocument()
    })

    it('titles an edit Edit refund and saves with Save', () => {
      render(<ExpenseForm {...refundProps} isEditing value="40" invoiceDate="2026-11" />)

      expect(screen.getByRole('heading', { name: 'Edit refund' })).toBeInTheDocument()
      expect(screen.getByLabelText(/^Amount/)).toHaveValue(40)
      expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
    })

    it('focuses the first invalid field with its message', () => {
      const { rerender } = render(<ExpenseForm {...refundProps} />)

      rerender(<ExpenseForm {...refundProps} saveError="Date must not be in the future." saveErrorFields={{ date: 'Date must not be in the future.', value: 'Amount must be greater than zero.' }} />)

      expect(screen.getByText('Date must not be in the future.')).toBeInTheDocument()
      expect(screen.getByText('Amount must be greater than zero.')).toBeInTheDocument()
      expect(screen.getByLabelText(/^Date/)).toHaveFocus()
    })

    it('shows a server error in the message bar and moves focus to it', () => {
      const { rerender } = render(<ExpenseForm {...refundProps} />)

      rerender(<ExpenseForm {...refundProps} saveError="This charge's statement is paid; unmark it first." />)

      expect(getErrorAlert()).toHaveTextContent("This charge's statement is paid; unmark it first.")
      expect(getErrorAlert()).toHaveFocus()
    })
  })

  it('leaves a charge date unlimited', () => {
    render(<ExpenseForm {...cardProps} date="2026-10-14" lockedCard={{ id: 'card-baamex', name: 'BaAmex' }} />)

    expect(screen.getByLabelText(/^Date/)).not.toHaveAttribute('max')
  })
})
