import { useState } from 'react'
import {
  Button,
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  MessageBar,
  MessageBarBody,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
} from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import ReserveBucketFormDialog from '../components/ReserveBucketFormDialog'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { useReserveBuckets } from '../hooks/useReserveBuckets'
import type { ReserveBucketDto } from '../api/types'
import { formatN2 } from '../utils/formatters'
import './ReserveBucketsPage.css'

export default function ReserveBucketsPage() {
  const styles = useFormPanelStyles()
  const {
    reserveBuckets,
    isLoading,
    error,
    retry,
    createReserveBucket,
    updateReserveBucket,
    savingId,
    saveError,
    deactivateReserveBucket,
    activeSplitWarning,
  } = useReserveBuckets()
  const [editingBucket, setEditingBucket] = useState<ReserveBucketDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [confirmingDeactivate, setConfirmingDeactivate] = useState<ReserveBucketDto | null>(null)

  const handleSubmit = (name: string, splitPercentage: number, isActive: boolean) =>
    editingBucket
      ? updateReserveBucket(editingBucket.id, { name, splitPercentage, isActive })
      : createReserveBucket({ name, splitPercentage, isActive })

  const closeFormDialog = () => {
    setEditingBucket(null)
    setIsCreating(false)
  }

  const handleConfirmDeactivate = () => {
    if (!confirmingDeactivate) return
    deactivateReserveBucket(confirmingDeactivate)
    setConfirmingDeactivate(null)
  }

  return (
    <section className="reserve-buckets-page">
      <header className="reserve-buckets-page__header">
        <h2>Reserve Buckets</h2>
        <Button appearance="primary" icon={<AddRegular />} onClick={() => setIsCreating(true)}>
          Create Reserve Bucket
        </Button>
      </header>

      {activeSplitWarning && (
        <MessageBar intent="warning">
          <MessageBarBody>{activeSplitWarning}</MessageBarBody>
        </MessageBar>
      )}

      {saveError && (
        <MessageBar intent="error">
          <MessageBarBody>{saveError}</MessageBarBody>
        </MessageBar>
      )}

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : reserveBuckets.length === 0 ? (
        <p className="reserve-buckets-page__empty">No reserve buckets yet — create one to get started.</p>
      ) : (
        <Table aria-label="Reserve Buckets">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Name</TableHeaderCell>
              <TableHeaderCell>Split Percentage</TableHeaderCell>
              <TableHeaderCell>Active</TableHeaderCell>
              <TableHeaderCell className="data-table__col--action" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {reserveBuckets.map((bucket) => (
              <TableRow key={bucket.id}>
                <TableCell>{bucket.name}</TableCell>
                <TableCell className="data-table__col--numeric">{formatN2(bucket.splitPercentage)}</TableCell>
                <TableCell>{bucket.isActive ? 'Yes' : 'No'}</TableCell>
                <TableCell className="data-table__col--action">
                  <div className="data-table__actions-cell">
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<EditRegular />}
                      aria-label={`Edit ${bucket.name}`}
                      onClick={() => setEditingBucket(bucket)}
                    />
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<DeleteRegular />}
                      aria-label={`Delete ${bucket.name}`}
                      disabled={!bucket.isActive || savingId === bucket.id}
                      onClick={() => setConfirmingDeactivate(bucket)}
                    />
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {(isCreating || editingBucket) && (
        <ReserveBucketFormDialog reserveBucket={editingBucket} onCancel={closeFormDialog} onSubmit={handleSubmit} />
      )}

      {confirmingDeactivate && (
        <Dialog open onOpenChange={(_, data) => { if (!data.open) setConfirmingDeactivate(null) }}>
          <DialogSurface aria-describedby={undefined}>
            <DialogBody>
              <DialogTitle>Delete Reserve Bucket</DialogTitle>
              <DialogContent>
                <p>
                  &ldquo;{confirmingDeactivate.name}&rdquo; will be deactivated, not removed. Existing reserve
                  movements linked to it remain valid.
                </p>
              </DialogContent>
              <div className={styles.actions}>
                <Button appearance="primary" onClick={handleConfirmDeactivate}>
                  Delete
                </Button>
                <Button appearance="secondary" onClick={() => setConfirmingDeactivate(null)}>
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
