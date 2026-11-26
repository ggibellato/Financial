import { useState } from 'react'
import { Combobox, Field, InfoLabel, Input, Option, Select, type LabelProps } from '@fluentui/react-components'
import type { AssetAdminDto, BrokerDto, PortfolioDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import ReadOnlyInput from './ReadOnlyInput'
import { useDialogSubmit } from '../hooks/useDialogSubmit'
import { useEntityForm } from '../hooks/useEntityForm'
import { useDuplicateNameField } from '../hooks/useDuplicateNameField'
import { useFreeformCombobox } from '../hooks/useFreeformCombobox'
import { isValidIsin } from '../utils/validators'
import { MAX_SEGMENT_LENGTH, SEGMENT_CLASSES, segmentKey } from '../utils/assetSegments'
import { PICK_SOURCE_LABELS, PICK_SOURCE_ORDER } from '../utils/pickSource'
import { optionalChoiceOptions } from '../utils/selectOptions'

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
const PICK_SOURCE_OPTIONS = optionalChoiceOptions(PICK_SOURCE_ORDER, PICK_SOURCE_LABELS, 'Not set')

export interface AssetFormValues {
  brokerName: string
  portfolioName: string
  name: string
  isin: string
  exchange: string
  ticker: string
  country: string
  localTypeCode: string
  assetClass: string
  segment: string
  pickSource: string
}

interface AssetFormDialogProps {
  asset: AssetAdminDto | null
  activeBrokers: BrokerDto[]
  portfolios: PortfolioDto[]
  segments: string[]
  onCancel: () => void
  onSubmit: (values: AssetFormValues) => Promise<unknown>
}

export default function AssetFormDialog({ asset, activeBrokers, portfolios, segments, onCancel, onSubmit }: AssetFormDialogProps) {
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
  const [segment, setSegment] = useState(asset?.segment ?? '')
  const [pickSource, setPickSource] = useState<string>(asset?.pickSource ?? '')
  const { isSaving, error, failure, submit } = useDialogSubmit('The asset could not be saved.')
  const form = useEntityForm({ brokerName, portfolioName, name, isin, exchange, ticker, country, localTypeCode, assetClass, segment, pickSource })

  const brokerPortfolios = portfolios.filter((p) => p.brokerName === brokerName)

  const handleBrokerChange = (nextBroker: string) => {
    setBrokerName(nextBroker)
    setPortfolioName('')
  }

  const trimmedName = name.trim()
  const { nameRef, nameError, summaryError } = useDuplicateNameField(failure, error, JSON.stringify([brokerName, portfolioName, trimmedName]))
  const trimmedIsin = isin.trim()
  const nameValidationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const nameMessage = form.shown(nameValidationMessage) || nameError
  const brokerValidationMessage = !isEditing && brokerName.length === 0 ? 'A broker is required.' : ''
  const portfolioValidationMessage = !isEditing && portfolioName.length === 0 ? 'A portfolio is required.' : ''
  const isinValidationMessage =
    trimmedIsin.length > 0 && !isValidIsin(trimmedIsin)
      ? 'ISIN must be 2 letters, 9 alphanumeric characters, and a check digit (e.g. US0378331005).'
      : ''
  const hasSegments = SEGMENT_CLASSES.includes(assetClass)
  const trimmedSegment = segment.trim()
  const segmentWillBeRemoved = !hasSegments && trimmedSegment.length > 0
  const segmentValidationMessage =
    hasSegments && trimmedSegment.length > MAX_SEGMENT_LENGTH ? `Segment must be ${MAX_SEGMENT_LENGTH} characters or fewer.` : ''
  const typedSegmentKey = segmentKey(segment)
  const isChosenSegment = segments.some((s) => segmentKey(s) === typedSegmentKey)
  const shownSegments = isChosenSegment ? segments : segments.filter((s) => segmentKey(s).includes(typedSegmentKey))
  const segmentCombobox = useFreeformCombobox(shownSegments.length > 0)
  const canSubmit =
    nameValidationMessage.length === 0 &&
    brokerValidationMessage.length === 0 &&
    portfolioValidationMessage.length === 0 &&
    isinValidationMessage.length === 0 &&
    segmentValidationMessage.length === 0

  const handleSubmit = () => {
    form.revealValidation()
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
        segment: trimmedSegment,
        pickSource,
      }),
    )
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit asset' : 'New asset'}
      saveLabel={isEditing ? 'Save' : 'Add asset'}
      error={summaryError}
      isSaving={isSaving}
      isDirty={form.isDirty}
      onSave={handleSubmit}
      onCancel={onCancel}
    >
      {isEditing ? (
        <>
          <Field label="Broker">
            <ReadOnlyInput value={asset.brokerName} />
          </Field>
          <Field label="Portfolio">
            <ReadOnlyInput value={asset.portfolioName} />
          </Field>
        </>
      ) : (
        <>
          <Field
            label="Broker"
            required
            {...form.fieldValidation(brokerValidationMessage)}
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
            {...form.fieldValidation(portfolioValidationMessage)}
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
        {...form.fieldValidation(isinValidationMessage)}
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

      <Field
        label="Class"
        validationState={segmentWillBeRemoved ? 'warning' : 'none'}
        validationMessage={segmentWillBeRemoved ? 'The segment will be removed when you save' : undefined}
      >
        <Select value={assetClass} onChange={(e) => setAssetClass(e.target.value)}>
          {CLASS_OPTIONS.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </Select>
      </Field>

      {hasSegments && (
        <Field label="Segment" {...form.fieldValidation(segmentValidationMessage)}>
          <Combobox
            freeform
            value={segment}
            selectedOptions={segment ? [segment] : []}
            onChange={(e) => setSegment(e.target.value)}
            onOptionSelect={(_event, data) => {
              if (data.optionValue !== undefined) setSegment(data.optionValue)
            }}
            {...segmentCombobox}
          >
            {shownSegments.map((s) => (
              <Option key={s} value={s}>
                {s}
              </Option>
            ))}
          </Combobox>
        </Field>
      )}

      <Field
        label={{
          children: (_: unknown, props: LabelProps) => (
            <InfoLabel {...props} info="Recommended: picked from an analysis service. My choice: picked by you.">
              Pick source
            </InfoLabel>
          ),
        }}
      >
        <Select value={pickSource} onChange={(e) => setPickSource(e.target.value)}>
          {PICK_SOURCE_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </Select>
      </Field>
    </EntityFormDialog>
  )
}
