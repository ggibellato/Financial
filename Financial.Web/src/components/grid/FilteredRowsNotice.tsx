import { useState, type RefObject } from 'react'
import { Button, makeStyles, tokens } from '@fluentui/react-components'
import { useMediaQuery } from '../../hooks/useMediaQuery'
import type { FilterSummary } from '../../hooks/useColumnFilters'
import { MOBILE_MEDIA_QUERY } from '../../styles/breakpoints'
import { useVisuallyHiddenStyles } from '../../styles/visuallyHidden'

const useStyles = makeStyles({
  notice: {
    display: 'flex',
    flexShrink: 0,
    flexWrap: 'wrap',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
})

function rowsNoun(count: number) {
  return count === 1 ? 'row' : 'rows'
}

interface FilteredRowsNoticeProps extends FilterSummary {
  tableRef: RefObject<HTMLTableElement | null>
}

export default function FilteredRowsNotice({ isFiltered, shownCount, totalCount, onClearFilters, tableRef }: FilteredRowsNoticeProps) {
  const styles = useStyles()
  const hidden = useVisuallyHiddenStyles()
  const isStackedLayout = useMediaQuery(MOBILE_MEDIA_QUERY)
  const isShown = isStackedLayout && isFiltered
  const [lastChange, setLastChange] = useState<{ isFiltered: boolean; clearedCount: number | null }>({ isFiltered, clearedCount: null })
  if (lastChange.isFiltered !== isFiltered) {
    setLastChange({ isFiltered, clearedCount: isFiltered ? null : totalCount })
  }
  const message = isFiltered
    ? `Filtered: ${shownCount} of ${totalCount} ${rowsNoun(totalCount)}`
    : lastChange.clearedCount !== null && `Filters cleared: showing all ${lastChange.clearedCount} ${rowsNoun(lastChange.clearedCount)}`

  const clearFilters = () => {
    onClearFilters()
    tableRef.current?.focus()
  }

  return (
    <div className={isShown ? styles.notice : undefined}>
      <span role="status" className={isShown ? undefined : hidden.visuallyHidden}>
        {message}
      </span>
      {isShown && (
        <Button appearance="primary" onClick={clearFilters}>
          Clear filters
        </Button>
      )}
    </div>
  )
}
