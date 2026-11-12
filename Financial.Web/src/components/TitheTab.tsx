import { Button, Checkbox, makeStyles, Spinner, Table, TableBody, TableHeader, TableHeaderCell, TableRow, Text, tokens } from '@fluentui/react-components'
import { type ReactNode, useId } from 'react'
import type { TitheMonthDto, TitheSummaryDto } from '../api/types'
import { TITHE_DETAIL_LOAD_ERROR, TITHE_HISTORY_LOAD_ERROR, useTithe } from '../hooks/useTithe'
import { formatLongMonthYear, formatMonthInputValue, formatN2, SAVING_LABEL, sumPence } from '../utils/formatters'
import ActionErrorAlert from './ActionErrorAlert'
import DataTableCell from './grid/DataTableCell'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import MissingValue from './MissingValue'
import SectionTotals from './SectionTotals'

type TitheCarryForwardDto = NonNullable<TitheSummaryDto['carryForward']>

const useStyles = makeStyles({
  carryForward: {
    display: 'flex',
    flexWrap: 'wrap',
    alignItems: 'center',
    columnGap: tokens.spacingHorizontalM,
  },
  historyScroll: {
    overflowX: 'auto',
  },
})

interface TitheTabProps {
  year: number
  month: number
  onSelectMonth: (monthInputValue: string) => void
}

function titheBalanceText(balance: number): string {
  if (balance > 0) return `${formatN2(balance)} owed`
  if (balance < 0) return `${formatN2(-balance)} in credit`
  return formatN2(0)
}

function carryInText(carryForward: TitheCarryForwardDto | null | undefined): string {
  return carryForward ? `${formatN2(carryForward.amount)} from ${formatLongMonthYear(carryForward.fromYear, carryForward.fromMonth)}` : formatN2(0)
}

function includedCell(carryForward: TitheCarryForwardDto | null | undefined): ReactNode {
  if (!carryForward) return <MissingValue label="No carry-in" />
  return carryForward.included ? 'Yes' : 'No'
}

function TitheDetail({ summary, monthLabel, carryForwardUpdating, carryForwardError, onToggleCarryForward }: {
  summary: TitheSummaryDto
  monthLabel: string
  carryForwardUpdating: boolean
  carryForwardError: string | null
  onToggleCarryForward: (included: boolean) => void
}) {
  const styles = useStyles()
  const carryForward = summary.carryForward
  return (
    <div className="accounts-page__section">
      <SectionTotals
        label={`Tithe for ${monthLabel}`}
        items={[
          { label: 'Income base', value: formatN2(summary.incomeBase) },
          { label: 'Tithe', value: formatN2(summary.calculatedTithe) },
          { label: 'Tithe paid', value: formatN2(summary.tithePaid) },
          { label: 'Carry-in', value: carryInText(carryForward) },
          { label: 'Balance', value: titheBalanceText(summary.titheBalance) },
        ]}
      />
      {summary.incomeBase === 0 && <Text>No income this month.</Text>}
      <ActionErrorAlert message={carryForwardError} />
      {carryForward && (
        <div className={styles.carryForward}>
          <Checkbox
            label={`Include ${formatLongMonthYear(carryForward.fromYear, carryForward.fromMonth)}'s balance of ${formatN2(carryForward.amount)}`}
            checked={carryForward.included}
            aria-disabled={carryForwardUpdating}
            onChange={(_, data) => {
              if (!carryForwardUpdating) onToggleCarryForward(data.checked === true)
            }}
          />
          {carryForwardUpdating && <Spinner size="tiny" label={SAVING_LABEL} labelPosition="after" />}
        </div>
      )}
    </div>
  )
}

function emphasise(content: ReactNode, isCurrent: boolean): ReactNode {
  return isCurrent ? <strong>{content}</strong> : content
}

interface HistoryColumn {
  label: string
  numeric: boolean
  render: (summary: TitheSummaryDto) => ReactNode
  total?: (summary: TitheSummaryDto) => number
}

const HISTORY_COLUMNS: HistoryColumn[] = [
  { label: 'Income base', numeric: true, render: (s) => formatN2(s.incomeBase), total: (s) => s.incomeBase },
  { label: 'Tithe', numeric: true, render: (s) => formatN2(s.calculatedTithe), total: (s) => s.calculatedTithe },
  { label: 'Tithe paid', numeric: true, render: (s) => formatN2(s.tithePaid), total: (s) => s.tithePaid },
  { label: 'Carry-in', numeric: true, render: (s) => formatN2(s.carryForward?.amount ?? 0) },
  { label: 'Included', numeric: false, render: (s) => includedCell(s.carryForward) },
  { label: 'Balance', numeric: true, render: (s) => titheBalanceText(s.titheBalance) },
]

function columnClass({ numeric }: HistoryColumn): string | undefined {
  return numeric ? 'data-table__col--numeric' : undefined
}

function sumInPence(rows: TitheMonthDto[], value: (summary: TitheSummaryDto) => number): number {
  return sumPence(rows, (row) => value(row.summary)) / 100
}

function TitheHistory({ year, month, rows, onSelectMonth }: { year: number; month: number; rows: TitheMonthDto[]; onSelectMonth: (monthInputValue: string) => void }) {
  const styles = useStyles()
  const captionId = useId()
  return (
    <div className={styles.historyScroll}>
      <Table className="data-table" aria-labelledby={captionId}>
        <caption id={captionId}>Tithe by month, {year}</caption>
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Month</TableHeaderCell>
            {HISTORY_COLUMNS.map((column) => (
              <TableHeaderCell key={column.label} className={columnClass(column)}>
                {column.label}
              </TableHeaderCell>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.map((row) => {
            const isCurrent = row.year === year && row.month === month
            const monthLabel = formatLongMonthYear(row.year, row.month)
            return (
              <TableRow key={row.month} aria-current={isCurrent ? 'date' : undefined}>
                <DataTableCell label="Month">
                  <Button appearance="transparent" size="small" aria-label={`Show ${monthLabel}`} aria-current={isCurrent ? 'date' : undefined} onClick={() => onSelectMonth(formatMonthInputValue(row.year, row.month))}>
                    {emphasise(monthLabel, isCurrent)}
                  </Button>
                </DataTableCell>
                {HISTORY_COLUMNS.map((column) => (
                  <DataTableCell key={column.label} label={column.label} className={columnClass(column)}>
                    {emphasise(column.render(row.summary), isCurrent)}
                  </DataTableCell>
                ))}
              </TableRow>
            )
          })}
        </TableBody>
        <tfoot>
          <TableRow>
            <DataTableCell label="Total">Total</DataTableCell>
            {HISTORY_COLUMNS.map((column) =>
              column.total ? (
                <DataTableCell key={column.label} label={column.label} className={columnClass(column)}>
                  {formatN2(sumInPence(rows, column.total))}
                </DataTableCell>
              ) : (
                <DataTableCell key={column.label} label={column.label}>
                  <MissingValue label="Not totalled" />
                </DataTableCell>
              ),
            )}
          </TableRow>
        </tfoot>
      </Table>
    </div>
  )
}

export default function TitheTab({ year, month, onSelectMonth }: TitheTabProps) {
  const { detail, history, carryForwardUpdating, carryForwardError, updateCarryForwardInclusion } = useTithe(year, month)
  const monthLabel = formatLongMonthYear(year, month)

  return (
    <div className="monthly-page__summary-groups">
      <Text as="h2" weight="semibold" size={400}>
        Tithe for {monthLabel}
      </Text>
      <div>
        {detail.isLoading ? (
          <LoadingState message="Loading the tithe…" />
        ) : detail.error || !detail.data ? (
          <ErrorState message={TITHE_DETAIL_LOAD_ERROR} onRetry={detail.retry} />
        ) : (
          <TitheDetail
            summary={detail.data}
            monthLabel={monthLabel}
            carryForwardUpdating={carryForwardUpdating}
            carryForwardError={carryForwardError}
            onToggleCarryForward={updateCarryForwardInclusion}
          />
        )}
      </div>
      <div className="accounts-page__section">
        <Text as="h2" weight="semibold" size={400}>
          History
        </Text>
        {history.isLoading ? (
          <LoadingState message="Loading the tithe history…" />
        ) : history.error || !history.data ? (
          <ErrorState message={TITHE_HISTORY_LOAD_ERROR} onRetry={history.retry} />
        ) : (
          <TitheHistory year={year} month={month} rows={history.data} onSelectMonth={onSelectMonth} />
        )}
      </div>
    </div>
  )
}
