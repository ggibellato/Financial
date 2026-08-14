import { useSyncStatus } from '../hooks/useSyncStatus'
import { formatDateTime } from '../utils/formatters'
import type { SyncStatusDto } from '../api/types'
import './SyncStatusBanner.css'

interface FailedContext {
  label: string
  lastError: string | null
  lastSuccessfulSaveUtc: string | null
}

function toFailedContext(label: string, status: SyncStatusDto): FailedContext | null {
  if (status.state !== 'Failed') return null
  return { label, lastError: status.lastError, lastSuccessfulSaveUtc: status.lastSuccessfulSaveUtc }
}

export default function SyncStatusBanner() {
  const { status } = useSyncStatus()

  if (!status) return null

  const failedContexts = [toFailedContext('CashFlow', status.cashFlow), toFailedContext('Investment', status.investment)].filter(
    (context): context is FailedContext => context !== null,
  )

  if (failedContexts.length === 0) return null

  return (
    <div className="sync-status-banner" role="alert">
      {failedContexts.map((context) => (
        <p key={context.label} className="sync-status-banner__message">
          {context.label} changes could not be saved to Google Drive (last error: {context.lastError}). Last
          successful save: {context.lastSuccessfulSaveUtc ? formatDateTime(context.lastSuccessfulSaveUtc) : 'Never'}.
        </p>
      ))}
    </div>
  )
}
