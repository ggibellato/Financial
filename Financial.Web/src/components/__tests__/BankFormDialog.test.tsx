import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import BankFormDialog from '../BankFormDialog'

describe('BankFormDialog', () => {
  it('renders in create mode with an empty name and round-up disabled', () => {
    render(<BankFormDialog bank={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Create Bank' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
    expect(screen.getByLabelText('Round-up')).not.toBeChecked()
  })

  it('renders in edit mode pre-filled with the bank being edited', () => {
    render(
      <BankFormDialog
        bank={{
          id: 'b1',
          name: 'Barclays',
          roundUpEnabled: true,
          openingBalance: 0,
          openingBalanceDate: '2026-01-01',
          hasReferences: false,
        }}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit Bank' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('Barclays')
    expect(screen.getByLabelText('Round-up')).toBeChecked()
  })

  it('submits the trimmed name and the toggled round-up flag', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<BankFormDialog bank={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  Barclays  ' } })
    fireEvent.click(screen.getByLabelText('Round-up'))
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Barclays', true))
  })
})
