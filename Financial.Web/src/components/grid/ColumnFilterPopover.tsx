import { useId, type ReactNode } from 'react'
import { Button, Popover, PopoverSurface, PopoverTrigger } from '@fluentui/react-components'
import { FilterFilled, FilterRegular } from '@fluentui/react-icons'
import { useMediaQuery } from '../../hooks/useMediaQuery'
import { MOBILE_MEDIA_QUERY } from '../../styles/breakpoints'
import { FILTER_APPLIED_DESCRIPTION, filterTriggerLabel } from './columnFilterText'
import './ColumnFilterMenu.css'

interface ColumnFilterPopoverProps {
  label: string
  isFiltered: boolean
  children: ReactNode
}

export default function ColumnFilterPopover({ label, isFiltered, children }: ColumnFilterPopoverProps) {
  const isStackedLayout = useMediaQuery(MOBILE_MEDIA_QUERY)
  const appliedId = useId()

  return (
    <>
      <Popover positioning="below-end">
        <PopoverTrigger disableButtonEnhancement>
          <Button
            appearance="subtle"
            size="small"
            className={`column-filter-menu__trigger${isFiltered ? ' column-filter-menu__trigger--active' : ''}`}
            icon={isFiltered ? <FilterFilled /> : <FilterRegular />}
            aria-label={filterTriggerLabel(label)}
            aria-describedby={isFiltered ? appliedId : undefined}
            tabIndex={isStackedLayout ? -1 : undefined}
          />
        </PopoverTrigger>
        <PopoverSurface className="column-filter-menu__surface">{children}</PopoverSurface>
      </Popover>
      {isFiltered && (
        <span id={appliedId} hidden>
          {FILTER_APPLIED_DESCRIPTION}
        </span>
      )}
    </>
  )
}
