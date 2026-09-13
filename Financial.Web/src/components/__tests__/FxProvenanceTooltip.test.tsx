import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { render, screen } from '../../test/renderWithFluent'
import FxProvenanceTooltip from '../FxProvenanceTooltip'
import type { FxRateSnapshotDto } from '../../api/types'

const SNAPSHOT: FxRateSnapshotDto = {
  toCurrency: 'GBP',
  rate: 0.146,
  source: 'Frankfurter',
  retrievedAt: '2026-07-01T08:00:00Z',
}

describe('FxProvenanceTooltip', () => {
  it('[AC P49-F04-react-reporting-currency-03] renders_nothing_when_fxRateSnapshot_is_null', () => {
    render(<FxProvenanceTooltip currency="BRL" fxRateSnapshot={null} />)

    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('[AC P49-F04-react-reporting-currency-03] shows_rate_source_and_retrieved_at_on_hover', async () => {
    const user = userEvent.setup()
    render(<FxProvenanceTooltip currency="BRL" fxRateSnapshot={SNAPSHOT} />)

    await user.hover(screen.getByRole('button', { name: 'FX conversion details' }))

    expect(await screen.findByText(/1 BRL = 0.146 GBP/)).toBeInTheDocument()
    expect(screen.getByText(/Source: Frankfurter/)).toBeInTheDocument()
    expect(screen.getByText(/Retrieved:/)).toBeInTheDocument()
  })
})
