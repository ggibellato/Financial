import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import StatusMenuButton from '../StatusMenuButton'

const STATUSES = ['Unset', 'Scheduled', 'Paid']

describe('StatusMenuButton', () => {
  it('renders the current status as the button label', () => {
    render(<StatusMenuButton statuses={STATUSES} status="Scheduled" onChange={vi.fn()} />)

    expect(screen.getByRole('button')).toHaveTextContent('Scheduled')
  })

  it('opens the menu and lists all statuses on click', async () => {
    render(<StatusMenuButton statuses={STATUSES} status="Unset" onChange={vi.fn()} />)

    fireEvent.click(screen.getByRole('button'))

    for (const status of STATUSES) {
      expect(await screen.findByRole('menuitem', { name: status })).toBeInTheDocument()
    }
  })

  it('disables and checks the menu item matching the current status', async () => {
    render(<StatusMenuButton statuses={STATUSES} status="Paid" onChange={vi.fn()} />)

    fireEvent.click(screen.getByRole('button'))

    const currentItem = await screen.findByRole('menuitem', { name: 'Paid' })
    expect(currentItem).toHaveAttribute('aria-disabled', 'true')
  })

  it('calls onChange when a different status is selected', async () => {
    const onChange = vi.fn()
    render(<StatusMenuButton statuses={STATUSES} status="Unset" onChange={onChange} />)

    fireEvent.click(screen.getByRole('button'))
    fireEvent.click(await screen.findByRole('menuitem', { name: 'Scheduled' }))

    expect(onChange).toHaveBeenCalledExactlyOnceWith('Scheduled')
  })

  it('does not call onChange when clicking the current status item', async () => {
    const onChange = vi.fn()
    render(<StatusMenuButton statuses={STATUSES} status="Unset" onChange={onChange} />)

    fireEvent.click(screen.getByRole('button'))
    fireEvent.click(await screen.findByRole('menuitem', { name: 'Unset' }))

    expect(onChange).not.toHaveBeenCalled()
  })

  it('disables the trigger when isUpdating is true', () => {
    render(<StatusMenuButton statuses={STATUSES} status="Unset" isUpdating onChange={vi.fn()} />)

    expect(screen.getByRole('button')).toBeDisabled()
  })

  it('is keyboard operable: Tab focuses the trigger, Enter opens the menu, Enter selects a focused item', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<StatusMenuButton statuses={STATUSES} status="Unset" onChange={onChange} />)

    await user.tab()
    expect(screen.getByRole('button')).toHaveFocus()

    await user.keyboard('{Enter}')
    const scheduledItem = await screen.findByRole('menuitem', { name: 'Scheduled' })

    scheduledItem.focus()
    await user.keyboard('{Enter}')

    expect(onChange).toHaveBeenCalledExactlyOnceWith('Scheduled')
  })
})
