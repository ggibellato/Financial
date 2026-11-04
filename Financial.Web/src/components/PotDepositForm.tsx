import { useState } from 'react'
import {
  Checkbox,
  Field,
  Input,
  Radio,
  RadioGroup,
  Spinner,
  Table,
  TableBody,
  TableHeader,
  TableHeaderCell,
  TableRow,
  Text,
} from '@fluentui/react-components'
import type { BankDto, IncomeSourceDto, PotBalanceDto } from '../api/types'
import { useFieldError } from '../hooks/useFieldError'
import { useFormErrorFocus } from '../hooks/useFormErrorFocus'
import { POTS_LOAD_ERROR } from '../hooks/usePots'
import type { DepositInto, PotDepositFormModel } from '../hooks/usePotDepositForm'
import { formatN2OrDash, formatN2To4 } from '../utils/formatters'
import { activePotsTotal, canSplitAcrossActivePots, previewPotSplit } from '../utils/potSplitPreview'
import DataTableCell from './grid/DataTableCell'
import ErrorState from './ErrorState'
import FundingLinesField from './FundingLinesField'
import { useFormPanelStyles } from './formPanelStyles'
import PotField from './PotField'
import PotFormFooter from './PotFormFooter'
import './PotDepositForm.css'

const MANUAL_SOURCE = 'Manual'
const INCOME_SOURCE = 'Income'

interface PotSplitPreviewProps {
  pots: readonly PotBalanceDto[]
  amount: number | null
  currency: string
}

function PotSplitPreview({ pots, amount, currency }: PotSplitPreviewProps) {
  if (!canSplitAcrossActivePots(pots)) {
    return (
      <p role="status">
        Active pots total <strong>{formatN2To4(activePotsTotal(pots))}%</strong>. They must total exactly 100% to deposit by split: change them with
        Manage pots, or deposit into one pot.
      </p>
    )
  }

  const amountHeader = `Amount (${currency})`

  return (
    <Table className="data-table pot-split-preview" aria-label="Split preview">
      <TableHeader>
        <TableRow>
          <TableHeaderCell>Pot</TableHeaderCell>
          <TableHeaderCell className="data-table__col--numeric">Split %</TableHeaderCell>
          <TableHeaderCell className="data-table__col--numeric">{amountHeader}</TableHeaderCell>
        </TableRow>
      </TableHeader>
      <TableBody>
        {previewPotSplit(pots, amount ?? 0).map((line) => (
          <TableRow key={line.pot.id}>
            <DataTableCell label="Pot">{line.pot.name}</DataTableCell>
            <DataTableCell label="Split %" className="data-table__col--numeric">
              {formatN2To4(line.pot.splitPercentage)}
            </DataTableCell>
            <DataTableCell label={amountHeader} className="data-table__col--numeric">
              {formatN2OrDash(amount === null ? null : line.amount)}
            </DataTableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}

interface PotDepositFormProps {
  form: PotDepositFormModel
  banks: BankDto[]
  incomeSources: IncomeSourceDto[]
  onFromIncome: () => void
  onDelete: () => void
  onCancel: () => void
}

export default function PotDepositForm({ form, banks, incomeSources, onFromIncome, onDelete, onCancel }: PotDepositFormProps) {
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(form.errorFields)
  const { formRef, saveRef } = useFormErrorFocus(form.errorFields, form.error)
  const [source, setSource] = useState(MANUAL_SOURCE)
  const isFromIncome = !form.isEditing && source === INCOME_SOURCE
  const { values } = form
  if (!values) return null
  const canSplitFromIncome = incomeSources.some((source) => source.isActive && source.potsAccountId != null)

  return (
    <form
      ref={formRef}
      className={styles.panel}
      noValidate
      aria-labelledby="pot-deposit-title"
      onSubmit={(event) => {
        event.preventDefault()
        if (isFromIncome) onFromIncome()
        else form.submit()
      }}
    >
      <fieldset disabled={form.isSaving} className={styles.savingFieldset}>
        <Text as="h2" id="pot-deposit-title" weight="semibold" size={400}>
          {form.isEditing ? 'Edit deposit' : 'New deposit'}
        </Text>

        {form.hasPotsError && !isFromIncome && <ErrorState message={POTS_LOAD_ERROR} onRetry={form.retryPots} />}

        <div className={styles.grid}>
          {form.isEditing ? (
            <div className={styles.spanTwo} />
          ) : (
            <Field
              className={styles.spanTwo}
              label="Source"
              hint={canSplitFromIncome ? undefined : 'No active income source names a Pots account, so a deposit can only be manual.'}
            >
              <RadioGroup
                className={styles.radioRow}
                layout="horizontal"
                value={source}
                onChange={(_, data) => setSource(data.value)}
              >
                <Radio value={MANUAL_SOURCE} label="Manual" />
                <Radio value={INCOME_SOURCE} label="From an income" disabled={!canSplitFromIncome} />
              </RadioGroup>
            </Field>
          )}

          {!isFromIncome && (
            <>
              <Field className={styles.spanTwo} label="Into">
                <RadioGroup className={styles.radioRow} layout="horizontal" value={values.into} onChange={(_, data) => form.setInto(data.value as DepositInto)}>
                  <Radio value="ActivePots" label="Active pots by split" />
                  <Radio value="OnePot" label="One pot" />
                </RadioGroup>
              </Field>

              {values.into === 'OnePot' && (
                <PotField
                  value={values.potId}
                  pots={form.pots}
                  hasLoadError={form.hasPotsError}
                  error={fieldError('potId')}
                  onChange={(potId) => form.setField('potId', potId)}
                />
              )}

              <Field
                label={`Amount (${form.currency})`}
                required
                validationState={fieldError('amount') ? 'error' : 'none'}
                validationMessage={fieldError('amount')}
              >
                <Input type="number" step="0.01" min="0" value={values.amount} onChange={(event) => form.setAmount(event.target.value)} />
              </Field>

              <Field label="Date" required validationState={fieldError('date') ? 'error' : 'none'} validationMessage={fieldError('date')}>
                <Input type="date" value={values.date} onChange={(event) => form.setField('date', event.target.value)} />
              </Field>

              <Field
                className={styles.spanTwo}
                label="Description"
                required
                validationState={fieldError('description') ? 'error' : 'none'}
                validationMessage={fieldError('description')}
              >
                <Input name="description" value={values.description} onChange={(event) => form.setField('description', event.target.value)} />
              </Field>
            </>
          )}
        </div>

        {!isFromIncome && (
          <>
            <Text as="h3" weight="semibold">
              Funded from
            </Text>
            <Checkbox
              label="No bank — money from outside CashFlow"
              checked={values.hasNoBank}
              onChange={(_, data) => form.setHasNoBank(data.checked === true)}
            />
            {!values.hasNoBank && (
              <FundingLinesField
                lines={values.lines}
                banks={banks}
                keptBankIds={form.openedBankIds}
                mismatch={form.mismatch}
                currency={form.currency}
                errors={form.errorFields}
                onChange={form.changeLine}
                onAdd={form.addLine}
                onRemove={form.removeLine}
              />
            )}

            {values.into === 'ActivePots' && (
              <>
                <Text as="h3" weight="semibold">
                  Split preview
                </Text>
                {form.pots ? (
                  <PotSplitPreview pots={form.pots} amount={form.depositAmount} currency={form.currency} />
                ) : (
                  !form.hasPotsError && <Spinner size="tiny" labelPosition="after" label="Loading pots…" />
                )}
              </>
            )}
          </>
        )}

        <PotFormFooter
          saveRef={saveRef}
          saveLabel={isFromIncome ? 'Continue to the income form' : form.isEditing ? 'Save' : 'Add deposit'}
          deleteLabel={form.isEditing ? 'Delete deposit' : null}
          isSaving={form.isSaving}
          isSaveDisabled={!isFromIncome && (form.mismatch !== null || form.pots === null)}
          error={form.error}
          onDelete={onDelete}
          onCancel={onCancel}
        />
      </fieldset>
    </form>
  )
}
