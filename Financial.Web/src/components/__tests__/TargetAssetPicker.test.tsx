import { useState } from 'react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '../../test/renderWithFluent'
import type { AssetSearchOptionsData } from '../../hooks/useAssetSearchOptions'
import type { AssetAdminDto } from '../../api/types'
import TargetAssetPicker from '../TargetAssetPicker'
import { BLANK_TARGET_ASSET_PICKER_VALUE, type TargetAssetPickerValue } from '../targetAssetPickerValue'

const retryMock = vi.fn()

const mockHookValue: AssetSearchOptionsData = {
  options: [],
  isLoading: false,
  error: null,
  retry: retryMock,
}

vi.mock('../../hooks/useAssetSearchOptions', () => ({
  useAssetSearchOptions: () => mockHookValue,
}))

function makeAsset(name: string): AssetAdminDto {
  return {
    name,
    brokerName: 'XPI',
    portfolioName: 'Acoes',
    brokerStatus: 'Active',
    isin: '',
    exchange: '',
    ticker: '',
    country: 'Unknown',
    localTypeCode: '',
    class: 'Unknown',
    valuationMethod: 'Unspecified',
    incomePolicy: 'Unknown',
    quantity: 0,
  }
}

const PETR4 = makeAsset('PETR4')
const VALE3 = makeAsset('VALE3')

function setOptions(options: AssetAdminDto[]) {
  mockHookValue.options = options
}
function setLoading(isLoading: boolean) {
  mockHookValue.isLoading = isLoading
}
function setError(error: string | null) {
  mockHookValue.error = error
}

function Harness({ nameError }: { nameError?: string | null }) {
  const [value, setValue] = useState<TargetAssetPickerValue>(BLANK_TARGET_ASSET_PICKER_VALUE)
  return <TargetAssetPicker label="Target Asset" value={value} onChange={setValue} nameError={nameError} />
}

describe('TargetAssetPicker', () => {
  beforeEach(() => {
    retryMock.mockReset()
    setOptions([PETR4, VALE3])
    setLoading(false)
    setError(null)
  })

  it('renders_with_a_visible_required_label', () => {
    render(<Harness />)
    expect(screen.getByRole('combobox', { name: 'Target Asset' })).toBeInTheDocument()
  })

  it('search_filters_existing_candidates', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    const combobox = screen.getByRole('combobox', { name: 'Target Asset' })
    await user.type(combobox, 'VAL')

    expect(screen.getByRole('option', { name: 'VALE3' })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: 'PETR4' })).not.toBeInTheDocument()
  })

  it('selecting_an_existing_asset_sets_createInline_false_and_hides_identity_fields', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    const combobox = screen.getByRole('combobox', { name: 'Target Asset' })
    await user.click(combobox)
    await user.click(await screen.findByRole('option', { name: 'PETR4' }))

    expect(combobox).toHaveValue('PETR4')
    expect(screen.queryByLabelText('ISIN')).not.toBeInTheDocument()
  })

  it('typing_an_unmatched_name_reveals_identity_fields_and_sets_createInline_true', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    const combobox = screen.getByRole('combobox', { name: 'Target Asset' })
    await user.type(combobox, 'NEWCO')

    await waitFor(() => expect(screen.getByLabelText('ISIN')).toBeInTheDocument())
    expect(screen.getByLabelText('Exchange')).toBeInTheDocument()
    expect(screen.getByLabelText('Ticker')).toBeInTheDocument()
    expect(screen.getByLabelText('Country')).toBeInTheDocument()
    expect(screen.getByLabelText('Class')).toBeInTheDocument()
    expect(screen.getByText('No existing asset named "NEWCO" — it will be created.')).toBeInTheDocument()
  })

  it('surfaces_a_server_side_name_collision_message_as_the_fields_own_error', () => {
    render(<Harness nameError='An asset named "NEWCO" already exists in portfolio "Acoes".' />)

    expect(screen.getByText('An asset named "NEWCO" already exists in portfolio "Acoes".')).toBeInTheDocument()
  })

  it('disables_the_combobox_while_options_are_loading', () => {
    setLoading(true)
    render(<Harness />)

    expect(screen.getByRole('combobox', { name: 'Target Asset' })).toBeDisabled()
  })

  it('shows_an_error_state_with_retry_when_options_fail_to_load', async () => {
    setError('Unable to load assets')
    const user = userEvent.setup()
    render(<Harness />)

    expect(screen.getByRole('alert')).toHaveTextContent('Unable to load assets')
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(retryMock).toHaveBeenCalledTimes(1)
  })

  it('is_keyboard_operable_to_search_and_select_an_option', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    await user.tab()
    const combobox = screen.getByRole('combobox', { name: 'Target Asset' })
    expect(combobox).toHaveFocus()

    await user.keyboard('PETR4')
    await user.keyboard('{ArrowDown}{Enter}')

    expect(combobox).toHaveValue('PETR4')
  })
})
