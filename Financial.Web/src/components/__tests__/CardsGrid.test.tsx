import { fireEvent, render, screen, within } from '@testing-library/react'
import type { ReactElement } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import CardsGrid from '../CardsGrid'
import type { AccountGroupMemberDto, CurrencyTotalDto } from '../../api/types'

function card(accountId: string, name: string, outstandingTotal: number, totalOwed: number | null, isPaid = false): AccountGroupMemberDto {
  return { accountId, name, templateName: 'CreditCard', currency: 'GBP', balance: -(totalOwed ?? 0), currencyBalances: [{ currency: 'GBP', balance: -(totalOwed ?? 0) }], otherParticipant: null, roundUpTotal: null, statement: { outstandingTotal, isPaid }, balanceIsOwed: true, isActive: true, totalOwed, nextStatement: null }
}

const CARDS = [card('card-baamex', 'BaAmex', 100, 325.5), card('card-chase', 'ChaseMaster4023', 0, 0, true)]

const GBP_TOTALS: CurrencyTotalDto[] = [{ currency: 'GBP', balance: 500, roundUpTotal: 0, cardOutstandingTotal: 100 }]

const renderInRouter = (ui: ReactElement) => render(<MemoryRouter>{ui}</MemoryRouter>)

const renderGrid = (cards = CARDS, currencyTotals = GBP_TOTALS) =>
  renderInRouter(<CardsGrid cards={cards} currencyTotals={currencyTotals} viewedMonth="2026-07" />)

const rowOf = (name: string) => screen.getByRole('row', { name: new RegExp(name) })

describe('CardsGrid', () => {
  it('renders a row per card with status and the footer adjustment total', () => {
    renderGrid()

    expect(screen.getByText('BaAmex')).toBeInTheDocument()
    expect(screen.getByText('Unpaid')).toBeInTheDocument()
    expect(screen.getByText('Paid')).toBeInTheDocument()
    expect(screen.getByText(/Combined adjustment figure/)).toHaveTextContent('Combined adjustment figure: 100.00')
  })

  it('labels the adjustment figure as covering all cards while a Card filter is active, and plainly when none is', () => {
    renderGrid()
    const footer = () => document.querySelector('.monthly-page__section-total')!.textContent

    expect(footer()).toBe('Combined adjustment figure: 100.00')

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Card' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'ChaseMaster4023' }))

    expect(footer()).toBe('Combined adjustment figure (all cards): 100.00')

    fireEvent.click(screen.getByRole('checkbox', { name: 'ChaseMaster4023' }))

    expect(footer()).toBe('Combined adjustment figure: 100.00')
  })

  it('[AC P60-F13-consolidated-navigation-and-retirement-02] links each row to its account screen at the viewed month', () => {
    renderGrid()

    expect(screen.getByRole('link', { name: 'BaAmex' })).toHaveAttribute('href', '/cashflow/accounts/card-baamex?month=2026-07')
    expect(screen.getByRole('link', { name: 'ChaseMaster4023' })).toHaveAttribute('href', '/cashflow/accounts/card-chase?month=2026-07')
  })

  it('[AC P60-F13-consolidated-navigation-and-retirement-02] is read-only: no Mark Paid, Unmark, due date or Active', () => {
    renderGrid()

    expect(screen.queryByRole('button', { name: /Mark Paid|Unmark/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('combobox')).not.toBeInTheDocument()
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument()
    expect(screen.queryByText('Next Invoice Due Date')).not.toBeInTheDocument()
    expect(screen.queryByRole('columnheader', { name: 'Active' })).not.toBeInTheDocument()
  })

  it('labels the adjustment figure of each currency with its code when the totals hold several currencies', () => {
    renderGrid(CARDS, [...GBP_TOTALS, { currency: 'BRL', balance: 0, roundUpTotal: 0, cardOutstandingTotal: 0 }])

    expect(screen.getByText(/Combined adjustment figure/)).toHaveTextContent(
      'Combined adjustment figure GBP: 100.00 · Combined adjustment figure BRL: 0.00',
    )
  })

  it('adds a Currency column when the totals hold several currencies', () => {
    renderGrid(
      [card('card-baamex', 'BaAmex', 100, 325.5), { ...card('card-itau', 'Itaú', 50, 50), currency: 'BRL' }],
      [...GBP_TOTALS, { currency: 'BRL', balance: 0, roundUpTotal: 0, cardOutstandingTotal: 50 }],
    )

    expect(screen.getByRole('columnheader', { name: 'Currency' })).toBeInTheDocument()
    expect(within(rowOf('Itaú')).getByText('BRL')).toBeInTheDocument()
    expect(within(rowOf('BaAmex')).getByText('GBP')).toBeInTheDocument()
  })

  it('has no Currency column for a single currency', () => {
    renderGrid()

    expect(screen.queryByRole('columnheader', { name: 'Currency' })).not.toBeInTheDocument()
  })

  it('shows its empty message and a zero adjustment figure when the group has no card', () => {
    renderGrid([], [])

    expect(screen.getByText('No cards in the default group for this month.')).toBeInTheDocument()
    expect(screen.getByText(/Combined adjustment figure/)).toHaveTextContent('Combined adjustment figure: 0.00')
  })

  it('renders the period and accumulated outstanding figures in their own columns', () => {
    renderGrid()

    expect(screen.getByRole('button', { name: 'Outstanding (period)' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Accumulated outstanding' })).toBeInTheDocument()
    expect(within(rowOf('BaAmex')).getByText('100.00')).toBeInTheDocument()
    expect(within(rowOf('BaAmex')).getByText('325.50')).toBeInTheDocument()
  })

  it('[AC P60-F15-card-refunds-and-credit-02] shows an unpaid statement with nothing outstanding as Nothing due', () => {
    renderGrid([card('card-baamex', 'BaAmex', 0, 0)])

    expect(within(rowOf('BaAmex')).getByText('Nothing due')).toBeInTheDocument()
    expect(within(rowOf('BaAmex')).queryByText('Unpaid')).not.toBeInTheDocument()
  })

  it('[AC P60-F15-card-refunds-and-credit-06] shows a card in credit as Credit x in the accumulated cell', () => {
    renderGrid([card('card-baamex', 'BaAmex', 0, -35)])

    expect(within(rowOf('BaAmex')).getByText('Credit 35.00')).toBeInTheDocument()
    expect(within(rowOf('BaAmex')).getByText('Nothing due')).toBeInTheDocument()
  })

  it('shows a dash when the accumulated figure is missing', () => {
    renderGrid([card('card-baamex', 'BaAmex', 0, null)])

    expect(within(rowOf('BaAmex')).getByText('—')).toBeInTheDocument()
  })

  it('sorts rows by the outstanding column', () => {
    renderGrid()

    fireEvent.click(screen.getByRole('button', { name: 'Outstanding (period)' }))

    const rows = screen.getAllByRole('row').slice(1)
    expect(within(rows[0]).getByText('ChaseMaster4023')).toBeInTheDocument()
    expect(within(rows[1]).getByText('BaAmex')).toBeInTheDocument()
  })

  it('filters rows by Card via the header checklist', () => {
    renderGrid()

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Card' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'ChaseMaster4023' }))

    const tbody = within(document.querySelector('tbody')!)
    expect(tbody.queryByText('ChaseMaster4023')).not.toBeInTheDocument()
    expect(tbody.getByText('BaAmex')).toBeInTheDocument()
  })

  it('shows the "no rows match" message when every card is filtered out', () => {
    renderGrid()

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Card' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'BaAmex' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'ChaseMaster4023' }))

    expect(screen.getByText('No rows match the current filters')).toBeInTheDocument()
  })
})
