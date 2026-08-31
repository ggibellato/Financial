import { useState } from 'react'
import {
  Button,
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Switch,
} from '@fluentui/react-components'
import type { ReserveBucketDto } from '../api/types'
import { useFormPanelStyles } from './formPanelStyles'
import { getErrorMessage } from '../utils/formatters'

interface ReserveBucketFormDialogProps {
  reserveBucket: ReserveBucketDto | null
  onCancel: () => void
  onSubmit: (name: string, splitPercentage: number, isActive: boolean) => Promise<ReserveBucketDto>
}

export default function ReserveBucketFormDialog({ reserveBucket, onCancel, onSubmit }: ReserveBucketFormDialogProps) {
  const styles = useFormPanelStyles()
  const isEditing = reserveBucket !== null
  const [name, setName] = useState(reserveBucket?.name ?? '')
  const [splitPercentage, setSplitPercentage] = useState(String(reserveBucket?.splitPercentage ?? ''))
  const [isActive, setIsActive] = useState(reserveBucket?.isActive ?? true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const trimmedName = name.trim()
  const parsedSplit = Number(splitPercentage)
  const nameError = trimmedName.length === 0 ? 'Name is required.' : ''
  const splitError =
    splitPercentage.trim().length === 0 || Number.isNaN(parsedSplit) || parsedSplit < 0 || parsedSplit > 100
      ? 'Split percentage must be between 0 and 100.'
      : ''

  const canSubmit = !nameError && !splitError && !isSaving

  const handleSubmit = async () => {
    if (!canSubmit) return

    setIsSaving(true)
    setError(null)
    try {
      await onSubmit(trimmedName, parsedSplit, isActive)
      onCancel()
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'The reserve bucket could not be saved.'))
      setIsSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open) onCancel() }}>
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>{isEditing ? 'Edit Reserve Bucket' : 'Create Reserve Bucket'}</DialogTitle>
          <DialogContent>
            <Field
              label="Name"
              required
              validationState={nameError ? 'error' : 'none'}
              validationMessage={nameError}
            >
              <Input value={name} onChange={(e) => setName(e.target.value)} disabled={isSaving} autoFocus />
            </Field>

            <Field
              label="Split Percentage"
              required
              validationState={splitError ? 'error' : 'none'}
              validationMessage={splitError}
            >
              <Input
                type="number"
                value={splitPercentage}
                onChange={(e) => setSplitPercentage(e.target.value)}
                disabled={isSaving}
              />
            </Field>

            <Field label="Active">
              <Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} disabled={isSaving} />
            </Field>

            {error && (
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            )}
          </DialogContent>
          <div className={styles.actions}>
            <Button appearance="primary" onClick={() => void handleSubmit()} disabled={!canSubmit}>
              Save
            </Button>
            <Button appearance="secondary" onClick={onCancel} disabled={isSaving}>
              Cancel
            </Button>
          </div>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
