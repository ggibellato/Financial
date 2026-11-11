import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { TaxRuleDto } from '../../api/types'
import TaxRuleFormDialog from '../TaxRuleFormDialog'

const RULE: TaxRuleDto = {
  id: 'rule-1',
  jurisdiction: 'UK',
  eventCategory: 'Interest',
  label: 'UK interest rule',
  description: 'Some notes',
  effectiveFrom: '2026-01-01',
  effectiveTo: '2026-12-31',
}

describe('TaxRuleFormDialog', () => {
  it('renders in create mode with defaults and an empty label', () => {
    render(<TaxRuleFormDialog taxRule={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'New tax rule' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Label/)).toHaveValue('')
    expect(screen.getByLabelText('Jurisdiction')).not.toBeDisabled()
    expect(screen.getByLabelText('Event category')).not.toBeDisabled()
  })

  it('renders in edit mode pre-filled, with jurisdiction and event category locked', () => {
    render(<TaxRuleFormDialog taxRule={RULE} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Edit tax rule' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Label/)).toHaveValue('UK interest rule')
    expect(screen.getByLabelText('Description')).toHaveValue('Some notes')
    expect(screen.getByLabelText(/^Effective from/)).toHaveValue('2026-01-01')
    expect(screen.getByLabelText('Effective to')).toHaveValue('2026-12-31')
    expect(screen.getByLabelText('Jurisdiction')).toHaveAttribute('readonly')
    expect(screen.getByLabelText('Event category')).toHaveAttribute('readonly')
  })

  it('rejects an effective range where From is on or after To, under Effective from, after Save', async () => {
    const onSubmit = vi.fn()
    render(<TaxRuleFormDialog taxRule={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Label/), { target: { value: 'BR dividend rule' } })
    fireEvent.change(screen.getByLabelText(/^Effective from/), { target: { value: '2026-06-01' } })
    fireEvent.change(screen.getByLabelText('Effective to'), { target: { value: '2026-01-01' } })

    fireEvent.click(screen.getByRole('button', { name: 'Add tax rule' }))

    expect(await screen.findByText('Effective from must be before effective to.')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByLabelText(/^Effective from/)).toHaveFocus())
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('submits the create fields, including jurisdiction and event category', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<TaxRuleFormDialog taxRule={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText('Jurisdiction'), { target: { value: 'UK' } })
    fireEvent.change(screen.getByLabelText('Event category'), { target: { value: 'Interest' } })
    fireEvent.change(screen.getByLabelText(/^Label/), { target: { value: '  UK interest rule  ' } })
    fireEvent.change(screen.getByLabelText('Description'), { target: { value: 'Notes' } })
    fireEvent.change(screen.getByLabelText(/^Effective from/), { target: { value: '2026-01-01' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add tax rule' }))

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith('UK', 'Interest', 'UK interest rule', 'Notes', '2026-01-01', null),
    )
  })

  it('focuses Jurisdiction, the first field, when creating', async () => {
    render(<TaxRuleFormDialog taxRule={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    await waitFor(() => expect(screen.getByLabelText('Jurisdiction')).toHaveFocus())
  })

  it('focuses Label when editing, past the read-only jurisdiction and event category', async () => {
    render(<TaxRuleFormDialog taxRule={RULE} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    await waitFor(() => expect(screen.getByLabelText(/^Label/)).toHaveFocus())
  })
})
