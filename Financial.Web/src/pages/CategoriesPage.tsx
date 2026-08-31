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
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
} from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import CategoryFormDialog from '../components/CategoryFormDialog'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { useCategories } from '../hooks/useCategories'
import type { CategoryDto } from '../api/types'
import './CategoriesPage.css'

export default function CategoriesPage() {
  const styles = useFormPanelStyles()
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
    <section className="categories-page">
      <header className="categories-page__header">
        <h2>Categories</h2>
        <Button appearance="primary" icon={<AddRegular />} onClick={() => setIsCreating(true)}>
          Create Category
        </Button>
      </header>

      <Switch
        label="Show inactive"
        checked={showInactive}
        onChange={(e) => setShowInactive(e.target.checked)}
      />

      {deleteError && (
        <MessageBar intent="error">
          <MessageBarBody>{deleteError}</MessageBarBody>
        </MessageBar>
      )}

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : visibleCategories.length === 0 ? (
        <p className="categories-page__empty">
          {categories.length === 0 ? 'No categories yet — create one to get started.' : 'No active categories.'}
        </p>
      ) : (
        <Table aria-label="Categories">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Name</TableHeaderCell>
              <TableHeaderCell>Active</TableHeaderCell>
              <TableHeaderCell>Investment</TableHeaderCell>
              <TableHeaderCell>Tithe</TableHeaderCell>
              <TableHeaderCell>Actions</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {visibleCategories.map((category) => (
              <TableRow key={category.id}>
                <TableCell>{category.name}</TableCell>
                <TableCell>{category.active ? 'Yes' : 'No'}</TableCell>
                <TableCell>{category.isInvestment ? 'Yes' : 'No'}</TableCell>
                <TableCell>{category.isTithe ? 'Yes' : 'No'}</TableCell>
                <TableCell>
                  <Button
                    appearance="subtle"
                    size="small"
                    icon={<EditRegular />}
                    aria-label={`Edit ${category.name}`}
                    onClick={() => setEditingCategory(category)}
                  />
                  <Button
                    appearance="subtle"
                    size="small"
                    icon={<DeleteRegular />}
                    aria-label={`Delete ${category.name}`}
                    disabled={deletingId === category.id}
                    onClick={() => setConfirmingDelete(category)}
                  />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {(isCreating || editingCategory) && (
        <CategoryFormDialog category={editingCategory} onCancel={closeFormDialog} onSubmit={handleFormSubmit} />
      )}

      {confirmingDelete && (
        <Dialog open onOpenChange={(_, data) => { if (!data.open) setConfirmingDelete(null) }}>
          <DialogSurface aria-describedby={undefined}>
            <DialogBody>
              <DialogTitle>Delete Category</DialogTitle>
              <DialogContent>
                {confirmingDelete.hasReferences ? (
                  <p>&ldquo;{confirmingDelete.name}&rdquo; is still used by a transaction and cannot be deleted.</p>
                ) : (
                  <p>&ldquo;{confirmingDelete.name}&rdquo; will be permanently removed.</p>
                )}
              </DialogContent>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleConfirmDelete}
                  disabled={confirmingDelete.hasReferences}
                >
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
