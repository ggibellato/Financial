import type { components } from './generated/openapi'

type Schema<Name extends keyof components['schemas']> = DeepRequired<components['schemas'][Name]>

/**
 * The generated schema marks a property optional whenever the backend's OpenAPI generator didn't infer
 * it as required (mainly non-nullable value types - see OpenApiContractTests for why), even though every
 * DTO property is always present on the wire. `Required<T>` is shallow, so a plain alias would still leave
 * every nested object/array (e.g. AssetDetailsDto.transactions[*].quantity) optional one level down; this
 * recurses so the whole tree matches the "every key present, nullability via `| null`" shape this file
 * used before codegen.
 */
type DeepRequired<T> = T extends (infer Item)[]
  ? DeepRequired<Item>[]
  : T extends object
    ? { [Key in keyof T]-?: DeepRequired<T[Key]> }
    : T

export type NodeType = 'Asset' | 'Portfolio' | 'Broker'

export type PositionType = Schema<'PositionType'>

export type InvestmentScope = 'active' | 'historic'

export interface SelectedNode {
  nodeType: NodeType
  brokerName: string
  portfolioName?: string
  assetName?: string
  ticker?: string
  exchange?: string
  currency?: string
  positionType?: PositionType
  assetClass?: string
  /**
   * Carried so the move dialog can tell a closed position from an open one. Not derived from
   * positionType, which defaults to 'Flat' when metadata is missing and would read as closed.
   */
  quantity?: number
  /** For a portfolio: how many assets it holds, so an empty one can be offered for deletion. */
  assetCount?: number
}

export interface SelectedNodeContextValue {
  selectedNode: SelectedNode | null
  setSelectedNode: (node: SelectedNode | null) => void
  scope: InvestmentScope
  /** Re-fetches the navigation tree. The tree is built from a snapshot, so it does not observe a move. */
  reload: () => void
  /** Bumped by reload(); the tree re-fetches when it changes. */
  reloadToken: number
}

export type TreeNodeDto = Schema<'TreeNodeDTO'>
export type BrokerNodeDto = Schema<'BrokerNodeDTO'>
export type BrokerDto = Schema<'BrokerDTO'>
export type BrokerCreateDto = Schema<'BrokerCreateDTO'>
export type BrokerUpdateDto = Schema<'BrokerUpdateDTO'>
export type PortfolioDto = Schema<'PortfolioDTO'>
export type PortfolioCreateDto = Schema<'PortfolioCreateDTO'>
export type PortfolioUpdateDto = Schema<'PortfolioUpdateDTO'>
export type PortfolioNodeDto = Schema<'PortfolioNodeDTO'>
export type AssetNodeDto = Schema<'AssetNodeDTO'>
export type TransactionDto = Schema<'TransactionDTO'>
export type TransactionSummaryItemDto = Schema<'TransactionSummaryItemDTO'>
export type CreditDto = Schema<'CreditDTO'>
export type AssetPriceSnapshotDto = Schema<'AssetPriceSnapshotDTO'>
export type AssetDetailsDto = Schema<'AssetDetailsDTO'>
export type ArchiveAssetRequestDto = Schema<'ArchiveAssetRequestDTO'>
export type MoveAssetRequestDto = Schema<'MoveAssetRequestDTO'>
export type AssetAdminDto = Schema<'AssetAdminDTO'>
export type AssetAdminCreateDto = Schema<'AssetAdminCreateDTO'>
export type AssetAdminUpdateDto = Schema<'AssetAdminUpdateDTO'>
export type TransactionCreateDto = Schema<'TransactionCreateDTO'>
export type TransactionUpdateDto = Schema<'TransactionUpdateDTO'>
export type TransactionDeleteDto = Schema<'TransactionDeleteDTO'>
export type CreditCreateDto = Schema<'CreditCreateDTO'>
export type CreditUpdateDto = Schema<'CreditUpdateDTO'>
export type CreditDeleteDto = Schema<'CreditDeleteDTO'>
export type SetAssetPriceDto = Schema<'SetAssetPriceDTO'>
export type DeleteAssetPriceDto = Schema<'DeleteAssetPriceDTO'>
export type DividendHistoryItemDto = Schema<'DividendHistoryItemDTO'>
export type DividendYearTotalDto = Schema<'DividendYearTotalDTO'>
export type DividendSummaryDto = Schema<'DividendSummaryDTO'>
export type AssetPriceDto = Schema<'AssetPriceDTO'>
export type AggregatedSummaryDto = Schema<'AggregatedSummaryDTO'>
export type AssetBreakdownItemDto = Schema<'AssetBreakdownItemDTO'>
export type PortfolioBreakdownItemDto = Schema<'PortfolioBreakdownItemDTO'>
export type WatchlistItemDto = Schema<'WatchlistItemDTO'>
export type PortfolioReferenceDto = Schema<'PortfolioReferenceDTO'>
export type AssetCashFlowDto = Schema<'AssetCashFlowDTO'>
export type CalculateXirrRequestDto = Schema<'CalculateXirrRequestDTO'>
export type XirrResultDto = Schema<'XirrResultDTO'>
export type PortfolioAssetSummaryItemDto = Schema<'PortfolioAssetSummaryItemDTO'>
export type ReserveBucketBalanceDto = Schema<'ReserveBucketBalanceDTO'>
export type ReserveBucketDto = Schema<'ReserveBucketDTO'>
export type ReserveBucketCreateDto = Schema<'ReserveBucketCreateDTO'>
export type ReserveBucketUpdateDto = Schema<'ReserveBucketUpdateDTO'>
export type ReserveMovementDto = Schema<'ReserveMovementDTO'>
export type IncomeSplitRequestDto = Schema<'IncomeSplitRequestDTO'>
export type BucketSplitAmountDto = Schema<'BucketSplitAmountDTO'>
export type IncomeSplitResultDto = Schema<'IncomeSplitResultDTO'>
export type WithdrawalRequestDto = Schema<'WithdrawalRequestDTO'>
export type ReserveMovementUpdateDto = Schema<'ReserveMovementUpdateDTO'>
export type RecurringBillDto = Schema<'RecurringBillDTO'>
export type RecurringBillCreateDto = Schema<'RecurringBillCreateDTO'>
export type RecurringBillUpdateDto = Schema<'RecurringBillUpdateDTO'>
export type RecurringBillStatusUpdateDto = Schema<'RecurringBillStatusUpdateDTO'>
export type MaeLedgerEntryDto = Schema<'MaeLedgerEntryDTO'>
export type MaeLedgerEntryCreateDto = Schema<'MaeLedgerEntryCreateDTO'>
export type MaeLedgerTotalsDto = Schema<'MaeLedgerTotalsDTO'>
export type MaeLedgerEntryValuesUpdateDto = Schema<'MaeLedgerEntryValuesUpdateDTO'>
export type ExpenseDto = Schema<'ExpenseDTO'>
export type ExpenseCreateDto = Schema<'ExpenseCreateDTO'>
export type ExpenseUpdateDto = Schema<'ExpenseUpdateDTO'>
export type BankDto = Schema<'BankDTO'>
export type BankCreateDto = Schema<'BankCreateDTO'>
export type BankUpdateDto = Schema<'BankUpdateDTO'>
export type IncomeSourceDto = Schema<'IncomeSourceDTO'>
export type IncomeSourceCreateDto = Schema<'IncomeSourceCreateDTO'>
export type IncomeSourceUpdateDto = Schema<'IncomeSourceUpdateDTO'>
export type CategoryDto = Schema<'CategoryDTO'>
export type CategoryCreateDto = Schema<'CategoryCreateDTO'>
export type CategoryUpdateDto = Schema<'CategoryUpdateDTO'>
export type BankBalanceDto = Schema<'BankBalanceDTO'>
export type TitheSummaryDto = Schema<'TitheSummaryDTO'>
export type TitheCarryForwardDto = Schema<'TitheCarryForwardDTO'>
export type TitheCarryForwardUpdateDto = Schema<'TitheCarryForwardUpdateDTO'>
export type IncomeDto = Schema<'IncomeDTO'>
export type IncomeCreateDto = Schema<'IncomeCreateDTO'>
export type IncomeUpdateDto = Schema<'IncomeUpdateDTO'>
export type CategoryTotalDto = Schema<'CategoryTotalDTO'>
export type CardStatementDto = Schema<'CardStatementDTO'>
export type CreditCardDto = Schema<'CreditCardDTO'>
export type CreditCardCreateDto = Schema<'CreditCardCreateDTO'>
export type CreditCardUpdateDto = Schema<'CreditCardUpdateDTO'>
export type InvestmentAccountDto = Schema<'InvestmentAccountDTO'>
export type InvestmentAccountCreateDto = Schema<'InvestmentAccountCreateDTO'>
export type InvestmentAccountUpdateDto = Schema<'InvestmentAccountUpdateDTO'>
export type MarkCardStatementPaidDto = Schema<'MarkCardStatementPaidDTO'>
export type CategoryAnnualTotalDto = Schema<'CategoryAnnualTotalDTO'>
export type InvestmentAccountAnnualDiffDto = Schema<'InvestmentAccountAnnualDiffDTO'>
export type NetPositionAnnualDiffDto = Schema<'NetPositionAnnualDiffDTO'>
export type InvestmentAnnualResultDto = Schema<'InvestmentAnnualResultDTO'>
export type IncomeAnnualSummaryDto = Schema<'IncomeAnnualSummaryDTO'>
export type CategoryTotalsAnnualDto = Schema<'CategoryTotalsAnnualDTO'>
export type InvestmentSnapshotDto = Schema<'InvestmentSnapshotDTO'>
export type InvestmentSnapshotValueUpdateDto = Schema<'InvestmentSnapshotValueUpdateDTO'>
export type CategoryAnnualAverageDto = Schema<'CategoryAnnualAverageDTO'>
export type CategoryAverageDto = Schema<'CategoryAverageDTO'>
export type TransferDto = Schema<'TransferDTO'>
export type TransferCreateDto = Schema<'TransferCreateDTO'>
export type TransferUpdateDto = Schema<'TransferUpdateDTO'>
export type BalanceAdjustmentDto = Schema<'BalanceAdjustmentDTO'>
export type BalanceAdjustmentCreateDto = Schema<'BalanceAdjustmentCreateDTO'>
export type BalanceAdjustmentUpdateDto = Schema<'BalanceAdjustmentUpdateDTO'>
export type SyncStatusDto = Schema<'SyncStatusDTO'>
export type SyncStatusResponseDto = Schema<'SyncStatusResponseDTO'>
