import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import CardsGrid from '../CardsGrid'
import type { BankDto, CardStatementDto } from '../../api/types'

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true },
]

const CARD_STATEMENTS: CardStatementDto[] = [
  { id: 'c1', creditCardId: 'card-baamex', creditCardName: 'BaAmex', year: 2026, month: 7, isPaid: false, outstandingTotal: 100 },
  { id: 'c2', creditCardId: 'card-chase', creditCardName: 'ChaseMaster4023', year: 2026, month: 7, isPaid: true, outstandingTotal: 0 },
]

describe('CardsGrid', () => {
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
