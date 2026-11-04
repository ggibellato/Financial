import { useState } from 'react'
import { describe, expect, it, vi } from 'vitest'
import type { BankDto } from '../../api/types'
import { fireEvent, render, screen, within } from '../../test/renderWithFluent'
import { fundingLineErrorKey, type FundingLineValues } from '../../utils/fundingLines'
import FundingLinesField from '../FundingLinesField'

const bank = (id: string, name: string, isActive = true): BankDto => ({
  id,
  name,
  roundUpEnabled: false,
  openingBalance: 0,
  openingBalanceDate: '2026-01-01',
  hasReferences: false,
  isActive,
})

const BANKS = [bank('b1', 'Barclays'), bank('b2', 'Trading 212'), bank('b3', 'Closed', false), bank('b4', 'Old', false)]

const line = (key: string, bankId = '', amount = ''): FundingLineValues => ({ key, bankId, amount })

function renderField(lines: FundingLineValues[], props: Partial<Parameters<typeof FundingLinesField>[0]> = {}) {
  const handlers = { onChange: vi.fn(), onAdd: vi.fn(), onRemove: vi.fn() }
  render(<FundingLinesField lines={lines} banks={BANKS} depositAmount={90} currency="GBP" errors={{}} {...handlers} {...props} />)
  return handlers
}

function Stateful() {
  const [lines, setLines] = useState([line('a', 'b1', '90')])
  return (
    <FundingLinesField
      lines={lines}
      banks={BANKS}
      depositAmount={90}
      currency="GBP"
      errors={{}}
      onChange={() => undefined}
      onAdd={() => setLines((current) => [...current, line(`line-${current.length}`)])}
      onRemove={(key) => setLines((current) => current.filter((candidate) => candidate.key !== key))}
    />
  )
}

describe('FundingLinesField', () => {
  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-02] says how far the lines are from the deposit', () => {
    renderField([line('a', 'b1', '85'), line('b', 'b2', '3')])

    expect(screen.getByRole('status')).toHaveTextContent('Lines add up to £88.00; the deposit is £90.00.')
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-02] says nothing when the lines add up exactly', () => {
    renderField([line('a', 'b1', '85.10'), line('b', 'b2', '4.90')])

    expect(screen.getByRole('status')).toBeEmptyDOMElement()
  })

  it('says nothing until the deposit has an amount', () => {
    renderField([line('a', 'b1', '85')], { depositAmount: null })

    expect(screen.getByRole('status')).toBeEmptyDOMElement()
  })

  it('names each line, lists active banks and keeps a bank this deposit already uses, marked inactive', () => {
    renderField([line('a', 'b3', '90')], { keptBankIds: ['b3'] })

    const group = screen.getByRole('group', { name: 'Funding line 1' })
    const options = within(within(group).getByLabelText(/^Bank/)).getAllByRole('option').map((option) => option.textContent)
    expect(options).toEqual(['Select a bank', 'Barclays', 'Trading 212', 'Closed (inactive)'])
    expect(within(group).getByLabelText(/^Amount \(GBP\)/)).toHaveValue(90)
  })

  it('offers no remove button on the only line', () => {
    renderField([line('a')])

    expect(screen.queryByRole('button', { name: /Remove funding line/ })).not.toBeInTheDocument()
  })

  it('reports changes, adds and removals', () => {
    const { onChange, onAdd, onRemove } = renderField([line('a', 'b1', '85'), line('b', 'b2', '5')])

    fireEvent.change(within(screen.getByRole('group', { name: 'Funding line 2' })).getByLabelText(/^Amount/), { target: { value: '6' } })
    fireEvent.change(within(screen.getByRole('group', { name: 'Funding line 1' })).getByLabelText(/^Bank/), { target: { value: 'b2' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add line' }))
    fireEvent.click(screen.getByRole('button', { name: 'Remove funding line 2' }))

    expect(onChange).toHaveBeenCalledWith('b', { amount: '6' })
    expect(onChange).toHaveBeenCalledWith('a', { bankId: 'b2' })
    expect(onAdd).toHaveBeenCalledOnce()
    expect(onRemove).toHaveBeenCalledWith('b')
  })

  it('stops at five lines and says why', () => {
    renderField(['a', 'b', 'c', 'd', 'e'].map((key) => line(key, 'b1', '18')))

    expect(screen.getByRole('button', { name: 'Add line' })).toBeDisabled()
    expect(screen.getByText('A deposit has at most 5 funding lines.')).toBeInTheDocument()
  })

  it('shows each line its own errors', () => {
    const first = line('a')
    renderField([first], { errors: { [fundingLineErrorKey(first, 'bankId')]: 'Choose a bank.', [fundingLineErrorKey(first, 'amount')]: 'Amount must be greater than zero.' } })

    expect(screen.getByLabelText(/^Bank/)).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByText('Choose a bank.')).toBeInTheDocument()
    expect(screen.getByText('Amount must be greater than zero.')).toBeInTheDocument()
  })

  it('moves focus to a new line, and to Add line after a removal', () => {
    render(<Stateful />)

    fireEvent.click(screen.getByRole('button', { name: 'Add line' }))
    expect(within(screen.getByRole('group', { name: 'Funding line 2' })).getByLabelText(/^Bank/)).toHaveFocus()

    fireEvent.click(screen.getByRole('button', { name: 'Remove funding line 2' }))
    expect(screen.getByRole('button', { name: 'Add line' })).toHaveFocus()
  })
})
