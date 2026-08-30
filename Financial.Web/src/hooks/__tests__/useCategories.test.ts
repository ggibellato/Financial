import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CategoryDto } from '../../api/types'
import { useCategories } from '../useCategories'

const { getCategoriesMock, createCategoryMock, updateCategoryMock, deleteCategoryMock } = vi.hoisted(() => ({
  getCategoriesMock: vi.fn<FinancialApiClient['getCategories']>(),
  createCategoryMock: vi.fn<FinancialApiClient['createCategory']>(),
  updateCategoryMock: vi.fn<FinancialApiClient['updateCategory']>(),
  deleteCategoryMock: vi.fn<FinancialApiClient['deleteCategory']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getCategories: getCategoriesMock,
    createCategory: createCategoryMock,
    updateCategory: updateCategoryMock,
    deleteCategory: deleteCategoryMock,
  } as Partial<FinancialApiClient>,
}))

const CATEGORIES: CategoryDto[] = [
  { id: 'c1', name: 'Mercado', active: true, isInvestment: false, isTithe: false, hasReferences: true },
  { id: 'c2', name: 'Extras', active: true, isInvestment: false, isTithe: false, hasReferences: false },
]

describe('useCategories', () => {
  beforeEach(() => {
    getCategoriesMock.mockReset()
    createCategoryMock.mockReset()
    updateCategoryMock.mockReset()
    deleteCategoryMock.mockReset()
    getCategoriesMock.mockResolvedValue(CATEGORIES)
  })

  it('fetches the category list once on mount', async () => {
    const { result } = renderHook(() => useCategories())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getCategoriesMock).toHaveBeenCalledTimes(1)
    expect(result.current.categories).toEqual(CATEGORIES)
  })

  it('surfaces a fetch error', async () => {
    getCategoriesMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useCategories())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('retry re-fetches the list', async () => {
    const { result } = renderHook(() => useCategories())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.retry())

    await waitFor(() => expect(getCategoriesMock).toHaveBeenCalledTimes(2))
  })

  it('createCategory calls the API and re-fetches the list', async () => {
    createCategoryMock.mockResolvedValue({
      id: 'c3',
      name: 'Lazer',
      active: true,
      isInvestment: false,
      isTithe: false,
      hasReferences: false,
    })
    const { result } = renderHook(() => useCategories())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.createCategory({ name: 'Lazer', active: true, isInvestment: false, isTithe: false })
    })

    expect(createCategoryMock).toHaveBeenCalledWith({ name: 'Lazer', active: true, isInvestment: false, isTithe: false })
    await waitFor(() => expect(getCategoriesMock).toHaveBeenCalledTimes(2))
  })

  it('createCategory propagates a rejected promise to the caller without swallowing it', async () => {
    createCategoryMock.mockRejectedValue(new Error('A category named "Mercado" already exists.'))
    const { result } = renderHook(() => useCategories())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await expect(
      result.current.createCategory({ name: 'Mercado', active: true, isInvestment: false, isTithe: false }),
    ).rejects.toThrow('A category named "Mercado" already exists.')
  })

  it('updateCategory calls the API and re-fetches the list', async () => {
    updateCategoryMock.mockResolvedValue({
      id: 'c1',
      name: 'Mercado Renamed',
      active: false,
      isInvestment: true,
      isTithe: true,
      hasReferences: true,
    })
    const { result } = renderHook(() => useCategories())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.updateCategory('c1', { name: 'Mercado Renamed', active: false, isInvestment: true, isTithe: true })
    })

    expect(updateCategoryMock).toHaveBeenCalledWith('c1', {
      name: 'Mercado Renamed',
      active: false,
      isInvestment: true,
      isTithe: true,
    })
    await waitFor(() => expect(getCategoriesMock).toHaveBeenCalledTimes(2))
  })

  it('deleteCategory calls the API and re-fetches the list', async () => {
    deleteCategoryMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useCategories())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteCategory('c2'))

    await waitFor(() => expect(result.current.deletingId).toBeNull())
    expect(deleteCategoryMock).toHaveBeenCalledWith('c2')
    await waitFor(() => expect(getCategoriesMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a delete error without re-fetching', async () => {
    deleteCategoryMock.mockRejectedValue(new Error('Cannot delete a category that is still used by a transaction.'))
    const { result } = renderHook(() => useCategories())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteCategory('c1'))

    await waitFor(() =>
      expect(result.current.deleteError).toBe('Cannot delete a category that is still used by a transaction.'),
    )
    expect(getCategoriesMock).toHaveBeenCalledTimes(1)
  })
})
