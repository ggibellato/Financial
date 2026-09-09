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

  it('pressing ArrowDown while closed opens the dropdown and activates the first option', () => {
    Element.prototype.scrollIntoView = vi.fn()
    renderCombobox()
    const input = screen.getByLabelText('Ticker')

    fireEvent.keyDown(input, { key: 'ArrowDown' })

    expect(screen.getByRole('listbox')).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'KLBN4' })).toHaveClass('ticker-combobox__option--active')
  })

  it('pressing ArrowDown repeatedly moves the active option forward, capped at the last one', () => {
    Element.prototype.scrollIntoView = vi.fn()
    renderCombobox()
    const input = screen.getByLabelText('Ticker')

    for (let i = 0; i < 10; i++) {
      fireEvent.keyDown(input, { key: 'ArrowDown' })
    }

    expect(screen.getByRole('option', { name: 'CSAN3' })).toHaveClass('ticker-combobox__option--active')
  })

  it('pressing ArrowUp moves the active option back, capped at the first one', () => {
    Element.prototype.scrollIntoView = vi.fn()
    renderCombobox()
    const input = screen.getByLabelText('Ticker')

    fireEvent.keyDown(input, { key: 'ArrowDown' })
    fireEvent.keyDown(input, { key: 'ArrowDown' })
    fireEvent.keyDown(input, { key: 'ArrowUp' })
    fireEvent.keyDown(input, { key: 'ArrowUp' })
    fireEvent.keyDown(input, { key: 'ArrowUp' })

    expect(screen.getByRole('option', { name: 'KLBN4' })).toHaveClass('ticker-combobox__option--active')
  })

  it('pressing Enter on the active option calls onChange and closes the dropdown', () => {
    Element.prototype.scrollIntoView = vi.fn()
    const { onChange } = renderCombobox()
    const input = screen.getByLabelText('Ticker')

    fireEvent.keyDown(input, { key: 'ArrowDown' })
    fireEvent.keyDown(input, { key: 'ArrowDown' })
    fireEvent.keyDown(input, { key: 'Enter' })

    expect(onChange).toHaveBeenCalledWith('TASA4')
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
  })

  it('pressing Enter with no active option does nothing', () => {
    const { onChange } = renderCombobox()
    openDropdown()

    fireEvent.keyDown(screen.getByLabelText('Ticker'), { key: 'Enter' })

    expect(onChange).not.toHaveBeenCalled()
  })
})
