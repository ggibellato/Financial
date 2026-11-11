import { useState } from 'react'
import { Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import AdminEntityPage from '../components/AdminEntityPage'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import TaxRuleFormDialog from '../components/TaxRuleFormDialog'
import TruncatedText from '../components/TruncatedText'
import DataTableCell from '../components/grid/DataTableCell'
import RowActionsCell from '../components/grid/RowActionsCell'
import { rowDeleteState } from '../components/grid/rowDeleteState'
import { useTaxRules } from '../hooks/useTaxRules'
import type { TaxRuleDto } from '../api/types'
import ActionsColumnHeader from '../components/grid/ActionsColumnHeader'

export default function TaxRulesPage() {
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
    <AdminEntityPage
      title="Tax rules"
      createLabel="New tax rule"
      onCreate={() => setIsCreating(true)}
      actionError={deleteError}
      isLoading={isLoading}
      loadError={error}
      onRetry={retry}
      isEmpty={taxRules.length === 0}
      emptyMessage="No tax rules configured yet — create one to get started."
      dialogs={
        <>
          {(isCreating || editingTaxRule) && (
            <TaxRuleFormDialog taxRule={editingTaxRule} onCancel={closeFormDialog} onSubmit={handleSubmit} />
          )}
          {confirmingDelete && (
            <ConfirmDeleteDialog
              entityLabel="tax rule"
              message={<>&ldquo;{confirmingDelete.label}&rdquo; will be permanently deleted.</>}
              onConfirm={handleConfirmDelete}
              onCancel={() => setConfirmingDelete(null)}
            />
          )}
        </>
      }
    >
      <Table aria-label="Tax rules" className="data-table">
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Jurisdiction</TableHeaderCell>
            <TableHeaderCell>Event category</TableHeaderCell>
            <TableHeaderCell>Label</TableHeaderCell>
            <TableHeaderCell>Effective from</TableHeaderCell>
            <TableHeaderCell>Effective to</TableHeaderCell>
            <ActionsColumnHeader />
          </TableRow>
        </TableHeader>
        <TableBody>
          {taxRules.map((rule) => (
            <TableRow key={rule.id}>
              <DataTableCell label="Jurisdiction">{rule.jurisdiction}</DataTableCell>
              <DataTableCell label="Event category">{rule.eventCategory}</DataTableCell>
              <DataTableCell label="Label">
                <TruncatedText text={rule.label} />
              </DataTableCell>
              <DataTableCell label="Effective from">{rule.effectiveFrom}</DataTableCell>
              <DataTableCell label="Effective to">{rule.effectiveTo ?? '—'}</DataTableCell>
              <RowActionsCell
                itemName={rule.label}
                {...rowDeleteState(deletingId, rule.id)}
                onEdit={() => setEditingTaxRule(rule)}
                onDelete={() => setConfirmingDelete(rule)}
              />
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </AdminEntityPage>
  )
}
