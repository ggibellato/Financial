import {
  Button,
  Checkbox,
  Input,
  ProgressBar,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
  Text,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import { useFormPanelStyles } from './formPanelStyles'
import type { SuggestedValuesPhase, SuggestionRow } from '../hooks/useSuggestedValues'
import type { InvestmentSnapshotSuggestionSkippedDto } from '../api/types'
import { formatN2 } from '../utils/formatters'

const useStyles = makeStyles({
  mutedValue: {
    color: tokens.colorNeutralForeground3,
    textDecorationLine: 'line-through',
    marginRight: tokens.spacingHorizontalXS,
  },
})

export interface ApplyProgress {
  current: number
  total: number
  accountName: string
}

interface SuggestedValuesPanelProps {
  phase: SuggestedValuesPhase
  fetchError: string | null
  rows: SuggestionRow[]
  notUpdated: InvestmentSnapshotSuggestionSkippedDto[]
  checkedCount: number
  applyProgress: ApplyProgress | null
  succeededCount: number
  failedRows: SuggestionRow[]
  onToggleIncluded: (accountId: string) => void
  onSetValue: (accountId: string, value: string) => void
  onApply: () => void
  onRetryFailed: () => void
  onRetryFetch: () => void
  onClose: () => void
}

function NotUpdatedList({ notUpdated }: { notUpdated: InvestmentSnapshotSuggestionSkippedDto[] }) {
  if (notUpdated.length === 0) return null

  return (
    <div>
      <Text weight="semibold">Not updated</Text>
      <Table size="small">
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Account</TableHeaderCell>
            <TableHeaderCell>Reason</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {notUpdated.map((item) => (
            <TableRow key={item.accountId}>
              <TableCell>{item.accountName}</TableCell>
              <TableCell>{item.reason}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}

export default function SuggestedValuesPanel({
  phase,
  fetchError,
  rows,
  notUpdated,
  checkedCount,
  applyProgress,
  succeededCount,
  failedRows,
  onToggleIncluded,
  onSetValue,
  onApply,
  onRetryFailed,
  onRetryFetch,
  onClose,
}: SuggestedValuesPanelProps) {
  const formStyles = useFormPanelStyles()
  const styles = useStyles()
  const isApplying = phase === 'applying'
  const attemptedCount = rows.filter((r) => r.status !== 'pending').length

  return (
    <div className={formStyles.panel}>
      <Text as="h2" weight="semibold" size={400}>
        Suggest Values
      </Text>

      {phase === 'loading' && <LoadingState message="Loading suggestions..." />}

      {phase === 'error' && <ErrorState message={fetchError ?? "Couldn't load suggestions."} onRetry={onRetryFetch} />}

      {(phase === 'ready' || phase === 'applying' || phase === 'completed') && (
        <>
          {rows.length === 0 ? (
            <p>No suggestions available for this month.</p>
          ) : (
            <Table aria-label="Suggested Values">
              <TableHeader>
                <TableRow>
                  <TableHeaderCell>Account</TableHeaderCell>
                  <TableHeaderCell className="data-table__col--numeric">Current Value</TableHeaderCell>
                  <TableHeaderCell className="data-table__col--numeric">Suggested Value</TableHeaderCell>
                  <TableHeaderCell>Source</TableHeaderCell>
                  <TableHeaderCell>Include</TableHeaderCell>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rows.map((row) => (
                  <TableRow key={row.accountId}>
                    <TableCell>{row.accountName}</TableCell>
                    <TableCell className="data-table__col--numeric">
                      {row.included && row.currentValue !== 0 ? (
                        <span className={styles.mutedValue}>{formatN2(row.currentValue)}</span>
                      ) : (
                        formatN2(row.currentValue)
                      )}
                    </TableCell>
                    <TableCell className="data-table__col--numeric">
                      <Input
                        type="number"
                        step="0.01"
                        min="0"
                        aria-label={`Suggested value for ${row.accountName}`}
                        value={row.suggestedValue}
                        disabled={isApplying}
                        onChange={(e) => onSetValue(row.accountId, e.target.value)}
                      />
                    </TableCell>
                    <TableCell>{row.sourceDescription}</TableCell>
                    <TableCell>
                      <Checkbox
                        aria-label={`Include ${row.accountName}`}
                        checked={row.included}
                        disabled={isApplying}
                        onChange={() => onToggleIncluded(row.accountId)}
                      />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}

          <NotUpdatedList notUpdated={notUpdated} />

          {isApplying && applyProgress && (
            <div>
              <ProgressBar value={applyProgress.current / applyProgress.total} />
              <Text size={200}>
                Applying {applyProgress.current} of {applyProgress.total}: {applyProgress.accountName}...
              </Text>
            </div>
          )}

          {phase === 'completed' && (
            <Text>
              Applied {succeededCount} of {attemptedCount}.
              {failedRows.length > 0 &&
                ` ${failedRows.length} failed: ${failedRows.map((r) => r.accountName).join(', ')} — try again.`}
            </Text>
          )}

          <div className={formStyles.actions}>
            {phase === 'ready' && rows.length > 0 && (
              <Button appearance="primary" disabled={checkedCount === 0} onClick={onApply}>
                Apply {checkedCount} Suggestions
              </Button>
            )}
            {phase === 'completed' && failedRows.length > 0 && (
              <Button appearance="primary" onClick={onRetryFailed}>
                Retry Failed
              </Button>
            )}
            <Button appearance="secondary" disabled={isApplying} onClick={onClose}>
              {phase === 'completed' ? 'Close' : 'Cancel'}
            </Button>
          </div>
        </>
      )}

      {phase === 'error' && (
        <div className={formStyles.actions}>
          <Button appearance="secondary" onClick={onClose}>
            Cancel
          </Button>
        </div>
      )}
    </div>
  )
}
