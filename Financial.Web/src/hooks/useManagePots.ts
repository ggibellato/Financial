import { useRef, useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { PotBalanceDto, PotUpdateDto } from '../api/types'
import { useDialogSubmit } from './useDialogSubmit'
import { useDiscardPrompt } from './useDiscardPrompt'

const MIN_SPLIT = 0
const MAX_SPLIT = 100
const REQUIRED_ACTIVE_TOTAL = 100
const TOTAL_ROUNDING = 10_000
const NEW_POT_SPLIT = '0'

export const POT_NAME_REQUIRED = 'Pot name is required.'
export const POT_SPLIT_OUT_OF_RANGE = `Split percentage must be between ${MIN_SPLIT} and ${MAX_SPLIT}.`

export interface PotRow {
  key: string
  id: string | null
  label: string
  name: string
  splitPercentage: string
  isActive: boolean
}

export type PotRowChange = Partial<Pick<PotRow, 'name' | 'splitPercentage' | 'isActive'>>

function parseSplit(text: string): number | null {
  const split = text.trim() === '' ? Number.NaN : Number(text)
  return Number.isNaN(split) ? null : split
}

function toRow(pot: PotBalanceDto): PotRow {
  return { key: pot.id, id: pot.id, label: pot.name, name: pot.name, splitPercentage: String(pot.splitPercentage), isActive: pot.isActive }
}

export function potNameError(row: PotRow): string {
  return row.name.trim() === '' ? POT_NAME_REQUIRED : ''
}

export function potSplitError(row: PotRow): string {
  const split = parseSplit(row.splitPercentage)
  return split === null || split < MIN_SPLIT || split > MAX_SPLIT ? POT_SPLIT_OUT_OF_RANGE : ''
}

function activeTotalOf(rows: readonly PotRow[]): number {
  const total = rows.filter((row) => row.isActive).reduce((sum, row) => sum + (parseSplit(row.splitPercentage) ?? 0), 0)
  return Math.round(total * TOTAL_ROUNDING) / TOTAL_ROUNDING
}

function isChanged(row: PotRow, saved: PotBalanceDto | undefined): boolean {
  return (
    saved === undefined ||
    row.name.trim() !== saved.name ||
    parseSplit(row.splitPercentage) !== saved.splitPercentage ||
    row.isActive !== saved.isActive
  )
}

function toRequest(row: PotRow): PotUpdateDto {
  return { id: row.id, name: row.name.trim(), splitPercentage: Number(row.splitPercentage), isActive: row.isActive }
}

export function useManagePots(potsAccountId: string, initialPots: readonly PotBalanceDto[], onSaved: () => void) {
  const [savedPots] = useState(initialPots)
  const [rows, setRows] = useState(() => savedPots.map(toRow))
  const [lastAddedKey, setLastAddedKey] = useState<string | null>(null)
  const addedPotCount = useRef(0)
  const { isSaving, error, submit } = useDialogSubmit('The pots could not be saved.')

  const isDirty = rows.some((row) => isChanged(row, savedPots.find((pot) => pot.id === row.id)))
  const hasFieldErrors = rows.some((row) => potNameError(row) !== '' || potSplitError(row) !== '')
  const discardPrompt = useDiscardPrompt(isDirty)
  const activeTotal = activeTotalOf(rows)

  const changeRow = (key: string, change: PotRowChange) =>
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...change } : row)))

  const addPot = () => {
    addedPotCount.current += 1
    const key = `new-pot-${addedPotCount.current}`
    setRows((current) => [
      ...current,
      { key, id: null, label: `new pot ${addedPotCount.current}`, name: '', splitPercentage: NEW_POT_SPLIT, isActive: true },
    ])
    setLastAddedKey(key)
  }

  const removeNewPot = (key: string) => setRows((current) => current.filter((row) => row.key !== key))

  const canSave = isDirty && !hasFieldErrors && !isSaving

  const save = () => {
    if (!canSave) return
    void submit(async () => {
      await apiClient.updatePots(potsAccountId, { pots: rows.map(toRequest) })
      onSaved()
    })
  }

  return {
    rows,
    lastAddedKey,
    activeTotal,
    isTotalRight: activeTotal === REQUIRED_ACTIVE_TOTAL,
    hasActivePots: rows.some((row) => row.isActive),
    isSaving,
    error,
    canSave,
    discardPrompt,
    changeRow,
    addPot,
    removeNewPot,
    save,
  }
}
