import { Tab, TabList } from '@fluentui/react-components'
import type { SelectTabData, SelectTabEvent } from '@fluentui/react-components'
import { useState } from 'react'
import type { MonthTransactionDto } from '../api/types'
import BanksGrid from '../components/BanksGrid'
import CardsGrid from '../components/CardsGrid'
import CategoryTotalsGrid from '../components/CategoryTotalsGrid'
import ErrorState from '../components/ErrorState'
import IncomingGrid from '../components/IncomingGrid'
import LoadingState from '../components/LoadingState'
import MonthTransactionsGrid from '../components/MonthTransactionsGrid'
import { useMonthly } from '../hooks/useMonthly'
import { useMonthTransactions } from '../hooks/useMonthTransactions'
import './MonthlyPage.css'

type MonthlyTabId = 'summary' | 'transactions'

const MONTHLY_TABS: { id: MonthlyTabId; label: string }[] = [
  { id: 'summary', label: 'Summary' },
  { id: 'transactions', label: 'All transactions' },
]

const NO_ROWS: MonthTransactionDto[] = []

function AllTransactionsTab({ year, month }: { year: number; month: number }) {
  const { data, isLoading, error, retry } = useMonthTransactions(year, month)
  return <MonthTransactionsGrid year={year} month={month} rows={data ?? NO_ROWS} isLoading={isLoading} error={error} onRetry={retry} />
}

export default function MonthlyPage() {
  const {
    year,
    month,
    monthInputValue,
    setMonthInputValue,
    categoryTotals,
    categoryTotalsSum,
    bankTotals,
    cardMembers,
    currencyTotals,
    isLoading,
    error,
    retry,
    incomeTotals,
    totalIncoming,
    titheSummary,
    carryForwardUpdating,
    updateCarryForwardInclusion,
    carryForwardError,
  } = useMonthly()

  const [activeTab, setActiveTab] = useState<MonthlyTabId>('summary')

  const handleTabSelect = (_event: SelectTabEvent, data: SelectTabData) => setActiveTab(data.value as MonthlyTabId)

  return (
    <div className="monthly-page">
      <div className="monthly-page__header">
        <div className="monthly-page__month-picker">
          <label htmlFor="monthly-month">Month</label>
          <input
            id="monthly-month"
            type="month"
            value={monthInputValue}
            onChange={(e) => setMonthInputValue(e.target.value)}
          />
        </div>
      </div>

      <TabList selectedValue={activeTab} onTabSelect={handleTabSelect}>
        {MONTHLY_TABS.map((tab) => (
          <Tab key={tab.id} value={tab.id}>
            {tab.label}
          </Tab>
        ))}
      </TabList>

      {activeTab === 'transactions' ? (
        <div className="monthly-page__content">
          <AllTransactionsTab key={`${year}-${month}`} year={year} month={month} />
        </div>
      ) : isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <div className="monthly-page__content">
          <div className="monthly-page__summary-groups">
            <div className="monthly-page__grids-row">
              <CategoryTotalsGrid categoryTotals={categoryTotals} categoryTotalsSum={categoryTotalsSum} />
              <CardsGrid cards={cardMembers} currencyTotals={currencyTotals} viewedMonth={monthInputValue} />
            </div>
            <div className="monthly-page__grids-row">
              <BanksGrid bankTotals={bankTotals} currencyTotals={currencyTotals} viewedMonth={monthInputValue} />
              <IncomingGrid
                incomeTotals={incomeTotals}
                totalIncoming={totalIncoming}
                titheSummary={titheSummary}
                carryForwardUpdating={carryForwardUpdating}
                onToggleCarryForward={updateCarryForwardInclusion}
                carryForwardActionError={carryForwardError}
              />
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
