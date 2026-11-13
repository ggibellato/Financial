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

export type BankOption = Pick<AccountDto, 'id' | 'name' | 'isActive' | 'roundUpEnabled'>

export type PositionType = Schema<'PositionType'>

export type InvestmentScope = 'active' | 'historic'

export type Currency = 'BRL' | 'GBP' | 'USD'

export type BrokerCurrencyFilter = 'ALL' | Currency

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
  /** For a portfolio: how many assets it holds, so an empty one can be offered for deletion. */
  assetCount?: number
  /** Set only when this selection came from a dashboard deep-link to a specific corporate action; read once by DetailPanel, then not needed again. */
  pendingCorporateActionId?: string
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
export type SetCostBasisMethodRequestDto = Schema<'SetCostBasisMethodRequestDTO'>
export type BrokerUpdateDto = Schema<'BrokerUpdateDTO'>
export type PortfolioDto = Schema<'PortfolioDTO'>
export type PortfolioCreateDto = Schema<'PortfolioCreateDTO'>
export type PortfolioUpdateDto = Schema<'PortfolioUpdateDTO'>
export type TransactionDto = Schema<'TransactionDTO'>
export type TransactionSummaryItemDto = Schema<'TransactionSummaryItemDTO'>
export type CreditDto = Schema<'CreditDTO'>
export type FxRateSnapshotDto = Schema<'FxRateSnapshotDTO'>
export type ReportingCurrencySettingDto = Schema<'ReportingCurrencySettingDTO'>
export type SetReportingCurrencyEnabledRequestDto = Schema<'SetReportingCurrencyEnabledRequestDTO'>
export type AssetPriceSnapshotDto = Schema<'AssetPriceSnapshotDTO'>
export type AssetDetailsDto = Schema<'AssetDetailsDTO'>
export type CostBasisMethod = Schema<'CostBasisMethod'>
export type DisposalRecordDto = Schema<'DisposalRecordDTO'>
export type OpenLotDto = Schema<'OpenLotDTO'>
export type CorporateActionDto = Schema<'CorporateActionDTO'>
export type CorporateActionMergerCreateDto = Schema<'CorporateActionMergerCreateDTO'>
export type CorporateActionMergerUpdateDto = Schema<'CorporateActionMergerUpdateDTO'>
export type CorporateActionMergerResultDto = Schema<'CorporateActionMergerResultDTO'>
export type CorporateActionSplitCreateDto = Schema<'CorporateActionSplitCreateDTO'>
export type CorporateActionSplitUpdateDto = Schema<'CorporateActionSplitUpdateDTO'>
export type CorporateActionSpinOffCreateDto = Schema<'CorporateActionSpinOffCreateDTO'>
export type CorporateActionSpinOffUpdateDto = Schema<'CorporateActionSpinOffUpdateDTO'>
export type CorporateActionSpinOffResultDto = Schema<'CorporateActionSpinOffResultDTO'>
export type CorporateActionDeleteDto = Schema<'CorporateActionDeleteDTO'>
export type ArchiveAssetRequestDto = Schema<'ArchiveAssetRequestDTO'>
export type MoveAssetRequestDto = Schema<'MoveAssetRequestDTO'>
export type AssetAdminDto = Schema<'AssetAdminDTO'>
export type AssetAdminCreateDto = Schema<'AssetAdminCreateDTO'>
export type AssetAdminUpdateDto = Schema<'AssetAdminUpdateDTO'>
export type SpecificLotAllocationDto = Schema<'SpecificLotAllocationDTO'>
export type TransactionCreateDto = Schema<'TransactionCreateDTO'>
export type TransactionUpdateDto = Schema<'TransactionUpdateDTO'>
export type TransactionDeleteDto = Schema<'TransactionDeleteDTO'>
export type TransactionTypeEffectDto = Schema<'TransactionTypeEffectDTO'>
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
export type PortfolioBreakdownItemDto = Schema<'PortfolioBreakdownItemDTO'>
export type WatchlistItemDto = Schema<'WatchlistItemDTO'>
export type PortfolioReferenceDto = Schema<'PortfolioReferenceDTO'>
export type PortfolioAssetSummaryItemDto = Schema<'PortfolioAssetSummaryItemDTO'>
export type PortfolioDashboardDto = Schema<'PortfolioDashboardDTO'>
export type AllocationBreakdownDto = Schema<'AllocationBreakdownDTO'>
export type DataQualityReportDto = Schema<'DataQualityReportDTO'>
export type UpcomingIncomeDto = Schema<'UpcomingIncomeDTO'>
export type PotBalanceDto = Schema<'PotBalanceDTO'>
export type PotsSummaryDto = Schema<'PotsSummaryDTO'>
export type PotUpdateDto = Schema<'PotUpdateDTO'>
export type PotsUpdateDto = Schema<'PotsUpdateDTO'>
export type FundingLineDto = Schema<'FundingLineDTO'>
export type PotDepositRequestDto = Schema<'PotDepositRequestDTO'>
export type PotWithdrawalRequestDto = Schema<'PotWithdrawalRequestDTO'>
export type PotOperationDto = Schema<'PotOperationDTO'>
export type AccountTransactionOperationDto = Schema<'AccountTransactionOperationDTO'>
export type ReserveMovementDto = Schema<'ReserveMovementDTO'>
export type ReserveMovementUpdateDto = Schema<'ReserveMovementUpdateDTO'>
export type RecurringBillDto = Schema<'RecurringBillDTO'>
export type RecurringBillPaymentExpenseDto = Schema<'RecurringBillPaymentExpenseDTO'>
export type RecurringBillCreateDto = Schema<'RecurringBillCreateDTO'>
export type RecurringBillUpdateDto = Schema<'RecurringBillUpdateDTO'>
export type RecurringBillStatusUpdateDto = Schema<'RecurringBillStatusUpdateDTO'>
export type RecurringBillMarkPaidDto = Schema<'RecurringBillMarkPaidDTO'>
export type ExpenseDto = Schema<'ExpenseDTO'>
export type ExpenseCreateDto = Schema<'ExpenseCreateDTO'>
export type ExpenseUpdateDto = Schema<'ExpenseUpdateDTO'>
export type MoveChargeToStatementDto = Schema<'MoveChargeToStatementDTO'>
export type IncomeSourceDto = Schema<'IncomeSourceDTO'>
export type IncomeSourceCreateDto = Schema<'IncomeSourceCreateDTO'>
export type IncomeSourceUpdateDto = Schema<'IncomeSourceUpdateDTO'>
export type CategoryDto = Schema<'CategoryDTO'>
export type CategoryCreateDto = Schema<'CategoryCreateDTO'>
export type CategoryUpdateDto = Schema<'CategoryUpdateDTO'>
export type AccountDto = Schema<'AccountDTO'>
export type AccountTemplateDto = Schema<'AccountTemplateDTO'>
export type AccountCreateDto = Schema<'AccountCreateDTO'>
export type AccountUpdateDto = Schema<'AccountUpdateDTO'>
export type AccountDetailDto = Schema<'AccountDetailDTO'>
export type AccountTransactionDto = Schema<'AccountTransactionDTO'>
export type SettlementEntryDto = Schema<'SettlementEntryDTO'>
export type SettlementEntryCreateDto = Schema<'SettlementEntryCreateDTO'>
export type SettlementEntryUpdateDto = Schema<'SettlementEntryUpdateDTO'>
export type SettleRequestDto = Schema<'SettleRequestDTO'>
export type MonthTransactionDto = Schema<'MonthTransactionDTO'>
export type AccountStatementDto = Schema<'AccountStatementDTO'>
export type StatementDueDateUpdateDto = Schema<'StatementDueDateUpdateDTO'>
export type AccountTransactionStatementDto = Schema<'AccountTransactionStatementDTO'>
export type PayableStatementDto = Schema<'PayableStatementDTO'>
export type AccountTransferDto = Schema<'AccountTransferDTO'>
export type AccountTransferCreateDto = Schema<'AccountTransferCreateDTO'>
export type AccountTransferUpdateDto = Schema<'AccountTransferUpdateDTO'>
export type TransferAccountOptionDto = Schema<'TransferAccountOptionDTO'>
export type AccountGroupSummaryDto = Schema<'AccountGroupSummaryDTO'>
export type AccountGroupMemberDto = Schema<'AccountGroupMemberDTO'>
export type CurrencyTotalDto = Schema<'CurrencyTotalDTO'>
export type AccountTemplateGroupDto = Schema<'AccountTemplateGroupDTO'>
export type IncomeSourceTotalDto = Schema<'IncomeSourceTotalDTO'>
export type TitheSummaryDto = Schema<'TitheSummaryDTO'>
export type TitheMonthDto = Schema<'TitheMonthDTO'>
export type TitheCarryForwardUpdateDto = Schema<'TitheCarryForwardUpdateDTO'>
export type IncomeDto = Schema<'IncomeDTO'>
export type IncomeCreateDto = Schema<'IncomeCreateDTO'>
export type IncomeUpdateDto = Schema<'IncomeUpdateDTO'>
export type CategoryTotalDto = Schema<'CategoryTotalDTO'>
export type CardStatementDto = Schema<'CardStatementDTO'>
export type CardCreditDto = Schema<'CardCreditDTO'>
export type InvestmentAccountDto = Schema<'InvestmentAccountDTO'>
export type InvestmentAccountCreateDto = Schema<'InvestmentAccountCreateDTO'>
export type InvestmentAccountUpdateDto = Schema<'InvestmentAccountUpdateDTO'>
export type MarkCardStatementPaidDto = Schema<'MarkCardStatementPaidDTO'>
export type CategoryAnnualTotalDto = Schema<'CategoryAnnualTotalDTO'>
export type InvestmentAnnualResultDto = Schema<'InvestmentAnnualResultDTO'>
export type IncomeAnnualSummaryDto = Schema<'IncomeAnnualSummaryDTO'>
export type CategoryTotalsAnnualDto = Schema<'CategoryTotalsAnnualDTO'>
export type InvestmentSnapshotDto = Schema<'InvestmentSnapshotDTO'>
export type InvestmentSnapshotValueUpdateDto = Schema<'InvestmentSnapshotValueUpdateDTO'>
export type InvestmentSnapshotSuggestionDto = Schema<'InvestmentSnapshotSuggestionDTO'>
export type InvestmentSnapshotSuggestionSkippedDto = Schema<'InvestmentSnapshotSuggestionSkippedDTO'>
export type InvestmentSnapshotSuggestionsDto = Schema<'InvestmentSnapshotSuggestionsDTO'>
export type CategoryAnnualAverageDto = Schema<'CategoryAnnualAverageDTO'>
export type BalanceAdjustmentDto = Schema<'BalanceAdjustmentDTO'>
export type BalanceAdjustmentCreateDto = Schema<'BalanceAdjustmentCreateDTO'>
export type BalanceAdjustmentUpdateDto = Schema<'BalanceAdjustmentUpdateDTO'>
export type SyncStatusDto = Schema<'SyncStatusDTO'>
export type SyncStatusResponseDto = Schema<'SyncStatusResponseDTO'>
export type PaymentDueDto = Schema<'PaymentDueDTO'>
export type CalendarConnectionStatusDto = Schema<'CalendarConnectionStatusDTO'>
export type CalendarDisconnectResultDto = Schema<'CalendarDisconnectResultDTO'>
export type CreditCardCalendarSyncStatusDto = Schema<'CreditCardCalendarSyncStatusDTO'>
export type TaxWorkbookOptionDto = Schema<'TaxWorkbookOptionDTO'>
export type TaxWorkbookEntryDto = Schema<'TaxWorkbookEntryDTO'>
export type TaxCategoryTotalDto = Schema<'TaxCategoryTotalDTO'>
export type TaxWorkbookDto = Schema<'TaxWorkbookDTO'>
export type TaxRuleDto = Schema<'TaxRuleDTO'>
export type TaxRuleCreateDto = Schema<'TaxRuleCreateDTO'>
export type TaxRuleUpdateDto = Schema<'TaxRuleUpdateDTO'>
