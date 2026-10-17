import { useCallback } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { CategoryCreateDto, CategoryDto, CategoryUpdateDto } from '../api/types'
import { useCrudResource } from './useCrudResource'

export interface CategoriesData {
  categories: CategoryDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createCategory: (request: CategoryCreateDto) => Promise<CategoryDto>
  updateCategory: (id: string, request: CategoryUpdateDto) => Promise<CategoryDto>
  deletingId: string | null
  deleteError: string | null
  deleteCategory: (id: string) => void
}

const NO_CATEGORIES: CategoryDto[] = []

export function useCategories(): CategoriesData {
  const resource = useCrudResource({
    load: () => apiClient.getCategories(),
    initialData: NO_CATEGORIES,
    loadErrorMessage: 'Unable to load categories',
  })
  const { mutateAndRefresh, runPendingMutation } = resource

  const createCategory = useCallback(
    (request: CategoryCreateDto) => mutateAndRefresh(() => apiClient.createCategory(request)),
    [mutateAndRefresh],
  )

  const updateCategory = useCallback(
    (id: string, request: CategoryUpdateDto) => mutateAndRefresh(() => apiClient.updateCategory(id, request)),
    [mutateAndRefresh],
  )

  const deleteCategory = useCallback(
    (id: string) => runPendingMutation(id, () => apiClient.deleteCategory(id), 'Failed to delete category'),
    [runPendingMutation],
  )

  return {
    categories: resource.data,
    isLoading: resource.isLoading,
    error: resource.error,
    retry: resource.retry,
    createCategory,
    updateCategory,
    deletingId: resource.pendingId,
    deleteError: resource.pendingError,
    deleteCategory,
  }
}
