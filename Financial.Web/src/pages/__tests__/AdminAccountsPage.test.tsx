import { Toaster } from '@fluentui/react-components'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountTemplateDto } from '../../api/types'
import { APP_TOASTER_ID } from '../../hooks/useSuccessToast'
import { account } from '../../test/account'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import AdminAccountsPage from '../AdminAccountsPage'

const { getAccountsMock, getAccountTemplatesMock, createAccountMock, updateAccountMock, deleteAccountMock } = vi.hoisted(
  () => ({
    getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
    getAccountTemplatesMock: vi.fn<FinancialApiClient['getAccountTemplates']>(),
    createAccountMock: vi.fn<FinancialApiClient['createAccount']>(),
    updateAccountMock: vi.fn<FinancialApiClient['updateAccount']>(),
    deleteAccountMock: vi.fn<FinancialApiClient['deleteAccount']>(),
  }),
)

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAccounts: getAccountsMock,
    getAccountTemplates: getAccountTemplatesMock,
    createAccount: createAccountMock,
    updateAccount: updateAccountMock,
    deleteAccount: deleteAccountMock,
  } as Partial<FinancialApiClient>,
}))

const TEMPLATES: AccountTemplateDto[] = [
  { name: 'Bank', currencies: ['GBP'], capabilities: ['HasOpeningBalance', 'SupportsRoundUp'] },
]
const BARCLAYS = account({ id: 'a1', name: 'Barclays', hasRecords: true })
const CHASE = account({ id: 'a2', name: 'Chase', hasRecords: true, isActive: false })
const MONZO = account({ id: 'a3', name: 'Monzo' })

function renderPage() {
  render(
    <>
      <Toaster toasterId={APP_TOASTER_ID} />
      <AdminAccountsPage />
    </>,
  )
}

const rowOf = (name: string) => screen.getByRole('row', { name: new RegExp(name) })

describe('AdminAccountsPage', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    getAccountsMock.mockResolvedValue([BARCLAYS, CHASE, MONZO])
    getAccountTemplatesMock.mockResolvedValue(TEMPLATES)
  })

  it('lists each account with its template, currency and status in words', async () => {
    renderPage()

    expect(await screen.findByRole('table', { name: 'Accounts' })).toBeInTheDocument()
    expect(within(rowOf('Barclays')).getByText('Bank')).toBeInTheDocument()
    expect(within(rowOf('Barclays')).getByText('GBP')).toBeInTheDocument()
    expect(within(rowOf('Barclays')).getByText('Active')).toBeInTheDocument()
    expect(within(rowOf('Chase')).getByText('Inactive')).toBeInTheDocument()
  })

  it('shows the loading state, then the empty state when there are no accounts', async () => {
    getAccountsMock.mockResolvedValue([])
    renderPage()

    expect(screen.getByText('Loading...')).toBeInTheDocument()
    expect(await screen.findByText('No accounts yet')).toBeInTheDocument()
  })

  it('shows a load error with a retry that loads again', async () => {
    getAccountsMock.mockRejectedValueOnce(new Error('Network down'))
    renderPage()

    expect(await screen.findByText('Network down')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('table', { name: 'Accounts' })).toBeInTheDocument()
  })

  it('[AC P60-F06-admin-accounts-page-01] creates a Bank account through New account, shows it in the grid and confirms with a toast', async () => {
    const created = account({ id: 'a4', name: 'Starling' })
    createAccountMock.mockResolvedValue(created)
    renderPage()
    await screen.findByRole('table', { name: 'Accounts' })
    getAccountsMock.mockResolvedValue([BARCLAYS, CHASE, MONZO, created])

    fireEvent.click(screen.getByRole('button', { name: 'New account' }))
    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Starling' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add account' }))

    expect(await screen.findByText('Account created')).toBeInTheDocument()
    expect(createAccountMock).toHaveBeenCalledWith(expect.objectContaining({ templateName: 'Bank', name: 'Starling', currency: 'GBP' }))
    expect(createAccountMock.mock.calls[0][0]).not.toHaveProperty('isActive')
    expect(rowOf('Starling')).toBeInTheDocument()
    expect(screen.queryByRole('dialog', { name: 'New account' })).not.toBeInTheDocument()
  })

  it('[AC P60-F06-admin-accounts-page-02] keeps the dialog open with the duplicate message under Name on a 409', async () => {
    createAccountMock.mockRejectedValue(new ApiError('An account named "Barclays" already exists.', 409))
    renderPage()
    await screen.findByRole('table', { name: 'Accounts' })

    fireEvent.click(screen.getByRole('button', { name: 'New account' }))
    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'barclays' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add account' }))

    expect(await screen.findByText('An account named "Barclays" already exists.')).toBeInTheDocument()
    expect(screen.getByRole('dialog', { name: 'New account' })).toBeInTheDocument()
  })

  it('[AC P60-F06-admin-accounts-page-03] edits an account with records with template and currency read-only, then toasts', async () => {
    updateAccountMock.mockResolvedValue({ ...BARCLAYS, name: 'Barclays UK' })
    renderPage()
    await screen.findByRole('table', { name: 'Accounts' })

    fireEvent.click(screen.getByRole('button', { name: 'Edit Barclays' }))
    expect(screen.getByLabelText(/Template/)).toHaveAttribute('readonly')
    expect(screen.getByLabelText(/Currency/)).toHaveAttribute('readonly')
    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Barclays UK' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Account updated')).toBeInTheDocument()
    expect(updateAccountMock).toHaveBeenCalledWith('a1', expect.objectContaining({ name: 'Barclays UK', isActive: true }))
  })

  it('[AC P60-F06-admin-accounts-page-04] offers Deactivate instead of Delete for an account with records', async () => {
    updateAccountMock.mockResolvedValue({ ...BARCLAYS, isActive: false })
    renderPage()
    await screen.findByRole('table', { name: 'Accounts' })

    fireEvent.click(screen.getByRole('button', { name: 'Delete Barclays' }))
    const dialog = await screen.findByRole('dialog', { name: "Can't delete Barclays" })
    expect(dialog).toHaveTextContent('Barclays has transactions and cannot be deleted. Deactivate it instead.')
    expect(within(dialog).queryByRole('button', { name: 'Delete' })).not.toBeInTheDocument()
    fireEvent.click(within(dialog).getByRole('button', { name: 'Deactivate' }))

    expect(await screen.findByText('Account deactivated')).toBeInTheDocument()
    expect(updateAccountMock).toHaveBeenCalledWith('a1', expect.objectContaining({ isActive: false }))
    expect(deleteAccountMock).not.toHaveBeenCalled()
  })

  it('keeps the Deactivate dialog open with the error when deactivating fails', async () => {
    updateAccountMock.mockRejectedValue(new Error('Server unavailable'))
    renderPage()
    await screen.findByRole('table', { name: 'Accounts' })

    fireEvent.click(screen.getByRole('button', { name: 'Delete Barclays' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Deactivate' }))

    expect(await screen.findByText('Server unavailable')).toBeInTheDocument()
    expect(screen.getByRole('dialog', { name: "Can't delete Barclays" })).toBeInTheDocument()
  })

  it('shows only Close for an inactive account with records', async () => {
    renderPage()
    await screen.findByRole('table', { name: 'Accounts' })

    fireEvent.click(screen.getByRole('button', { name: 'Delete Chase' }))
    const dialog = await screen.findByRole('dialog', { name: "Can't delete Chase" })

    expect(within(dialog).queryByRole('button', { name: 'Deactivate' })).not.toBeInTheDocument()
    fireEvent.click(within(dialog).getByRole('button', { name: 'Close' }))
    await waitFor(() => expect(screen.queryByRole('dialog', { name: "Can't delete Chase" })).not.toBeInTheDocument())
  })

  it('[AC P60-F06-admin-accounts-page-04] deletes an unused account after confirmation and toasts', async () => {
    deleteAccountMock.mockResolvedValue(undefined)
    renderPage()
    await screen.findByRole('table', { name: 'Accounts' })
    getAccountsMock.mockResolvedValue([BARCLAYS, CHASE])

    fireEvent.click(screen.getByRole('button', { name: 'Delete Monzo' }))
    const dialog = await screen.findByRole('dialog', { name: 'Delete Account' })
    fireEvent.click(within(dialog).getByRole('button', { name: 'Delete' }))

    expect(await screen.findByText('Account deleted')).toBeInTheDocument()
    expect(deleteAccountMock).toHaveBeenCalledWith('a3')
    await waitFor(() => expect(screen.queryByRole('row', { name: /Monzo/ })).not.toBeInTheDocument())
  })

  it('shows a refused delete on the page', async () => {
    deleteAccountMock.mockRejectedValue(new ApiError('Monzo has transactions and cannot be deleted. Deactivate it instead.', 409))
    renderPage()
    await screen.findByRole('table', { name: 'Accounts' })

    fireEvent.click(screen.getByRole('button', { name: 'Delete Monzo' }))
    fireEvent.click(within(await screen.findByRole('dialog', { name: 'Delete Account' })).getByRole('button', { name: 'Delete' }))

    expect(await screen.findByText('Monzo has transactions and cannot be deleted. Deactivate it instead.')).toBeInTheDocument()
    expect(screen.queryByText('Account deleted')).not.toBeInTheDocument()
  })
})
