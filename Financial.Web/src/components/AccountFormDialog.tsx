import { useEffect, useRef, useState } from 'react'
import { Field, InfoLabel, Input, Select, Switch } from '@fluentui/react-components'
import type { LabelProps } from '@fluentui/react-components'
import type { AccountDto, AccountTemplateDto, AccountUpdateDto, CategoryDto } from '../api/types'
import DiscardChangesDialog from './DiscardChangesDialog'
import EntityFormDialog from './EntityFormDialog'
import { useFormPanelStyles } from './formPanelStyles'
import { hasChangedFields } from '../hooks/inlineFormSlice'
import { useDialogSubmit } from '../hooks/useDialogSubmit'
import { useDiscardPrompt } from '../hooks/useDiscardPrompt'
import { accountTemplateLabel } from '../utils/accountTemplateLabel'
import { todayIsoDate } from '../utils/formatters'

const UNSET_START_DATE = '0001-01-01'
const NAME_MAX_LENGTH = 60
const CONFLICT_STATUS = 409
const LOCKED_HINT = "Can't change once the account has transactions."
const CATEGORY_REQUIRED = "Choose the category for this account's bank expenses."
const CATEGORY_INFO = "When money moves between a bank and this account, the bank's entry is recorded in this category."

const FIELD_ORDER = ['template', 'name', 'currency', 'category', 'openingBalance', 'balanceStartDate'] as const

type FieldName = (typeof FIELD_ORDER)[number]

type FieldErrors = Partial<Record<FieldName, string>>

type FieldControl = HTMLInputElement & HTMLSelectElement

interface FormValues {
  templateName: string
  name: string
  currency: string
  categoryId: string
  openingBalance: string
  balanceStartDate: string
  roundUpEnabled: boolean
  isActive: boolean
}

const FORM_FIELDS: readonly (keyof FormValues)[] = [
  'templateName',
  'name',
  'currency',
  'categoryId',
  'openingBalance',
  'balanceStartDate',
  'roundUpEnabled',
  'isActive',
]

interface AccountFormDialogProps {
  account: AccountDto | null
  templates: AccountTemplateDto[]
  categories: CategoryDto[]
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
      categoryId: '',
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
    categoryId: account.categoryId ?? '',
    openingBalance: account.openingBalance?.toFixed(2) ?? '0.00',
    balanceStartDate: !account.balanceStartDate || account.balanceStartDate === UNSET_START_DATE ? '' : account.balanceStartDate,
    roundUpEnabled: account.roundUpEnabled ?? false,
    isActive: account.isActive,
  }
}

function validateAccountForm(
  values: FormValues,
  template: AccountTemplateDto | undefined,
  keepsUnsetStartDate: boolean,
): FieldErrors {
  const errors: FieldErrors = {}
  const name = values.name.trim()
  const openingBalance = Number(values.openingBalance)
  if (!template) errors.template = 'Choose a template.'
  if (name.length === 0) errors.name = 'Account name is required.'
  else if (name.length > NAME_MAX_LENGTH) errors.name = `Account name must be ${NAME_MAX_LENGTH} characters or fewer.`
  if (!values.currency) errors.currency = 'Choose a currency.'
  if (template?.capabilities.includes('HasOpeningBalance')) {
    if (values.openingBalance.trim().length === 0) errors.openingBalance = 'Opening balance is required.'
    else if (!Number.isFinite(openingBalance)) errors.openingBalance = 'Enter the opening balance as a number, for example 250.00.'
    else if (openingBalance < 0) errors.openingBalance = 'Opening balance cannot be negative.'
    if (values.balanceStartDate.length === 0 && !keepsUnsetStartDate) errors.balanceStartDate = 'Balance start date is required.'
    else if (values.balanceStartDate > todayIsoDate()) errors.balanceStartDate = "Balance start date can't be in the future."
  }
  if (template?.capabilities.includes('HasPots') && !values.categoryId) errors.category = CATEGORY_REQUIRED
  return errors
}

function categoryOptions(categories: CategoryDto[], keptId: string): CategoryDto[] {
  return categories
    .filter((category) => (category.active && !category.isInvestment) || category.id === keptId)
    .sort((a, b) => a.name.localeCompare(b.name))
}

export default function AccountFormDialog({ account, templates, categories, onCancel, onSubmit }: AccountFormDialogProps) {
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
  const categoryRef = useRef<FieldControl>(null)
  const openingBalanceRef = useRef<FieldControl>(null)
  const balanceStartDateRef = useRef<FieldControl>(null)
  const discardPrompt = useDiscardPrompt(hasChangedFields(values, initial, FORM_FIELDS))

  const template = templates.find((candidate) => candidate.name === values.templateName)
  const capabilities = template?.capabilities ?? []
  const hasOpeningBalance = capabilities.includes('HasOpeningBalance')
  const supportsRoundUp = capabilities.includes('SupportsRoundUp')
  const hasPots = capabilities.includes('HasPots')
  const selectableCategories = categoryOptions(categories, initial.categoryId)
  const currencies = template?.currencies ?? [values.currency]
  const isCurrencyFixed = isLocked || currencies.length <= 1
  const keepsUnsetStartDate = isEditing && account.balanceStartDate === UNSET_START_DATE
  const trimmedName = values.name.trim()
  const isConflict = errorStatus === CONFLICT_STATUS
  const isNameConflict = isConflict && submittedName === trimmedName
  const errors = validateAccountForm(values, template, keepsUnsetStartDate)

  useEffect(() => {
    if (isNameConflict) nameRef.current?.focus()
  }, [isNameConflict])

  const fieldMessage = (field: FieldName) => {
    if (field === 'name' && isNameConflict) return error
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
    categoryId: hasPots ? values.categoryId : null,
    openingBalance: hasOpeningBalance ? Number(values.openingBalance) : null,
    balanceStartDate: hasOpeningBalance ? values.balanceStartDate || UNSET_START_DATE : null,
    roundUpEnabled: supportsRoundUp ? values.roundUpEnabled : null,
    isActive: values.isActive,
  })

  const handleSave = () => {
    setShowValidation(true)
    const firstInvalid = FIELD_ORDER.find((field) => errors[field])
    if (firstInvalid) {
      const fieldRefs = {
        template: templateRef,
        name: nameRef,
        currency: currencyRef,
        category: categoryRef,
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

  const hasCategoryChoices = selectableCategories.length > 0
  const categoryHint = !hasCategoryChoices
    ? 'Add a category in Admin → Categories first.'
    : isEditing
      ? 'Changing it applies to later deposits and withdrawals; past ones keep their category.'
      : undefined

  const templateLabel = accountTemplateLabel(values.templateName)
  const currencyHint = isLocked ? LOCKED_HINT : isCurrencyFixed ? `${templateLabel} accounts use ${values.currency}.` : undefined

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit account' : 'New account'}
      saveLabel={isEditing ? 'Save' : 'Add account'}
      error={isConflict ? null : error}
      isSaving={isSaving}
      canSave={!isSaving}
      onSave={handleSave}
      onCancel={requestClose}
    >
      <fieldset disabled={isSaving} className={styles.savingFieldset}>
        <Field
          label="Template"
          required={!isLocked}
          hint={isLocked ? LOCKED_HINT : undefined}
          validationState={fieldState('template')}
          validationMessage={fieldMessage('template')}
        >
          {isLocked ? (
            <Input ref={templateRef} value={templateLabel} readOnly />
          ) : (
            <Select ref={templateRef} value={values.templateName} onChange={(_, data) => changeTemplate(data.value)}>
              {templates.map((candidate) => (
                <option key={candidate.name} value={candidate.name}>
                  {accountTemplateLabel(candidate.name)}
                </option>
              ))}
            </Select>
          )}
        </Field>

        <Field label="Name" required validationState={fieldState('name')} validationMessage={fieldMessage('name')}>
          <Input ref={nameRef} value={values.name} onChange={(_, data) => setField('name', data.value)} />
        </Field>

        <Field
          label="Currency"
          required={!isCurrencyFixed}
          hint={currencyHint}
          validationState={fieldState('currency')}
          validationMessage={fieldMessage('currency')}
        >
          {isCurrencyFixed ? (
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

        {hasPots && (
          <Field
            label={{
              children: (_: unknown, props: LabelProps) => (
                <InfoLabel {...props} info={CATEGORY_INFO}>
                  Bank expense category
                </InfoLabel>
              ),
            }}
            required
            hint={categoryHint}
            validationState={fieldState('category')}
            validationMessage={fieldMessage('category')}
          >
            <Select
              ref={categoryRef}
              value={values.categoryId}
              disabled={!hasCategoryChoices}
              onChange={(_, data) => setField('categoryId', data.value)}
            >
              <option value="">{hasCategoryChoices ? 'Select a category' : 'No active categories'}</option>
              {selectableCategories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.active ? category.name : `${category.name} (inactive)`}
                </option>
              ))}
            </Select>
          </Field>
        )}

        {hasOpeningBalance && (
          <>
            <Field
              label={`Opening balance (${values.currency})`}
              required
              hint={isLocked ? "Changing it changes this account's past balances." : undefined}
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
              hint={keepsUnsetStartDate ? 'Not set. Leave it empty to keep it that way.' : undefined}
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
