import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { PotBalanceDto } from '../../api/types'
import ManagePotsDialog from '../ManagePotsDialog'

const { updatePotsMock } = vi.hoisted(() => ({
  updatePotsMock: vi.fn<FinancialApiClient['updatePots']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { updatePots: updatePotsMock } as Partial<FinancialApiClient>,
}))

const POTS: PotBalanceDto[] = [
  { id: 'pot-home', name: 'Home', splitPercentage: 60, isActive: true, balance: 100 },
  { id: 'pot-trips', name: 'Trips', splitPercentage: 40, isActive: true, balance: 50 },
  { id: 'pot-old', name: 'Old car', splitPercentage: 0, isActive: false, balance: 0 },
]

function renderDialog(pots: PotBalanceDto[] = POTS) {
  const onSaved = vi.fn()
  const onClose = vi.fn()
  render(<ManagePotsDialog potsAccountId="reserva" accountName="Reserva" initialPots={pots} onSaved={onSaved} onClose={onClose} />)
  return { onSaved, onClose, user: userEvent.setup() }
}

// Under jsdom, Tabster can leave aria-hidden on a newly opened Fluent dialog, which hides its contents from role queries.
function button(name: string) {
  return screen.getByText(name, { selector: 'button' })
}

function total() {
  return screen.getByText(/Active pots total/, { selector: 'p' })
}

function potField(pot: string, label: RegExp) {
  return within(screen.getByRole('group', { name: `Pot ${pot}`, hidden: true })).getByLabelText(label)
}

const NAME = /^Name/
const SPLIT = /^Split %/
const ACTIVE = /^Active/

async function setText(user: ReturnType<typeof userEvent.setup>, pot: string, label: RegExp, value: string) {
  const input = potField(pot, label)
  await user.clear(input)
  if (value) await user.type(input, value)
}

beforeEach(() => {
  updatePotsMock.mockReset().mockResolvedValue(undefined)
})

describe('ManagePotsDialog', () => {
  it('names the account, lists every pot and says pots cannot be deleted', () => {
    renderDialog()

    expect(screen.getByText('Reserva', { selector: 'strong' })).toBeInTheDocument()
    expect(potField('Home', NAME)).toHaveValue('Home')
    expect(potField('Trips', SPLIT)).toHaveValue(40)
    expect(potField('Old car', ACTIVE)).not.toBeChecked()
    expect(screen.getByText(/can't be deleted/)).toBeInTheDocument()
    expect(screen.queryByLabelText(/^Remove/)).not.toBeInTheDocument()
    expect(within(screen.getByRole('group', { name: 'Pot Home', hidden: true })).getByText('Split %')).toBeInTheDocument()
  })

  it('keeps Save disabled while the list is unchanged, also after an edit is undone', async () => {
    const { user } = renderDialog()
    expect(button('Save')).toBeDisabled()

    await setText(user, 'Home', SPLIT, '55')
    expect(button('Save')).toBeEnabled()

    await setText(user, 'Home', SPLIT, '60')
    expect(button('Save')).toBeDisabled()
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-14] keeps a running total of the active pots that says what to fix', async () => {
    const { user } = renderDialog()
    expect(total()).toHaveTextContent(/^Active pots total: 100\.00%\.$/)

    await setText(user, 'Home', SPLIT, '55')
    expect(total()).toHaveTextContent('Active pots total: 95.00%. Active pots must total exactly 100% to save.')

    await user.click(potField('Trips', ACTIVE))
    expect(total()).toHaveTextContent('Active pots total: 55.00%.')
  })

  it('shows up to four decimals, so an almost-right total does not read as 100', async () => {
    const { user } = renderDialog()

    await setText(user, 'Home', SPLIT, '60.004')

    expect(total()).toHaveTextContent('Active pots total: 100.004%.')
  })

  it('explains that every pot being inactive cannot be saved', async () => {
    const { user } = renderDialog()

    await user.click(potField('Home', ACTIVE))
    await user.click(potField('Trips', ACTIVE))

    expect(total()).toHaveTextContent('Active pots total: 0.00%. Keep at least one pot active. Active pots must total exactly 100% to save.')
  })

  it('on an account with no pots says so, and that the first save must total 100', async () => {
    const { user } = renderDialog([])

    expect(screen.getByText('This account has no pots yet.')).toBeInTheDocument()
    expect(total()).toHaveTextContent('Active pots total: 0.00%. Add a pot. Active pots must total exactly 100% to save.')
    expect(button('Save')).toBeDisabled()

    await user.click(button('Add pot'))
    await user.type(potField('new pot 1', NAME), 'Home')
    await setText(user, 'Home', SPLIT, '100')
    await user.click(button('Save'))

    await waitFor(() =>
      expect(updatePotsMock).toHaveBeenCalledWith('reserva', { pots: [{ id: null, name: 'Home', splitPercentage: 100, isActive: true }] }),
    )
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-14] saves all pots together, trimmed, with a new one without an id', async () => {
    const { user, onSaved } = renderDialog()

    await setText(user, 'Home', NAME, '  House  ')
    await setText(user, 'House', SPLIT, '50')
    await user.click(button('Add pot'))
    expect(potField('new pot 1', NAME)).toHaveFocus()
    await user.type(potField('new pot 1', NAME), 'Car')
    await setText(user, 'Car', SPLIT, '10')
    await user.click(button('Save'))

    await waitFor(() =>
      expect(updatePotsMock).toHaveBeenCalledWith('reserva', {
        pots: [
          { id: 'pot-home', name: 'House', splitPercentage: 50, isActive: true },
          { id: 'pot-trips', name: 'Trips', splitPercentage: 40, isActive: true },
          { id: 'pot-old', name: 'Old car', splitPercentage: 0, isActive: false },
          { id: null, name: 'Car', splitPercentage: 10, isActive: true },
        ],
      }),
    )
    expect(onSaved).toHaveBeenCalledTimes(1)
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-14] shows the server 400, keeps the values and allows another save', async () => {
    updatePotsMock.mockRejectedValue(new ApiError('Active pots must total 100% (now 95%).', 400))
    const { user, onSaved } = renderDialog()

    await setText(user, 'Home', SPLIT, '55')
    await user.click(button('Save'))

    expect(await screen.findByText('Active pots must total 100% (now 95%).')).toBeInTheDocument()
    expect(potField('Home', SPLIT)).toHaveValue(55)
    expect(button('Save')).toBeEnabled()
    expect(onSaved).not.toHaveBeenCalled()
  })

  it('shows the server 409 for a repeated name', async () => {
    updatePotsMock.mockRejectedValue(new ApiError('A pot named "Trips" already exists.', 409))
    const { user } = renderDialog()

    await setText(user, 'Home', NAME, 'Trips')
    await user.click(button('Save'))

    expect(await screen.findByText('A pot named "Trips" already exists.')).toBeInTheDocument()
  })

  it('blocks Save and says why on a blank name or a split out of range', async () => {
    const { user } = renderDialog()

    await setText(user, 'Home', NAME, '   ')
    expect(screen.getByText('Pot name is required.')).toBeInTheDocument()
    expect(total()).toHaveTextContent('Fix the marked pots to save.')
    expect(button('Save')).toBeDisabled()

    await setText(user, 'Home', NAME, 'Home')
    await setText(user, 'Home', SPLIT, '100.5')
    expect(screen.getByText('Split percentage must be between 0 and 100.')).toBeInTheDocument()
    expect(button('Save')).toBeDisabled()

    await setText(user, 'Home', SPLIT, '')
    expect(screen.getByText('Split percentage must be between 0 and 100.')).toBeInTheDocument()
    expect(button('Save')).toBeDisabled()
  })

  it('removes a pot added by mistake, leaving the list unchanged and focus on Add pot', async () => {
    const { user } = renderDialog()

    await user.click(button('Add pot'))
    expect(screen.getByLabelText('Remove new pot 1')).toBeInTheDocument()
    await user.click(screen.getByLabelText('Remove new pot 1'))

    expect(screen.queryByRole('group', { name: 'Pot new pot 1', hidden: true })).not.toBeInTheDocument()
    expect(button('Save')).toBeDisabled()
    expect(button('Add pot')).toHaveFocus()
  })

  it('disables every control and shows progress while saving', async () => {
    updatePotsMock.mockReturnValue(new Promise(() => {}))
    const { user } = renderDialog()

    await setText(user, 'Home', SPLIT, '55')
    await user.click(button('Save'))

    expect(potField('Home', SPLIT)).toBeDisabled()
    expect(potField('Trips', ACTIVE)).toBeDisabled()
    expect(button('Add pot')).toBeDisabled()
    const saving = button('Saving…')
    expect(saving).toHaveAttribute('aria-disabled', 'true')
    expect(saving).toHaveFocus()
    expect(button('Cancel')).toBeDisabled()
  })

  it('closes at once when nothing changed', async () => {
    const { user, onClose } = renderDialog()

    await user.click(button('Cancel'))

    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('asks before discarding unsaved changes, and keeps them on Keep editing', async () => {
    const { user, onClose } = renderDialog()
    await setText(user, 'Home', SPLIT, '55')

    await user.click(button('Cancel'))

    const prompt = await waitFor(() => {
      const found = screen
        .getAllByRole('dialog', { hidden: true })
        .find((candidate) => candidate.querySelector('h2')?.textContent === 'Discard unsaved changes?')
      expect(found).toBeDefined()
      return found as HTMLElement
    })
    expect(onClose).not.toHaveBeenCalled()

    await user.click(within(prompt).getByText('Keep editing', { selector: 'button' }))
    expect(onClose).not.toHaveBeenCalled()
    expect(potField('Home', SPLIT)).toHaveValue(55)
  })
})
