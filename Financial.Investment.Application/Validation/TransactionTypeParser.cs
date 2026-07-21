using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.Validation;

public static class TransactionTypeParser
{
    public static bool TryNormalize(string? value, out string normalized) =>
        EnumParser.TryNormalize<Transaction.TransactionType>(value, out normalized);

    public static bool TryParse(string? value, out Transaction.TransactionType transactionType) =>
        EnumParser.TryParseEnum(value, out transactionType);
}
