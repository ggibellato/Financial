import { render } from '../../test/renderWithFluent'
import { describe, expect, it } from 'vitest'
import ChartValueLabel from '../ChartValueLabel'
import { formatN2 } from '../../utils/formatters'

const nonZero = (value: number) => (value !== 0 ? formatN2(value) : '')

function renderLabel(props: { x?: number; y?: number; width?: number; height?: number; value?: unknown }) {
  const { container } = render(
    <svg>
      <ChartValueLabel formatValue={nonZero} {...props} />
    </svg>,
  )
  return container.querySelector('text')
}

describe('ChartValueLabel', () => {
  it('centres the value above a bar wide enough to hold it', () => {
    const label = renderLabel({ x: 10, y: 50, width: 80, value: 1234.5 })

    expect(label).toHaveTextContent('1,234.50')
    expect(label).toHaveAttribute('x', '50')
    expect(label).toHaveAttribute('y', '46')
  })

  it('puts a negative bar\'s value below the bar, outside its fill', () => {
    const label = renderLabel({ x: 10, y: 120, width: 80, height: -30, value: -250 })

    expect(label).toHaveTextContent('-250.00')
    expect(label).toHaveAttribute('y', '124')
    expect(label).toHaveAttribute('dominant-baseline', 'hanging')
  })

  it('leaves out the value when the bar is narrower than the text, so neighbouring labels cannot overlap', () => {
    expect(renderLabel({ x: 10, y: 50, width: 40, value: 1234.5 })).toBeNull()
  })

  it('leaves out a value the formatter blanks', () => {
    expect(renderLabel({ x: 10, y: 50, width: 80, value: 0 })).toBeNull()
  })

  it('draws nothing without a numeric value or position', () => {
    expect(renderLabel({ x: 10, y: 50, width: 80, value: '12' })).toBeNull()
    expect(renderLabel({ y: 50, width: 80, value: 12 })).toBeNull()
  })
})
