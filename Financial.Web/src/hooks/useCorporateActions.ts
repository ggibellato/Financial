import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AssetDetailsDto, CorporateActionDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { getErrorMessage, parseValidatedNumber, toInputDate, todayIsoDate } from '../utils/formatters'

export type CorporateActionFormField =
  | 'formEffectiveDate'
  | 'formType'
  | 'formRatioNumerator'
  | 'formRatioDenominator'
  | 'formRatio'
  | 'formNote'

interface CorporateActionsState {
  asset: AssetDetailsDto | null
  isLoading: boolean
  error: string | null
  retryCount: number
  isFormVisible: boolean
  editingId: string | null
  formEffectiveDate: string
  formType: string
  formRatioNumerator: string
  formRatioDenominator: string
  formNote: string
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<CorporateActionFormField, string>>
  deleteError: string | null
}

type CorporateActionsAction =
  | { type: 'RESET' }
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: AssetDetailsDto }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SHOW_NEW_FORM' }
  | { type: 'SHOW_EDIT_FORM'; payload: CorporateActionDto }
  | { type: 'CANCEL_FORM' }
  | { type: 'SET_FORM_FIELD'; payload: { field: CorporateActionFormField; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS'; payload: AssetDetailsDto }
  | { type: 'SAVE_ERROR'; payload: { message: string | null; fields: Partial<Record<CorporateActionFormField, string>> } }
  | { type: 'DELETE_SUCCESS'; payload: AssetDetailsDto }
  | { type: 'DELETE_ERROR'; payload: string }

const BLANK_FORM = {
  isFormVisible: false,
  editingId: null,
  formEffectiveDate: '',
  formType: 'Split',
  formRatioNumerator: '',
  formRatioDenominator: '',
  formNote: '',
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
} as const

const INITIAL_STATE: CorporateActionsState = {
  asset: null,
  isLoading: false,
  error: null,
  retryCount: 0,
  ...BLANK_FORM,
  deleteError: null,
}

function reducer(state: CorporateActionsState, action: CorporateActionsAction): CorporateActionsState {
  switch (action.type) {
    case 'RESET':
      return INITIAL_STATE
    case 'FETCH_START':
      return { ...INITIAL_STATE, isLoading: true, retryCount: state.retryCount }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, asset: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1, error: null }
    case 'SHOW_NEW_FORM':
      return {
        ...state,
        isFormVisible: true,
        editingId: null,
        formEffectiveDate: todayIsoDate(),
        formType: 'Split',
        formRatioNumerator: '',
        formRatioDenominator: '',
        formNote: '',
        saveError: null,
        saveErrorFields: {},
        isSaving: false,
      }
    case 'SHOW_EDIT_FORM': {
      const a = action.payload
      return {
        ...state,
        isFormVisible: true,
        editingId: a.id,
        formEffectiveDate: toInputDate(a.effectiveDate),
        formType: a.type,
        // The API only stores the converted decimal factor, not the N/M pair the user typed -
        // factor/1 reproduces the same factor for editing without inventing a fake denominator.
        formRatioNumerator: a.ratioFactor !== null ? String(a.ratioFactor) : '',
        formRatioDenominator: '1',
        formNote: a.note ?? '',
        saveError: null,
        saveErrorFields: {},
        isSaving: false,
      }
    }
    case 'CANCEL_FORM':
      return { ...state, ...BLANK_FORM }
    case 'SET_FORM_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null, saveErrorFields: {} }
    case 'SAVE_SUCCESS':
      return { ...state, ...BLANK_FORM, asset: action.payload, deleteError: state.deleteError }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload.message, saveErrorFields: action.payload.fields }
    case 'DELETE_SUCCESS':
      return { ...state, asset: action.payload, deleteError: null }
    case 'DELETE_ERROR':
      return { ...state, deleteError: action.payload }
    default:
      return state
  }
}

export interface CorporateActionsData {
  asset: AssetDetailsDto | null
  corporateActions: CorporateActionDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  isFormVisible: boolean
  editingId: string | null
  formEffectiveDate: string
  formType: string
  formRatioNumerator: string
  formRatioDenominator: string
  formNote: string
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<CorporateActionFormField, string>>
  deleteError: string | null
  showNewForm: () => void
  showEditForm: (action: CorporateActionDto) => void
  cancelForm: () => void
  setFormField: (field: CorporateActionFormField, value: string) => void
  saveForm: () => void
  deleteCorporateAction: (id: string) => void
}

// Asset-only, like useDisposals - a broker/portfolio has no single corporate-action history to show.
export function useCorporateActions(): CorporateActionsData {
  const { selectedNode, scope } = useSelectedNode()
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    if (
      !selectedNode ||
      selectedNode.nodeType !== 'Asset' ||
      !selectedNode.portfolioName ||
      !selectedNode.assetName
    ) {
      dispatch({ type: 'RESET' })
      return
    }

    dispatch({ type: 'FETCH_START' })

    void apiClient
      .getAssetDetails(selectedNode.brokerName, selectedNode.portfolioName, selectedNode.assetName, scope)
      .then((result) => dispatch({ type: 'FETCH_SUCCESS', payload: result }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load corporate actions') })
      })
  }, [selectedNode, scope, state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const showNewForm = useCallback(() => dispatch({ type: 'SHOW_NEW_FORM' }), [])

  const showEditForm = useCallback((action: CorporateActionDto) => {
    dispatch({ type: 'SHOW_EDIT_FORM', payload: action })
  }, [])

  const cancelForm = useCallback(() => dispatch({ type: 'CANCEL_FORM' }), [])

  const setFormField = useCallback((field: CorporateActionFormField, value: string) => {
    dispatch({ type: 'SET_FORM_FIELD', payload: { field, value } })
  }, [])

  const saveForm = useCallback(() => {
    if (!selectedNode?.portfolioName || !selectedNode.assetName) return

    const { formEffectiveDate, formRatioNumerator, formRatioDenominator, formNote, editingId } = state
    const errors: Partial<Record<CorporateActionFormField, string>> = {}

    if (!formEffectiveDate.trim()) {
      errors.formEffectiveDate = 'Effective date is required'
    }

    const numerator = parseValidatedNumber(formRatioNumerator)
    const denominator = parseValidatedNumber(formRatioDenominator)
    let ratioFactor = 0
    if (numerator === null || numerator <= 0 || denominator === null || denominator <= 0) {
      errors.formRatio = 'Enter a valid split ratio other than 1-for-1'
    } else {
      ratioFactor = numerator / denominator
      if (ratioFactor === 1) {
        errors.formRatio = 'Enter a valid split ratio other than 1-for-1'
      }
    }

    if (Object.keys(errors).length > 0) {
      dispatch({ type: 'SAVE_ERROR', payload: { message: Object.values(errors)[0] ?? null, fields: errors } })
      return
    }

    dispatch({ type: 'SAVE_START' })

    const base = {
      brokerName: selectedNode.brokerName,
      portfolioName: selectedNode.portfolioName,
      assetName: selectedNode.assetName,
      effectiveDate: formEffectiveDate,
      ratioFactor,
      note: formNote.trim() === '' ? null : formNote,
    }

    const call = editingId ? apiClient.updateSplit({ ...base, id: editingId }) : apiClient.addSplit(base)

    void call
      .then((result) => dispatch({ type: 'SAVE_SUCCESS', payload: result }))
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: { message: getErrorMessage(err, 'Failed to save corporate action'), fields: {} },
        })
      })
  }, [selectedNode, state])

  const deleteCorporateAction = useCallback(
    (id: string) => {
      if (!selectedNode?.portfolioName || !selectedNode.assetName) return

      void apiClient
        .deleteCorporateAction({
          brokerName: selectedNode.brokerName,
          portfolioName: selectedNode.portfolioName,
          assetName: selectedNode.assetName,
          id,
        })
        .then((result) => dispatch({ type: 'DELETE_SUCCESS', payload: result }))
        .catch((err: unknown) => {
          dispatch({
            type: 'DELETE_ERROR',
            payload: getErrorMessage(err, 'Failed to delete corporate action'),
          })
        })
    },
    [selectedNode],
  )

  return {
    asset: state.asset,
    corporateActions: state.asset?.corporateActions ?? [],
    isLoading: state.isLoading,
    error: state.error,
    retry,
    isFormVisible: state.isFormVisible,
    editingId: state.editingId,
    formEffectiveDate: state.formEffectiveDate,
    formType: state.formType,
    formRatioNumerator: state.formRatioNumerator,
    formRatioDenominator: state.formRatioDenominator,
    formNote: state.formNote,
    isSaving: state.isSaving,
    saveError: state.saveError,
    saveErrorFields: state.saveErrorFields,
    deleteError: state.deleteError,
    showNewForm,
    showEditForm,
    cancelForm,
    setFormField,
    saveForm,
    deleteCorporateAction,
  }
}
