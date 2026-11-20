import type { ReactNode } from 'react'

interface MockPieDatum {
  name: string
  value: number
  percentage: number
}

const TOOLTIP_SAMPLE: MockPieDatum = { name: 'Equity', value: 1234.5, percentage: 25.6 }

export const PieChart = ({ children }: { children: ReactNode }) => <div data-testid="pie-chart">{children}</div>

export const Pie = ({ data, children }: { data: MockPieDatum[]; children?: ReactNode }) => (
  <div data-testid="pie">
    {data.map((d) => (
      <span key={d.name} data-testid="pie-slice">
        {d.name}
      </span>
    ))}
    {children}
  </div>
)

export const Cell = () => null

export const Tooltip = ({
  content,
}: {
  content?: (props: { active: boolean; payload: { payload: MockPieDatum }[] }) => ReactNode
}) => <div data-testid="tooltip">{content ? content({ active: true, payload: [{ payload: TOOLTIP_SAMPLE }] }) : null}</div>

export const Legend = () => <div data-testid="legend" />

export const ResponsiveContainer = ({ children }: { children: ReactNode }) => (
  <div data-testid="responsive-container">{children}</div>
)
