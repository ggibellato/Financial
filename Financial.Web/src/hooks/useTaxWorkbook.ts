import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { TaxWorkbookDto, TaxWorkbookOptionDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'
import { buildTaxWorkbookCsv, buildTaxWorkbookCsvFilename, downloadCsv } from '../utils/taxWorkbookCsv'

interface TaxWorkbookState {
  options: TaxWorkbookOptionDto[]
  isLoadingOptions: boolean
  optionsError: string | null
  optionsRetryCount: number
  jurisdiction: string | null
  taxYear: string | null
  workbook: TaxWorkbookDto | null
  isLoadingWorkbook: boolean
  workbookError: string | null
  workbookRetryCount: number
}

type TaxWorkbookAction =
  | { type: 'OPTIONS_FETCH_START' }
  | { type: 'OPTIONS_FETCH_SUCCESS'; payload: TaxWorkbookOptionDto[] }
  | { type: 'OPTIONS_FETCH_ERROR'; payload: string }
  | { type: 'OPTIONS_RETRY' }
  | { type: 'SELECT'; jurisdiction: string; taxYear: string }
  | { type: 'WORKBOOK_FETCH_START' }
  | { type: 'WORKBOOK_FETCH_SUCCESS'; payload: TaxWorkbookDto }
  | { type: 'WORKBOOK_FETCH_ERROR'; payload: string }
  | { type: 'WORKBOOK_RETRY' }

const INITIAL_STATE: TaxWorkbookState = {
  options: [],
  isLoadingOptions: true,
  optionsError: null,
  optionsRetryCount: 0,
  jurisdiction: null,
  taxYear: null,
  workbook: null,
  isLoadingWorkbook: false,
  workbookError: null,
  workbookRetryCount: 0,
}

function reducer(state: TaxWorkbookState, action: TaxWorkbookAction): TaxWorkbookState {
  switch (action.type) {
    case 'OPTIONS_FETCH_START':
      return { ...state, isLoadingOptions: true, optionsError: null }
    case 'OPTIONS_FETCH_SUCCESS': {
      const first = action.payload[0]
      return {
        ...state,
        isLoadingOptions: false,
        options: action.payload,
        jurisdiction: first?.jurisdiction ?? null,
        taxYear: first?.taxYear ?? null,
      }
    }
    case 'OPTIONS_FETCH_ERROR':
      return { ...state, isLoadingOptions: false, optionsError: action.payload }
    case 'OPTIONS_RETRY':
      return { ...state, optionsRetryCount: state.optionsRetryCount + 1 }
    case 'SELECT':
      return { ...state, jurisdiction: action.jurisdiction, taxYear: action.taxYear }
    case 'WORKBOOK_FETCH_START':
      return { ...state, isLoadingWorkbook: true, workbookError: null }
    case 'WORKBOOK_FETCH_SUCCESS':
      return { ...state, isLoadingWorkbook: false, workbook: action.payload }
    case 'WORKBOOK_FETCH_ERROR':
      return { ...state, isLoadingWorkbook: false, workbookError: action.payload, workbook: null }
    case 'WORKBOOK_RETRY':
      return { ...state, workbookRetryCount: state.workbookRetryCount + 1 }
    default:
      return state
  }
}

export interface TaxWorkbookData {
  options: TaxWorkbookOptionDto[]
  isLoadingOptions: boolean
  optionsError: string | null
  retryOptions: () => void
  jurisdictions: string[]
  taxYearsForJurisdiction: string[]
  jurisdiction: string | null
  taxYear: string | null
  selectJurisdiction: (jurisdiction: string) => void
  selectTaxYear: (taxYear: string) => void
  workbook: TaxWorkbookDto | null
  isLoadingWorkbook: boolean
  workbookError: string | null
  retryWorkbook: () => void
  canExportCsv: boolean
  exportCsv: () => void
}

export function useTaxWorkbook(): TaxWorkbookData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'OPTIONS_FETCH_START' })
    void apiClient
      .getTaxWorkbookOptions()
      .then((options) => dispatch({ type: 'OPTIONS_FETCH_SUCCESS', payload: options }))
      .catch((err: unknown) => {
        dispatch({ type: 'OPTIONS_FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load tax years') })
      })
  }, [state.optionsRetryCount])

  useEffect(() => {
    if (!state.jurisdiction || !state.taxYear) return

    dispatch({ type: 'WORKBOOK_FETCH_START' })
    void apiClient
      .getTaxWorkbook(state.jurisdiction, state.taxYear)
      .then((workbook) => dispatch({ type: 'WORKBOOK_FETCH_SUCCESS', payload: workbook }))
      .catch((err: unknown) => {
        dispatch({ type: 'WORKBOOK_FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load the tax workbook') })
      })
  }, [state.jurisdiction, state.taxYear, state.workbookRetryCount])

  const retryOptions = useCallback(() => dispatch({ type: 'OPTIONS_RETRY' }), [])
  const retryWorkbook = useCallback(() => dispatch({ type: 'WORKBOOK_RETRY' }), [])

  const jurisdictions = [...new Set(state.options.map((o) => o.jurisdiction))]
  const taxYearsForJurisdiction = state.options
    .filter((o) => o.jurisdiction === state.jurisdiction)
    .map((o) => o.taxYear)

  const selectJurisdiction = useCallback(
    (jurisdiction: string) => {
      const firstTaxYear = state.options.find((o) => o.jurisdiction === jurisdiction)?.taxYear
      if (!firstTaxYear) return
      dispatch({ type: 'SELECT', jurisdiction, taxYear: firstTaxYear })
    },
    [state.options],
  )

  const selectTaxYear = useCallback(
    (taxYear: string) => {
      if (!state.jurisdiction) return
      dispatch({ type: 'SELECT', jurisdiction: state.jurisdiction, taxYear })
    },
    [state.jurisdiction],
  )

  const canExportCsv = (state.workbook?.entries.length ?? 0) > 0

  const exportCsv = useCallback(() => {
    if (!state.workbook || state.workbook.entries.length === 0) return
    downloadCsv(
      buildTaxWorkbookCsvFilename(state.workbook.jurisdiction, state.workbook.taxYear),
      buildTaxWorkbookCsv(state.workbook),
    )
  }, [state.workbook])

  return {
    options: state.options,
    isLoadingOptions: state.isLoadingOptions,
    optionsError: state.optionsError,
    retryOptions,
    jurisdictions,
    taxYearsForJurisdiction,
    jurisdiction: state.jurisdiction,
    taxYear: state.taxYear,
    selectJurisdiction,
    selectTaxYear,
    workbook: state.workbook,
    isLoadingWorkbook: state.isLoadingWorkbook,
    workbookError: state.workbookError,
    retryWorkbook,
    canExportCsv,
    exportCsv,
  }
}
