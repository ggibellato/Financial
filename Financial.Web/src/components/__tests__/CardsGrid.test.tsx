import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import CardsGrid from '../CardsGrid'
import type { BankDto, CardStatementDto, CreditCardDto } from '../../api/types'

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01' },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01' },
]

const CARD_STATEMENTS: CardStatementDto[] = [
  { id: 'c1', creditCardId: 'card-baamex', creditCardName: 'BaAmex', year: 2026, month: 7, isPaid: false, outstandingTotal: 100, warning: null },
  { id: 'c2', creditCardId: 'card-chase', creditCardName: 'ChaseMaster4023', year: 2026, month: 7, isPaid: true, outstandingTotal: 0, warning: null },
]

const CREDIT_CARDS: CreditCardDto[] = [
  { id: 'card-baamex', name: 'BaAmex', isActive: true, nextInvoiceDueDate: '2026-09-05' },
  { id: 'card-chase', name: 'ChaseMaster4023', isActive: true, nextInvoiceDueDate: null },
  { id: 'card-paypal', name: 'PaypalCredit', isActive: false, nextInvoiceDueDate: null },
]

describe('CardsGrid (statement-only, no creditCards prop — Summary tab)', () => {
  it('renders a row per card with status and the footer adjustment total', () => {
    render(
      <CardsGrid
        cardStatements={CARD_STATEMENTS}
        banks={BANKS}
        adjustmentTotal={100}
        markPaidSources={{}}
        setMarkPaidSource={vi.fn()}
        markStatementPaid={vi.fn()}
        unmarkStatementPaid={vi.fn()}
      />,
    )

    expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument()
    expect(screen.getByText('Unpaid')).toBeInTheDocument()
    expect(screen.getByText('Paid')).toBeInTheDocument()
    expect(screen.getByText(/Combined adjustment figure/)).toBeInTheDocument()
  })

  it('renders a statement action warning as a status, not an alert', () => {
    render(
      <CardsGrid
        cardStatements={CARD_STATEMENTS}
        banks={BANKS}
        adjustmentTotal={100}
        markPaidSources={{}}
        setMarkPaidSource={vi.fn()}
        markStatementPaid={vi.fn()}
        unmarkStatementPaid={vi.fn()}
        statementActionWarning="This statement was already marked paid; nothing changed."
      />,
    )

    expect(screen.getByRole('status')).toHaveTextContent('already marked paid')
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('renders a statement action error as an alert', () => {
    render(
      <CardsGrid
        cardStatements={CARD_STATEMENTS}
        banks={BANKS}
        adjustmentTotal={100}
        markPaidSources={{}}
        setMarkPaidSource={vi.fn()}
        markStatementPaid={vi.fn()}
        unmarkStatementPaid={vi.fn()}
        statementActionError="Failed to mark statement paid"
      />,
    )

    expect(screen.getByRole('alert')).toHaveTextContent('Failed to mark statement paid')
  })

  it('renders neither banner when there is nothing to report', () => {
    render(
      <CardsGrid
        cardStatements={CARD_STATEMENTS}
        banks={BANKS}
        adjustmentTotal={100}
        markPaidSources={{}}
        setMarkPaidSource={vi.fn()}
        markStatementPaid={vi.fn()}
        unmarkStatementPaid={vi.fn()}
      />,
    )

    expect(screen.queryByRole('status')).not.toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('does not render the Next Invoice Due Date/Active columns', () => {
    render(
      <CardsGrid
        cardStatements={CARD_STATEMENTS}
        banks={BANKS}
        adjustmentTotal={100}
        markPaidSources={{}}
        setMarkPaidSource={vi.fn()}
        markStatementPaid={vi.fn()}
        unmarkStatementPaid={vi.fn()}
      />,
    )

    expect(screen.queryByText('Next Invoice Due Date')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Active for BaAmex')).not.toBeInTheDocument()
  })

  it('disables Mark Paid until a bank is selected, then calls markStatementPaid with the chosen bank', () => {
    const setMarkPaidSource = vi.fn()
    const markStatementPaid = vi.fn()
    render(
      <CardsGrid
        cardStatements={CARD_STATEMENTS}
        banks={BANKS}
        adjustmentTotal={100}
        markPaidSources={{}}
        setMarkPaidSource={setMarkPaidSource}
        markStatementPaid={markStatementPaid}
        unmarkStatementPaid={vi.fn()}
      />,
    )

    const markPaidButton = screen.getByRole('button', { name: 'Mark Paid' })
    expect(markPaidButton).toBeDisabled()

    fireEvent.change(screen.getByLabelText('Paying bank for BaAmex'), { target: { value: 'bank-trading212' } })
    expect(setMarkPaidSource).toHaveBeenCalledWith('c1', 'bank-trading212')
  })

  it('calls unmarkStatementPaid for a paid card', () => {
    const unmarkStatementPaid = vi.fn()
    render(
      <CardsGrid
        cardStatements={CARD_STATEMENTS}
        banks={BANKS}
        adjustmentTotal={100}
        markPaidSources={{}}
        setMarkPaidSource={vi.fn()}
        markStatementPaid={vi.fn()}
        unmarkStatementPaid={unmarkStatementPaid}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Unmark Paid' }))
    expect(unmarkStatementPaid).toHaveBeenCalledWith('c2')
  })
})

describe('CardsGrid (merged with creditCards — Credit Card tab)', () => {
  const baseProps = {
    cardStatements: CARD_STATEMENTS,
    banks: BANKS,
    adjustmentTotal: 100,
    markPaidSources: {},
    setMarkPaidSource: vi.fn(),
    markStatementPaid: vi.fn(),
    unmarkStatementPaid: vi.fn(),
    creditCards: CREDIT_CARDS,
    updatingCardId: null,
    updateError: null,
    onUpdateCreditCard: vi.fn(),
  }

  it('renders one row per credit card, including one with no statement this month', () => {
    render(<CardsGrid {...baseProps} />)

    expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'ChaseMaster4023' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'PaypalCredit' })).toBeInTheDocument()
  })

  it('shows a dash for outstanding/status on a card with no statement this month, and no mark-paid action', () => {
    render(<CardsGrid {...baseProps} />)

    const paypalRow = screen.getByRole('cell', { name: 'PaypalCredit' }).closest('tr')!
    expect(paypalRow).toHaveTextContent('—')
    expect(screen.queryByLabelText('Paying bank for PaypalCredit')).not.toBeInTheDocument()
  })

  it('still supports Mark Paid for a card that has a statement', () => {
    const setMarkPaidSource = vi.fn()
    const markStatementPaid = vi.fn()
    render(<CardsGrid {...baseProps} setMarkPaidSource={setMarkPaidSource} markStatementPaid={markStatementPaid} />)

    fireEvent.change(screen.getByLabelText('Paying bank for BaAmex'), { target: { value: 'bank-trading212' } })
    expect(setMarkPaidSource).toHaveBeenCalledWith('c1', 'bank-trading212')

    fireEvent.click(screen.getByRole('button', { name: 'Unmark Paid' }))
    expect(baseProps.unmarkStatementPaid).toHaveBeenCalledWith('c2')
  })

  it('renders due date and active state per card', () => {
    render(<CardsGrid {...baseProps} />)

    expect(screen.getByLabelText('Next invoice due date for BaAmex')).toHaveValue('2026-09-05')
    expect(screen.getByLabelText('Active for BaAmex')).toBeChecked()
    expect(screen.getByLabelText('Next invoice due date for PaypalCredit')).toHaveValue('')
    expect(screen.getByLabelText('Active for PaypalCredit')).not.toBeChecked()
  })

  it('changing the due date calls onUpdateCreditCard with the new date and the current active flag', () => {
    const onUpdateCreditCard = vi.fn()
    render(<CardsGrid {...baseProps} onUpdateCreditCard={onUpdateCreditCard} />)

    fireEvent.change(screen.getByLabelText('Next invoice due date for BaAmex'), { target: { value: '2026-10-01' } })

    expect(onUpdateCreditCard).toHaveBeenCalledWith('card-baamex', { nextInvoiceDueDate: '2026-10-01', isActive: true })
  })

  it('clearing the due date sends null', () => {
    const onUpdateCreditCard = vi.fn()
    render(<CardsGrid {...baseProps} onUpdateCreditCard={onUpdateCreditCard} />)

    fireEvent.change(screen.getByLabelText('Next invoice due date for BaAmex'), { target: { value: '' } })

    expect(onUpdateCreditCard).toHaveBeenCalledWith('card-baamex', { nextInvoiceDueDate: null, isActive: true })
  })

  it('toggling active calls onUpdateCreditCard with the flipped flag and the current due date, even for a card with no statement', () => {
    const onUpdateCreditCard = vi.fn()
    render(<CardsGrid {...baseProps} onUpdateCreditCard={onUpdateCreditCard} />)

    fireEvent.click(screen.getByLabelText('Active for PaypalCredit'))

    expect(onUpdateCreditCard).toHaveBeenCalledWith('card-paypal', { nextInvoiceDueDate: null, isActive: true })
  })

  it('disables the row being updated', () => {
    render(<CardsGrid {...baseProps} updatingCardId="card-baamex" />)

    expect(screen.getByLabelText('Next invoice due date for BaAmex')).toBeDisabled()
    expect(screen.getByLabelText('Active for BaAmex')).toBeDisabled()
    expect(screen.getByLabelText('Next invoice due date for PaypalCredit')).not.toBeDisabled()
  })

  it('shows an update error when present', () => {
    render(<CardsGrid {...baseProps} updateError="Credit card was not found." />)

    expect(screen.getByText('Credit card was not found.')).toBeInTheDocument()
  })

  it('sorts rows by clicking the Card column header, keeping the footer total fixed', () => {
    render(<CardsGrid {...baseProps} />)

    const rowsBefore = screen.getAllByRole('row').slice(1)
    expect(rowsBefore.map((r) => r.textContent)).toEqual([
      expect.stringContaining('BaAmex'),
      expect.stringContaining('ChaseMaster4023'),
      expect.stringContaining('PaypalCredit'),
    ])

    fireEvent.click(screen.getByRole('button', { name: 'Card' }))

    const rowsAfterAsc = screen.getAllByRole('row').slice(1)
    expect(rowsAfterAsc.map((r) => r.textContent)).toEqual([
      expect.stringContaining('BaAmex'),
      expect.stringContaining('ChaseMaster4023'),
      expect.stringContaining('PaypalCredit'),
    ])

    fireEvent.click(screen.getByRole('button', { name: 'Card' }))

    const rowsAfterDesc = screen.getAllByRole('row').slice(1)
    expect(rowsAfterDesc.map((r) => r.textContent)).toEqual([
      expect.stringContaining('PaypalCredit'),
      expect.stringContaining('ChaseMaster4023'),
      expect.stringContaining('BaAmex'),
    ])

    expect(screen.getByText(/Combined adjustment figure/)).toBeInTheDocument()
  })

  it('filters rows by Card via the header checklist', () => {
    render(<CardsGrid {...baseProps} />)

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Card' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'ChaseMaster4023' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'PaypalCredit' }))

    expect(screen.queryByRole('cell', { name: 'ChaseMaster4023' })).not.toBeInTheDocument()
    expect(screen.queryByRole('cell', { name: 'PaypalCredit' })).not.toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument()
  })
})
