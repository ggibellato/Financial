import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts'
import { Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import DataTableCell from '../grid/DataTableCell'
import { formatN2, formatPercent1 } from '../../utils/formatters'
import './AllocationPieChart.css'

// Validated categorical palette (fixed hue order, CVD-safe adjacency).
const CATEGORICAL_PALETTE = [
  '#2a78d6',
  '#1baf7a',
  '#eda100',
  '#008300',
  '#4a3aa7',
  '#e34948',
  '#e87ba4',
  '#eb6834',
]

export interface AllocationChartEntry {
  label: string
  marketValue: number
  percentage: number
}

interface AllocationTooltipContentProps {
  name: string
  value: number
  percentage: number
}

function AllocationTooltipContent({ name, value, percentage }: AllocationTooltipContentProps) {
  return (
    <div className="allocation-chart__tooltip">
      <div className="allocation-chart__tooltip-name">{name}</div>
      <div className="allocation-chart__tooltip-value">{formatN2(value)}</div>
      <div className="allocation-chart__tooltip-percent">{formatPercent1(percentage)}</div>
    </div>
  )
}

// recharts' Pie tooltip payload entry doesn't carry our own fields itself —
// they surface under `.payload`, the original datum object.
function readAllocationTooltipEntry(entry: unknown): AllocationTooltipContentProps {
  const record = entry as { payload?: { name?: unknown; value?: unknown; percentage?: unknown } }
  const source = record.payload ?? {}
  return {
    name: typeof source.name === 'string' ? source.name : '',
    value: typeof source.value === 'number' ? source.value : 0,
    percentage: typeof source.percentage === 'number' ? source.percentage : 0,
  }
}

interface AllocationPieChartProps {
  title: string
  entries: AllocationChartEntry[]
}

export default function AllocationPieChart({ title, entries }: AllocationPieChartProps) {
  const sliceData = entries.map((entry) => ({
    name: entry.label,
    value: entry.marketValue,
    percentage: entry.percentage,
  }))
  const titleId = `allocation-chart-title-${title.toLowerCase().replace(/\s+/g, '-')}`

  return (
    <div className="allocation-chart">
      <h4 id={titleId} className="allocation-chart__title">
        {title}
      </h4>
      <div className="allocation-chart__grid">
        <div className="allocation-chart__graphic" role="img" aria-labelledby={titleId}>
          <ResponsiveContainer width="100%" height={280}>
            <PieChart>
              <Pie data={sliceData} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={90}>
                {sliceData.map((slice, index) => (
                  <Cell key={slice.name} fill={CATEGORICAL_PALETTE[index % CATEGORICAL_PALETTE.length]} />
                ))}
              </Pie>
              <Tooltip
                content={({ active, payload }) => {
                  if (!active || !payload || payload.length === 0) {
                    return null
                  }
                  return <AllocationTooltipContent {...readAllocationTooltipEntry(payload[0])} />
                }}
              />
              <Legend />
            </PieChart>
          </ResponsiveContainer>
        </div>
        <Table className="allocation-chart__legend data-table" aria-labelledby={titleId}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Label</TableHeaderCell>
              <TableHeaderCell className="data-table__col--numeric">Market Value</TableHeaderCell>
              <TableHeaderCell className="data-table__col--numeric">Percentage</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {entries.map((entry) => (
              <TableRow key={entry.label}>
                <DataTableCell label="Label">{entry.label}</DataTableCell>
                <DataTableCell label="Market Value" className="data-table__col--numeric">
                  {formatN2(entry.marketValue)}
                </DataTableCell>
                <DataTableCell label="Percentage" className="data-table__col--numeric">
                  {formatPercent1(entry.percentage)}
                </DataTableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
