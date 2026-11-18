import { Button, Text, makeStyles, tokens } from '@fluentui/react-components'
import { useMediaQuery } from '../../hooks/useMediaQuery'
import type { FilterSummary } from '../../hooks/useColumnFilters'
import { MOBILE_MEDIA_QUERY } from '../../styles/breakpoints'
import { filteredRowsMessage } from './columnFilterText'

const useStyles = makeStyles({
  toolbar: {
    display: 'flex',
    flexShrink: 0,
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-start',
    marginBottom: tokens.spacingVerticalM,
  },
})

interface ClearFiltersToolbarProps {
  isFiltered: boolean
  onClearFilters: () => void
  rowCount?: Pick<FilterSummary, 'shownCount' | 'totalCount'>
}

export default function ClearFiltersToolbar({ isFiltered, onClearFilters, rowCount }: ClearFiltersToolbarProps) {
  const styles = useStyles()
  const isStackedLayout = useMediaQuery(MOBILE_MEDIA_QUERY)
  if (!isFiltered || isStackedLayout) return null

  return (
    <div className={styles.toolbar}>
      <Button appearance="primary" onClick={onClearFilters}>
        Clear filters
      </Button>
      {rowCount && <Text>{filteredRowsMessage(rowCount.shownCount, rowCount.totalCount)}</Text>}
    </div>
  )
}
