import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { formatN2 } from '../formatters'
import { orMissing } from '../orMissing'

describe('orMissing', () => {
  it.each([null, undefined])('names a %s value Not available', (value) => {
    render(<p>{orMissing(value, formatN2)}</p>)

    expect(screen.getByRole('img', { name: 'Not available' })).toHaveTextContent('—')
  })

  it('formats a present value, zero included', () => {
    render(
      <ul>
        <li>{orMissing(0, formatN2)}</li>
        <li>{orMissing(1234.5, formatN2)}</li>
        <li>{orMissing('PT123', (isin) => `ISIN ${isin}`)}</li>
      </ul>,
    )

    expect(screen.getAllByRole('listitem').map((item) => item.textContent)).toEqual(['0.00', '1,234.50', 'ISIN PT123'])
    expect(screen.queryByRole('img', { name: 'Not available' })).not.toBeInTheDocument()
  })
})
