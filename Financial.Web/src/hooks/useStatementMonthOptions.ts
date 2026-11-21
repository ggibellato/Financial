import { apiClient } from '../api/financialApiClient'
import { statementMonthOptions } from '../utils/statementMonths'
import { useAsyncResource } from './useAsyncResource'
import { STATEMENTS_LOAD_ERROR } from './useCardStatements'

export function useStatementMonthOptions(cardId: string, date: string, keptMonth: string) {
  const statements = useAsyncResource(() => (cardId ? apiClient.getAccountStatements(cardId) : null), [cardId], STATEMENTS_LOAD_ERROR)

  return {
    options: cardId && statements.data ? statementMonthOptions(date, statements.data, keptMonth) : [],
    isLoading: statements.isLoading,
    error: statements.error,
    retry: statements.retry,
  }
}
