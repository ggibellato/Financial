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
import type { CategoryDto } from '../api/types'
import { useFormPanelStyles } from './formPanelStyles'
import { getErrorMessage } from '../utils/formatters'

interface CategoryFormDialogProps {
  category: CategoryDto | null
  onCancel: () => void
  onSubmit: (name: string, active: boolean, isInvestment: boolean, isTithe: boolean) => Promise<unknown>
}

export default function CategoryFormDialog({ category, onCancel, onSubmit }: CategoryFormDialogProps) {
  const styles = useFormPanelStyles()
  const isEditing = category !== null
  const [name, setName] = useState(category?.name ?? '')
  const [active, setActive] = useState(category?.active ?? true)
  const [isInvestment, setIsInvestment] = useState(category?.isInvestment ?? false)
  const [isTithe, setIsTithe] = useState(category?.isTithe ?? false)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const trimmedName = name.trim()
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const canSubmit = validationMessage.length === 0 && !isSaving

  const handleSubmit = async () => {
    if (!canSubmit) return

    setIsSaving(true)
    setError(null)
    try {
      await onSubmit(trimmedName, active, isInvestment, isTithe)
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'The category could not be saved.'))
      setIsSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open) onCancel() }}>
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>{isEditing ? 'Edit Category' : 'Create Category'}</DialogTitle>
          <DialogContent>
            <Field
              label="Name"
              required
              validationState={validationMessage ? 'error' : 'none'}
              validationMessage={validationMessage}
            >
              <Input value={name} onChange={(e) => setName(e.target.value)} disabled={isSaving} autoFocus />
            </Field>

            <Field label="Active">
              <Switch checked={active} onChange={(e) => setActive(e.target.checked)} disabled={isSaving} />
            </Field>

            <Field label="Investment">
              <Switch
                checked={isInvestment}
                onChange={(e) => setIsInvestment(e.target.checked)}
                disabled={isSaving}
              />
            </Field>

            <Field label="Tithe">
              <Switch checked={isTithe} onChange={(e) => setIsTithe(e.target.checked)} disabled={isSaving} />
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
