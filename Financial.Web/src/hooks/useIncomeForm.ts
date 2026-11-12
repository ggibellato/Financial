import { useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import { isNotFound } from '../api/apiError'
import type { IncomeFormField } from '../components/IncomeForm'
import type { AccountDto, BankOption, IncomeDto, IncomeSourceDto } from '../api/types'
import { getSaveErrorMessage, parseValidatedNumber } from '../utils/formatters'
import { getCreateFormDate, getStoredDefault, setStoredDefault } from '../utils/createFormDefaults'
import { accountsWith } from '../utils/accountCapabilities'
import {
  comparableLines,
  fundingLineErrors,
  fundingLinesMismatch,
  incomeDepositAmount,
  newFundingLine,
  toFundingLineRequests,
  withAddedLine,
  withChangedLine,
  withoutLine,
  type FundingLineChange,
  type FundingLineValues,
} from '../utils/fundingLines'
import { selectableBanks } from '../utils/selectableBanks'
import type { AccountFormSettings } from './formSettings'
import { hasChangedFields, snapshotFields, type FieldErrors } from './inlineFormSlice'
import { type AccountsStatus, useAccounts } from './useAccounts'

export const INCOME_SOURCES_WITH_GROSS_VALUE = ['Gleison', 'Ariana']

const DATE_KEY = 'income.date'
const BANK_KEY = 'income.bank'
const SOURCE_KEY = 'income.incomeSource'

export const SPLIT_CONFIRMATION_DELAY_MS = 4000

export function incomeSavedMessage(potsAccountName: string | null): string {
  return potsAccountName ? `Income added and split to ${potsAccountName}` : 'Income added'
}

/** Matches the previous hardcoded INCOME_SOURCES array's declaration order. Names outside this
 * list (unexpected but not invalid) sort last rather than being dropped or erroring. */
const INCOME_SOURCE_DISPLAY_ORDER = ['Gleison', 'Ariana', 'Lottery', 'DividendoJuros']

export function selectActiveIncomeSources(sources: IncomeSourceDto[]): IncomeSourceDto[] {
  return sources
    .filter((s) => s.isActive)
    .slice()
    .sort((a, b) => {
      const rank = (name: string) => {
        const index = INCOME_SOURCE_DISPLAY_ORDER.indexOf(name)
        return index === -1 ? INCOME_SOURCE_DISPLAY_ORDER.length : index
      }
      return rank(a.name) - rank(b.name)
    })
}

function resolveIncomeSourceName(sources: IncomeSourceDto[], incomeSourceId: string): string {
  return sources.find((s) => s.id === incomeSourceId)?.name ?? ''
}

function sourcePotsAccountId(sources: IncomeSourceDto[], incomeSourceId: string): string {
  return sources.find((s) => s.id === incomeSourceId)?.potsAccountId ?? ''
}

function offersSplit(sources: IncomeSourceDto[], incomeSourceId: string): boolean {
  return sourcePotsAccountId(sources, incomeSourceId) !== ''
}

function findAccount(accounts: AccountDto[], accountId: string | null | undefined): AccountDto | undefined {
  return accounts.find((account) => account.id === accountId)
}

type SplitFields = Pick<IncomeFormState, 'splitToReserve' | 'potsAccountId'>

function defaultPotsAccountId(sources: IncomeSourceDto[], accounts: AccountDto[], incomeSourceId: string): string {
  const potsAccountId = sourcePotsAccountId(sources, incomeSourceId)
  return isInactive(accounts, potsAccountId) ? '' : potsAccountId
}

function sourceSplit(sources: IncomeSourceDto[], accounts: AccountDto[], incomeSourceId: string): SplitFields {
  return {
    splitToReserve: String(offersSplit(sources, incomeSourceId)),
    potsAccountId: defaultPotsAccountId(sources, accounts, incomeSourceId),
  }
}

function requestedPotsAccountId(state: IncomeFormState, sources: IncomeSourceDto[]): string | null {
  return offersSplit(sources, state.incomeSource) && state.splitToReserve === 'true' ? state.potsAccountId || null : null
}

function isInactive(accounts: AccountDto[], accountId: string): boolean {
  return findAccount(accounts, accountId)?.isActive === false
}

const UNNAMED_POTS_ACCOUNT = 'pots'

const EDITABLE_FIELDS = [
  'date',
  'incomeSource',
  'grossValue',
  'netValue',
  'bank',
  'description',
  'splitToReserve',
  'potsAccountId',
] as const

type EditableValues = Pick<IncomeFormState, (typeof EDITABLE_FIELDS)[number]>

const NO_FUNDING_ERRORS: FieldErrors<string> = {}
const DEFAULT_DEPOSIT_CURRENCY = 'GBP'

interface FundingFields {
  isOlderSplit: boolean
  fundingLines: readonly FundingLineValues[]
}

const DEFAULT_FUNDING: FundingFields = { isOlderSplit: false, fundingLines: [] }
const WHOLE_DEPOSIT_LINE_KEY = 'whole-deposit-line'

function comparableFunding({ fundingLines }: FundingFields): string {
  return JSON.stringify(comparableLines(fundingLines))
}

function withOpenedValues(state: IncomeFormState): IncomeFormState {
  return { ...state, openedValues: snapshotFields(state, EDITABLE_FIELDS), openedFunding: comparableFunding(state) }
}

function isOlderSplitIncome(income: IncomeDto): boolean {
  return income.potsAccountId != null && !income.hasLinkedDeposit
}

function isDefaultFunding(income: IncomeDto): boolean {
  return income.fundingLines.length === 0 || (income.fundingLines.length === 1 && income.fundingLines[0].bankId === income.bankId)
}

interface IncomeFormState extends FundingFields {
  openedValues: EditableValues | null
  openedFunding: string
  fundingErrors: FieldErrors<string>
  isOpen: boolean
  isEditing: boolean
  editingId: string | null
  date: string
  incomeSource: string
  grossValue: string
  netValue: string
  bank: string
  description: string
  splitToReserve: string
  potsAccountId: string
  isSaving: boolean
  saveError: string | null
  saveErrorFields: Partial<Record<IncomeFormField, string>>
  splitConfirmationMessage: string | null
}

type IncomeFormAction =
  | { type: 'SHOW_CREATE_FORM'; payload: { date: string; bank: string; source: string; split: SplitFields } }
  | { type: 'SHOW_EDIT_FORM'; payload: { income: IncomeDto; funding: FundingFields } }
  | { type: 'CANCEL_FORM' }
  | { type: 'SET_FIELD'; payload: { field: IncomeFormField; value: string } }
  | { type: 'SET_SPLIT'; payload: SplitFields }
  | { type: 'SET_FUNDING_LINES'; payload: readonly FundingLineValues[] }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS'; payload: { splitConfirmationMessage: string | null } }
  | { type: 'SAVE_SUCCESS_KEEP_OPEN'; payload: { splitConfirmationMessage: string | null; nextSplit: SplitFields } }
  | {
      type: 'SAVE_ERROR'
      payload: { message: string | null; fields: Partial<Record<IncomeFormField, string>>; fundingErrors?: FieldErrors<string> }
    }
  | { type: 'DISMISS_SPLIT_CONFIRMATION' }
  | { type: 'CLEAR_UNTOUCHED_POTS_ACCOUNT' }

const BLANK_ENTRY = {
  grossValue: '',
  netValue: '',
  description: '',
  ...DEFAULT_FUNDING,
  fundingErrors: NO_FUNDING_ERRORS,
}

const BLANK_FORM = {
  ...BLANK_ENTRY,
  date: '',
  incomeSource: '',
  bank: '',
  splitToReserve: 'false',
  potsAccountId: '',
}

const INITIAL_STATE: IncomeFormState = {
  openedValues: null,
  openedFunding: comparableFunding(DEFAULT_FUNDING),
  isOpen: false,
  isEditing: false,
  editingId: null,
  ...BLANK_FORM,
  isSaving: false,
  saveError: null,
  saveErrorFields: {},
  splitConfirmationMessage: null,
}

function reducer(state: IncomeFormState, action: IncomeFormAction): IncomeFormState {
  switch (action.type) {
    case 'SHOW_CREATE_FORM':
      return withOpenedValues({
        ...state,
        ...BLANK_FORM,
        isOpen: true,
        isEditing: false,
        editingId: null,
        saveError: null,
        saveErrorFields: {},
        date: action.payload.date,
        bank: action.payload.bank,
        incomeSource: action.payload.source,
        ...action.payload.split,
      })
    case 'SHOW_EDIT_FORM': {
      const { income, funding } = action.payload
      return withOpenedValues({
        ...state,
        ...funding,
        fundingErrors: NO_FUNDING_ERRORS,
        isOpen: true,
        isEditing: true,
        editingId: income.id,
        date: income.date,
        incomeSource: income.incomeSourceId,
        grossValue: income.grossValue != null ? String(income.grossValue) : '',
        netValue: String(income.netValue),
        bank: income.bankId ?? '',
        description: income.description ?? '',
        splitToReserve: String(income.potsAccountId != null),
        potsAccountId: income.potsAccountId ?? '',
        saveError: null,
        saveErrorFields: {},
      })
    }
    case 'CANCEL_FORM':
      return { ...state, ...BLANK_FORM, isOpen: false, isEditing: false, editingId: null, saveError: null, saveErrorFields: {} }
    case 'SET_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SET_SPLIT':
      return { ...state, ...action.payload }
    case 'SET_FUNDING_LINES':
      return { ...state, fundingLines: action.payload, fundingErrors: NO_FUNDING_ERRORS }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null, saveErrorFields: {}, fundingErrors: NO_FUNDING_ERRORS }
    case 'SAVE_SUCCESS':
      return {
        ...state,
        ...BLANK_FORM,
        isOpen: false,
        isEditing: false,
        editingId: null,
        isSaving: false,
        splitConfirmationMessage: action.payload.splitConfirmationMessage,
      }
    case 'SAVE_SUCCESS_KEEP_OPEN':
      return withOpenedValues({
        ...state,
        ...BLANK_ENTRY,
        ...action.payload.nextSplit,
        isSaving: false,
        splitConfirmationMessage: action.payload.splitConfirmationMessage,
      })
    case 'SAVE_ERROR':
      return {
        ...state,
        isSaving: false,
        saveError: action.payload.message,
        saveErrorFields: action.payload.fields,
        fundingErrors: action.payload.fundingErrors ?? NO_FUNDING_ERRORS,
      }
    case 'CLEAR_UNTOUCHED_POTS_ACCOUNT':
      return {
        ...state,
        potsAccountId: '',
        openedValues: state.openedValues && { ...state.openedValues, potsAccountId: '' },
      }
    case 'DISMISS_SPLIT_CONFIRMATION':
      return { ...state, splitConfirmationMessage: null }
    default:
      return state
  }
}

export interface IncomeFunding {
  isShown: boolean
  isHandSplit: boolean
  lines: readonly FundingLineValues[]
  openedBankIds: readonly string[]
  bankName: string | null
  depositAmount: number | null
  currency: string
  mismatch: string | null
  errors: FieldErrors<string>
  splitAcrossBanks: () => void
  changeLine: (key: string, change: FundingLineChange) => void
  addLine: () => void
  removeLine: (key: string) => void
}

export interface UseIncomeFormResult {
  incomeFunding: IncomeFunding
  incomeLockedBank: AccountFormSettings['lockedBank']
  isIncomeFormOpen: boolean
  isIncomeEditing: boolean
  editingIncomeId: string | null
  incomeDate: string
  incomeSource: string
  incomeGrossValue: string
  incomeNetValue: string
  incomeBank: string
  incomeDescription: string
  incomeSplitToReserve: string
  incomePotsAccountId: string
  potsAccounts: AccountDto[]
  potsAccountsStatus: AccountsStatus
  retryPotsAccounts: () => void
  isSavingIncome: boolean
  isIncomeDirty: boolean
  saveIncomeError: string | null
  saveIncomeErrorFields: Partial<Record<IncomeFormField, string>>
  splitConfirmationMessage: string | null
  showCreateIncomeForm: () => void
  showDepositIncomeForm: (potsAccountId: string) => void
  showEditIncomeForm: (income: IncomeDto) => void
  cancelIncomeForm: () => void
  setIncomeField: (field: IncomeFormField, value: string) => void
  submitIncome: () => void
}

export function useIncomeForm(
  incomeSources: IncomeSourceDto[],
  banks: BankOption[],
  onSaved: (saved: IncomeDto, isEdit: boolean, potsAccountName: string | null) => void,
  { lockedBank, keepOpenAfterSave = false, viewedMonth, noBankByDefault = false, onNotFound }: AccountFormSettings = {},
): UseIncomeFormResult {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)
  const { accounts, status: potsAccountsStatus, retry: retryPotsAccounts } = useAccounts()

  const incomeBankLock = !state.isEditing || state.openedValues?.bank === lockedBank?.id ? lockedBank : undefined
  const incomeBankId = incomeBankLock?.id ?? state.bank
  const isFundingShown = offersSplit(incomeSources, state.incomeSource) && state.splitToReserve === 'true' && !state.isOlderSplit
  const netValue = parseValidatedNumber(state.netValue, { min: 0 })
  const depositAmount = netValue === null ? null : incomeDepositAmount(netValue)
  const fundingCurrency = findAccount(accounts, state.potsAccountId)?.currency ?? DEFAULT_DEPOSIT_CURRENCY
  const wholeDeposit = depositAmount?.toFixed(2) ?? ''
  const followsDeposit = !state.isEditing && incomeBankLock !== undefined && state.fundingLines.length === 0
  const fundingLines = followsDeposit
    ? [{ key: WHOLE_DEPOSIT_LINE_KEY, bankId: incomeBankLock.id, amount: wholeDeposit }]
    : state.fundingLines
  const isHandSplit = fundingLines.length > 0
  const sendsFundingLines = isFundingShown && isHandSplit && !followsDeposit
  const fundingMismatch = sendsFundingLines ? fundingLinesMismatch(fundingLines, depositAmount, fundingCurrency) : null

  const potsAccountName = (potsAccountId: string | null | undefined): string | null =>
    potsAccountId == null ? null : (findAccount(accounts, potsAccountId)?.name ?? UNNAMED_POTS_ACCOUNT)

  const isUntouchedInactiveDefault =
    state.isOpen &&
    !state.isEditing &&
    state.potsAccountId !== '' &&
    state.potsAccountId === state.openedValues?.potsAccountId &&
    isInactive(accounts, state.potsAccountId)

  useEffect(() => {
    if (isUntouchedInactiveDefault) dispatch({ type: 'CLEAR_UNTOUCHED_POTS_ACCOUNT' })
  }, [isUntouchedInactiveDefault])

  useEffect(() => {
    if (!state.splitConfirmationMessage) return
    const timeoutId = setTimeout(() => dispatch({ type: 'DISMISS_SPLIT_CONFIRMATION' }), SPLIT_CONFIRMATION_DELAY_MS)
    return () => clearTimeout(timeoutId)
  }, [state.splitConfirmationMessage])

  function defaultBank(): string {
    if (noBankByDefault) return ''
    const storedBank = getStoredDefault(BANK_KEY)
    return lockedBank?.id ?? (storedBank && selectableBanks(banks).some((b) => b.id === storedBank) ? storedBank : '')
  }

  function showDepositIncomeForm(potsAccountId: string) {
    const activeSources = selectActiveIncomeSources(incomeSources)
    const source = activeSources.find((s) => s.potsAccountId === potsAccountId) ?? activeSources.find((s) => s.potsAccountId != null)
    dispatch({
      type: 'SHOW_CREATE_FORM',
      payload: {
        date: getCreateFormDate(DATE_KEY, viewedMonth),
        bank: defaultBank(),
        source: source?.id ?? '',
        split: { splitToReserve: 'true', potsAccountId },
      },
    })
  }

  function showCreateIncomeForm() {
    const date = getCreateFormDate(DATE_KEY, viewedMonth)
    const bank = defaultBank()
    const storedSource = getStoredDefault(SOURCE_KEY)
    const source =
      state.incomeSource ||
      (storedSource && incomeSources.some((s) => s.id === storedSource) ? storedSource : (selectActiveIncomeSources(incomeSources)[0]?.id ?? ''))
    dispatch({ type: 'SHOW_CREATE_FORM', payload: { date, bank, source, split: sourceSplit(incomeSources, accounts, source) } })
  }

  const showEditIncomeForm = (income: IncomeDto) =>
    dispatch({
      type: 'SHOW_EDIT_FORM',
      payload: {
        income,
        funding: {
          isOlderSplit: isOlderSplitIncome(income),
          fundingLines: isDefaultFunding(income) ? [] : income.fundingLines.map((line) => newFundingLine(line.bankId, String(line.amount))),
        },
      },
    })

  const cancelIncomeForm = () => dispatch({ type: 'CANCEL_FORM' })

  const setFundingLines = (lines: readonly FundingLineValues[]) => dispatch({ type: 'SET_FUNDING_LINES', payload: lines })

  const splitAcrossBanks = () => setFundingLines([newFundingLine(incomeBankId, wholeDeposit)])

  const changeFundingLine = (key: string, change: FundingLineChange) => setFundingLines(withChangedLine(fundingLines, key, change))

  const addFundingLine = () => setFundingLines(withAddedLine(fundingLines, depositAmount))

  const removeFundingLine = (key: string) => setFundingLines(withoutLine(fundingLines, key))

  function setIncomeField(field: IncomeFormField, value: string) {
    dispatch({ type: 'SET_FIELD', payload: { field, value } })
    if (field === 'incomeSource') {
      if (!INCOME_SOURCES_WITH_GROSS_VALUE.includes(resolveIncomeSourceName(incomeSources, value))) {
        dispatch({ type: 'SET_FIELD', payload: { field: 'grossValue', value: '' } })
      }
      dispatch({ type: 'SET_SPLIT', payload: sourceSplit(incomeSources, accounts, value) })
    }
    if (field === 'splitToReserve' && value === 'true' && state.potsAccountId === '') {
      dispatch({ type: 'SET_FIELD', payload: { field: 'potsAccountId', value: defaultPotsAccountId(incomeSources, accounts, state.incomeSource) } })
    }
  }

  function submitIncome() {
    if (fundingMismatch) return

    const errors: Partial<Record<IncomeFormField, string>> = {}
    const fundingErrors = sendsFundingLines ? fundingLineErrors(fundingLines) : NO_FUNDING_ERRORS

    if (!state.date.trim()) {
      errors.date = 'Date is required.'
    }

    if (!state.incomeSource.trim()) {
      errors.incomeSource = 'Income source is required.'
    }

    if (netValue === null) {
      errors.netValue = 'Net value must be a non-negative number.'
    } else if (isFundingShown && netValue === 0) {
      errors.netValue = 'Net value must be greater than zero to split to pots.'
    }

    if (state.splitToReserve === 'true' && state.potsAccountId === '' && offersSplit(incomeSources, state.incomeSource)) {
      errors.potsAccountId = 'Choose a Pots account.'
    }

    if (isFundingShown && !incomeBankId) {
      errors.bank = 'Choose the bank this income landed in.'
    }

    let grossValue: number | null = null
    if (state.grossValue.trim() !== '') {
      grossValue = parseValidatedNumber(state.grossValue, { min: netValue ?? 0 })
      if (grossValue === null) {
        errors.grossValue = 'Gross value must be at least the net value.'
      }
    }

    if (Object.keys(errors).length > 0 || Object.keys(fundingErrors).length > 0) {
      const message = Object.values(errors)[0] ?? Object.values(fundingErrors)[0] ?? null
      dispatch({ type: 'SAVE_ERROR', payload: { message, fields: errors, fundingErrors } })
      return
    }

    dispatch({ type: 'SAVE_START' })

    const payload = {
      date: state.date,
      incomeSourceId: state.incomeSource,
      grossValue,
      netValue: netValue as number,
      bankId: incomeBankId || null,
      description: state.description || null,
      potsAccountId: requestedPotsAccountId(state, incomeSources),
      fundingLines: sendsFundingLines ? toFundingLineRequests(fundingLines) : null,
    }

    const request =
      state.isEditing && state.editingId ? apiClient.updateIncome(state.editingId, payload) : apiClient.createIncome(payload)

    void request
      .then((savedIncome) => {
        setStoredDefault(DATE_KEY, state.date)
        if (!incomeBankLock) setStoredDefault(BANK_KEY, state.bank)
        setStoredDefault(SOURCE_KEY, state.incomeSource)
        const savedPotsAccountName = potsAccountName(savedIncome.potsAccountId)
        const splitConfirmationMessage = savedPotsAccountName ? `Income saved and split to ${savedPotsAccountName}` : null
        if (keepOpenAfterSave && !state.isEditing) {
          dispatch({
            type: 'SAVE_SUCCESS_KEEP_OPEN',
            payload: { splitConfirmationMessage, nextSplit: sourceSplit(incomeSources, accounts, state.incomeSource) },
          })
        } else {
          dispatch({ type: 'SAVE_SUCCESS', payload: { splitConfirmationMessage } })
        }
        onSaved(savedIncome, state.isEditing, savedPotsAccountName)
      })
      .catch((err: unknown) => {
        if (onNotFound && state.isEditing && isNotFound(err)) {
          dispatch({ type: 'CANCEL_FORM' })
          onNotFound()
          return
        }
        dispatch({
          type: 'SAVE_ERROR',
          payload: {
            message: getSaveErrorMessage(err, state.isEditing ? 'Failed to update income.' : 'Failed to create income.'),
            fields: {},
          },
        })
      })
  }

  const incomeFunding: IncomeFunding = {
    isShown: isFundingShown,
    isHandSplit,
    lines: fundingLines,
    openedBankIds: state.openedValues?.bank ? [state.openedValues.bank] : [],
    bankName: incomeBankLock?.name ?? banks.find((bank) => bank.id === incomeBankId)?.name ?? null,
    depositAmount,
    currency: fundingCurrency,
    mismatch: fundingMismatch,
    errors: state.fundingErrors,
    splitAcrossBanks,
    changeLine: changeFundingLine,
    addLine: addFundingLine,
    removeLine: removeFundingLine,
  }

  return {
    incomeFunding,
    incomeLockedBank: incomeBankLock,
    isIncomeFormOpen: state.isOpen,
    isIncomeEditing: state.isEditing,
    editingIncomeId: state.editingId,
    incomeDate: state.date,
    incomeSource: state.incomeSource,
    incomeGrossValue: state.grossValue,
    incomeNetValue: state.netValue,
    incomeBank: state.bank,
    incomeDescription: state.description,
    incomeSplitToReserve: state.splitToReserve,
    incomePotsAccountId: state.potsAccountId,
    potsAccounts: accountsWith(accounts, 'ReceivesIncomeSplit', state.openedValues?.potsAccountId || undefined),
    potsAccountsStatus,
    retryPotsAccounts,
    isSavingIncome: state.isSaving,
    isIncomeDirty:
      state.isOpen && (hasChangedFields(state, state.openedValues, EDITABLE_FIELDS) || comparableFunding(state) !== state.openedFunding),
    saveIncomeError: state.saveError,
    saveIncomeErrorFields: state.saveErrorFields,
    splitConfirmationMessage: state.splitConfirmationMessage,
    showCreateIncomeForm,
    showDepositIncomeForm,
    showEditIncomeForm,
    cancelIncomeForm,
    setIncomeField,
    submitIncome,
  }
}
