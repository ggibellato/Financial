import { useState } from 'react'
import { Field, Input, Select } from '@fluentui/react-components'
import type { AssetAdminDto, BrokerDto, PortfolioDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import { useDialogSubmit } from '../hooks/useDialogSubmit'
import { useDuplicateNameField } from '../hooks/useDuplicateNameField'
import { isValidIsin } from '../utils/validators'

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
  const { isSaving, error, failure, submit } = useDialogSubmit('The asset could not be saved.')

  const brokerPortfolios = portfolios.filter((p) => p.brokerName === brokerName)

  const handleBrokerChange = (nextBroker: string) => {
    setBrokerName(nextBroker)
    setPortfolioName('')
  }

  const trimmedName = name.trim()
  const { nameRef, nameError, summaryError } = useDuplicateNameField(failure, error, JSON.stringify([brokerName, portfolioName, trimmedName]))
  const trimmedIsin = isin.trim()
  const nameValidationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const nameMessage = nameValidationMessage || nameError
  const brokerValidationMessage = !isEditing && brokerName.length === 0 ? 'A broker is required.' : ''
  const portfolioValidationMessage = !isEditing && portfolioName.length === 0 ? 'A portfolio is required.' : ''
  const isinValidationMessage =
    trimmedIsin.length > 0 && !isValidIsin(trimmedIsin)
      ? 'ISIN must be 2 letters, 9 alphanumeric characters, and a check digit (e.g. US0378331005).'
      : ''
  const canSubmit =
    nameValidationMessage.length === 0 &&
    brokerValidationMessage.length === 0 &&
    portfolioValidationMessage.length === 0 &&
    isinValidationMessage.length === 0 &&
    !isSaving

  const handleSubmit = () => {
    if (!canSubmit) return
    void submit(() =>
      onSubmit({
        brokerName,
        portfolioName,
        name: trimmedName,
        isin: trimmedIsin,
        exchange: exchange.trim(),
        ticker: ticker.trim(),
        country,
        localTypeCode: localTypeCode.trim(),
        assetClass,
      }),
    )
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit asset' : 'New asset'}
      saveLabel={isEditing ? 'Save' : 'Add asset'}
      error={summaryError}
      isSaving={isSaving}
      canSave={canSubmit}
      onSave={handleSubmit}
      onCancel={onCancel}
    >
      {isEditing ? (
        <>
          <Field label="Broker">
            <Input value={asset.brokerName} readOnly appearance="filled-darker" />
          </Field>
          <Field label="Portfolio">
            <Input value={asset.portfolioName} readOnly appearance="filled-darker" />
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
            <Select value={brokerName} onChange={(e) => handleBrokerChange(e.target.value)}>
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
            <Select value={portfolioName} onChange={(e) => setPortfolioName(e.target.value)} disabled={brokerPortfolios.length === 0}>
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
        validationState={nameMessage ? 'error' : 'none'}
        validationMessage={nameMessage}
      >
        <Input ref={nameRef} value={name} onChange={(e) => setName(e.target.value)} />
      </Field>

      <Field
        label="ISIN"
        validationState={isinValidationMessage ? 'error' : 'none'}
        validationMessage={isinValidationMessage}
      >
        <Input value={isin} onChange={(e) => setIsin(e.target.value)} />
      </Field>

      <Field label="Exchange">
        <Input value={exchange} onChange={(e) => setExchange(e.target.value)} />
      </Field>

      <Field label="Ticker">
        <Input value={ticker} onChange={(e) => setTicker(e.target.value)} />
      </Field>

      <Field label="Country">
        <Select value={country} onChange={(e) => setCountry(e.target.value)}>
          {COUNTRY_OPTIONS.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </Select>
      </Field>

      <Field label="Local type code">
        <Input value={localTypeCode} onChange={(e) => setLocalTypeCode(e.target.value)} />
      </Field>

      <Field label="Class">
        <Select value={assetClass} onChange={(e) => setAssetClass(e.target.value)}>
          {CLASS_OPTIONS.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </Select>
      </Field>
    </EntityFormDialog>
  )
}
