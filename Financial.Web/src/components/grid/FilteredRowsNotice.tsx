import type { RefObject } from 'react'
import { Button, makeStyles, tokens } from '@fluentui/react-components'
import { useMediaQuery } from '../../hooks/useMediaQuery'
import type { FilterSummary } from '../../hooks/useColumnFilters'
import { MOBILE_MEDIA_QUERY } from '../../styles/breakpoints'

const useStyles = makeStyles({
  notice: {
    display: 'flex',
    flexShrink: 0,
    flexWrap: 'wrap',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  visuallyHidden: {
    position: 'absolute',
    width: '1px',
    height: '1px',
    overflow: 'hidden',
    clip: 'rect(0 0 0 0)',
    whiteSpace: 'nowrap',
  },
})

interface FilteredRowsNoticeProps extends FilterSummary {
  tableRef: RefObject<HTMLTableElement | null>
}

export default function FilteredRowsNotice({ isFiltered, shownCount, totalCount, onClearFilters, tableRef }: FilteredRowsNoticeProps) {
  const styles = useStyles()
  const isStackedLayout = useMediaQuery(MOBILE_MEDIA_QUERY)
  const isShown = isStackedLayout && isFiltered

  const clearFilters = () => {
    onClearFilters()
    tableRef.current?.focus()
  }

  return (
    <div className={isShown ? styles.notice : undefined}>
      <span role="status" className={isStackedLayout ? undefined : styles.visuallyHidden}>
        {isFiltered && `Filtered: ${shownCount} of ${totalCount} ${totalCount === 1 ? 'row' : 'rows'}`}
      </span>
      {isShown && (
        <Button appearance="primary" onClick={clearFilters}>
          Clear filters
        </Button>
      )}
    </div>
  )
}
