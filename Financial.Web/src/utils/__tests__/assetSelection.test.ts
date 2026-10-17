import { describe, expect, it } from 'vitest'
import type { SelectedNode } from '../../api/types'
import { isAssetSelection } from '../assetSelection'

const asset: SelectedNode = { nodeType: 'Asset', brokerName: 'XP', portfolioName: 'Main', assetName: 'PETR4' }

describe('isAssetSelection', () => {
  it('accepts an asset with a portfolio and asset name', () => {
    expect(isAssetSelection(asset)).toBe(true)
  })

  it('rejects no selection', () => {
    expect(isAssetSelection(null)).toBe(false)
  })

  it.each(['Broker', 'Portfolio'] as const)('rejects a %s node', (nodeType) => {
    expect(isAssetSelection({ ...asset, nodeType })).toBe(false)
  })

  it.each([
    ['portfolioName', { portfolioName: undefined }],
    ['portfolioName', { portfolioName: '' }],
    ['assetName', { assetName: undefined }],
    ['assetName', { assetName: '' }],
  ])('rejects an asset with a missing %s', (_field, override) => {
    expect(isAssetSelection({ ...asset, ...override })).toBe(false)
  })
})
