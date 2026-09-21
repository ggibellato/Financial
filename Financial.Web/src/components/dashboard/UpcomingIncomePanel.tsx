import { useState } from 'react'
import { Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import DataTableCell from '../grid/DataTableCell'
import ErrorState from '../ErrorState'
import FilterTabList from '../FilterTabList'
import LoadingState from '../LoadingState'
import { formatN2, formatShortDate } from '../../utils/formatters'
import {
  DEFAULT_UPCOMING_INCOME_WINDOW,
  UPCOMING_INCOME_WINDOW_OPTIONS,
  isWithinUpcomingIncomeWindow,
} from '../../utils/upcomingIncomeWindow'
import type { UpcomingIncomeWindowDays } from '../../utils/upcomingIncomeWindow'
import type { UpcomingIncomeDto } from '../../api/types'
import './UpcomingIncomePanel.css'

interface UpcomingIncomePanelProps {
  entries: UpcomingIncomeDto[] | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

export default function UpcomingIncomePanel({ entries, isLoading, error, retry }: UpcomingIncomePanelProps) {
  const [windowDays, setWindowDays] = useState<UpcomingIncomeWindowDays>(DEFAULT_UPCOMING_INCOME_WINDOW)

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  if (!entries) {
    return null
  }

  const visibleEntries = entries.filter((entry) => isWithinUpcomingIncomeWindow(entry.projectedNextDate, windowDays))

  return (
    <div className="upcoming-income">
      <FilterTabList
        label="Window"
        options={UPCOMING_INCOME_WINDOW_OPTIONS}
        selected={windowDays}
        onSelect={setWindowDays}
      />

      {visibleEntries.length === 0 ? (
        <p className="upcoming-income__empty">No upcoming payments detected in the next {windowDays} days</p>
      ) : (
        <div className="upcoming-income__table-wrapper">
          <Table className="data-table" aria-label="Upcoming income">
            <TableHeader>
              <TableRow>
                <TableHeaderCell>Asset</TableHeaderCell>
                <TableHeaderCell>Broker</TableHeaderCell>
                <TableHeaderCell>Projected Date</TableHeaderCell>
                <TableHeaderCell className="data-table__col--numeric">Projected Amount</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {visibleEntries.map((entry) => (
                <TableRow key={`${entry.brokerName}-${entry.assetName}-${entry.projectedNextDate}`}>
                  <DataTableCell label="Asset">{entry.assetName}</DataTableCell>
                  <DataTableCell label="Broker">{entry.brokerName}</DataTableCell>
                  <DataTableCell label="Projected Date">{formatShortDate(entry.projectedNextDate)}</DataTableCell>
                  <DataTableCell label="Projected Amount" className="data-table__col--numeric">
                    {formatN2(entry.projectedAmount)}
                  </DataTableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  )
}
