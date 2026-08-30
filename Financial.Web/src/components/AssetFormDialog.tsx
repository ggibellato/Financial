import { useState } from 'react'
import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Select,
} from '@fluentui/react-components'
import type { AssetAdminDto, BrokerDto, PortfolioDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

const COUNTRY_OPTIONS = ['Unknown', 'BR', 'US', 'UK'] as const
const CLASS_OPTIONS = [
  'Unknown',
  'Equity',
  'RealEstate',
  'Bond',
  'Fund',
  'ETF',
  'Cash',
  'Pension',
  'Other',
  'Cryptocurrency',
  'PrivateCredit',
] as const

interface AssetFormValues {
  brokerName: string
  portfolioName: string
  name: string
  isin: string
  exchange: string
  ticker: string
  country: string
  localTypeCode: string
  assetClass: string
}

interface AssetFormDialogProps {
  asset: AssetAdminDto | null
  activeBrokers: BrokerDto[]
  portfolios: PortfolioDto[]
  onCancel: () => void
  onSubmit: (values: AssetFormValues) => Promise<unknown>
}

/** ISO 6166 shape: 2-letter country code + 9 alphanumeric + 1 check digit. Blank is valid (optional field). */
const ISIN_PATTERN = /^[A-Z]{2}[A-Z0-9]{9}[0-9]$/

export default function AssetFormDialog({ asset, activeBrokers, portfolios, onCancel, onSubmit }: AssetFormDialogProps) {
  const isEditing = asset !== null
  const [brokerName, setBrokerName] = useState(asset?.brokerName ?? activeBrokers[0]?.name ?? '')
  const [portfolioName, setPortfolioName] = useState(asset?.portfolioName ?? '')
  const [name, setName] = useState(asset?.name ?? '')
  const [isin, setIsin] = useState(asset?.isin ?? '')
  const [exchange, setExchange] = useState(asset?.exchange ?? '')
  const [ticker, setTicker] = useState(asset?.ticker ?? '')
  const [country, setCountry] = useState<string>(asset?.country ?? 'Unknown')
  const [localTypeCode, setLocalTypeCode] = useState(asset?.localTypeCode ?? '')
  const [assetClass, setAssetClass] = useState<string>(asset?.class ?? 'Unknown')
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const brokerPortfolios = portfolios.filter((p) => p.brokerName === brokerName)

  const handleBrokerChange = (nextBroker: string) => {
    setBrokerName(nextBroker)
    setPortfolioName('')
  }

  const trimmedName = name.trim()
  const trimmedIsin = isin.trim()
  const nameValidationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const brokerValidationMessage = !isEditing && brokerName.length === 0 ? 'A broker is required.' : ''
  const portfolioValidationMessage = !isEditing && portfolioName.length === 0 ? 'A portfolio is required.' : ''
  const isinValidationMessage =
    trimmedIsin.length > 0 && !ISIN_PATTERN.test(trimmedIsin)
      ? 'ISIN must be 2 letters, 9 alphanumeric characters, and a check digit (e.g. US0378331005).'
      : ''
  const canSubmit =
    nameValidationMessage.length === 0 &&
    brokerValidationMessage.length === 0 &&
    portfolioValidationMessage.length === 0 &&
    isinValidationMessage.length === 0 &&
    !isSaving

  const handleSubmit = async () => {
    if (!canSubmit) return

    setIsSaving(true)
    setError(null)
    try {
      await onSubmit({
        brokerName,
        portfolioName,
        name: trimmedName,
        isin: trimmedIsin,
        exchange: exchange.trim(),
        ticker: ticker.trim(),
        country,
        localTypeCode: localTypeCode.trim(),
        assetClass,
      })
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'The asset could not be saved.'))
      setIsSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open) onCancel() }}>
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>{isEditing ? 'Edit Asset' : 'Create Asset'}</DialogTitle>
          <DialogContent>
            {isEditing ? (
              <>
                <Field label="Broker">
                  <Input value={asset.brokerName} disabled readOnly />
                </Field>
                <Field label="Portfolio">
                  <Input value={asset.portfolioName} disabled readOnly />
                </Field>
              </>
            ) : (
              <>
                <Field
                  label="Broker"
                  required
                  validationState={brokerValidationMessage ? 'error' : 'none'}
                  validationMessage={brokerValidationMessage}
                >
                  <Select value={brokerName} onChange={(e) => handleBrokerChange(e.target.value)} disabled={isSaving}>
                    {activeBrokers.length === 0 && <option value="">No active brokers available</option>}
                    {activeBrokers.map((b) => (
                      <option key={b.name} value={b.name}>
                        {b.name}
                      </option>
                    ))}
                  </Select>
                </Field>

                <Field
                  label="Portfolio"
                  required
                  validationState={portfolioValidationMessage ? 'error' : 'none'}
                  validationMessage={portfolioValidationMessage}
                >
                  <Select value={portfolioName} onChange={(e) => setPortfolioName(e.target.value)} disabled={isSaving || brokerPortfolios.length === 0}>
                    <option value="">
                      {brokerPortfolios.length === 0 ? 'No portfolios under this broker' : 'Select a portfolio'}
                    </option>
                    {brokerPortfolios.map((p) => (
                      <option key={p.name} value={p.name}>
                        {p.name}
                      </option>
                    ))}
                  </Select>
                </Field>
              </>
            )}

            <Field
              label="Name"
              required
              validationState={nameValidationMessage ? 'error' : 'none'}
              validationMessage={nameValidationMessage}
            >
              <Input value={name} onChange={(e) => setName(e.target.value)} disabled={isSaving} autoFocus />
            </Field>

            <Field
              label="ISIN"
              validationState={isinValidationMessage ? 'error' : 'none'}
              validationMessage={isinValidationMessage}
            >
              <Input value={isin} onChange={(e) => setIsin(e.target.value)} disabled={isSaving} />
            </Field>

            <Field label="Exchange">
              <Input value={exchange} onChange={(e) => setExchange(e.target.value)} disabled={isSaving} />
            </Field>

            <Field label="Ticker">
              <Input value={ticker} onChange={(e) => setTicker(e.target.value)} disabled={isSaving} />
            </Field>

            <Field label="Country">
              <Select value={country} onChange={(e) => setCountry(e.target.value)} disabled={isSaving}>
                {COUNTRY_OPTIONS.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </Select>
            </Field>

            <Field label="Local Type Code">
              <Input value={localTypeCode} onChange={(e) => setLocalTypeCode(e.target.value)} disabled={isSaving} />
            </Field>

            <Field label="Class">
              <Select value={assetClass} onChange={(e) => setAssetClass(e.target.value)} disabled={isSaving}>
                {CLASS_OPTIONS.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </Select>
            </Field>

            {error && (
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" onClick={() => void handleSubmit()} disabled={!canSubmit}>
              Save
            </Button>
            <Button appearance="secondary" onClick={onCancel} disabled={isSaving}>
              Cancel
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
