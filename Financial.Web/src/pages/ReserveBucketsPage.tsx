import { useState } from 'react'
import {
  MessageBar,
  MessageBarBody,
  Table,
  TableBody,
  TableHeader,
  TableHeaderCell,
  TableRow,
} from '@fluentui/react-components'
import AdminEntityPage from '../components/AdminEntityPage'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import ReserveBucketFormDialog from '../components/ReserveBucketFormDialog'
import DataTableCell from '../components/grid/DataTableCell'
import RowActionsCell from '../components/grid/RowActionsCell'
import { useReserveBuckets } from '../hooks/useReserveBuckets'
import type { ReserveBucketDto } from '../api/types'
import { formatN2 } from '../utils/formatters'

export default function ReserveBucketsPage() {
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
    <AdminEntityPage
      title="Reserve Buckets"
      createLabel="Create Reserve Bucket"
      onCreate={() => setIsCreating(true)}
      belowHeader={
        activeSplitWarning && (
          <MessageBar intent="warning">
            <MessageBarBody>{activeSplitWarning}</MessageBarBody>
          </MessageBar>
        )
      }
      actionError={saveError}
      isLoading={isLoading}
      loadError={error}
      onRetry={retry}
      isEmpty={reserveBuckets.length === 0}
      emptyMessage="No reserve buckets yet — create one to get started."
      dialogs={
        <>
          {(isCreating || editingBucket) && (
            <ReserveBucketFormDialog reserveBucket={editingBucket} onCancel={closeFormDialog} onSubmit={handleSubmit} />
          )}
          {confirmingDeactivate && (
            <ConfirmDeleteDialog
              entityLabel="Reserve Bucket"
              message={
                <>
                  &ldquo;{confirmingDeactivate.name}&rdquo; will be deactivated, not removed. Existing reserve
                  movements linked to it remain valid.
                </>
              }
              onConfirm={handleConfirmDeactivate}
              onCancel={() => setConfirmingDeactivate(null)}
            />
          )}
        </>
      }
    >
      <Table aria-label="Reserve Buckets" className="data-table">
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
              <DataTableCell label="Name">{bucket.name}</DataTableCell>
              <DataTableCell label="Split Percentage" className="data-table__col--numeric">
                {formatN2(bucket.splitPercentage)}
              </DataTableCell>
              <DataTableCell label="Active">{bucket.isActive ? 'Yes' : 'No'}</DataTableCell>
              <RowActionsCell
                itemName={bucket.name}
                deleteDisabled={!bucket.isActive || savingId === bucket.id}
                onEdit={() => setEditingBucket(bucket)}
                onDelete={() => setConfirmingDeactivate(bucket)}
              />
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </AdminEntityPage>
  )
}
