import { useState } from 'react'
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
          <table className="data-table" aria-label="Upcoming income">
            <thead>
              <tr>
                <th scope="col">Asset</th>
                <th scope="col">Broker</th>
                <th scope="col">Projected Date</th>
                <th scope="col" className="data-table__col--numeric">
                  Projected Amount
                </th>
              </tr>
            </thead>
            <tbody>
              {visibleEntries.map((entry) => (
                <tr key={`${entry.brokerName}-${entry.assetName}-${entry.projectedNextDate}`}>
                  <td>{entry.assetName}</td>
                  <td>{entry.brokerName}</td>
                  <td>{formatShortDate(entry.projectedNextDate)}</td>
                  <td className="data-table__col--numeric">{formatN2(entry.projectedAmount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
