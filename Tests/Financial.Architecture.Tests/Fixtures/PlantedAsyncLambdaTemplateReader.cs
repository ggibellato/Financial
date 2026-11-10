using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.Architecture.Tests.Fixtures;

internal static class PlantedAsyncLambdaTemplateReader
{
    public static Func<Task<Account>, Task<bool>> IsBankAsync() => async pending =>
    {
        var account = await pending;
        return account.Template == AccountTemplate.Bank;
    };
}
