import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import TickerCombobox, { type TickerGroup } from '../TickerCombobox'

const GROUPS: TickerGroup[] = [
  { label: 'Ja possuidas', tickers: ['KLBN4', 'TASA4', 'TAEE3'] },
  { label: 'Outras Barse', tickers: ['UNIP6', 'CMIG4', 'TRPL4', 'BBAS3'] },
  { label: 'Outras', tickers: ['CSAN3'] },
]

function renderCombobox(value = 'KLBN4', onChange = vi.fn()) {
  return { onChange, ...render(<TickerCombobox groups={GROUPS} value={value} onChange={onChange} />) }
}

function openDropdown() {
  fireEvent.focus(screen.getByLabelText('Ticker'))
}

describe('TickerCombobox', () => {
  it('displays the initial value in the input', () => {
    renderCombobox('KLBN4')
    expect(screen.getByLabelText('Ticker')).toHaveValue('KLBN4')
  })

  it('renders all three group labels when open', () => {
    renderCombobox()
    openDropdown()
    expect(screen.getByText('Ja possuidas')).toBeInTheDocument()
    expect(screen.getByText('Outras Barse')).toBeInTheDocument()
    expect(screen.getByText('Outras')).toBeInTheDocument()
  })

  it('renders all 8 tickers across groups when open', () => {
    renderCombobox()
    openDropdown()
    for (const ticker of ['KLBN4', 'TASA4', 'TAEE3', 'UNIP6', 'CMIG4', 'TRPL4', 'BBAS3', 'CSAN3']) {
      expect(screen.getByRole('option', { name: ticker })).toBeInTheDocument()
    }
  })

  it('clicking an option calls onChange with that ticker', () => {
    const { onChange } = renderCombobox()
    openDropdown()
    fireEvent.mouseDown(screen.getByRole('option', { name: 'TASA4' }))
    expect(onChange).toHaveBeenCalledWith('TASA4')
  })

  it('typing into the input calls onChange with the typed value', () => {
    const { onChange } = renderCombobox()
    fireEvent.change(screen.getByLabelText('Ticker'), { target: { value: 'CXSE3' } })
    expect(onChange).toHaveBeenCalledWith('CXSE3')
  })

  it('pressing Escape closes the dropdown', () => {
    renderCombobox()
    openDropdown()
    expect(screen.getByRole('listbox')).toBeInTheDocument()
    fireEvent.keyDown(screen.getByLabelText('Ticker'), { key: 'Escape' })
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
  })

  it('clicking outside the component closes the dropdown', () => {
    renderCombobox()
    openDropdown()
    expect(screen.getByRole('listbox')).toBeInTheDocument()
    fireEvent.mouseDown(document.body)
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
  })
})
