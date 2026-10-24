import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountTemplateDto } from '../../api/types'
import { account } from '../../test/account'
import { useAdminAccounts } from '../useAdminAccounts'

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

const BARCLAYS = account({ id: 'a1', name: 'Barclays', hasRecords: true, openingBalance: 10, roundUpEnabled: true })
const TEMPLATES: AccountTemplateDto[] = [
  { name: 'Bank', currencies: ['GBP'], capabilities: ['HasOpeningBalance', 'SupportsRoundUp'] },
]

describe('useAdminAccounts', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    getAccountsMock.mockResolvedValue([BARCLAYS])
    getAccountTemplatesMock.mockResolvedValue(TEMPLATES)
  })

  it('loads the accounts and the templates together', async () => {
    const { result } = renderHook(() => useAdminAccounts())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.accounts).toEqual([BARCLAYS])
    expect(result.current.templates).toEqual(TEMPLATES)
  })

  it('surfaces a load error when the templates fail, and retry loads again', async () => {
    getAccountTemplatesMock.mockRejectedValueOnce(new Error('Network down'))
    const { result } = renderHook(() => useAdminAccounts())
    await waitFor(() => expect(result.current.error).toBe('Network down'))

    act(() => result.current.retry())

    await waitFor(() => expect(result.current.templates).toEqual(TEMPLATES))
    expect(result.current.error).toBeNull()
    expect(getAccountsMock).toHaveBeenCalledTimes(2)
  })

  it('creates an account and refreshes the list', async () => {
    const monzo = account({ id: 'a2', name: 'Monzo' })
    createAccountMock.mockResolvedValue(monzo)
    const { result } = renderHook(() => useAdminAccounts())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    getAccountsMock.mockResolvedValue([BARCLAYS, monzo])
    const request = { templateName: 'Bank', name: 'Monzo', currency: 'GBP', openingBalance: 0, balanceStartDate: '2026-10-24', roundUpEnabled: false }

    await act(() => result.current.createAccount(request))

    expect(createAccountMock).toHaveBeenCalledWith(request)
    expect(result.current.accounts).toEqual([BARCLAYS, monzo])
  })

  it('updates an account and refreshes the list', async () => {
    updateAccountMock.mockResolvedValue(BARCLAYS)
    const { result } = renderHook(() => useAdminAccounts())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    const request = { templateName: 'Bank', name: 'Barclays UK', currency: 'GBP', openingBalance: 10, balanceStartDate: '2026-01-01', roundUpEnabled: true, isActive: true }

    await act(() => result.current.updateAccount('a1', request))

    expect(updateAccountMock).toHaveBeenCalledWith('a1', request)
    expect(getAccountsMock).toHaveBeenCalledTimes(2)
  })

  it('deactivates an account by sending its current values with isActive false', async () => {
    updateAccountMock.mockResolvedValue({ ...BARCLAYS, isActive: false })
    const { result } = renderHook(() => useAdminAccounts())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(() => result.current.deactivateAccount(BARCLAYS))

    expect(updateAccountMock).toHaveBeenCalledWith('a1', {
      templateName: 'Bank',
      name: 'Barclays',
      currency: 'GBP',
      openingBalance: 10,
      balanceStartDate: '2026-01-01',
      roundUpEnabled: true,
      isActive: false,
    })
  })

  it('deletes an account and refreshes the list', async () => {
    deleteAccountMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useAdminAccounts())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    getAccountsMock.mockResolvedValue([])

    const onDeleted = vi.fn()

    act(() => result.current.deleteAccount('a1', onDeleted))

    await waitFor(() => expect(result.current.accounts).toEqual([]))
    expect(deleteAccountMock).toHaveBeenCalledWith('a1')
    expect(onDeleted).toHaveBeenCalledOnce()
  })

  it('reports a failed delete as the delete error without refreshing the list', async () => {
    deleteAccountMock.mockRejectedValue(new Error('Barclays has transactions and cannot be deleted. Deactivate it instead.'))
    const { result } = renderHook(() => useAdminAccounts())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    const onDeleted = vi.fn()

    act(() => result.current.deleteAccount('a1', onDeleted))

    await waitFor(() => expect(result.current.deleteError).not.toBeNull())
    expect(onDeleted).not.toHaveBeenCalled()
    expect(result.current.deleteError).toBe('Barclays has transactions and cannot be deleted. Deactivate it instead.')
    expect(result.current.deletingId).toBeNull()
    expect(getAccountsMock).toHaveBeenCalledTimes(1)
  })
})
