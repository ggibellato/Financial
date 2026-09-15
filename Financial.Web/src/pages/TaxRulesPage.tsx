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
import TaxRuleFormDialog from '../components/TaxRuleFormDialog'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { useTaxRules } from '../hooks/useTaxRules'
import type { TaxRuleDto } from '../api/types'
import './TaxRulesPage.css'

export default function TaxRulesPage() {
  const styles = useFormPanelStyles()
  const {
    taxRules,
    isLoading,
    error,
    retry,
    createTaxRule,
    updateTaxRule,
    deletingId,
    deleteError,
    deleteTaxRule,
  } = useTaxRules()
  const [editingTaxRule, setEditingTaxRule] = useState<TaxRuleDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState<TaxRuleDto | null>(null)

  const handleSubmit = (
    jurisdiction: string,
    eventCategory: string,
    label: string,
    description: string,
    effectiveFrom: string,
    effectiveTo: string | null,
  ) =>
    editingTaxRule
      ? updateTaxRule(editingTaxRule.id, { label, description, effectiveFrom, effectiveTo })
      : createTaxRule({
          jurisdiction: jurisdiction as TaxRuleDto['jurisdiction'],
          eventCategory: eventCategory as TaxRuleDto['eventCategory'],
          label,
          description,
          effectiveFrom,
          effectiveTo,
        })

  const closeFormDialog = () => {
    setEditingTaxRule(null)
    setIsCreating(false)
  }

  const handleConfirmDelete = () => {
    if (!confirmingDelete) return
    deleteTaxRule(confirmingDelete)
    setConfirmingDelete(null)
  }

  return (
    <section className="tax-rules-page">
      <header className="tax-rules-page__header">
        <h2>Tax Rules</h2>
        <Button appearance="primary" icon={<AddRegular />} onClick={() => setIsCreating(true)}>
          Create Tax Rule
        </Button>
      </header>

      {deleteError && (
        <MessageBar intent="error">
          <MessageBarBody>{deleteError}</MessageBarBody>
        </MessageBar>
      )}

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : taxRules.length === 0 ? (
        <p className="tax-rules-page__empty">No tax rules configured yet — create one to get started.</p>
      ) : (
        <Table aria-label="Tax Rules">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Jurisdiction</TableHeaderCell>
              <TableHeaderCell>Event Category</TableHeaderCell>
              <TableHeaderCell>Label</TableHeaderCell>
              <TableHeaderCell>Effective From</TableHeaderCell>
              <TableHeaderCell>Effective To</TableHeaderCell>
              <TableHeaderCell className="data-table__col--action" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {taxRules.map((rule) => (
              <TableRow key={rule.id}>
                <TableCell>{rule.jurisdiction}</TableCell>
                <TableCell>{rule.eventCategory}</TableCell>
                <TableCell>{rule.label}</TableCell>
                <TableCell>{rule.effectiveFrom}</TableCell>
                <TableCell>{rule.effectiveTo ?? '—'}</TableCell>
                <TableCell className="data-table__col--action">
                  <div className="data-table__actions-cell">
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<EditRegular />}
                      aria-label={`Edit ${rule.label}`}
                      onClick={() => setEditingTaxRule(rule)}
                    />
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<DeleteRegular />}
                      aria-label={`Delete ${rule.label}`}
                      disabled={deletingId === rule.id}
                      onClick={() => setConfirmingDelete(rule)}
                    />
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {(isCreating || editingTaxRule) && (
        <TaxRuleFormDialog taxRule={editingTaxRule} onCancel={closeFormDialog} onSubmit={handleSubmit} />
      )}

      {confirmingDelete && (
        <Dialog open onOpenChange={(_, data) => { if (!data.open) setConfirmingDelete(null) }}>
          <DialogSurface aria-describedby={undefined}>
            <DialogBody>
              <DialogTitle>Delete Tax Rule</DialogTitle>
              <DialogContent>
                <p>&ldquo;{confirmingDelete.label}&rdquo; will be permanently deleted.</p>
              </DialogContent>
              <div className={styles.actions}>
                <Button appearance="primary" onClick={handleConfirmDelete}>
                  Delete
                </Button>
                <Button appearance="secondary" onClick={() => setConfirmingDelete(null)}>
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
