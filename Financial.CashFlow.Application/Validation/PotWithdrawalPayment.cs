using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Application.Validation;

internal sealed record PotWithdrawalPayment(Account? Bank, PotWithdrawalCard? Card, Category? Category);
