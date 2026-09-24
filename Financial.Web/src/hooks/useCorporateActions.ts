import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type {
  AssetDetailsDto,
  CorporateActionDto,
  CorporateActionMergerCreateDto,
  CorporateActionMergerResultDto,
} from '../api/types'
import {
  BLANK_TARGET_ASSET_IDENTITY,
  BLANK_TARGET_ASSET_PICKER_VALUE,
  findAssetByName,
  type TargetAssetPickerValue,
} from '../components/targetAssetPickerValue'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { getErrorMessage, parseValidatedNumber, toInputDate, todayIsoDate } from '../utils/formatters'
import { mapCorporateActionErrorToField } from './mapCorporateActionErrorToField'
import { useAssetSearchOptions, type AssetSearchOptionsData } from './useAssetSearchOptions'

export type CorporateActionFormField =
  | 'formEffectiveDate'
  | 'formType'
  | 'formRatioNumerator'
  | 'formRatioDenominator'
  | 'formRatio'
  | 'formNote'
  | 'formTargetAsset'
  | 'formExchangeRatio'
  | 'formCashInLieu'

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
  formStep: 'fields' | 'confirm'
  formTargetAsset: TargetAssetPickerValue
  formExchangeRatio: string
  formCashInLieu: string
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
  | { type: 'SET_STEP'; payload: 'fields' | 'confirm' }
  | { type: 'SET_TARGET_ASSET'; payload: TargetAssetPickerValue }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS'; payload: AssetDetailsDto }
  | { type: 'SAVE_ERROR'; payload: { message: string | null; fields: Partial<Record<CorporateActionFormField, string>> } }
  | { type: 'DELETE_SUCCESS'; payload: AssetDetailsDto }
  | { type: 'DELETE_ERROR'; payload: string }

// The API only stores the converted decimal factor, not the N/M pair the user typed. A factor
// below 1 (a reverse split) reopens as "1 for 1/factor" rather than "factor for 1" so a 1-for-10
// reverse split shows the whole-number denominator the user actually typed, not "0.1 for 1".
function ratioFactorToFraction(ratioFactor: number | null): [string, string] {
  if (ratioFactor === null) {
    return ['', '1']
  }
  if (ratioFactor >= 1) {
    return [String(ratioFactor), '1']
  }
  return ['1', String(Math.round((1 / ratioFactor) * 10000) / 10000)]
}

const BLANK_FORM = {
  isFormVisible: false,
  editingId: null,
  formEffectiveDate: '',
  formType: 'Split',
  formRatioNumerator: '',
  formRatioDenominator: '',
  formNote: '',
  formStep: 'fields',
  formTargetAsset: BLANK_TARGET_ASSET_PICKER_VALUE,
  formExchangeRatio: '',
  formCashInLieu: '',
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
        formStep: 'fields',
        formTargetAsset: BLANK_TARGET_ASSET_PICKER_VALUE,
        formExchangeRatio: '',
        formCashInLieu: '',
        saveError: null,
        saveErrorFields: {},
        isSaving: false,
      }
    case 'SHOW_EDIT_FORM': {
      const a = action.payload
      const [formRatioNumerator, formRatioDenominator] = ratioFactorToFraction(a.ratioFactor)
      return {
        ...state,
        isFormVisible: true,
        editingId: a.id,
        formEffectiveDate: toInputDate(a.effectiveDate),
        formType: a.type,
        formRatioNumerator,
        formRatioDenominator,
        formNote: a.note ?? '',
        formStep: 'fields',
        formTargetAsset:
          a.type === 'Merger'
            ? { assetName: a.linkedAssetName ?? '', identity: BLANK_TARGET_ASSET_IDENTITY }
            : BLANK_TARGET_ASSET_PICKER_VALUE,
        formExchangeRatio: a.exchangeRatio !== null ? String(a.exchangeRatio) : '',
        formCashInLieu: a.cashInLieu !== null ? String(a.cashInLieu) : '',
        saveError: null,
        saveErrorFields: {},
        isSaving: false,
      }
    }
    case 'CANCEL_FORM':
      return { ...state, ...BLANK_FORM }
    case 'SET_FORM_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SET_STEP':
      return { ...state, formStep: action.payload, saveError: null, saveErrorFields: {} }
    case 'SET_TARGET_ASSET':
      return { ...state, formTargetAsset: action.payload }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null, saveErrorFields: {} }
    case 'SAVE_SUCCESS':
      return { ...state, ...BLANK_FORM, asset: action.payload }
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
  formStep: 'fields' | 'confirm'
  formTargetAsset: TargetAssetPickerValue
  formExchangeRatio: string
  formCashInLieu: string
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<CorporateActionFormField, string>>
  deleteError: string | null
  targetAssetOptions: AssetSearchOptionsData
  showNewForm: () => void
  showEditForm: (action: CorporateActionDto) => void
  cancelForm: () => void
  setFormField: (field: CorporateActionFormField, value: string) => void
  setTargetAsset: (value: TargetAssetPickerValue) => void
  advanceToConfirm: () => void
  backToFields: () => void
  saveForm: () => void
  deleteCorporateAction: (id: string) => void
}

// Asset-only, like useDisposals - a broker/portfolio has no single corporate-action history to show.
export function useCorporateActions(): CorporateActionsData {
  const { selectedNode, scope } = useSelectedNode()
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)
  const targetAssetOptions = useAssetSearchOptions(
    state.formType === 'Merger' && state.isFormVisible && !state.editingId,
  )

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

  const setTargetAsset = useCallback((value: TargetAssetPickerValue) => {
    dispatch({ type: 'SET_TARGET_ASSET', payload: value })
  }, [])

  const advanceToConfirm = useCallback(() => {
    const errors: Partial<Record<CorporateActionFormField, string>> = {}

    if (!state.formEffectiveDate.trim()) {
      errors.formEffectiveDate = 'Effective date is required'
    }
    if (!state.editingId && !state.formTargetAsset.assetName.trim()) {
      errors.formTargetAsset = 'Target asset is required'
    }
    const exchangeRatio = parseValidatedNumber(state.formExchangeRatio)
    if (exchangeRatio === null || exchangeRatio <= 0) {
      errors.formExchangeRatio = 'Exchange ratio must be greater than zero'
    }
    if (state.formCashInLieu.trim() !== '') {
      const cashInLieu = parseValidatedNumber(state.formCashInLieu)
      if (cashInLieu === null || cashInLieu < 0) {
        errors.formCashInLieu = 'Cash-in-lieu amount cannot be negative'
      }
    }

    if (Object.keys(errors).length > 0) {
      dispatch({ type: 'SAVE_ERROR', payload: { message: Object.values(errors)[0] ?? null, fields: errors } })
      return
    }

    dispatch({ type: 'SET_STEP', payload: 'confirm' })
  }, [state])

  const backToFields = useCallback(() => dispatch({ type: 'SET_STEP', payload: 'fields' }), [])

  const saveForm = useCallback(() => {
    if (!selectedNode?.portfolioName || !selectedNode.assetName) return

    if (state.formType === 'Merger') {
      if (state.formStep !== 'confirm') return

      const { formEffectiveDate, formExchangeRatio, formCashInLieu, formNote, formTargetAsset, editingId } = state
      const exchangeRatio = parseValidatedNumber(formExchangeRatio) ?? 0
      const cashInLieu = formCashInLieu.trim() === '' ? null : parseValidatedNumber(formCashInLieu)
      const note = formNote.trim() === '' ? null : formNote

      dispatch({ type: 'SAVE_START' })

      const call = editingId
        ? apiClient.updateMerger({
            id: editingId,
            brokerName: selectedNode.brokerName,
            portfolioName: selectedNode.portfolioName,
            sourceAssetName: selectedNode.assetName,
            effectiveDate: formEffectiveDate,
            exchangeRatio,
            cashInLieuAmount: cashInLieu,
            note,
          })
        : apiClient.addMerger({
            brokerName: selectedNode.brokerName,
            portfolioName: selectedNode.portfolioName,
            sourceAssetName: selectedNode.assetName,
            targetAssetName: formTargetAsset.assetName,
            createTargetAssetInline: !findAssetByName(targetAssetOptions.options, formTargetAsset.assetName),
            effectiveDate: formEffectiveDate,
            exchangeRatio,
            cashInLieuAmount: cashInLieu,
            note,
            targetISIN: formTargetAsset.identity.isin.trim() === '' ? null : formTargetAsset.identity.isin,
            targetExchange: formTargetAsset.identity.exchange.trim() === '' ? null : formTargetAsset.identity.exchange,
            targetTicker: formTargetAsset.identity.ticker.trim() === '' ? null : formTargetAsset.identity.ticker,
            targetLocalTypeCode: null,
            targetCountry: formTargetAsset.identity.country as CorporateActionMergerCreateDto['targetCountry'],
            targetClass: formTargetAsset.identity.assetClass as CorporateActionMergerCreateDto['targetClass'],
          })

      void call
        .then((result: CorporateActionMergerResultDto) => {
          const resolved =
            result.source?.name === selectedNode.assetName
              ? result.source
              : result.target?.name === selectedNode.assetName
                ? result.target
                : (result.source ?? result.target)
          if (resolved) {
            dispatch({ type: 'SAVE_SUCCESS', payload: resolved })
          } else {
            dispatch({
              type: 'SAVE_ERROR',
              payload: { message: 'Merger saved but the asset could not be refreshed — reload the page.', fields: {} },
            })
          }
        })
        .catch((err: unknown) => {
          const message = getErrorMessage(err, 'Failed to save corporate action')
          const field = mapCorporateActionErrorToField(message)
          dispatch({ type: 'SAVE_ERROR', payload: { message, fields: field === null ? {} : { [field]: message } } })
        })
      return
    }

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
  }, [selectedNode, state, targetAssetOptions.options])

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
    formStep: state.formStep,
    formTargetAsset: state.formTargetAsset,
    formExchangeRatio: state.formExchangeRatio,
    formCashInLieu: state.formCashInLieu,
    isSaving: state.isSaving,
    saveError: state.saveError,
    saveErrorFields: state.saveErrorFields,
    deleteError: state.deleteError,
    targetAssetOptions,
    showNewForm,
    showEditForm,
    cancelForm,
    setFormField,
    setTargetAsset,
    advanceToConfirm,
    backToFields,
    saveForm,
    deleteCorporateAction,
  }
}
