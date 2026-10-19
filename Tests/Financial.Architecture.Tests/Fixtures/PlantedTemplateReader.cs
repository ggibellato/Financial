using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.Architecture.Tests.Fixtures;

internal static class PlantedTemplateReader
{
    public static bool IsBank(Account account) => account.Template == AccountTemplate.Bank;
}
