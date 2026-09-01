import { useState } from 'react'
import {
  Button,
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Select,
} from '@fluentui/react-components'
import type { BankDto, CategoryDto, RecurringBillDto } from '../api/types'
import type { ExpensePromptValues } from '../hooks/useMensais'
import { todayIsoDate } from '../utils/formatters'
import { useFormPanelStyles } from './formPanelStyles'

interface UkExpensePromptDialogProps {
  bill: RecurringBillDto
  banks: BankDto[]
  categories: CategoryDto[]
  isCreatingExpense: boolean
  isUpdatingStatus: boolean
  expenseCreateError: string | null
  statusUpdateError: string | null
  isRetryOnly: boolean
  onConfirm: (values: ExpensePromptValues) => void
  onSkip: () => void
  onRetry: () => void
  onCancel: () => void
}

export default function UkExpensePromptDialog({
  bill,
  banks,
  categories,
  isCreatingExpense,
  isUpdatingStatus,
  expenseCreateError,
  statusUpdateError,
  isRetryOnly,
  onConfirm,
  onSkip,
  onRetry,
  onCancel,
}: UkExpensePromptDialogProps) {
  const styles = useFormPanelStyles()
  const [description, setDescription] = useState(bill.description)
  const [value, setValue] = useState(String(bill.value))
  const [date, setDate] = useState(todayIsoDate())
  const [bankId, setBankId] = useState('')
  const [categoryId, setCategoryId] = useState('')

  const isBusy = isCreatingExpense || isUpdatingStatus
  const parsedValue = Number(value)
  const canConfirm =
    description.trim().length > 0 &&
    !Number.isNaN(parsedValue) &&
    parsedValue > 0 &&
    bankId !== '' &&
    categoryId !== '' &&
    !isBusy

  const handleConfirm = () => {
    if (!canConfirm) return
    onConfirm({ description: description.trim(), value: parsedValue, date, bankId, categoryId })
  }

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open) onCancel() }}>
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>Generate expense for this payment?</DialogTitle>
          <DialogContent>
            {isRetryOnly ? (
              <>
                <MessageBar intent="error">
                  <MessageBarBody>
                    The expense was created, but the status could not be updated: {statusUpdateError}
                  </MessageBarBody>
                </MessageBar>
              </>
            ) : (
              <div className={styles.grid}>
                <Field label="Date" required>
                  <Input type="date" value={date} onChange={(e) => setDate(e.target.value)} disabled={isBusy} />
                </Field>

                <Field label="Description" required className={styles.spanTwo}>
                  <Input
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    disabled={isBusy}
                  />
                </Field>

                <Field label="Value" required>
                  <Input
                    type="number"
                    step="0.01"
                    value={value}
                    onChange={(e) => setValue(e.target.value)}
                    disabled={isBusy}
                  />
                </Field>

                <Field label="Bank" required>
                  <Select value={bankId} onChange={(e) => setBankId(e.target.value)} disabled={isBusy}>
                    <option value="">Select bank…</option>
                    {banks.map((b) => (
                      <option key={b.id} value={b.id}>
                        {b.name}
                      </option>
                    ))}
                  </Select>
                </Field>

                <Field label="Category" required>
                  <Select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} disabled={isBusy}>
                    <option value="">Select category…</option>
                    {categories.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                  </Select>
                </Field>

                {expenseCreateError && (
                  <div className={styles.spanTwo}>
                    <MessageBar intent="error">
                      <MessageBarBody>{expenseCreateError}</MessageBarBody>
                    </MessageBar>
                  </div>
                )}
              </div>
            )}
          </DialogContent>
          <div className={styles.actions}>
            {isRetryOnly ? (
              <>
                <Button appearance="primary" onClick={onRetry} disabled={isBusy}>
                  {isUpdatingStatus ? 'Retrying...' : 'Retry marking as Paid'}
                </Button>
                <Button appearance="secondary" onClick={onCancel} disabled={isBusy}>
                  Close
                </Button>
              </>
            ) : (
              <>
                <Button appearance="primary" onClick={handleConfirm} disabled={!canConfirm}>
                  {isCreatingExpense ? 'Creating...' : 'Confirm'}
                </Button>
                <Button appearance="secondary" onClick={onSkip} disabled={isBusy}>
                  {isUpdatingStatus ? 'Marking as Paid...' : 'Skip'}
                </Button>
                <Button appearance="subtle" onClick={onCancel} disabled={isBusy}>
                  Cancel
                </Button>
              </>
            )}
          </div>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
