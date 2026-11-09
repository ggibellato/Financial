import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountGroupMemberDto, AccountTemplateGroupDto } from '../../api/types'
import { fireEvent, render, screen, within } from '../../test/renderWithFluent'
import { pinDate } from '../../test-utils/pinDate'
import AccountsPage from '../AccountsPage'

const { getAccountTemplateGroupsMock } = vi.hoisted(() => ({
  getAccountTemplateGroupsMock: vi.fn<FinancialApiClient['getAccountTemplateGroups']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { getAccountTemplateGroups: getAccountTemplateGroupsMock } as Partial<FinancialApiClient>,
}))

function member(accountId: string, name: string, templateName: string, fields: Partial<AccountGroupMemberDto> = {}): AccountGroupMemberDto {
  return {
    accountId,
    name,
    templateName,
    currency: 'GBP',
    isActive: true,
    balance: 0,
    currencyBalances: [{ currency: fields.currency ?? 'GBP', balance: fields.balance ?? 0 }],
    otherParticipant: null,
    roundUpTotal: null,
    statement: null,
    balanceIsOwed: false,
    totalOwed: null,
    nextStatement: null,
    ...fields,
  }
}

const BANKS: AccountTemplateGroupDto = {
  templateName: 'Bank',
  members: [
    member('barclays', 'Barclays', 'Bank', { balance: 2800.25, roundUpTotal: 1.2 }),
    member('chase', 'Chase', 'Bank', { balance: 500 }),
    member('itau', 'Itau', 'Bank', { currency: 'BRL', balance: 1000 }),
    member('old', 'Old Savings', 'Bank', { balance: 10, isActive: false }),
  ],
  totals: [
    { currency: 'GBP', total: 3310.25 },
    { currency: 'BRL', total: 1000 },
  ],
}

const CARDS: AccountTemplateGroupDto = {
  templateName: 'CreditCard',
  members: [
    member('amex', 'BA Amex', 'CreditCard', {
      balance: -740.12,
      balanceIsOwed: true,
      statement: { outstandingTotal: 228, isPaid: false },
      totalOwed: 740.12,
      nextStatement: { year: 2026, month: 10, outstandingTotal: 512.3 },
    }),
    member('visa', 'Old Visa', 'CreditCard', { balanceIsOwed: true, isActive: false, totalOwed: 0 }),
  ],
  totals: [{ currency: 'GBP', total: 740.12 }],
}

const POTS: AccountTemplateGroupDto = {
  templateName: 'Pots',
  members: [
    member('old-pots', 'Old pots', 'Pots', { balance: 12.5, isActive: false }),
    member('reserva', 'Reserva', 'Pots', { balance: 704.08 }),
  ],
  totals: [{ currency: 'GBP', total: 716.58 }],
}

const SETTLEMENT: AccountTemplateGroupDto = {
  templateName: 'Settlement',
  members: [
    member('mae', 'Mãe', 'Settlement', {
      currency: 'BRL',
      balance: 167.8768,
      otherParticipant: 'Mãe',
      currencyBalances: [
        { currency: 'BRL', balance: 167.8768 },
        { currency: 'GBP', balance: 7.94214099 },
      ],
    }),
    member('samuel', 'Samuel', 'Settlement', { isActive: false, otherParticipant: 'Samuel' }),
    member('tia', 'Tia', 'Settlement', { balance: -15 }),
  ],
  totals: [],
}

function renderPage() {
  return render(
    <MemoryRouter>
      <AccountsPage />
    </MemoryRouter>,
  )
}

function renderPageAfterMovedRedirect() {
  return render(
    <MemoryRouter initialEntries={[{ pathname: '/cashflow/accounts', state: { movedNotice: true } }]}>
      <AccountsPage />
    </MemoryRouter>,
  )
}

function bodyRows(table: HTMLElement) {
  return within(table).getAllByRole('row').slice(1)
}

function accountNames(table: HTMLElement) {
  return bodyRows(table).map((row) => within(row).getAllByRole('cell')[0].textContent?.replace(/^Account:/, ''))
}

function headers(table: HTMLElement) {
  return within(within(table).getAllByRole('row')[0]).getAllByRole('columnheader').map((cell) => cell.textContent)
}

function footer(table: HTMLElement) {
  return table.closest('section')?.querySelector('.monthly-page__section-total')?.textContent
}

describe('AccountsPage', () => {
  beforeEach(() => {
    pinDate('2026-10-15T12:00:00+01:00')
    getAccountTemplateGroupsMock.mockReset().mockResolvedValue([BANKS, CARDS, POTS])
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('[AC P60-F13-consolidated-navigation-and-retirement-04] shows the moved notice above the grids when a redirect found no account', async () => {
    renderPageAfterMovedRedirect()

    const banks = await screen.findByRole('table', { name: 'Banks' })
    const notice = screen.getByRole('status')
    expect(notice).toHaveTextContent('That page moved to the account screen.')
    expect(notice.compareDocumentPosition(banks) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it('shows no moved notice on an ordinary visit', async () => {
    renderPage()

    expect(await screen.findByRole('table', { name: 'Banks' })).toBeInTheDocument()
    expect(screen.queryByText('That page moved to the account screen.')).not.toBeInTheDocument()
  })

  it('[AC P60-F14-accounts-page-by-template-and-card-totals-01] shows one grid per template in order, each listing its accounts with the Inactive badge and a per-currency footer', async () => {
    renderPage()

    const banks = await screen.findByRole('table', { name: 'Banks' })
    expect(getAccountTemplateGroupsMock).toHaveBeenCalledTimes(1)
    expect(getAccountTemplateGroupsMock).toHaveBeenCalledWith(2026, 10)
    expect(screen.getAllByRole('heading', { level: 2 }).map((heading) => heading.textContent)).toEqual(['Banks', 'Credit cards', 'Pots'])
    expect(headers(banks)).toEqual(['Account', 'Currency', 'Balance'])
    expect(accountNames(banks)).toEqual(['Barclays', 'Chase', 'Itau', 'Old SavingsInactive'])
    expect(within(bodyRows(banks)[0]).getByText('2,800.25')).toBeInTheDocument()
    expect(within(bodyRows(banks)[2]).getByText('BRL')).toBeInTheDocument()
    expect(within(bodyRows(banks)[0]).queryByText('Inactive')).not.toBeInTheDocument()
    expect(footer(banks)).toBe('Bank total GBP: 3,310.25 · Bank total BRL: 1,000.00')

    const pots = screen.getByRole('table', { name: 'Pots' })
    expect(headers(pots)).toEqual(['Account', 'Currency', 'Balance'])
    expect(accountNames(pots)).toEqual(['Old potsInactive', 'Reserva'])
    expect(within(bodyRows(pots)[1]).getByText('704.08')).toBeInTheDocument()
    expect(footer(pots)).toBe('Pots total GBP: 716.58')
  })

  it('shows no grid for a template without listed accounts', async () => {
    getAccountTemplateGroupsMock.mockResolvedValue([BANKS, CARDS])

    renderPage()

    expect(await screen.findByRole('table', { name: 'Banks' })).toBeInTheDocument()
    expect(screen.getAllByRole('heading', { level: 2 }).map((heading) => heading.textContent)).toEqual(['Banks', 'Credit cards'])
    expect(screen.queryByRole('table', { name: 'Pots' })).not.toBeInTheDocument()
  })

  it('[AC P60-F14-accounts-page-by-template-and-card-totals-02] shows each card’s next statement to pay, or a dash, and its total owed', async () => {
    renderPage()

    const cards = await screen.findByRole('table', { name: 'Credit cards' })
    expect(headers(cards)).toEqual(['Account', 'Currency', 'Next statement', 'Total owed'])
    const [amex, visa] = bodyRows(cards)
    expect(within(amex).getByText('Oct 2026 · 512.30')).toBeInTheDocument()
    expect(within(amex).getByText('740.12')).toBeInTheDocument()
    expect(within(amex).queryByText('Owed 740.12')).not.toBeInTheDocument()
    expect(within(visa).getByText('—')).toBeInTheDocument()
    expect(within(visa).getByText('0.00')).toBeInTheDocument()
    expect(within(visa).getByText('Inactive')).toBeInTheDocument()
    expect(footer(cards)).toBe('Net owed GBP: 740.12')
  })

  it('[AC P60-F15-card-refunds-and-credit-06] shows a card in credit as Credit x in its Total owed cell and the footer, with no next statement', async () => {
    getAccountTemplateGroupsMock.mockResolvedValue([
      {
        templateName: 'CreditCard',
        members: [member('amex', 'BA Amex', 'CreditCard', { balance: 35, balanceIsOwed: true, totalOwed: -35, nextStatement: null })],
        totals: [{ currency: 'GBP', total: -35 }],
      },
    ])

    renderPage()

    const cards = await screen.findByRole('table', { name: 'Credit cards' })
    const [amex] = bodyRows(cards)
    expect(within(amex).getByText('Credit 35.00')).toBeInTheDocument()
    expect(within(amex).getByText('—')).toBeInTheDocument()
    expect(footer(cards)).toBe('Net owed GBP: Credit 35.00')
  })

  it('keeps a negative balance total as a plain figure on a bank grid', async () => {
    getAccountTemplateGroupsMock.mockResolvedValue([{ ...BANKS, totals: [{ currency: 'GBP', total: -20 }] }])

    renderPage()

    expect(footer(await screen.findByRole('table', { name: 'Banks' }))).toBe('Bank total GBP: -20.00')
  })

  it('sorts the cards by total owed', async () => {
    renderPage()

    const cards = await screen.findByRole('table', { name: 'Credit cards' })
    fireEvent.click(within(cards).getByRole('button', { name: 'Total owed' }))

    expect(bodyRows(screen.getByRole('table', { name: 'Credit cards' })).map((row) => within(row).getByRole('link').textContent)).toEqual(['Old Visa', 'BA Amex'])
  })

  it('[AC P60-F14-accounts-page-by-template-and-card-totals-04] links bank and card rows to their account screen for the month', async () => {
    renderPage()

    const banks = await screen.findByRole('table', { name: 'Banks' })
    expect(within(banks).getByRole('link', { name: 'Barclays' })).toHaveAttribute('href', '/cashflow/accounts/barclays?month=2026-10')
    expect(within(screen.getByRole('table', { name: 'Credit cards' })).getByRole('link', { name: 'BA Amex' })).toHaveAttribute(
      'href',
      '/cashflow/accounts/amex?month=2026-10',
    )
  })

  it('links a Pots row to the Pots tab of its account screen for the month', async () => {
    renderPage()

    const pots = await screen.findByRole('table', { name: 'Pots' })
    expect(within(pots).getByRole('link', { name: 'Reserva' })).toHaveAttribute('href', expect.stringMatching(/\?month=2026-10&tab=pots$/))
  })

  it('[AC P60-F11-settlement-template-and-mae-migration-07] shows the Settlement grid with who owes whom per currency and no totals', async () => {
    getAccountTemplateGroupsMock.mockResolvedValue([BANKS, CARDS, POTS, SETTLEMENT])

    renderPage()

    const settlement = await screen.findByRole('table', { name: 'Settlement' })
    expect(screen.getAllByRole('heading', { level: 2 }).map((heading) => heading.textContent)).toEqual(['Banks', 'Credit cards', 'Pots', 'Settlement'])
    expect(headers(settlement)).toEqual(['Account', 'Balance'])
    expect(accountNames(settlement)).toEqual(['Mãe', 'SamuelInactive', 'Tia'])
    const [mae, samuel, tia] = bodyRows(settlement)
    expect(within(mae).getByText('Mãe owes you R$167.88 / £7.94')).toBeInTheDocument()
    expect(within(samuel).getByText('Settled')).toBeInTheDocument()
    expect(within(tia).getByText('You owe Tia £15.00')).toBeInTheDocument()
    expect(footer(settlement)).toBeFalsy()
  })

  it('links a Settlement row to its account screen for the month, without a tab', async () => {
    getAccountTemplateGroupsMock.mockResolvedValue([SETTLEMENT])

    renderPage()

    const settlement = await screen.findByRole('table', { name: 'Settlement' })
    expect(within(settlement).getByRole('link', { name: 'Mãe' })).toHaveAttribute('href', '/cashflow/accounts/mae?month=2026-10')
  })

  it('sorts the Settlement grid by the main-currency balance', async () => {
    getAccountTemplateGroupsMock.mockResolvedValue([SETTLEMENT])

    renderPage()

    const settlement = await screen.findByRole('table', { name: 'Settlement' })
    fireEvent.click(within(settlement).getByRole('button', { name: 'Balance' }))

    expect(accountNames(screen.getByRole('table', { name: 'Settlement' }))).toEqual(['Tia', 'SamuelInactive', 'Mãe'])
  })

  it('shows an unknown template under its own name with a "Total" footer and plain-text rows', async () => {
    getAccountTemplateGroupsMock.mockResolvedValue([
      { templateName: 'Loans', members: [member('loan', 'Car loan', 'Loans', { balance: 50 })], totals: [{ currency: 'GBP', total: 50 }] },
    ])

    renderPage()

    const table = await screen.findByRole('table', { name: 'Loans' })
    expect(headers(table)).toEqual(['Account', 'Currency', 'Balance'])
    expect(within(table).queryByRole('link')).not.toBeInTheDocument()
    expect(footer(table)).toBe('Total GBP: 50.00')
  })

  it('shows the month the balances belong to', async () => {
    renderPage()

    expect(await screen.findByText('Balances at end of October 2026')).toBeInTheDocument()
  })

  it('shows one loading state while the groups are pending', () => {
    getAccountTemplateGroupsMock.mockReturnValue(new Promise(() => {}))

    renderPage()

    expect(screen.getAllByText('Loading...')).toHaveLength(1)
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('shows one error with Try again and no partial grid, and Try again reloads', async () => {
    getAccountTemplateGroupsMock.mockRejectedValueOnce(new Error('boom'))

    renderPage()

    expect(await screen.findByText('Unable to load the accounts')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('table', { name: 'Banks' })).toBeInTheDocument()
    expect(getAccountTemplateGroupsMock).toHaveBeenCalledTimes(2)
  })

  it('shows "No accounts this month" when no template has a listed account', async () => {
    getAccountTemplateGroupsMock.mockResolvedValue([])

    renderPage()

    expect(await screen.findByText('No accounts this month')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('opens the account screen when a row link is followed', async () => {
    render(
      <MemoryRouter initialEntries={['/cashflow/accounts']}>
        <Routes>
          <Route path="/cashflow/accounts" element={<AccountsPage />} />
          <Route path="/cashflow/accounts/:accountId" element={<p>Account screen</p>} />
        </Routes>
      </MemoryRouter>,
    )

    fireEvent.click(await screen.findByRole('link', { name: 'Chase' }))

    expect(screen.getByText('Account screen')).toBeInTheDocument()
  })
})
