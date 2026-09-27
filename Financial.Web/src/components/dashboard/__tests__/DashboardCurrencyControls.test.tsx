import type { ComponentProps } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '../../../test/renderWithFluent'
import DashboardCurrencyControls from '../DashboardCurrencyControls'

function renderControls(overrides: Partial<ComponentProps<typeof DashboardCurrencyControls>> = {}) {
  return render(
    <DashboardCurrencyControls
      displayCurrency="GBP"
      onDisplayCurrencyChange={vi.fn()}
      brokerCurrencyFilter="ALL"
      onBrokerCurrencyFilterChange={vi.fn()}
      {...overrides}
    />,
  )
}

describe('DashboardCurrencyControls', () => {
  it('renders_exactly_3_currency_radio_options_with_the_seeded_one_selected', () => {
    renderControls()

    const radios = screen.getAllByRole('radio')
    expect(radios).toHaveLength(3)
    expect(screen.getByRole('radio', { name: 'GBP' })).toBeChecked()
    expect(screen.getByRole('radio', { name: 'BRL' })).not.toBeChecked()
    expect(screen.getByRole('radio', { name: 'USD' })).not.toBeChecked()
  })

  it('renders_exactly_4_broker_filter_tabs_with_ALL_selected_by_default', () => {
    renderControls()

    const tabs = screen.getAllByRole('tab')
    expect(tabs).toHaveLength(4)
    expect(screen.getByRole('tab', { name: 'All currencies' })).toHaveAttribute('aria-selected', 'true')
  })

  it('selecting_a_currency_option_calls_onDisplayCurrencyChange_with_the_new_value', () => {
    const onDisplayCurrencyChange = vi.fn()
    renderControls({ onDisplayCurrencyChange })

    fireEvent.click(screen.getByRole('radio', { name: 'BRL' }))

    expect(onDisplayCurrencyChange).toHaveBeenCalledWith('BRL')
  })

  it('selecting_a_broker_filter_tab_calls_onBrokerCurrencyFilterChange', () => {
    const onBrokerCurrencyFilterChange = vi.fn()
    renderControls({ onBrokerCurrencyFilterChange })

    fireEvent.click(screen.getByRole('tab', { name: 'BRL' }))

    expect(onBrokerCurrencyFilterChange).toHaveBeenCalledWith('BRL')
  })

  it('no_option_in_either_control_can_be_deselected', () => {
    renderControls({ displayCurrency: 'USD', brokerCurrencyFilter: 'GBP' })

    expect(screen.getByRole('radio', { name: 'USD' })).toBeChecked()
    expect(screen.getByRole('tab', { name: 'GBP' })).toHaveAttribute('aria-selected', 'true')
  })
})
