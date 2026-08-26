import { Button, MessageBar, MessageBarBody, makeStyles, tokens } from '@fluentui/react-components'
import { AddRegular, DeleteRegular } from '@fluentui/react-icons'
import type { IncomeDto } from '../api/types'
import { formatN2, formatShortDate } from '../utils/formatters'
import './IncomeSection.css'

// Grid create/new actions: left-aligned primary button with an add icon,
// matching ExpensesSection.tsx (docs/ui/forms-data-and-visualisations.md).
const useStyles = makeStyles({
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'flex-start',
    marginBottom: tokens.spacingVerticalM,
  },
})

interface IncomeRowProps {
  income: IncomeDto
  onEdit: (income: IncomeDto) => void
  onDelete: (id: string) => void
}

function IncomeRow({ income, onEdit, onDelete }: IncomeRowProps) {
  return (
    <tr>
      <td>
        <button
          className="data-table__action-btn"
          type="button"
          aria-label="Edit income"
          onClick={() => onEdit(income)}
        >
          ✏
        </button>
      </td>
      <td>
        <button
          className="data-table__action-btn"
          type="button"
          aria-label="Delete income"
          onClick={() => onDelete(income.id)}
        >
          <DeleteRegular />
        </button>
      </td>
      <td>{formatShortDate(income.date)}</td>
      <td>{income.incomeSourceName}</td>
      <td className="data-table__col--numeric">{income.grossValue != null ? formatN2(income.grossValue) : '—'}</td>
      <td className="data-table__col--numeric">{formatN2(income.netValue)}</td>
      <td>{income.bankName ?? '—'}</td>
      <td>{income.description ?? ''}</td>
    </tr>
  )
}

interface IncomeSectionProps {
  incomes: IncomeDto[]
  onEdit: (income: IncomeDto) => void
  onDelete: (id: string) => void
  onNewIncome: () => void
  splitConfirmationMessage?: string | null
}

export default function IncomeSection({
  incomes,
  onEdit,
  onDelete,
  onNewIncome,
  splitConfirmationMessage,
}: IncomeSectionProps) {
  const styles = useStyles()
  return (
    <section className="income-section">
      <div className={styles.header}>
        <Button appearance="primary" icon={<AddRegular />} onClick={onNewIncome}>
          New Income
        </Button>
      </div>
      {splitConfirmationMessage && (
        <MessageBar intent="success">
          <MessageBarBody>{splitConfirmationMessage}</MessageBarBody>
        </MessageBar>
      )}
      <div className="income-section__table-wrapper">
        <table className="income-section__table data-table">
          <thead>
            <tr>
              <th />
              <th />
              <th>Date</th>
              <th>Source</th>
              <th className="data-table__col--numeric">Gross</th>
              <th className="data-table__col--numeric">Net</th>
              <th>Bank</th>
              <th>Description</th>
            </tr>
          </thead>
          <tbody>
            {incomes.map((income) => (
              <IncomeRow key={income.id} income={income} onEdit={onEdit} onDelete={onDelete} />
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}
