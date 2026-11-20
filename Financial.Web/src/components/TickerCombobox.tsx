import { useCallback, useEffect, useId, useMemo, useRef, useState } from 'react'
import './TickerCombobox.css'

export interface TickerGroup {
  label: string
  tickers: string[]
}

interface TickerComboboxProps {
  groups: TickerGroup[]
  value: string
  onChange: (ticker: string) => void
}

export default function TickerCombobox({ groups, value, onChange }: TickerComboboxProps) {
  const [isOpen, setIsOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(-1)
  const containerRef = useRef<HTMLDivElement>(null)
  const listboxRef = useRef<HTMLUListElement>(null)
  const listboxId = useId()
  const optionId = (idx: number) => `${listboxId}-option-${idx}`

  const flatOptions = useMemo(() => groups.flatMap((g) => g.tickers), [groups])
  const hasActiveOption = isOpen && activeIndex >= 0 && activeIndex < flatOptions.length

  const structuredGroups = useMemo(() => {
    let idx = 0
    return groups.map((group) => ({
      label: group.label,
      options: group.tickers.map((ticker) => ({ ticker, idx: idx++ })),
    }))
  }, [groups])

  const select = useCallback(
    (ticker: string) => {
      onChange(ticker)
      setIsOpen(false)
      setActiveIndex(-1)
    },
    [onChange],
  )

  const handleInputChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      onChange(e.target.value)
      setIsOpen(true)
      setActiveIndex(-1)
    },
    [onChange],
  )

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === 'Escape') {
        setIsOpen(false)
        setActiveIndex(-1)
        return
      }
      if (e.key === 'ArrowDown') {
        e.preventDefault()
        if (!isOpen) {
          setIsOpen(true)
          setActiveIndex(flatOptions.length > 0 ? 0 : -1)
        } else {
          setActiveIndex((prev) => Math.min(prev + 1, flatOptions.length - 1))
        }
        return
      }
      if (e.key === 'ArrowUp') {
        e.preventDefault()
        if (!isOpen) {
          setIsOpen(true)
          setActiveIndex(flatOptions.length - 1)
        } else {
          setActiveIndex((prev) => Math.max(prev - 1, 0))
        }
        return
      }
      if (e.key === 'Enter' && hasActiveOption) {
        e.preventDefault()
        select(flatOptions[activeIndex])
      }
    },
    [isOpen, activeIndex, hasActiveOption, flatOptions, select],
  )

  useEffect(() => {
    if (!isOpen) return
    const handleMouseDown = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false)
        setActiveIndex(-1)
      }
    }
    document.addEventListener('mousedown', handleMouseDown)
    return () => document.removeEventListener('mousedown', handleMouseDown)
  }, [isOpen])

  useEffect(() => {
    if (activeIndex < 0 || !listboxRef.current) return
    const items = listboxRef.current.querySelectorAll<HTMLElement>('[role="option"]')
    items[activeIndex]?.scrollIntoView({ block: 'nearest' })
  }, [activeIndex])

  return (
    <div ref={containerRef} className="ticker-combobox">
      <input
        type="text"
        role="combobox"
        aria-label="Ticker"
        aria-haspopup="listbox"
        aria-controls={listboxId}
        aria-expanded={isOpen}
        aria-activedescendant={hasActiveOption ? optionId(activeIndex) : undefined}
        value={value}
        onChange={handleInputChange}
        onFocus={() => setIsOpen(true)}
        onKeyDown={handleKeyDown}
      />
      {isOpen && (
        <ul ref={listboxRef} id={listboxId} className="ticker-combobox__dropdown" role="listbox" aria-label="Ticker options">
          {structuredGroups.map((group, groupIdx) => (
            <li
              key={group.label}
              className="ticker-combobox__group"
              role="group"
              aria-labelledby={`${listboxId}-group-${groupIdx}`}
            >
              <span id={`${listboxId}-group-${groupIdx}`} className="ticker-combobox__group-label">
                {group.label}
              </span>
              <ul role="none">
                {group.options.map(({ ticker, idx }) => (
                  <li
                    key={ticker}
                    id={optionId(idx)}
                    role="option"
                    aria-selected={ticker === value}
                    className={`ticker-combobox__option${idx === activeIndex ? ' ticker-combobox__option--active' : ''}`}
                    onMouseDown={(e) => {
                      e.preventDefault()
                      select(ticker)
                    }}
                  >
                    {ticker}
                  </li>
                ))}
              </ul>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
