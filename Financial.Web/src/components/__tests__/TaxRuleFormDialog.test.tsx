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

    expect(screen.getByRole('heading', { name: 'Create Tax Rule' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Label/)).toHaveValue('')
    expect(screen.getByLabelText('Jurisdiction')).not.toBeDisabled()
    expect(screen.getByLabelText('Event Category')).not.toBeDisabled()
  })

  it('renders in edit mode pre-filled, with jurisdiction and event category locked', () => {
    render(<TaxRuleFormDialog taxRule={RULE} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Edit Tax Rule' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Label/)).toHaveValue('UK interest rule')
    expect(screen.getByLabelText('Description')).toHaveValue('Some notes')
    expect(screen.getByLabelText(/^Effective From/)).toHaveValue('2026-01-01')
    expect(screen.getByLabelText('Effective To')).toHaveValue('2026-12-31')
    expect(screen.getByLabelText('Jurisdiction')).toBeDisabled()
    expect(screen.getByLabelText('Event Category')).toBeDisabled()
  })

  it('disables Save and shows a validation message when the label is blank', () => {
    render(<TaxRuleFormDialog taxRule={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/^Effective From/), { target: { value: '2026-01-01' } })

    expect(screen.getByText('Label is required.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('rejects an effective range where From is on or after To, inline before submission', () => {
    const onSubmit = vi.fn()
    render(<TaxRuleFormDialog taxRule={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Label/), { target: { value: 'BR dividend rule' } })
    fireEvent.change(screen.getByLabelText(/^Effective From/), { target: { value: '2026-06-01' } })
    fireEvent.change(screen.getByLabelText('Effective To'), { target: { value: '2026-01-01' } })

    expect(screen.getByText('Effective From must be before Effective To.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('submits the create fields, including jurisdiction and event category', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<TaxRuleFormDialog taxRule={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText('Jurisdiction'), { target: { value: 'UK' } })
    fireEvent.change(screen.getByLabelText('Event Category'), { target: { value: 'Interest' } })
    fireEvent.change(screen.getByLabelText(/^Label/), { target: { value: '  UK interest rule  ' } })
    fireEvent.change(screen.getByLabelText('Description'), { target: { value: 'Notes' } })
    fireEvent.change(screen.getByLabelText(/^Effective From/), { target: { value: '2026-01-01' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith('UK', 'Interest', 'UK interest rule', 'Notes', '2026-01-01', null),
    )
  })

  it('shows a server-side overlap rejection inline naming the conflicting rule', async () => {
    const onSubmit = vi.fn().mockRejectedValue(
      new Error('This range overlaps existing rule "BR dividend withholding" (2026-01-01–present).'),
    )
    render(<TaxRuleFormDialog taxRule={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Label/), { target: { value: 'Another rule' } })
    fireEvent.change(screen.getByLabelText(/^Effective From/), { target: { value: '2026-06-01' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(
      await screen.findByText('This range overlaps existing rule "BR dividend withholding" (2026-01-01–present).'),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).not.toBeDisabled()
  })

  it('calls onCancel when Cancel is clicked', () => {
    const onCancel = vi.fn()
    render(<TaxRuleFormDialog taxRule={null} onCancel={onCancel} onSubmit={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
  })
})
