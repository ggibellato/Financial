import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { CategoryCreateDto, CategoryDto, CategoryUpdateDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

interface CategoriesState {
  categories: CategoryDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  deletingId: string | null
  deleteError: string | null
}

type CategoriesAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: CategoryDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'DELETE_START'; payload: string }
  | { type: 'DELETE_SUCCESS' }
  | { type: 'DELETE_ERROR'; payload: string }

const INITIAL_STATE: CategoriesState = {
  categories: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  deletingId: null,
  deleteError: null,
}

function reducer(state: CategoriesState, action: CategoriesAction): CategoriesState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, categories: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'DELETE_START':
      return { ...state, deletingId: action.payload, deleteError: null }
    case 'DELETE_SUCCESS':
      return { ...state, deletingId: null }
    case 'DELETE_ERROR':
      return { ...state, deletingId: null, deleteError: action.payload }
    default:
      return state
  }
}

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

export function useCategories(): CategoriesData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getCategories()
      .then((categories) => dispatch({ type: 'FETCH_SUCCESS', payload: categories }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load categories') })
      })
  }, [state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const createCategory = useCallback(async (request: CategoryCreateDto) => {
    const created = await apiClient.createCategory(request)
    dispatch({ type: 'RETRY' })
    return created
  }, [])

  const updateCategory = useCallback(async (id: string, request: CategoryUpdateDto) => {
    const updated = await apiClient.updateCategory(id, request)
    dispatch({ type: 'RETRY' })
    return updated
  }, [])

  const deleteCategory = useCallback((id: string) => {
    dispatch({ type: 'DELETE_START', payload: id })

    void apiClient
      .deleteCategory(id)
      .then(() => {
        dispatch({ type: 'DELETE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({ type: 'DELETE_ERROR', payload: getErrorMessage(err, 'Failed to delete category') })
      })
  }, [])

  return {
    categories: state.categories,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    createCategory,
    updateCategory,
    deletingId: state.deletingId,
    deleteError: state.deleteError,
    deleteCategory,
  }
}
