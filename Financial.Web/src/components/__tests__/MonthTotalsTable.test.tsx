import { render, screen, within } from '../../test/renderWithFluent'
import { describe, expect, it } from 'vitest'
import MonthTotalsTable from '../MonthTotalsTable'

interface Row {
  month: string
  dividend: number
  total: number
}

const COLUMNS = [
  { label: 'Dividend', value: (row: Row) => row.dividend },
  { label: 'Total', value: (row: Row) => row.total, isTotal: true },
]

const ROWS: Row[] = [
  { month: '01/2026', dividend: 12.5, total: 20 },
  { month: '02/2026', dividend: 0, total: 0 },
  { month: '03/2026', dividend: 0, total: 7.25 },
]

const cellTexts = (row: HTMLElement) => within(row).getAllByRole('cell').map((cell) => cell.textContent?.replace(/^[^:]+:/, ''))

describe('MonthTotalsTable', () => {
  it('lists each month that has a value, one column per series, and the period total, skipping empty months', () => {
    render(<MonthTotalsTable title="Credits by month" rows={ROWS} columns={COLUMNS} />)

    const table = screen.getByRole('table', { name: 'Credits by month' })
    expect(within(table).getAllByRole('columnheader').map((cell) => cell.textContent)).toEqual(['Month', 'Dividend', 'Total'])
    expect(within(table).getAllByRole('row').slice(1).map(cellTexts)).toEqual([
      ['01/2026', '12.50', '20.00'],
      ['03/2026', '0.00', '7.25'],
      ['Period total', '12.50', '27.25'],
    ])
  })

  it('labels the period-total row as the total when the table stacks into cards', () => {
    render(<MonthTotalsTable title="Credits by month" rows={ROWS} columns={COLUMNS} />)

    const footer = within(screen.getByRole('table')).getAllByRole('row').at(-1)!
    expect(within(footer).getAllByRole('cell')[0]).toHaveTextContent(/^Total:\s*Period total$/)
  })

  it('bolds the total column only', () => {
    render(<MonthTotalsTable title="Credits by month" rows={ROWS} columns={COLUMNS} />)

    const [, dividend, total] = within(within(screen.getByRole('table')).getAllByRole('row')[1]).getAllByRole('cell')
    expect(total.querySelector('strong')).toHaveTextContent('20.00')
    expect(dividend.querySelector('strong')).toBeNull()
  })

  it('scrolls on its own and can be reached with the keyboard', () => {
    render(<MonthTotalsTable title="Credits by month" rows={ROWS} columns={COLUMNS} />)

    expect(screen.getByRole('region', { name: 'Credits by month, monthly totals' })).toHaveAttribute('tabindex', '0')
  })

  it('says so when no month in the period has a value', () => {
    render(<MonthTotalsTable title="Credits by month" rows={[ROWS[1]]} columns={COLUMNS} />)

    expect(screen.getByText('No values in this period.')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })
})
