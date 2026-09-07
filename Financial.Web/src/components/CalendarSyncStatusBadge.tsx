import { Badge, Spinner } from '@fluentui/react-components'
import { CheckmarkCircleRegular, ErrorCircleRegular } from '@fluentui/react-icons'

interface CalendarSyncStatusBadgeProps {
  /** 'Synced' | 'Error' | 'Pending' (or absent - a card never yet synced this process lifetime). */
  state: string | undefined
  lastError?: string | null
}

export default function CalendarSyncStatusBadge({ state, lastError }: CalendarSyncStatusBadgeProps) {
  if (state === 'Synced') {
    return (
      <Badge appearance="filled" color="success" icon={<CheckmarkCircleRegular />}>
        Synced
      </Badge>
    )
  }

  if (state === 'Error') {
    return (
      <Badge appearance="filled" color="danger" icon={<ErrorCircleRegular />}>
        {`Sync failed: ${lastError ?? 'Unknown error'}`}
      </Badge>
    )
  }

  // 'Pending' or an absent status (never synced yet) both read as "still syncing".
  return <Spinner size="tiny" label="Syncing…" labelPosition="after" />
}
