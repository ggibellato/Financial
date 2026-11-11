import { makeStyles, Text, tokens } from '@fluentui/react-components'

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
})

interface LockedValueProps {
  label: string
  value: string
}

export default function LockedValue({ label, value }: LockedValueProps) {
  const styles = useStyles()

  return (
    <div className={styles.root}>
      <Text>{label}</Text>
      <Text weight="semibold">{value}</Text>
    </div>
  )
}
