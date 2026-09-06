> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# WPF Views, Components and Controls (`Financial.App/{Views,Components,Controls}/**/*.xaml` + code-behind)

## What to test

- **Binding contracts**: every `DataGridTextColumn Binding="{Binding X}"` in a grid resolves to
  a real property of the row DTO — WPF renders a blank column for a typo with no compile error.
  `ExpenseGridBindingTests` parses `ExpenseSectionView.xaml` and `CreditCardExpensesView.xaml`
  with a regex and checks against `typeof(ExpenseDTO).GetProperties()`; add the same guard for
  any new grid bound to a DTO (`BankOperationRow`, `CardStatementDTO`, …).
- **`Run.Text` bindings**: never `<Run Text="{Binding …}"/>` — defaults to TwoWay and crashes on
  private setters (`feedback_wpf_run_binding`); a regex test over `Views/**` that fails on
  `<Run Text="{Binding` is cheap insurance.
- **Automation names**: icon-only buttons and status icons carry `AutomationProperties.Name`
  (178 uses app-wide; `PaymentDueBanner.xaml` line 25 `"Dismiss upcoming payments"`, line 38
  bound to `UrgencyAccessibleLabel`), plus `AutomationProperties.HelpText` /
  `AutomationProperties.LiveSetting` where used — assertable by parsing the XAML the same way.
- **Numeric alignment**: currency/number columns right-aligned (`feedback_wpf_grid_numeric_alignment`).
- **Code-behind stays thin**: no service calls, no domain logic (`Financial.App/CLAUDE.md`).
  The largest today is `CardsGridView.xaml.cs` at 81 lines; anything beyond event forwarding
  moves into a ViewModel or behavior with its own Unit test.
- **Manual run** per `docs/rules/ui.md` "Completion requirement": launch the built app and look
  at each touched view (light/dark, narrow window, high DPI, keyboard-only path).

## Layer assignment

- **Contract tests (Integration-shaped, single process)** for XAML ↔ DTO agreement: they read
  the real XAML file from the repo (`FindRepoRoot()` walks up to `Financial.slnx`) and reflect
  over the real DTO — no WPF runtime needed.
- **No automated E2E** for WPF: no UI-automation harness exists (no FlaUI/WinAppDriver in any
  csproj). The manual run stands in. If one is added later, click by
  `GetClickablePoint()` from the automation element, never by screenshot-guessed coordinates
  (`feedback_wpf_automation_clickable_point`).
- Behaviour behind the view is Unit-tested in the ViewModel (`wpf-viewmodels.md`).

## Setup pattern

```csharp
[Theory]
[InlineData("ExpenseSectionView.xaml")]
[InlineData("CreditCardExpensesView.xaml")]
public void ExpenseDataGridColumns_BindOnlyToExistingExpenseDTOProperties(string xamlFileName)
{
    var xamlPath = Path.Combine(FindRepoRoot(), "Financial.App", "Views", "CashFlow", xamlFileName);
    File.Exists(xamlPath).Should().BeTrue($"expected to find {xamlPath}");
    var xaml = File.ReadAllText(xamlPath);

    var dataGridMatch = Regex.Match(xaml, @"<DataGrid\s+[^>]*ItemsSource=""\{Binding (Filtered)?(Expenses|UnpaidCardCharges)\}""[\s\S]*?</DataGrid>");
    dataGridMatch.Success.Should().BeTrue($"expected to find the expense DataGrid in {xamlFileName}");

    var expenseDtoProperties = typeof(ExpenseDTO).GetProperties().Select(p => p.Name).ToHashSet();

    var boundProperties = Regex.Matches(dataGridMatch.Value, @"DataGridTextColumn\s+Binding=""\{Binding\s+([A-Za-z0-9_]+)")
        .Select(m => m.Groups[1].Value)
        .ToList();

    boundProperties.Should().NotBeEmpty($"expected at least one DataGridTextColumn binding in {xamlFileName}'s expense grid");
    boundProperties.Should().OnlyContain(
        p => expenseDtoProperties.Contains(p),
        $"every DataGridTextColumn in {xamlFileName}'s expense grid should bind to a real ExpenseDTO property");
}
```

(Verbatim from `Tests/Financial.Presentation.Tests/Views/CashFlow/ExpenseGridBindingTests.cs`.)

## When to skip

- Rendering/layout assertions — WPF's layout engine is framework behaviour and needs a real
  window; the manual run covers it.
- `DialogService` (`Financial.App/Services`) — a `MessageBox`/modal wrapper behind
  `IDialogService`; ViewModels are tested with `StubDialogService`.
- Resource dictionaries and styles in `App.xaml`.

## Examples from project

- `Tests/Financial.Presentation.Tests/Views/CashFlow/ExpenseGridBindingTests.cs` — contract; the only view-level automated test today.
- `Financial.App/Components/PaymentDueBanner.xaml` + `PaymentDueBannerViewModelTests` — the banner's logic is fully in the ViewModel; the XAML carries `AutomationProperties.Name` on the dismiss button and urgency icon for the P42 F03 criteria.
