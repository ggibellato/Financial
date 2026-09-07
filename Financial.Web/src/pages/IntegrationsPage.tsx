import { useState } from 'react'
import {
  Button,
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Link,
  MessageBar,
  MessageBarBody,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
} from '@fluentui/react-components'
import CalendarSyncStatusBadge from '../components/CalendarSyncStatusBadge'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { useCalendarConnection } from '../hooks/useCalendarConnection'
import { useCalendarSyncStatuses } from '../hooks/useCalendarSyncStatuses'
import { formatDateTime, formatShortDateUtc } from '../utils/formatters'
import './IntegrationsPage.css'

function buildCalendarLink(calendarId: string): string {
  return `https://calendar.google.com/calendar/u/0/r?cid=${encodeURIComponent(calendarId)}`
}

export default function IntegrationsPage() {
  const panelStyles = useFormPanelStyles()
  const connection = useCalendarConnection()
  const syncStatuses = useCalendarSyncStatuses()
  const [confirmingDisconnect, setConfirmingDisconnect] = useState(false)

  const handleConfirmDisconnect = () => {
    setConfirmingDisconnect(false)
    connection.disconnect().catch(() => {
      // The failure is already surfaced via connection.disconnectError.
    })
  }

  return (
    <section className="integrations-page">
      <header className="integrations-page__header">
        <h2>Integrations</h2>
      </header>

      <div className={panelStyles.panel}>
        <h3>Google Calendar</h3>

        {connection.isLoading ? (
          <LoadingState message="Loading connection status…" />
        ) : connection.error ? (
          <ErrorState message={connection.error} onRetry={connection.retry} />
        ) : !connection.status?.connected ? (
          <div className="integrations-page__not-connected">
            <p>
              {connection.status?.disconnectReason === 'token_revoked'
                ? 'Connection lost — please reconnect.'
                : 'Connect your Google Calendar to get due-date reminders outside the app.'}
            </p>
            <Button appearance="primary" onClick={connection.connect} disabled={connection.isConnecting}>
              {connection.isConnecting ? 'Connecting…' : 'Connect Google Calendar'}
            </Button>
          </div>
        ) : (
          <div className="integrations-page__connected">
            <dl className="integrations-page__details">
              <div className="integrations-page__detail-row">
                <dt>Account</dt>
                <dd>{connection.status.accountEmail}</dd>
              </div>
              <div className="integrations-page__detail-row">
                <dt>Calendar</dt>
                <dd>
                  {connection.status.calendarId ? (
                    <Link href={buildCalendarLink(connection.status.calendarId)} target="_blank" rel="noopener noreferrer">
                      {connection.status.calendarName}
                    </Link>
                  ) : (
                    connection.status.calendarName
                  )}
                </dd>
              </div>
              <div className="integrations-page__detail-row">
                <dt>Connected since</dt>
                <dd>{formatDateTime(connection.status.connectedAtUtc)}</dd>
              </div>
            </dl>

            {connection.disconnectError && (
              <MessageBar intent="error">
                <MessageBarBody>{connection.disconnectError}</MessageBarBody>
              </MessageBar>
            )}

            <div className={panelStyles.actions}>
              <Button appearance="primary" disabled={connection.isDisconnecting} onClick={() => setConfirmingDisconnect(true)}>
                Disconnect
              </Button>
            </div>

            {syncStatuses.isLoading ? (
              <LoadingState message="Loading sync status…" />
            ) : syncStatuses.error ? (
              <ErrorState message={syncStatuses.error} onRetry={syncStatuses.retry} />
            ) : syncStatuses.rows.length === 0 ? (
              <p className="integrations-page__empty">No active credit cards with a due date yet.</p>
            ) : (
              <Table aria-label="Credit card calendar sync status">
                <TableHeader>
                  <TableRow>
                    <TableHeaderCell>Card</TableHeaderCell>
                    <TableHeaderCell>Due Date</TableHeaderCell>
                    <TableHeaderCell>Sync Status</TableHeaderCell>
                    <TableHeaderCell className="data-table__col--action" />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {syncStatuses.rows.map((row) => (
                    <TableRow key={row.creditCardId}>
                      <TableCell>{row.name}</TableCell>
                      <TableCell>{formatShortDateUtc(row.dueDate)}</TableCell>
                      <TableCell>
                        <CalendarSyncStatusBadge state={row.state} lastError={row.lastError} />
                      </TableCell>
                      <TableCell className="data-table__col--action">
                        {row.state === 'Error' && (
                          <Button
                            appearance="subtle"
                            size="small"
                            aria-label={`Retry calendar sync for ${row.name}`}
                            disabled={syncStatuses.retryingCardId === row.creditCardId}
                            onClick={() => syncStatuses.resyncCard(row.creditCardId)}
                          >
                            Retry
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </div>
        )}
      </div>

      {confirmingDisconnect && (
        <Dialog open onOpenChange={(_, data) => { if (!data.open) setConfirmingDisconnect(false) }}>
          <DialogSurface aria-describedby={undefined}>
            <DialogBody>
              <DialogTitle>Disconnect Google Calendar</DialogTitle>
              <DialogContent>
                <p>
                  This will delete the &ldquo;{connection.status?.calendarName ?? 'Financial - Credit Card Due Dates'}&rdquo; calendar
                  and all its events from Google Calendar. This cannot be undone.
                </p>
              </DialogContent>
              <div className={panelStyles.actions}>
                <Button appearance="primary" onClick={handleConfirmDisconnect}>
                  Disconnect
                </Button>
                <Button appearance="secondary" onClick={() => setConfirmingDisconnect(false)}>
                  Cancel
                </Button>
              </div>
            </DialogBody>
          </DialogSurface>
        </Dialog>
      )}
    </section>
  )
}
