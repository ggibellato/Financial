import type { ReactNode } from 'react'
import { Button, Popover, PopoverSurface, PopoverTrigger } from '@fluentui/react-components'
import { FilterFilled, FilterRegular } from '@fluentui/react-icons'
import { useMediaQuery } from '../../hooks/useMediaQuery'
import { MOBILE_MEDIA_QUERY } from '../../styles/breakpoints'
import { filterTriggerLabel } from './columnFilterText'
import './ColumnFilterMenu.css'

interface ColumnFilterPopoverProps {
  label: string
  isFiltered: boolean
  children: ReactNode
}

export default function ColumnFilterPopover({ label, isFiltered, children }: ColumnFilterPopoverProps) {
  const isStackedLayout = useMediaQuery(MOBILE_MEDIA_QUERY)

  return (
    <Popover positioning="below-end">
      <PopoverTrigger disableButtonEnhancement>
        <Button
          appearance="subtle"
          size="small"
          className={`column-filter-menu__trigger${isFiltered ? ' column-filter-menu__trigger--active' : ''}`}
          icon={isFiltered ? <FilterFilled /> : <FilterRegular />}
          aria-label={filterTriggerLabel(label)}
          tabIndex={isStackedLayout ? -1 : undefined}
        />
      </PopoverTrigger>
      <PopoverSurface className="column-filter-menu__surface">{children}</PopoverSurface>
    </Popover>
  )
}
