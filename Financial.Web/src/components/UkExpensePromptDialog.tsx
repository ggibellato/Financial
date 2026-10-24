import { useState } from 'react'
import {
  Button,
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  makeStyles,
  MessageBar,
  MessageBarBody,
  tokens,
} from '@fluentui/react-components'
import type { BankDto, CategoryDto, RecurringBillDto } from '../api/types'
import type { ExpensePromptValues } from '../hooks/useMensais'
import { todayIsoDate } from '../utils/formatters'
import BillExpenseFields from './BillExpenseFields'
import { billExpenseValuesFor, parseBillExpense, type BillExpenseField } from './billExpenseValues'
import { useFormPanelStyles } from './formPanelStyles'

// formPanelStyles' shared grid is 4 columns, sized for pages with more fields
// than this dialog has. At 4 columns, native <input type="number"> (spin
// buttons) and <input type="date"> render wider than a quarter of even a
// widened DialogSurface, overflowing their grid cell and forcing DialogContent
// into its own horizontal scrollbar. 3 columns gives each field enough room,
// and the widened surface gives that a comfortable margin.
const useDialogStyles = makeStyles({
  surface: {
    maxWidth: '640px',
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(3, minmax(0, 1fr))',
    gap: tokens.spacingVerticalM,
    '@media (max-width: 639px)': {
      gridTemplateColumns: '1fr',
    },
  },
})

interface UkExpensePromptDialogProps {
  bill: RecurringBillDto
  banks: BankDto[]
  categories: CategoryDto[]
  isSaving: boolean
  error: string | null
  onConfirm: (values: ExpensePromptValues) => void
  onSkip: () => void
  onCancel: () => void
}

export default function UkExpensePromptDialog({
  bill,
  banks,
  categories,
  isSaving,
  error,
  onConfirm,
  onSkip,
  onCancel,
}: UkExpensePromptDialogProps) {
  const styles = useFormPanelStyles()
  const dialogStyles = useDialogStyles()
  const [values, setValues] = useState(() => billExpenseValuesFor(bill, todayIsoDate()))
  const parsed = parseBillExpense(values)
  const canConfirm = parsed !== null && !isSaving

  const setField = (field: BillExpenseField, value: string) => setValues((current) => ({ ...current, [field]: value }))

  const handleConfirm = () => {
    if (parsed && !isSaving) onConfirm(parsed)
  }

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open) onCancel() }}>
      <DialogSurface aria-describedby={undefined} className={dialogStyles.surface}>
        <DialogBody>
          <DialogTitle>Generate expense for this payment?</DialogTitle>
          <DialogContent>
            <div className={dialogStyles.grid}>
              <BillExpenseFields
                values={values}
                banks={banks}
                categories={categories}
                disabled={isSaving}
                onFieldChange={setField}
              />

              {error && (
                <div className={styles.spanTwo}>
                  <MessageBar intent="error">
                    <MessageBarBody>{error}</MessageBarBody>
                  </MessageBar>
                </div>
              )}
            </div>
          </DialogContent>
          <div className={styles.actions}>
            <Button appearance="primary" onClick={handleConfirm} disabled={!canConfirm}>
              {isSaving ? 'Saving...' : 'Confirm'}
            </Button>
            <Button appearance="secondary" onClick={onSkip} disabled={isSaving}>
              Skip
            </Button>
            <Button appearance="subtle" onClick={onCancel} disabled={isSaving}>
              Cancel
            </Button>
          </div>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
