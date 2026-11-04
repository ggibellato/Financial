import {
  Button,
  Checkbox,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Radio,
  RadioGroup,
  Select,
  Spinner,
  Table,
  TableBody,
  TableHeader,
  TableHeaderCell,
  TableRow,
  Text,
} from '@fluentui/react-components'
import { DeleteRegular } from '@fluentui/react-icons'
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
  const generalError = Object.keys(form.errorFields).length === 0 ? form.error : null
  const { formRef, saveRef } = useFormErrorFocus(form.errorFields, generalError)
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
        form.submit()
      }}
    >
      <fieldset disabled={form.isSaving} className={styles.savingFieldset}>
        <Text as="h2" id="pot-deposit-title" weight="semibold" size={400}>
          {form.isEditing ? 'Edit deposit' : 'New deposit'}
        </Text>

        {form.hasPotsError && <ErrorState message={POTS_LOAD_ERROR} onRetry={form.retryPots} />}

        <div className={styles.grid}>
          {!form.isEditing && (
            <Field
              className={styles.spanTwo}
              label="Source"
              hint={canSplitFromIncome ? undefined : 'No active income source names a Pots account, so a deposit can only be manual.'}
            >
              <RadioGroup
                layout="horizontal"
                value={MANUAL_SOURCE}
                onChange={(_, data) => {
                  if (data.value === INCOME_SOURCE) onFromIncome()
                }}
              >
                <Radio value={MANUAL_SOURCE} label="Manual" />
                <Radio value={INCOME_SOURCE} label="From an income" disabled={!canSplitFromIncome} />
              </RadioGroup>
            </Field>
          )}

          <Field className={styles.spanTwo} label="Into">
            <RadioGroup layout="horizontal" value={values.into} onChange={(_, data) => form.setField('into', data.value as DepositInto)}>
              <Radio value="ActivePots" label="Active pots by split" />
              <Radio value="OnePot" label="One pot" />
            </RadioGroup>
          </Field>

          {values.into === 'OnePot' && (
            <Field
              label="Pot"
              required
              hint={form.pots === null && !form.hasPotsError ? 'Loading pots…' : undefined}
              validationState={fieldError('potId') ? 'error' : 'none'}
              validationMessage={fieldError('potId')}
            >
              <Select value={values.potId} disabled={form.pots === null} onChange={(event) => form.setField('potId', event.target.value)}>
                <option value="">Select a pot</option>
                {(form.pots ?? []).map((pot) => (
                  <option key={pot.id} value={pot.id}>
                    {pot.isActive ? pot.name : `${pot.name} (inactive)`}
                  </option>
                ))}
              </Select>
            </Field>
          )}

          <Field
            label={`Amount (${form.currency})`}
            required
            validationState={fieldError('amount') ? 'error' : 'none'}
            validationMessage={fieldError('amount')}
          >
            <Input type="number" step="0.01" min="0" value={values.amount} onChange={(event) => form.setField('amount', event.target.value)} />
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
        </div>

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
            depositAmount={form.depositAmount}
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

        <div className={styles.actions}>
          <Button
            ref={saveRef}
            type="submit"
            appearance="primary"
            disabled={form.isSaving || form.mismatch !== null || form.pots === null}
            icon={form.isSaving ? <Spinner size="tiny" /> : undefined}
          >
            {form.isSaving ? 'Saving...' : form.isEditing ? 'Save' : 'Add deposit'}
          </Button>
          <Button appearance="secondary" onClick={onCancel}>
            Cancel
          </Button>
          {form.isEditing && (
            <Button className={styles.deleteAction} appearance="primary" icon={<DeleteRegular />} onClick={onDelete}>
              Delete
            </Button>
          )}
        </div>

        {generalError && (
          <MessageBar role="alert" intent="error">
            <MessageBarBody>{generalError}</MessageBarBody>
          </MessageBar>
        )}
      </fieldset>
    </form>
  )
}
