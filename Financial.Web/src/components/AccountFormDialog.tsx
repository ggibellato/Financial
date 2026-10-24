import { useRef, useState } from 'react'
import { Field, Input, Select, Switch } from '@fluentui/react-components'
import type { AccountDto, AccountTemplateDto, AccountUpdateDto } from '../api/types'
import DiscardChangesDialog from './DiscardChangesDialog'
import EntityFormDialog from './EntityFormDialog'
import { useFormPanelStyles } from './formPanelStyles'
import { useDialogSubmit } from '../hooks/useDialogSubmit'
import { useDiscardPrompt } from '../hooks/useDiscardPrompt'
import { todayIsoDate } from '../utils/formatters'

const UNSET_START_DATE = '0001-01-01'
const NAME_MAX_LENGTH = 60
const CONFLICT_STATUS = 409

const FIELD_ORDER = ['template', 'name', 'currency', 'openingBalance', 'balanceStartDate'] as const

type FieldName = (typeof FIELD_ORDER)[number]

type FieldControl = HTMLInputElement & HTMLSelectElement

interface FormValues {
  templateName: string
  name: string
  currency: string
  openingBalance: string
  balanceStartDate: string
  roundUpEnabled: boolean
  isActive: boolean
}

interface AccountFormDialogProps {
  account: AccountDto | null
  templates: AccountTemplateDto[]
  onCancel: () => void
  onSubmit: (values: AccountUpdateDto) => Promise<unknown>
}

function initialValues(account: AccountDto | null, templates: AccountTemplateDto[]): FormValues {
  if (!account) {
    const template = templates[0]
    return {
      templateName: template?.name ?? '',
      name: '',
      currency: template?.currencies[0] ?? '',
      openingBalance: '0.00',
      balanceStartDate: todayIsoDate(),
      roundUpEnabled: false,
      isActive: true,
    }
  }
  return {
    templateName: account.templateName,
    name: account.name,
    currency: account.currency,
    openingBalance: account.openingBalance?.toFixed(2) ?? '0.00',
    balanceStartDate: !account.balanceStartDate || account.balanceStartDate === UNSET_START_DATE ? '' : account.balanceStartDate,
    roundUpEnabled: account.roundUpEnabled ?? false,
    isActive: account.isActive,
  }
}

function isSameValues(a: FormValues, b: FormValues): boolean {
  return (Object.keys(a) as (keyof FormValues)[]).every((key) => a[key] === b[key])
}

export default function AccountFormDialog({ account, templates, onCancel, onSubmit }: AccountFormDialogProps) {
  const styles = useFormPanelStyles()
  const isEditing = account !== null
  const isLocked = account?.hasRecords === true
  const [initial] = useState(() => initialValues(account, templates))
  const [values, setValues] = useState(initial)
  const [showValidation, setShowValidation] = useState(false)
  const [submittedName, setSubmittedName] = useState<string | null>(null)
  const { isSaving, error, errorStatus, submit } = useDialogSubmit('The account could not be saved.')
  const templateRef = useRef<FieldControl>(null)
  const nameRef = useRef<FieldControl>(null)
  const currencyRef = useRef<FieldControl>(null)
  const openingBalanceRef = useRef<FieldControl>(null)
  const balanceStartDateRef = useRef<FieldControl>(null)
  const discardPrompt = useDiscardPrompt(!isSameValues(values, initial))

  const template = templates.find((candidate) => candidate.name === values.templateName)
  const capabilities = template?.capabilities ?? []
  const hasOpeningBalance = capabilities.includes('HasOpeningBalance')
  const supportsRoundUp = capabilities.includes('SupportsRoundUp')
  const currencies = template?.currencies ?? [values.currency]
  const keepsUnsetStartDate = isEditing && account.balanceStartDate === UNSET_START_DATE
  const trimmedName = values.name.trim()
  const openingBalance = Number(values.openingBalance)
  const isNameConflict = errorStatus === CONFLICT_STATUS && submittedName === trimmedName

  const errors: Partial<Record<FieldName, string>> = {}
  if (!template) errors.template = 'Choose a template.'
  if (trimmedName.length === 0) errors.name = 'Account name is required.'
  else if (trimmedName.length > NAME_MAX_LENGTH) errors.name = `Account name must be ${NAME_MAX_LENGTH} characters or fewer.`
  if (!values.currency) errors.currency = 'Choose a currency.'
  if (hasOpeningBalance) {
    if (values.openingBalance.trim().length === 0) errors.openingBalance = 'Opening balance is required.'
    else if (Number.isNaN(openingBalance)) errors.openingBalance = 'Enter the opening balance as a number, for example 250.00.'
    else if (openingBalance < 0) errors.openingBalance = 'Opening balance cannot be negative.'
    if (values.balanceStartDate.length === 0 && !keepsUnsetStartDate) errors.balanceStartDate = 'Balance start date is required.'
    else if (values.balanceStartDate > todayIsoDate()) errors.balanceStartDate = "Balance start date can't be in the future."
  }

  const fieldMessage = (field: FieldName): string | undefined => {
    if (field === 'name' && isNameConflict) return error ?? undefined
    return showValidation ? errors[field] : undefined
  }

  const fieldState = (field: FieldName) => (fieldMessage(field) ? 'error' : 'none')

  const setField = <K extends keyof FormValues>(field: K, value: FormValues[K]) =>
    setValues((current) => ({ ...current, [field]: value }))

  const changeTemplate = (templateName: string) => {
    const next = templates.find((candidate) => candidate.name === templateName)
    setValues((current) => ({
      ...current,
      templateName,
      currency: next?.currencies.includes(current.currency) ? current.currency : (next?.currencies[0] ?? ''),
    }))
  }

  const toRequest = (): AccountUpdateDto => ({
    templateName: values.templateName,
    name: trimmedName,
    currency: values.currency,
    openingBalance: hasOpeningBalance ? openingBalance : null,
    balanceStartDate: hasOpeningBalance ? values.balanceStartDate || UNSET_START_DATE : null,
    roundUpEnabled: supportsRoundUp ? values.roundUpEnabled : null,
    isActive: values.isActive,
  })

  const handleSave = () => {
    if (isSaving) return
    setShowValidation(true)
    const firstInvalid = FIELD_ORDER.find((field) => errors[field])
    if (firstInvalid) {
      const fieldRefs = {
        template: templateRef,
        name: nameRef,
        currency: currencyRef,
        openingBalance: openingBalanceRef,
        balanceStartDate: balanceStartDateRef,
      }
      fieldRefs[firstInvalid].current?.focus()
      return
    }
    setSubmittedName(trimmedName)
    void submit(() => onSubmit(toRequest()))
  }

  const requestClose = () => {
    if (!isSaving) discardPrompt.confirmDiscard(onCancel)
  }


  const lockedCurrency = isLocked || currencies.length <= 1

  return (
      <EntityFormDialog
        title={isEditing ? 'Edit account' : 'New account'}
        saveLabel={isEditing ? 'Save' : 'Add account'}
        error={errorStatus === CONFLICT_STATUS ? null : error}
        isSaving={isSaving}
        canSave={!isSaving}
        onSave={handleSave}
        onCancel={requestClose}
      >
        <fieldset disabled={isSaving} className={styles.savingFieldset}>
          <Field label="Template" required validationState={fieldState('template')} validationMessage={fieldMessage('template')}>
            {isLocked ? (
              <Input ref={templateRef} value={values.templateName} readOnly />
            ) : (
              <Select ref={templateRef} value={values.templateName} onChange={(_, data) => changeTemplate(data.value)}>
                {templates.map((candidate) => (
                  <option key={candidate.name} value={candidate.name}>
                    {candidate.name}
                  </option>
                ))}
              </Select>
            )}
          </Field>

          <Field label="Name" required validationState={fieldState('name')} validationMessage={fieldMessage('name')}>
            <Input ref={nameRef} value={values.name} onChange={(_, data) => setField('name', data.value)} />
          </Field>

          <Field label="Currency" required validationState={fieldState('currency')} validationMessage={fieldMessage('currency')}>
            {lockedCurrency ? (
              <Input ref={currencyRef} value={values.currency} readOnly />
            ) : (
              <Select ref={currencyRef} value={values.currency} onChange={(_, data) => setField('currency', data.value)}>
                {currencies.map((currency) => (
                  <option key={currency} value={currency}>
                    {currency}
                  </option>
                ))}
              </Select>
            )}
          </Field>

          {hasOpeningBalance && (
            <>
              <Field
                label="Opening balance"
                required
                validationState={fieldState('openingBalance')}
                validationMessage={fieldMessage('openingBalance')}
              >
                <Input
                  ref={openingBalanceRef}
                  type="number"
                  step="0.01"
                  min="0"
                  inputMode="decimal"
                  value={values.openingBalance}
                  onChange={(_, data) => setField('openingBalance', data.value)}
                />
              </Field>

              <Field
                label="Balance start date"
                required={!keepsUnsetStartDate}
                validationState={fieldState('balanceStartDate')}
                validationMessage={fieldMessage('balanceStartDate')}
              >
                <Input
                  ref={balanceStartDateRef}
                  type="date"
                  max={todayIsoDate()}
                  value={values.balanceStartDate}
                  onChange={(_, data) => setField('balanceStartDate', data.value)}
                />
              </Field>
            </>
          )}

          {supportsRoundUp && (
            <Field label="Round-up">
              <Switch checked={values.roundUpEnabled} onChange={(_, data) => setField('roundUpEnabled', data.checked)} />
            </Field>
          )}

          {isEditing && (
            <Field label="Active">
              <Switch checked={values.isActive} onChange={(_, data) => setField('isActive', data.checked)} />
            </Field>
          )}
        </fieldset>

        <DiscardChangesDialog
          open={discardPrompt.isPromptOpen}
          onKeepEditing={discardPrompt.keepEditing}
          onDiscard={discardPrompt.discard}
        />
      </EntityFormDialog>
  )
}
