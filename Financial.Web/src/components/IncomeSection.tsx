import { Button, makeStyles, tokens } from '@fluentui/react-components'
import { AddRegular } from '@fluentui/react-icons'
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
          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M20 20H7L3 16a2 2 0 0 1 0-2.83L14.59 1.58a2 2 0 0 1 2.83 0l4 4a2 2 0 0 1 0 2.83L8 20" />
            <path d="M6.5 15.5 15 7" />
          </svg>
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
}

export default function IncomeSection({ incomes, onEdit, onDelete, onNewIncome }: IncomeSectionProps) {
  const styles = useStyles()
  return (
    <section className="income-section">
      <div className={styles.header}>
        <Button appearance="primary" icon={<AddRegular />} onClick={onNewIncome}>
          New Income
        </Button>
      </div>
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
