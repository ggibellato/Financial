import { useState } from 'react'
import { Switch, Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import AdminEntityPage from '../components/AdminEntityPage'
import CategoryFormDialog from '../components/CategoryFormDialog'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import DataTableCell from '../components/grid/DataTableCell'
import RowActionsCell from '../components/grid/RowActionsCell'
import { rowDeleteState } from '../components/grid/rowDeleteState'
import { useCategories } from '../hooks/useCategories'
import { usePendingDelete } from '../hooks/usePendingDelete'
import type { CategoryDto } from '../api/types'
import TruncatedText from '../components/TruncatedText'
import ActionsColumnHeader from '../components/grid/ActionsColumnHeader'

export default function CategoriesPage() {
  const {
    categories,
    isLoading,
    error,
    retry,
    createCategory,
    updateCategory,
    deletingId,
    deleteError,
    deleteCategory,
  } = useCategories()
  const [editingCategory, setEditingCategory] = useState<CategoryDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const deletion = usePendingDelete((row: CategoryDto) => deleteCategory(row.id))
  const pendingDelete = deletion.pendingRow
  const [showInactive, setShowInactive] = useState(false)

  const visibleCategories = showInactive ? categories : categories.filter((category) => category.active)

  const handleSubmit = (name: string, active: boolean, isInvestment: boolean, isTithe: boolean) =>
    editingCategory
      ? updateCategory(editingCategory.id, { name, active, isInvestment, isTithe })
      : createCategory({ name, active, isInvestment, isTithe })

  const closeFormDialog = () => {
    setEditingCategory(null)
    setIsCreating(false)
  }

  const handleFormSubmit = async (name: string, active: boolean, isInvestment: boolean, isTithe: boolean) => {
    const result = await handleSubmit(name, active, isInvestment, isTithe)
    closeFormDialog()
    return result
  }

  return (
    <AdminEntityPage
      title="Categories"
      createLabel="New category"
      onCreate={() => setIsCreating(true)}
      belowHeader={
        <Switch
          label="Show inactive"
          checked={showInactive}
          onChange={(e) => setShowInactive(e.target.checked)}
        />
      }
      actionError={deleteError}
      isLoading={isLoading}
      loadError={error}
      onRetry={retry}
      isEmpty={visibleCategories.length === 0}
      emptyMessage={categories.length === 0 ? 'No categories yet — create one to get started.' : 'No active categories.'}
      dialogs={
        <>
          {(isCreating || editingCategory) && (
            <CategoryFormDialog category={editingCategory} onCancel={closeFormDialog} onSubmit={handleFormSubmit} />
          )}
          {pendingDelete && (
            <ConfirmDeleteDialog
              entityLabel="category"
              message={
                pendingDelete.hasReferences ? (
                  <>&ldquo;{pendingDelete.name}&rdquo; is still used by a transaction and cannot be deleted.</>
                ) : (
                  <>&ldquo;{pendingDelete.name}&rdquo; will be permanently removed.</>
                )
              }
              confirmDisabled={pendingDelete.hasReferences}
              onConfirm={deletion.confirm}
              onCancel={deletion.cancel}
            />
          )}
        </>
      }
    >
      <Table aria-label="Categories" className="data-table">
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Name</TableHeaderCell>
            <TableHeaderCell>Active</TableHeaderCell>
            <TableHeaderCell>Investment</TableHeaderCell>
            <TableHeaderCell>Tithe</TableHeaderCell>
            <ActionsColumnHeader />
          </TableRow>
        </TableHeader>
        <TableBody>
          {visibleCategories.map((category) => (
            <TableRow key={category.id}>
              <DataTableCell label="Name"><TruncatedText text={category.name} /></DataTableCell>
              <DataTableCell label="Active">{category.active ? 'Yes' : 'No'}</DataTableCell>
              <DataTableCell label="Investment">{category.isInvestment ? 'Yes' : 'No'}</DataTableCell>
              <DataTableCell label="Tithe">{category.isTithe ? 'Yes' : 'No'}</DataTableCell>
              <RowActionsCell
                itemName={category.name}
                {...rowDeleteState(deletingId, category.id)}
                onEdit={() => setEditingCategory(category)}
                onDelete={() => deletion.request(category)}
              />
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </AdminEntityPage>
  )
}
