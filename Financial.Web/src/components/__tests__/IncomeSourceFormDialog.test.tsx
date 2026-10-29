import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import IncomeSourceFormDialog from '../IncomeSourceFormDialog'
import type { IncomeSourceDto } from '../../api/types'
import { account, potsAccount } from '../../test/account'
import { optionNames as optionsOf } from '../../test-utils/optionNames'

const RESERVA = potsAccount({ id: 'p1', name: 'Reserva' })
const HOLIDAYS = potsAccount({ id: 'p2', name: 'Holidays' })
const OLD_POTS = potsAccount({ id: 'p3', name: 'Old pots', isActive: false })
const ACCOUNTS = [RESERVA, HOLIDAYS, OLD_POTS, account({ id: 'b1', name: 'Barclays' })]

function source(fields: Partial<IncomeSourceDto>): IncomeSourceDto {
  return { id: 's1', name: 'Ariana', isActive: true, group: 'Salary', potsAccountId: null, hasReferences: false, ...fields }
}

function optionNames(): (string | null)[] {
  return optionsOf(screen.getByRole('combobox', { name: /^Pots account/ }))
}

describe('IncomeSourceFormDialog', () => {
  it('renders in create mode with an empty name, Salary group, active on, and no Pots account', () => {
    render(<IncomeSourceFormDialog incomeSource={null} accounts={ACCOUNTS} accountsStatus="ready" onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Create Income Source' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
    expect(screen.getByLabelText('Group')).toHaveValue('Salary')
    expect(screen.getByLabelText('Active')).toBeChecked()
    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toHaveValue('')
  })

  it('renders in edit mode pre-filled with the income source being edited', () => {
    render(
      <IncomeSourceFormDialog
        incomeSource={source({ name: 'Gleison', isActive: false, group: 'NonReportable', potsAccountId: 'p2' })}
        accounts={ACCOUNTS}
        accountsStatus="ready"
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit Income Source' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('Gleison')
    expect(screen.getByLabelText('Group')).toHaveValue('NonReportable')
    expect(screen.getByLabelText('Active')).not.toBeChecked()
    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toHaveValue('p2')
  })

  it('offers None and the active accounts that receive an income split, by name', () => {
    render(<IncomeSourceFormDialog incomeSource={null} accounts={ACCOUNTS} accountsStatus="ready" onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(optionNames()).toEqual(['None', 'Holidays', 'Reserva'])
    expect(screen.getByText("None means incomes from this source aren't split.")).toBeInTheDocument()

    fireEvent.change(screen.getByRole('combobox', { name: /^Pots account/ }), { target: { value: 'p1' } })

    expect(screen.getByText("Incomes from this source split into the account's active pots.")).toBeInTheDocument()
  })

  it('keeps a linked Pots account that is now inactive, marked as such', () => {
    render(
      <IncomeSourceFormDialog
        incomeSource={source({ potsAccountId: 'p3' })}
        accounts={ACCOUNTS}
        accountsStatus="ready"
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(optionNames()).toEqual(['None', 'Holidays', 'Old pots (inactive)', 'Reserva'])
    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toHaveValue('p3')
  })

  it('submits the trimmed name, selected group, active flag and chosen Pots account', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<IncomeSourceFormDialog incomeSource={null} accounts={ACCOUNTS} accountsStatus="ready" onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  Freelance  ' } })
    fireEvent.change(screen.getByLabelText('Group'), { target: { value: 'NonReportable' } })
    fireEvent.change(screen.getByRole('combobox', { name: /^Pots account/ }), { target: { value: 'p2' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Freelance', 'NonReportable', true, 'p2'))
  })

  it('submits null when None is chosen for a source that split before', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(
      <IncomeSourceFormDialog
        incomeSource={source({ potsAccountId: 'p1' })}
        accounts={ACCOUNTS}
        accountsStatus="ready"
        onCancel={vi.fn()}
        onSubmit={onSubmit}
      />,
    )

    fireEvent.change(screen.getByRole('combobox', { name: /^Pots account/ }), { target: { value: '' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Ariana', 'Salary', true, null))
  })

  it('says how to add a Pots account when there is none to choose', () => {
    render(
      <IncomeSourceFormDialog incomeSource={null} accounts={[OLD_POTS]} accountsStatus="ready" onCancel={vi.fn()} onSubmit={vi.fn()} />,
    )

    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toBeDisabled()
    expect(optionNames()).toEqual(['None'])
    expect(screen.getByText('Add a Pots account in Admin → CashFlow → Accounts to split income.')).toBeInTheDocument()
  })

  it('keeps the linked Pots account when accounts could not be loaded', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(
      <IncomeSourceFormDialog
        incomeSource={source({ potsAccountId: 'p1' })}
        accounts={[]}
        accountsStatus="failed"
        onCancel={vi.fn()}
        onSubmit={onSubmit}
      />,
    )

    const picker = screen.getByRole('combobox', { name: /^Pots account/ })
    expect(picker).toBeDisabled()
    expect(picker).toHaveValue('p1')
    expect(optionNames()).toEqual(['None', 'Current Pots account'])
    expect(
      screen.getByText("Accounts couldn't be loaded, so the Pots account can't be changed. Close this and use Retry on the page."),
    ).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Ariana', 'Salary', true, 'p1'))
  })

  it('says the accounts failed to load, not that there is no Pots account, when creating', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(
      <IncomeSourceFormDialog incomeSource={null} accounts={[]} accountsStatus="failed" onCancel={vi.fn()} onSubmit={onSubmit} />,
    )

    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toBeDisabled()
    expect(screen.queryByText(/Add a Pots account/)).not.toBeInTheDocument()
    expect(screen.getByText(/Accounts couldn't be loaded/)).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Freelance' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Freelance', 'Salary', true, null))
  })

  it('shows that accounts are loading and enables the choice once they arrive', () => {
    const { rerender } = render(
      <IncomeSourceFormDialog
        incomeSource={source({ potsAccountId: 'p1' })}
        accounts={[]}
        accountsStatus="loading"
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toBeDisabled()
    expect(screen.getByText('Loading accounts…')).toBeInTheDocument()

    rerender(
      <IncomeSourceFormDialog
        incomeSource={source({ potsAccountId: 'p1' })}
        accounts={ACCOUNTS}
        accountsStatus="ready"
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    const picker = screen.getByRole('combobox', { name: /^Pots account/ })
    expect(picker).toBeEnabled()
    expect(picker).toHaveValue('p1')
    expect(optionNames()).toEqual(['None', 'Holidays', 'Reserva'])
  })

  it('shows the server message when the Pots account is refused', async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error('Reserva is inactive.'))
    render(<IncomeSourceFormDialog incomeSource={null} accounts={ACCOUNTS} accountsStatus="ready" onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Freelance' } })
    fireEvent.change(screen.getByRole('combobox', { name: /^Pots account/ }), { target: { value: 'p1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Reserva is inactive.')).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: /^Pots account/ })).toHaveValue('p1')
  })

  it('disables the Pots account select while saving', async () => {
    render(
      <IncomeSourceFormDialog
        incomeSource={source({})}
        accounts={ACCOUNTS}
        accountsStatus="ready"
        onCancel={vi.fn()}
        onSubmit={() => new Promise(() => {})}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(screen.getByRole('combobox', { name: /^Pots account/ })).toBeDisabled())
  })
})
