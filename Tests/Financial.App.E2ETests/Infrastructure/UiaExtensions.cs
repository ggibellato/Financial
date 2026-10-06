using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;

namespace Financial.App.E2ETests.Infrastructure;

internal static class UiaExtensions
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    public static AutomationElement FindById(this AutomationElement root, string automationId) =>
        Find(root, () => root.FindFirstDescendant(root.ConditionFactory.ByAutomationId(automationId)), $"AutomationId '{automationId}'");

    public static AutomationElement FindByIdNamed(this AutomationElement root, string automationId, string name) =>
        Find(
            root,
            () => root.FindFirstDescendant(root.ConditionFactory.ByAutomationId(automationId)) is { } element && element.Name == name
                ? element
                : null,
            $"AutomationId '{automationId}' named '{name}'");

    public static AutomationElement FindByName(this AutomationElement root, string name) =>
        Find(root, () => root.FindFirstDescendant(root.ConditionFactory.ByName(name)), $"element named '{name}'");

    public static AutomationElement FindByNameStartingWith(this AutomationElement root, ControlType controlType, string namePrefix) =>
        Find(
            root,
            () => root.FindAllDescendants(root.ConditionFactory.ByControlType(controlType))
                .FirstOrDefault(element => element.Name?.StartsWith(namePrefix, StringComparison.Ordinal) == true),
            $"{controlType} named '{namePrefix}...'");

    public static bool HasName(this AutomationElement root, string name) =>
        root.FindFirstDescendant(root.ConditionFactory.ByName(name)) is not null;

    public static void OpenMonthly(this AutomationElement window, string tab)
    {
        window.FindById("nav-monthly").Press();
        window.FindByName(tab).AsTabItem().Select();
    }

    public static void ExpandTreeItem(this AutomationElement root, string namePrefix) =>
        root.FindByNameStartingWith(ControlType.TreeItem, namePrefix).Patterns.ExpandCollapse.Pattern.Expand();

    public static void Press(this AutomationElement element) => element.AsButton().Invoke();

    private static AutomationElement Find(AutomationElement root, Func<AutomationElement?> lookup, string description) =>
        Retry.WhileNull(lookup, DefaultTimeout, ignoreException: true).Result
            ?? throw new InvalidOperationException($"{description} not found in window '{root.Name}'.");
}
