import { useState } from 'react'
import { Tab, TabList } from '@fluentui/react-components'
import type { SelectTabData, SelectTabEvent } from '@fluentui/react-components'
import ErrorState from '../ErrorState'
import LoadingState from '../LoadingState'
import AllocationPieChart, { type AllocationChartEntry } from './AllocationPieChart'
import type { AllocationBreakdownDto, BrokerCurrencyFilter } from '../../api/types'
import './AllocationBreakdownPanel.css'

type AllocationDimension = 'class' | 'currency' | 'country' | 'broker'

interface DimensionDefinition {
  id: AllocationDimension
  label: string
  title: string
  entries: (breakdown: AllocationBreakdownDto) => AllocationChartEntry[]
}

const DIMENSIONS: DimensionDefinition[] = [
  {
    id: 'class',
    label: 'Class',
    title: 'Allocation by asset class',
    entries: (breakdown) =>
      breakdown.byClass.map((entry) => ({
        label: entry.class,
        marketValue: entry.marketValue,
        percentage: entry.percentage,
      })),
  },
  {
    id: 'currency',
    label: 'Currency',
    title: 'Allocation by currency',
    entries: (breakdown) =>
      breakdown.byCurrency.map((entry) => ({
        label: entry.currency,
        marketValue: entry.marketValue,
        percentage: entry.percentage,
      })),
  },
  {
    id: 'country',
    label: 'Country',
    title: 'Allocation by country',
    entries: (breakdown) =>
      breakdown.byCountry.map((entry) => ({
        label: entry.country,
        marketValue: entry.marketValue,
        percentage: entry.percentage,
      })),
  },
  {
    id: 'broker',
    label: 'Broker',
    title: 'Allocation by broker',
    entries: (breakdown) =>
      breakdown.byBroker.map((entry) => ({
        label: entry.brokerName,
        marketValue: entry.marketValue,
        percentage: entry.percentage,
      })),
  },
]

interface AllocationBreakdownPanelProps {
  breakdown: AllocationBreakdownDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
  brokerCurrencyFilter: BrokerCurrencyFilter
}

export default function AllocationBreakdownPanel({
  breakdown,
  isLoading,
  error,
  retry,
  brokerCurrencyFilter,
}: AllocationBreakdownPanelProps) {
  const [activeDimension, setActiveDimension] = useState<AllocationDimension>('class')

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  if (!breakdown) {
    return null
  }

  if (breakdown.isUnavailable) {
    return (
      <ErrorState
        message={`Converted totals unavailable — unable to convert figures into ${breakdown.displayCurrency} right now.`}
        onRetry={retry}
      />
    )
  }

  const dimension = DIMENSIONS.find((candidate) => candidate.id === activeDimension) ?? DIMENSIONS[0]
  const entries = dimension.entries(breakdown)
  const everyDimensionEmpty =
    entries.length === 0 &&
    DIMENSIONS.every((candidate) => candidate.id === dimension.id || candidate.entries(breakdown).length === 0)

  return (
    <div className="allocation-breakdown">
      {breakdown.displayCurrency && (
        <p className="allocation-breakdown__currency-line">Values shown in {breakdown.displayCurrency}</p>
      )}
      {breakdown.isPartial && (
        <p className="allocation-breakdown__notice allocation-breakdown__notice--partial" role="status">
          Some figures could not be converted to {breakdown.displayCurrency} — showing partial totals.
        </p>
      )}
      <TabList
        selectedValue={activeDimension}
        onTabSelect={(_event: SelectTabEvent, data: SelectTabData) =>
          setActiveDimension(data.value as AllocationDimension)
        }
      >
        {DIMENSIONS.map((candidate) => (
          <Tab key={candidate.id} value={candidate.id}>
            {candidate.label}
          </Tab>
        ))}
      </TabList>

      {entries.length === 0 ? (
        <p className="allocation-breakdown__empty">
          {everyDimensionEmpty && brokerCurrencyFilter !== 'ALL'
            ? `No brokers use the selected currency (${brokerCurrencyFilter}).`
            : 'No priced holdings to display for this view.'}
        </p>
      ) : (
        <AllocationPieChart title={dimension.title} entries={entries} />
      )}
    </div>
  )
}
