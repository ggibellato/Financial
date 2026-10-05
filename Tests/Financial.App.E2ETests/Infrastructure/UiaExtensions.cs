using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;

namespace Financial.App.E2ETests.Infrastructure;

internal static class UiaExtensions
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    public static AutomationElement FindById(this AutomationElement root, string automationId, TimeSpan? timeout = null)
    {
        var found = Retry.WhileNull(
            () => root.FindFirstDescendant(root.ConditionFactory.ByAutomationId(automationId)),
            timeout ?? DefaultTimeout,
            ignoreException: true).Result;

        return found ?? throw new InvalidOperationException($"AutomationId '{automationId}' not found in window '{root.Name}'.");
    }

    public static AutomationElement FindByNameStartingWith(this AutomationElement root, ControlType controlType, string namePrefix, TimeSpan? timeout = null)
    {
        var found = Retry.WhileNull(
            () => root.FindAllDescendants(root.ConditionFactory.ByControlType(controlType))
                .FirstOrDefault(element => element.Name?.StartsWith(namePrefix, StringComparison.Ordinal) == true),
            timeout ?? DefaultTimeout,
            ignoreException: true).Result;

        return found ?? throw new InvalidOperationException($"{controlType} named '{namePrefix}...' not found in window '{root.Name}'.");
    }

    public static void Press(this AutomationElement element) => element.AsButton().Invoke();
}
