import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AssetAdminCreateDto, AssetAdminDto, AssetAdminUpdateDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

interface AssetsState {
  assets: AssetAdminDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  deletingKey: string | null
  deleteError: string | null
}

type AssetsAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: AssetAdminDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'DELETE_START'; payload: string }
  | { type: 'DELETE_SUCCESS' }
  | { type: 'DELETE_ERROR'; payload: string }

const INITIAL_STATE: AssetsState = {
  assets: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  deletingKey: null,
  deleteError: null,
}

function reducer(state: AssetsState, action: AssetsAction): AssetsState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, assets: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'DELETE_START':
      return { ...state, deletingKey: action.payload, deleteError: null }
    case 'DELETE_SUCCESS':
      return { ...state, deletingKey: null }
    case 'DELETE_ERROR':
      return { ...state, deletingKey: null, deleteError: action.payload }
    default:
      return state
  }
}

/** Uniquely identifies an asset row: names are only unique within their own portfolio. */
export function assetKey(brokerName: string, portfolioName: string, assetName: string): string {
  return `${brokerName}/${portfolioName}/${assetName}`
}

export interface AssetsData {
  assets: AssetAdminDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createAsset: (request: AssetAdminCreateDto) => Promise<AssetAdminDto>
  updateAsset: (
    brokerName: string,
    portfolioName: string,
    currentName: string,
    request: AssetAdminUpdateDto,
  ) => Promise<AssetAdminDto>
  deletingKey: string | null
  deleteError: string | null
  /** Archives the asset in place: same portfolio name in Historic Investments. */
  deleteAsset: (brokerName: string, portfolioName: string, assetName: string) => void
}

export function useAssets(): AssetsData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getAdminAssets()
      .then((assets) => dispatch({ type: 'FETCH_SUCCESS', payload: assets }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load assets') })
      })
  }, [state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const createAsset = useCallback(async (request: AssetAdminCreateDto) => {
    const created = await apiClient.createAsset(request)
    dispatch({ type: 'RETRY' })
    return created
  }, [])

  const updateAsset = useCallback(
    async (brokerName: string, portfolioName: string, currentName: string, request: AssetAdminUpdateDto) => {
      const updated = await apiClient.updateAsset(brokerName, portfolioName, currentName, request)
      dispatch({ type: 'RETRY' })
      return updated
    },
    [],
  )

  const deleteAsset = useCallback((brokerName: string, portfolioName: string, assetName: string) => {
    dispatch({ type: 'DELETE_START', payload: assetKey(brokerName, portfolioName, assetName) })

    void apiClient
      .archiveAsset({
        brokerName,
        sourcePortfolioName: portfolioName,
        assetName,
        destinationPortfolioName: portfolioName,
      })
      .then(() => {
        dispatch({ type: 'DELETE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({ type: 'DELETE_ERROR', payload: getErrorMessage(err, 'Failed to delete asset') })
      })
  }, [])

  return {
    assets: state.assets,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    createAsset,
    updateAsset,
    deletingKey: state.deletingKey,
    deleteError: state.deleteError,
    deleteAsset,
  }
}
