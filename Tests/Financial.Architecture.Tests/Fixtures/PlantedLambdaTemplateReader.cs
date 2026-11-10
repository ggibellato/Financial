using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.Architecture.Tests.Fixtures;

internal static class PlantedLambdaTemplateReader
{
    public static int CountWithTemplate(IEnumerable<Account> accounts, AccountTemplate template) =>
        accounts.Count(account => account.Template == template);
}
