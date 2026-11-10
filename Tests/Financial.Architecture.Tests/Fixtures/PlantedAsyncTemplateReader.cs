using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.Architecture.Tests.Fixtures;

internal static class PlantedAsyncTemplateReader
{
    public static async Task<bool> IsBankAsync(Task<Account> pending)
    {
        var account = await pending;
        return account.Template == AccountTemplate.Bank;
    }
}
