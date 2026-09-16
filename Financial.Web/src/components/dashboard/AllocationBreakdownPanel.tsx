import { useState } from 'react'
import { Tab, TabList } from '@fluentui/react-components'
import type { SelectTabData, SelectTabEvent } from '@fluentui/react-components'
import ErrorState from '../ErrorState'
import LoadingState from '../LoadingState'
import AllocationPieChart, { type AllocationChartEntry } from './AllocationPieChart'
import type { AllocationBreakdownDto } from '../../api/types'
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
}

export default function AllocationBreakdownPanel({
  breakdown,
  isLoading,
  error,
  retry,
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

  const dimension = DIMENSIONS.find((candidate) => candidate.id === activeDimension) ?? DIMENSIONS[0]
  const entries = dimension.entries(breakdown)

  return (
    <div className="allocation-breakdown">
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
        <p className="allocation-breakdown__empty">No priced holdings to display for this view.</p>
      ) : (
        <AllocationPieChart title={dimension.title} entries={entries} />
      )}
    </div>
  )
}
