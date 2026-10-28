import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import InvestmentAccountFormDialog from '../InvestmentAccountFormDialog'
import type { CreditCardDto } from '../../api/types'
import { account, potsAccount } from '../../test/account'

const activeCard: CreditCardDto = {
  id: 'cc1',
  name: 'Platinum Visa 8003',
  isActive: true,
  hasReferences: true,
  nextInvoiceDueDate: null,
  latestInvoiceDate: null,
}

const inactiveCard: CreditCardDto = {
  id: 'cc2',
  name: 'Retired Card',
  isActive: false,
  hasReferences: true,
  nextInvoiceDueDate: null,
  latestInvoiceDate: null,
}

const RESERVA = potsAccount({ id: 'p1', name: 'Reserva' })
const HOLIDAYS = potsAccount({ id: 'p2', name: 'Holidays' })
const OLD_POTS = potsAccount({ id: 'p3', name: 'Old pots', isActive: false })
const ACCOUNTS = [RESERVA, HOLIDAYS, OLD_POTS, account({ id: 'b1', name: 'Barclays' })]

describe('InvestmentAccountFormDialog', () => {
  it('renders in create mode with an empty name, active on, liability off, source none', () => {
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[]} accounts={ACCOUNTS} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Create Investment Account' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
    expect(screen.getByLabelText('Active')).toBeChecked()
    expect(screen.getByLabelText('Liability')).not.toBeChecked()
    expect(screen.getByLabelText('Source')).toHaveValue('None')
  })

  it('renders in edit mode pre-filled with the account being edited', () => {
    render(
      <InvestmentAccountFormDialog
        investmentAccount={{
          id: 'a1',
          name: 'ChaseSave',
          isActive: false,
          isLiability: true,
          hasNonZeroInvestmentSnapshot: false,
          source: 'None',
          creditCardId: null,
          potsAccountId: null,
        }}
        creditCards={[]}
        accounts={ACCOUNTS}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit Investment Account' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('ChaseSave')
    expect(screen.getByLabelText('Active')).not.toBeChecked()
    expect(screen.getByLabelText('Liability')).toBeChecked()
  })

  it('submits the trimmed name, toggled flags, and source none with no card', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[]} accounts={ACCOUNTS} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  Monzo Pot  ' } })
    fireEvent.click(screen.getByLabelText('Liability'))
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Monzo Pot', true, true, 'None', null, null))
  })

  it('shows no extra field when source is none', () => {
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[activeCard]} accounts={ACCOUNTS} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.queryByLabelText('Credit Card')).not.toBeInTheDocument()
    expect(screen.queryByLabelText(/^Pots account/)).not.toBeInTheDocument()
  })

  it('shows the card picker populated from active cards when source is credit card', () => {
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[activeCard, inactiveCard]} accounts={ACCOUNTS} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText('Source'), { target: { value: 'CreditCard' } })

    const picker = screen.getByLabelText(/^Credit Card/)
    expect(picker).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Platinum Visa 8003' })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: 'Retired Card' })).not.toBeInTheDocument()
  })

  it('[AC P60-F09-pots-template-and-reserva-migration-03] lists the active Pots accounts by name when source is Pots account total', () => {
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[activeCard]} accounts={ACCOUNTS} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText('Source'), { target: { value: 'PotsAccountTotal' } })

    const picker = screen.getByRole('combobox', { name: /^Pots account/ })
    expect(picker).toBeRequired()
    expect(Array.from(picker.querySelectorAll('option')).map((option) => option.textContent)).toEqual([
      'Select Pots account…',
      'Holidays',
      'Reserva',
    ])
    expect(screen.getByText("Investment Snapshots will suggest the Pots account's total at the end of the previous month.")).toBeInTheDocument()
    expect(screen.queryByLabelText(/^Credit Card/)).not.toBeInTheDocument()
  })

  it('blocks save until a Pots account is chosen, then submits it', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[]} accounts={ACCOUNTS} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Holiday savings' } })
    fireEvent.change(screen.getByLabelText('Source'), { target: { value: 'PotsAccountTotal' } })

    expect(screen.getByText('Select a Pots account')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()

    fireEvent.change(screen.getByRole('combobox', { name: /^Pots account/ }), { target: { value: 'p2' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Holiday savings', true, false, 'PotsAccountTotal', null, 'p2'))
  })

  it('says when there is no active Pots account to choose', () => {
    render(
      <InvestmentAccountFormDialog
        investmentAccount={null}
        creditCards={[]}
        accounts={[OLD_POTS]}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    fireEvent.change(screen.getByLabelText('Source'), { target: { value: 'PotsAccountTotal' } })

    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toBeDisabled()
    expect(screen.getByRole('option', { name: 'No active Pots accounts' })).toBeInTheDocument()
    expect(screen.getByText('Add a Pots account in Admin → Accounts first.')).toBeInTheDocument()
    expect(screen.queryByText('Select a Pots account')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('treats a linked Pots account that is no longer listed as not chosen', () => {
    render(
      <InvestmentAccountFormDialog
        investmentAccount={{
          id: 'a2',
          name: 'Old savings',
          isActive: true,
          isLiability: false,
          hasNonZeroInvestmentSnapshot: false,
          source: 'PotsAccountTotal',
          creditCardId: null,
          potsAccountId: 'gone',
        }}
        creditCards={[]}
        accounts={ACCOUNTS}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toHaveValue('')
    expect(screen.getByText('Select a Pots account')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('editing an account linked to an inactive Pots account keeps it selected and marked inactive', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(
      <InvestmentAccountFormDialog
        investmentAccount={{
          id: 'a2',
          name: 'Old savings',
          isActive: true,
          isLiability: false,
          hasNonZeroInvestmentSnapshot: false,
          source: 'PotsAccountTotal',
          creditCardId: null,
          potsAccountId: 'p3',
        }}
        creditCards={[]}
        accounts={ACCOUNTS}
        onCancel={vi.fn()}
        onSubmit={onSubmit}
      />,
    )

    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toHaveValue('p3')
    expect(screen.getByRole('option', { name: 'Old pots (inactive)' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Old savings', true, false, 'PotsAccountTotal', null, 'p3'))
  })

  it('blocks save and shows an inline error when credit card source has no card selected', () => {
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[activeCard]} accounts={ACCOUNTS} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Monzo Pot' } })
    fireEvent.change(screen.getByLabelText('Source'), { target: { value: 'CreditCard' } })

    expect(screen.getByText('Select a credit card')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('submits the selected card id when source is credit card', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[activeCard]} accounts={ACCOUNTS} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Monzo Pot' } })
    fireEvent.change(screen.getByLabelText('Source'), { target: { value: 'CreditCard' } })
    fireEvent.change(screen.getByLabelText(/^Credit Card/), { target: { value: 'cc1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Monzo Pot', true, false, 'CreditCard', 'cc1', null))
  })

  it('editing an account with an inactive linked card still shows it selected', () => {
    render(
      <InvestmentAccountFormDialog
        investmentAccount={{
          id: 'a1',
          name: 'PlatinumVisa8003',
          isActive: true,
          isLiability: true,
          hasNonZeroInvestmentSnapshot: false,
          source: 'CreditCard',
          creditCardId: 'cc2',
          potsAccountId: null,
        }}
        creditCards={[activeCard, inactiveCard]}
        accounts={ACCOUNTS}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByLabelText(/^Credit Card/)).toHaveValue('cc2')
    expect(screen.getByRole('option', { name: 'Retired Card' })).toBeInTheDocument()
  })
})
