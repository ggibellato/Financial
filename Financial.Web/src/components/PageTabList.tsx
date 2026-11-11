import { makeStyles, mergeClasses, TabList, tokens, type TabListProps } from '@fluentui/react-components'

const useStyles = makeStyles({
  root: {
    flexShrink: 0,
    flexWrap: 'wrap',
    rowGap: tokens.spacingVerticalXS,
  },
})

type PageTabListProps = TabListProps & { 'aria-label': string }

export default function PageTabList({ className, ...props }: PageTabListProps) {
  const styles = useStyles()
  return <TabList {...props} className={mergeClasses(styles.root, className)} />
}
