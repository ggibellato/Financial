using System;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Domain.Rules;

public sealed record PotWithdrawalCard(Account Card, DateOnly StatementMonth);
