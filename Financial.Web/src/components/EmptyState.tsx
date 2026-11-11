import type { ReactNode } from 'react'
import { makeStyles, mergeClasses, tokens } from '@fluentui/react-components'

const useStyles = makeStyles({
  root: {
    color: tokens.colorNeutralForeground2,
  },
})

interface EmptyStateProps {
  children: ReactNode
  className?: string
}

export default function EmptyState({ children, className }: EmptyStateProps) {
  const styles = useStyles()
  return <p className={mergeClasses(styles.root, className)}>{children}</p>
}
