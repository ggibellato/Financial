import { useMemo } from 'react'
import { Combobox, Field, Input, Option, Select, Text } from '@fluentui/react-components'
import type { ComboboxProps } from '@fluentui/react-components'
import ErrorState from './ErrorState'
import { useAssetSearchOptions } from '../hooks/useAssetSearchOptions'
import { useFormPanelStyles } from './formPanelStyles'
import { isValidIsin } from '../utils/validators'
import { BLANK_TARGET_ASSET_IDENTITY, type TargetAssetIdentity, type TargetAssetPickerValue } from './targetAssetPickerValue'
import './TargetAssetPicker.css'

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

interface TargetAssetPickerProps {
  label: string
  value: TargetAssetPickerValue
  onChange: (value: TargetAssetPickerValue) => void
  disabled?: boolean
  nameError?: string | null
}

export default function TargetAssetPicker({ label, value, onChange, disabled, nameError }: TargetAssetPickerProps) {
  const styles = useFormPanelStyles()
  const { options, isLoading, error, retry } = useAssetSearchOptions()

  const trimmedName = value.assetName.trim()

  const matchedAsset = useMemo(
    () => options.find((asset) => asset.name.toLowerCase() === trimmedName.toLowerCase()) ?? null,
    [options, trimmedName],
  )

  const filteredOptions = useMemo(() => {
    if (!trimmedName) return options
    const query = trimmedName.toLowerCase()
    return options.filter((asset) => asset.name.toLowerCase().includes(query))
  }, [options, trimmedName])

  const showCreateFields = value.createInline && trimmedName.length > 0 && !matchedAsset

  const applyTypedName = (name: string) => {
    const matched = options.find((asset) => asset.name.toLowerCase() === name.trim().toLowerCase())
    onChange({
      assetName: name,
      createInline: name.trim().length > 0 && !matched,
      identity: matched ? BLANK_TARGET_ASSET_IDENTITY : value.identity,
    })
  }

  const handleOptionSelect: NonNullable<ComboboxProps['onOptionSelect']> = (_event, data) => {
    onChange({
      assetName: data.optionText ?? '',
      createInline: false,
      identity: BLANK_TARGET_ASSET_IDENTITY,
    })
  }

  const setIdentityField = (field: keyof TargetAssetIdentity, fieldValue: string) => {
    onChange({ ...value, identity: { ...value.identity, [field]: fieldValue } })
  }

  const trimmedIsin = value.identity.isin.trim()
  const isinValidationMessage =
    trimmedIsin.length > 0 && !isValidIsin(trimmedIsin)
      ? 'ISIN must be 2 letters, 9 alphanumeric characters, and a check digit (e.g. US0378331005).'
      : ''

  return (
    <>
      <div className={styles.spanTwo}>
        <Field
          label={label}
          required
          validationState={nameError ? 'error' : 'none'}
          validationMessage={nameError ?? undefined}
        >
          <Combobox
            freeform
            disabled={disabled || isLoading}
            value={value.assetName}
            selectedOptions={matchedAsset ? [matchedAsset.name] : []}
            onOptionSelect={handleOptionSelect}
            onChange={(e) => applyTypedName(e.target.value)}
            placeholder={isLoading ? 'Loading assets...' : 'Search assets or type a new name'}
          >
            {filteredOptions.map((asset) => (
              <Option key={asset.name} value={asset.name}>
                {asset.name}
              </Option>
            ))}
          </Combobox>
        </Field>

        {showCreateFields && (
          <Text as="p" size={200} className="target-asset-picker__create-hint">
            No existing asset named &quot;{trimmedName}&quot; — it will be created.
          </Text>
        )}

        {error && <ErrorState message={error} onRetry={retry} />}
      </div>

      {showCreateFields && (
        <>
          <Field
            label="ISIN"
            validationState={isinValidationMessage ? 'error' : 'none'}
            validationMessage={isinValidationMessage}
          >
            <Input
              value={value.identity.isin}
              disabled={disabled}
              onChange={(e) => setIdentityField('isin', e.target.value)}
            />
          </Field>

          <Field label="Exchange">
            <Input
              value={value.identity.exchange}
              disabled={disabled}
              onChange={(e) => setIdentityField('exchange', e.target.value)}
            />
          </Field>

          <Field label="Ticker">
            <Input
              value={value.identity.ticker}
              disabled={disabled}
              onChange={(e) => setIdentityField('ticker', e.target.value)}
            />
          </Field>

          <Field label="Country">
            <Select
              value={value.identity.country}
              disabled={disabled}
              onChange={(e) => setIdentityField('country', e.target.value)}
            >
              {COUNTRY_OPTIONS.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </Select>
          </Field>

          <Field label="Class">
            <Select
              value={value.identity.assetClass}
              disabled={disabled}
              onChange={(e) => setIdentityField('assetClass', e.target.value)}
            >
              {CLASS_OPTIONS.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </Select>
          </Field>
        </>
      )}
    </>
  )
}
