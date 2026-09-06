import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import InvestmentAccountFormDialog from '../InvestmentAccountFormDialog'
import type { CreditCardDto } from '../../api/types'

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

describe('InvestmentAccountFormDialog', () => {
  it('renders in create mode with an empty name, active on, liability off, source none', () => {
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[]} onCancel={vi.fn()} onSubmit={vi.fn()} />)

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
        }}
        creditCards={[]}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit Investment Account' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('ChaseSave')
    expect(screen.getByLabelText('Active')).not.toBeChecked()
    expect(screen.getByLabelText('Liability')).toBeChecked()
  })

  it('disables Save and shows a validation message when the name is blank', () => {
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[]} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '   ' } })

    expect(screen.getByText('Name is required.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('submits the trimmed name, toggled flags, and source none with no card', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[]} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  Monzo Pot  ' } })
    fireEvent.click(screen.getByLabelText('Liability'))
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Monzo Pot', true, true, 'None', null))
  })

  it('shows a server error and re-enables Save when the submit rejects', async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error('An investment account named "ChaseSave" already exists.'))
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[]} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'ChaseSave' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('An investment account named "ChaseSave" already exists.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).not.toBeDisabled()
  })

  it('calls onCancel when Cancel is clicked', () => {
    const onCancel = vi.fn()
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[]} onCancel={onCancel} onSubmit={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
  })

  it('shows no extra field when source is none', () => {
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[activeCard]} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.queryByLabelText('Credit Card')).not.toBeInTheDocument()
    expect(screen.queryByText('Uses the total balance across all reserve buckets')).not.toBeInTheDocument()
  })

  it('shows the card picker populated from active cards when source is credit card', () => {
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[activeCard, inactiveCard]} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText('Source'), { target: { value: 'CreditCard' } })

    const picker = screen.getByLabelText(/^Credit Card/)
    expect(picker).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Platinum Visa 8003' })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: 'Retired Card' })).not.toBeInTheDocument()
  })

  it('shows the reserve buckets caption and no card picker when source is reserve buckets sum', () => {
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[activeCard]} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText('Source'), { target: { value: 'ReserveBucketsSum' } })

    expect(screen.getByText('Uses the total balance across all reserve buckets')).toBeInTheDocument()
    expect(screen.queryByLabelText('Credit Card')).not.toBeInTheDocument()
  })

  it('blocks save and shows an inline error when credit card source has no card selected', () => {
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[activeCard]} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Monzo Pot' } })
    fireEvent.change(screen.getByLabelText('Source'), { target: { value: 'CreditCard' } })

    expect(screen.getByText('Select a credit card')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('submits the selected card id when source is credit card', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<InvestmentAccountFormDialog investmentAccount={null} creditCards={[activeCard]} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Monzo Pot' } })
    fireEvent.change(screen.getByLabelText('Source'), { target: { value: 'CreditCard' } })
    fireEvent.change(screen.getByLabelText(/^Credit Card/), { target: { value: 'cc1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Monzo Pot', true, false, 'CreditCard', 'cc1'))
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
        }}
        creditCards={[activeCard, inactiveCard]}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByLabelText(/^Credit Card/)).toHaveValue('cc2')
    expect(screen.getByRole('option', { name: 'Retired Card' })).toBeInTheDocument()
  })
})
