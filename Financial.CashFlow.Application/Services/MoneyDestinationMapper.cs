using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.Shared.Abstractions.Validation;

namespace Financial.CashFlow.Application.Services;

internal sealed record RequestedDestination(MoneyDestination Destination, Account? Target);

internal static class MoneyDestinationMapper
{
    private const string UnknownKindMessage = "Choose where the money goes.";

    internal static RequestedDestination? ToRequested(MoneyDestinationDTO? dto, IEnumerable<Account> accounts)
    {
        if (dto is null)
        {
            return null;
        }

        if (!EnumParser.TryParseEnum<MoneyDestinationKind>(dto.Kind, out var kind) || !Enum.IsDefined(kind) || int.TryParse(dto.Kind, out _))
        {
            throw new ArgumentException(UnknownKindMessage);
        }

        return kind switch
        {
            MoneyDestinationKind.None => new(MoneyDestination.None, null),
            MoneyDestinationKind.InvestedAutomatically => new(MoneyDestination.InvestedAutomatically, null),
            MoneyDestinationKind.Account when dto.AccountId is { } accountId =>
                new(MoneyDestination.ToAccount(accountId), CashFlowReferenceResolver.ResolveAccount(accounts, accountId)),
            _ => throw new ArgumentException(UnknownKindMessage)
        };
    }

    internal static MoneyDestinationDTO? ToDto(MoneyDestination? destination, IEnumerable<Account> accounts) => destination is null
        ? null
        : new MoneyDestinationDTO
        {
            Kind = destination.Kind.ToString(),
            AccountId = destination.AccountId,
            AccountName = destination.AccountId is { } id ? accounts.FirstOrDefault(account => account.Id == id)?.Name : null
        };
}
