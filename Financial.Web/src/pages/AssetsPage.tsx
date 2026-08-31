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
  Select,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
} from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import AssetFormDialog from '../components/AssetFormDialog'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { useBrokers } from '../hooks/useBrokers'
import { usePortfolios } from '../hooks/usePortfolios'
import { assetKey, useAssets } from '../hooks/useAssets'
import type { AssetAdminDto } from '../api/types'
import './AssetsPage.css'

export default function AssetsPage() {
  const styles = useFormPanelStyles()
  const { assets, isLoading, error, retry, createAsset, updateAsset, deletingKey, deleteError, deleteAsset } =
    useAssets()
  const { brokers } = useBrokers()
  const { portfolios } = usePortfolios()
  const activeBrokers = brokers.filter((b) => b.status === 'Active')

  const [brokerFilter, setBrokerFilter] = useState('')
  const [portfolioFilter, setPortfolioFilter] = useState('')
  const [classFilter, setClassFilter] = useState('')

  const [editingAsset, setEditingAsset] = useState<AssetAdminDto | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState<AssetAdminDto | null>(null)

  const closeFormDialog = () => {
    setEditingAsset(null)
    setIsCreating(false)
  }

  const handleFormSubmit = async (values: {
    brokerName: string
    portfolioName: string
    name: string
    isin: string
    exchange: string
    ticker: string
    country: string
    localTypeCode: string
    assetClass: string
  }) => {
    const result = editingAsset
      ? await updateAsset(editingAsset.brokerName, editingAsset.portfolioName, editingAsset.name, {
          name: values.name,
          isin: values.isin,
          exchange: values.exchange,
          ticker: values.ticker,
          country: values.country as AssetAdminDto['country'],
          localTypeCode: values.localTypeCode,
          class: values.assetClass as AssetAdminDto['class'],
        })
      : await createAsset({
          brokerName: values.brokerName,
          portfolioName: values.portfolioName,
          name: values.name,
          isin: values.isin,
          exchange: values.exchange,
          ticker: values.ticker,
          country: values.country as AssetAdminDto['country'],
          localTypeCode: values.localTypeCode,
          // Left at its default ('Unknown') means the user never touched the Class picker, so the
          // backend auto-resolves it from Country/LocalTypeCode; any other selection is an explicit
          // override.
          class: values.assetClass === 'Unknown' ? null : (values.assetClass as AssetAdminDto['class']),
        })
    closeFormDialog()
    return result
  }

  const handleConfirmDelete = () => {
    if (!confirmingDelete) return
    deleteAsset(confirmingDelete.brokerName, confirmingDelete.portfolioName, confirmingDelete.name)
    setConfirmingDelete(null)
  }

  const assetClasses = Array.from(new Set(assets.map((a) => a.class))).sort()

  const filteredAssets = assets.filter(
    (a) =>
      (brokerFilter === '' || a.brokerName === brokerFilter) &&
      (portfolioFilter === '' || a.portfolioName === portfolioFilter) &&
      (classFilter === '' || a.class === classFilter),
  )

  return (
    <section className="assets-page">
      <header className="assets-page__header">
        <h2>Assets</h2>
        <Button appearance="primary" icon={<AddRegular />} onClick={() => setIsCreating(true)}>
          Create Asset
        </Button>
      </header>

      <div className="assets-page__filters">
        <Select aria-label="Filter by broker" value={brokerFilter} onChange={(e) => setBrokerFilter(e.target.value)}>
          <option value="">All Brokers</option>
          {Array.from(new Set(assets.map((a) => a.brokerName))).sort().map((name) => (
            <option key={name} value={name}>
              {name}
            </option>
          ))}
        </Select>
        <Select
          aria-label="Filter by portfolio"
          value={portfolioFilter}
          onChange={(e) => setPortfolioFilter(e.target.value)}
        >
          <option value="">All Portfolios</option>
          {Array.from(new Set(assets.map((a) => a.portfolioName))).sort().map((name) => (
            <option key={name} value={name}>
              {name}
            </option>
          ))}
        </Select>
        <Select aria-label="Filter by class" value={classFilter} onChange={(e) => setClassFilter(e.target.value)}>
          <option value="">All Classes</option>
          {assetClasses.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </Select>
      </div>

      {deleteError && (
        <MessageBar intent="error">
          <MessageBarBody>{deleteError}</MessageBarBody>
        </MessageBar>
      )}

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : filteredAssets.length === 0 ? (
        <p className="assets-page__empty">No assets yet — create one to get started.</p>
      ) : (
        <Table aria-label="Assets">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Name</TableHeaderCell>
              <TableHeaderCell>Ticker</TableHeaderCell>
              <TableHeaderCell>Broker</TableHeaderCell>
              <TableHeaderCell>Portfolio</TableHeaderCell>
              <TableHeaderCell>Class</TableHeaderCell>
              <TableHeaderCell>Quantity</TableHeaderCell>
              <TableHeaderCell>Actions</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredAssets.map((asset) => {
              const key = assetKey(asset.brokerName, asset.portfolioName, asset.name)
              return (
                <TableRow key={key}>
                  <TableCell>{asset.name}</TableCell>
                  <TableCell>{asset.ticker}</TableCell>
                  <TableCell>{asset.brokerName}</TableCell>
                  <TableCell>{asset.portfolioName}</TableCell>
                  <TableCell>{asset.class}</TableCell>
                  <TableCell>{asset.quantity}</TableCell>
                  <TableCell>
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<EditRegular />}
                      aria-label={`Edit ${asset.name}`}
                      onClick={() => setEditingAsset(asset)}
                    />
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<DeleteRegular />}
                      aria-label={`Delete ${asset.name}`}
                      disabled={deletingKey === key}
                      onClick={() => setConfirmingDelete(asset)}
                    />
                  </TableCell>
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
      )}

      {(isCreating || editingAsset) && (
        <AssetFormDialog
          asset={editingAsset}
          activeBrokers={activeBrokers}
          portfolios={portfolios}
          onCancel={closeFormDialog}
          onSubmit={handleFormSubmit}
        />
      )}

      {confirmingDelete && (
        <Dialog open onOpenChange={(_, data) => { if (!data.open) setConfirmingDelete(null) }}>
          <DialogSurface aria-describedby={undefined}>
            <DialogBody>
              <DialogTitle>Delete Asset</DialogTitle>
              <DialogContent>
                {confirmingDelete.quantity !== 0 ? (
                  <p>
                    &ldquo;{confirmingDelete.name}&rdquo; still holds a position of {confirmingDelete.quantity} and
                    cannot be deleted.
                  </p>
                ) : (
                  <p>
                    &ldquo;{confirmingDelete.name}&rdquo; holds zero quantity and will be archived into Historic
                    Investments.
                  </p>
                )}
              </DialogContent>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleConfirmDelete}
                  disabled={confirmingDelete.quantity !== 0}
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
