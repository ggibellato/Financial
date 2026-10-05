using Financial.App.E2ETests.Infrastructure;
using FlaUI.Core.AutomationElements;
using FluentAssertions;

namespace Financial.App.E2ETests.Smoke;

[Trait("Category", "E2E")]
[Trait("Category", "Smoke")]
public class AddExpenseTests
{
    [Fact]
    public void AddExpense_AppearsInMonthlyList()
    {
        AppSession.Run(nameof(AddExpense_AppearsInMonthlyList), session =>
        {
            var window = session.Window;
            var description = $"e2e-{Guid.NewGuid():N}";

            FillExpenseForm(window, description);
            window.FindById("expense-form-value").AsTextBox().Text = "12.34";
            window.FindById("expense-form-save").Press();

            window.FindById("monthly-expenses-grid").FindByName(description);
        });
    }

    [Fact]
    public void AddExpense_BlankValue_ShowsFieldErrorAndAddsNothing()
    {
        AppSession.Run(nameof(AddExpense_BlankValue_ShowsFieldErrorAndAddsNothing), session =>
        {
            var window = session.Window;
            var description = $"e2e-blank-{Guid.NewGuid():N}";

            FillExpenseForm(window, description);
            window.FindById("expense-form-save").Press();

            window.FindById("expense-form-value-error").Name.Should().Be("Value must be a non-zero number.");
            window.FindById("expense-form-save");
            window.FindById("monthly-expenses-grid").HasName(description).Should().BeFalse();
        });
    }

    private static void FillExpenseForm(Window window, string description)
    {
        window.OpenMonthly("Bank expenses");
        window.FindById("monthly-new-expense").Press();
        window.FindById("expense-form-description").AsTextBox().Text = description;
        window.FindById("expense-form-category").AsComboBox().Select("Mercado");
        window.FindById("expense-form-payment-source").AsComboBox().Select("Barclays");
    }
}
