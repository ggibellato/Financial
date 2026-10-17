import { useState } from 'react'
import { Switch, Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import AdminEntityPage from '../components/AdminEntityPage'
import CategoryFormDialog from '../components/CategoryFormDialog'
import ConfirmDeleteDialog from '../components/ConfirmDeleteDialog'
import DataTableCell from '../components/grid/DataTableCell'
import RowActionsCell from '../components/grid/RowActionsCell'
import { useCategories } from '../hooks/useCategories'
import type { CategoryDto } from '../api/types'
import TruncatedText from '../components/TruncatedText'

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
  const [confirmingDelete, setConfirmingDelete] = useState<CategoryDto | null>(null)
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

  const handleConfirmDelete = () => {
    if (!confirmingDelete) return
    deleteCategory(confirmingDelete.id)
    setConfirmingDelete(null)
  }

  return (
    <AdminEntityPage
      title="Categories"
      createLabel="Create Category"
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
          {confirmingDelete && (
            <ConfirmDeleteDialog
              entityLabel="Category"
              message={
                confirmingDelete.hasReferences ? (
                  <>&ldquo;{confirmingDelete.name}&rdquo; is still used by a transaction and cannot be deleted.</>
                ) : (
                  <>&ldquo;{confirmingDelete.name}&rdquo; will be permanently removed.</>
                )
              }
              confirmDisabled={confirmingDelete.hasReferences}
              onConfirm={handleConfirmDelete}
              onCancel={() => setConfirmingDelete(null)}
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
            <TableHeaderCell className="data-table__col--action" />
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
                deleteDisabled={deletingId === category.id}
                onEdit={() => setEditingCategory(category)}
                onDelete={() => setConfirmingDelete(category)}
              />
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </AdminEntityPage>
  )
}
