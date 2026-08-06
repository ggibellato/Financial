using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Reporting;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.SheetImporters;

public class MonthlyExpenseSheetImporterTests
{
    private static readonly DateOnly Today = new(2026, 7, 15);

    private static readonly IReadOnlyCollection<Bank> Banks =
    [
        Bank.Create("Barclays", roundUpEnabled: false),
        Bank.Create("Trading212", roundUpEnabled: true),
        Bank.Create("Chase", roundUpEnabled: true)
    ];

    [Fact]
    public void Import_2017ShapedSheet_QuemThenMotivo_ParsesExpensesCorrectly()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Fev2017");
        // Header row: Dia, Quem, Motivo, Valor (2017 era - description in B, category in C)
        sheet.Cell(1, 1).Value = "Dia";
        sheet.Cell(1, 2).Value = "Quem";
        sheet.Cell(1, 3).Value = "Motivo";
        sheet.Cell(1, 4).Value = "Valor";

        sheet.Cell(2, 1).Value = 1;
        sheet.Cell(2, 2).Value = "Lidl UK";
        sheet.Cell(2, 3).Value = "Mercado";
        sheet.Cell(2, 4).Value = 71.04;

        sheet.Cell(3, 1).Value = 3;
        sheet.Cell(3, 2).Value = "Amazon Digital Video";
        sheet.Cell(3, 3).Value = "Extras";
        sheet.Cell(3, 4).Value = 9.99;
        sheet.Cell(3, 5).Value = "T";

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2017, 2, Today, report, Banks);

        expenses.Should().HaveCount(2);
        var first = expenses.Single(e => e.Description == "Lidl UK");
        first.Category.Should().Be(Category.Mercado);
        first.Value.Should().Be(71.04m);
        first.Date.Should().Be(new DateOnly(2017, 2, 1));
        first.PaymentSourceBank!.Name.Should().Be("Barclays");

        var second = expenses.Single(e => e.Description == "Amazon Digital Video");
        second.Category.Should().Be(Category.Extras);
        second.PaymentSourceBank!.Name.Should().Be("Trading212");
        report.RowIssues.Should().BeEmpty();
    }

    [Fact]
    public void Import_2026ShapedSheet_MotivoThenQuem_ParsesExpensesCorrectly()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Jul2026");
        // Header row: Dia, Motivo, Quem, Valor (2019+ era - description in B, category in C, same content order)
        sheet.Cell(1, 1).Value = "Dia";
        sheet.Cell(1, 2).Value = "Motivo";
        sheet.Cell(1, 3).Value = "Quem";
        sheet.Cell(1, 4).Value = "Valor";

        sheet.Cell(2, 1).Value = 1;
        sheet.Cell(2, 2).Value = "Chartered Society";
        sheet.Cell(2, 3).Value = "Ariana";
        sheet.Cell(2, 4).Value = 39.17;
        sheet.Cell(2, 5).Value = "C";

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2026, 7, Today, report, Banks);

        expenses.Should().ContainSingle();
        var expense = expenses.Single();
        expense.Description.Should().Be("Chartered Society");
        expense.Category.Should().Be(Category.Ariana);
        expense.PaymentSourceBank!.Name.Should().Be("Chase");
    }

    [Fact]
    public void Import_UnrecognizedCategory_SkipsExpenseAndFlagsRow()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Out2017");
        sheet.Cell(1, 1).Value = "Dia";
        sheet.Cell(1, 2).Value = "Quem";
        sheet.Cell(1, 3).Value = "Motivo";
        sheet.Cell(1, 4).Value = "Valor";

        sheet.Cell(2, 1).Value = 5;
        sheet.Cell(2, 2).Value = "Some Store";
        sheet.Cell(2, 3).Value = "TotallyUnknownCategory";
        sheet.Cell(2, 4).Value = 10.0;

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2017, 10, Today, report, Banks);

        expenses.Should().BeEmpty();
        report.RowIssues.Should().ContainSingle(i => i.RawValue == "TotallyUnknownCategory" && i.SheetName == "Out2017");
    }

    [Fact]
    public void Import_KnownTypoCasas_ResolvesToCasaCategory()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Out2017");
        sheet.Cell(1, 1).Value = "Dia";
        sheet.Cell(1, 2).Value = "Quem";
        sheet.Cell(1, 3).Value = "Motivo";
        sheet.Cell(1, 4).Value = "Valor";

        sheet.Cell(2, 1).Value = 5;
        sheet.Cell(2, 2).Value = "Some Store";
        sheet.Cell(2, 3).Value = "Casas";
        sheet.Cell(2, 4).Value = 10.0;

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2017, 10, Today, report, Banks);

        expenses.Should().ContainSingle().Which.Category.Should().Be(Category.Casa);
        report.RowIssues.Should().BeEmpty();
    }

    [Fact]
    public void Import_DayDoesNotExistInMonth_ClampsToLastValidDayAndFlagsRowWithoutDroppingTheExpense()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Fev2026");
        sheet.Cell(1, 1).Value = "Dia";
        sheet.Cell(1, 2).Value = "Motivo";
        sheet.Cell(1, 3).Value = "Quem";
        sheet.Cell(1, 4).Value = "Valor";

        // 2026 is not a leap year - February has only 28 days, but the sheet records a real
        // transaction under day 29 (a genuine data-entry slip in the source spreadsheet).
        sheet.Cell(2, 1).Value = 29;
        sheet.Cell(2, 2).Value = "Oxford Dental";
        sheet.Cell(2, 3).Value = "Ariana";
        sheet.Cell(2, 4).Value = 130.0;

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2026, 2, Today, report, Banks);

        expenses.Should().ContainSingle();
        expenses[0].Date.Should().Be(new DateOnly(2026, 2, 28));
        expenses[0].Value.Should().Be(130.0m);
        report.RowIssues.Should().ContainSingle(i => i.SheetName == "Fev2026" && i.RawValue == "29");
    }

    [Fact]
    public void Import_ValueEnteredAsTextWithCommaDecimalSeparator_ParsesAsDecimalNotInflated100x()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Mar2017");
        sheet.Cell(1, 1).Value = "Dia";
        sheet.Cell(1, 2).Value = "Motivo";
        sheet.Cell(1, 3).Value = "Quem";
        sheet.Cell(1, 4).Value = "Valor";

        // Confirmed real-workbook data quality issue (Mar2017, cell D82): the value was entered as
        // text "17,28" (comma decimal) instead of a genuine Excel number.
        sheet.Cell(2, 1).Value = 30;
        sheet.Cell(2, 2).Value = "THE RANGE";
        sheet.Cell(2, 3).Value = "Casa";
        sheet.Cell(2, 4).Value = "17,28";

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2017, 3, Today, report, Banks);

        expenses.Should().ContainSingle().Which.Value.Should().Be(17.28m);
        report.RowIssues.Should().BeEmpty();
    }

    [Theory]
    [InlineData(128, null)]
    [InlineData(129, CreditCard.BarclaysPlatinumVisa8003)]
    [InlineData(141, CreditCard.BarclaysPlatinumVisa8003)]
    [InlineData(142, CreditCard.BarclaysPlatinumVisa6007)]
    [InlineData(204, CreditCard.BarclaysPlatinumVisa6007)]
    [InlineData(205, CreditCard.ChaseMaster4023)]
    [InlineData(225, CreditCard.ChaseMaster4023)]
    [InlineData(226, CreditCard.BaAmex)]
    [InlineData(300, CreditCard.BaAmex)]
    public void Import_FixedCardSectionMonth_BlankPaymentSourceTag_SetsCardTagByRowPosition(int row, CreditCard? expectedCardTag)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Jul2026");
        WriteExpenseRow(sheet, row, paymentSourceTag: null);

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2026, 7, Today, report, Banks);

        var expense = expenses.Should().ContainSingle().Which;
        expense.CardTag.Should().Be(expectedCardTag);
        if (expectedCardTag is null)
        {
            expense.PaymentSourceBank!.Name.Should().Be("Barclays");
        }
        else
        {
            expense.PaymentSourceBank.Should().BeNull();
        }
    }

    [Fact]
    public void Import_CreditCardRow_SetsChargeDateEqualToImportedRowDate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Ago2026");
        WriteExpenseRow(sheet, row: 150, paymentSourceTag: null);

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2026, 8, Today, report, Banks);

        var expense = expenses.Should().ContainSingle().Subject;
        expense.CardTag.Should().NotBeNull();
        expense.ChargeDate.Should().Be(expense.Date);
        expense.ChargeDate.Should().Be(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public void Import_CreditCardRow_DefaultsInvoiceDateToFirstOfChargeMonth()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Ago2026");
        WriteExpenseRow(sheet, row: 150, paymentSourceTag: null);

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2026, 8, Today, report, Banks);

        var expense = expenses.Should().ContainSingle().Subject;
        expense.InvoiceDate.Should().Be(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public void Import_BankExpenseRow_ChargeDateAndInvoiceDateAreNull()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Ago2026");
        WriteExpenseRow(sheet, row: 150, paymentSourceTag: "C");

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2026, 8, Today, report, Banks);

        var expense = expenses.Should().ContainSingle().Subject;
        expense.CardTag.Should().BeNull();
        expense.ChargeDate.Should().BeNull();
        expense.InvoiceDate.Should().BeNull();
    }

    [Fact]
    public void Import_FixedCardSectionMonth_ExplicitPaymentSourceTagInCardRow_TakesPrecedenceOverRowPosition()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Ago2026");
        WriteExpenseRow(sheet, row: 150, paymentSourceTag: "C");

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2026, 8, Today, report, Banks);

        var expense = expenses.Should().ContainSingle().Subject;
        expense.PaymentSourceBank!.Name.Should().Be("Chase");
        expense.CardTag.Should().BeNull();
    }

    [Fact]
    public void Import_MixedSheet_NoExpenseEverCarriesBothPaymentSourceAndCardTag()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Jul2026");
        WriteExpenseRow(sheet, row: 10, paymentSourceTag: null);
        WriteExpenseRow(sheet, row: 11, paymentSourceTag: "T");
        WriteExpenseRow(sheet, row: 130, paymentSourceTag: null);
        WriteExpenseRow(sheet, row: 150, paymentSourceTag: "C");
        WriteExpenseRow(sheet, row: 210, paymentSourceTag: null);
        WriteExpenseRow(sheet, row: 230, paymentSourceTag: null);

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2026, 7, Today, report, Banks);

        expenses.Should().HaveCount(6);
        expenses.Should().NotContain(e => e.PaymentSourceBank != null && e.CardTag != null);
        expenses.Should().OnlyContain(e =>
            e.PaymentStatus == ExpensePaymentStatus.ImmediatePayment
            || e.PaymentStatus == ExpensePaymentStatus.CreditCardCharge);
        expenses.Count(e => e.PaymentStatus == ExpensePaymentStatus.CreditCardCharge).Should().Be(3);
    }

    [Fact]
    public void Import_PastMonth_BlankPaymentSourceInCardRowRange_CardTagStaysNullAndDefaultsToBarclays()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Out2017");
        WriteExpenseRow(sheet, row: 129, paymentSourceTag: null);

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2017, 10, Today, report, Banks);

        var expense = expenses.Should().ContainSingle().Which;
        expense.CardTag.Should().BeNull();
        expense.PaymentSourceBank!.Name.Should().Be("Barclays");
    }

    [Fact]
    public void Import_FutureMonthBeyondAnyPreviouslyConfirmedSheet_BlankPaymentSourceInCardRowRange_SetsCardTagByRowPosition()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Set2026");
        WriteExpenseRow(sheet, row: 129, paymentSourceTag: null);

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2026, 9, Today, report, Banks);

        var expense = expenses.Should().ContainSingle().Which;
        expense.CardTag.Should().Be(CreditCard.BarclaysPlatinumVisa8003);
        expense.PaymentSourceBank.Should().BeNull();
    }

    [Theory]
    [InlineData("X")]
    [InlineData("x")]
    [InlineData(" X ")]
    public void Import_FixedCardSectionMonth_CreditCardMarkerTag_SetsCardTagByRowPositionNotBarclays(string tag)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Jul2026");
        WriteExpenseRow(sheet, row: 150, paymentSourceTag: tag);

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2026, 7, Today, report, Banks);

        var expense = expenses.Should().ContainSingle().Which;
        expense.CardTag.Should().Be(CreditCard.BarclaysPlatinumVisa6007);
        expense.PaymentSourceBank.Should().BeNull();
        expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
        report.RowIssues.Should().BeEmpty();
    }

    [Fact]
    public void Import_CurrentMonth_CreditCardMarkerTagOutsideAnyCardSection_DefaultsToBarclaysWithoutFlagging()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Jul2026");
        // Row 50 is before the first card section (BarclaysPlatinumVisa8003StartRow = 129), so an
        // "X" here can't be matched to any card - it falls back to the normal column-E default.
        WriteExpenseRow(sheet, row: 50, paymentSourceTag: "X");

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2026, 7, Today, report, Banks);

        var expense = expenses.Should().ContainSingle().Which;
        expense.CardTag.Should().BeNull();
        expense.PaymentSourceBank!.Name.Should().Be("Barclays");
        report.RowIssues.Should().BeEmpty();
    }

    [Fact]
    public void Import_PastMonth_CreditCardMarkerTagInCardRowRange_ImportsAsBarclaysMovementNotACharge()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Out2017");
        WriteExpenseRow(sheet, row: 129, paymentSourceTag: "X");

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2017, 10, Today, report, Banks);

        var expense = expenses.Should().ContainSingle().Which;
        expense.CardTag.Should().BeNull();
        expense.PaymentSourceBank!.Name.Should().Be("Barclays");
        report.RowIssues.Should().BeEmpty();
    }

    [Fact]
    public void Import_FutureMonthBeyondAnyPreviouslyConfirmedSheet_CreditCardMarkerTag_SetsCardTagByRowPosition()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Set2026");
        WriteExpenseRow(sheet, row: 129, paymentSourceTag: "X");

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2026, 9, Today, report, Banks);

        var expense = expenses.Should().ContainSingle().Which;
        expense.CardTag.Should().Be(CreditCard.BarclaysPlatinumVisa8003);
        expense.PaymentSourceBank.Should().BeNull();
        report.RowIssues.Should().BeEmpty();
    }

    private static void WriteExpenseRow(IXLWorksheet sheet, int row, string? paymentSourceTag)
    {
        sheet.Cell(1, 1).Value = "Dia";
        sheet.Cell(1, 2).Value = "Motivo";
        sheet.Cell(1, 3).Value = "Quem";
        sheet.Cell(1, 4).Value = "Valor";

        sheet.Cell(row, 1).Value = 1;
        sheet.Cell(row, 2).Value = "Test Charge";
        sheet.Cell(row, 3).Value = "Casa";
        sheet.Cell(row, 4).Value = 10.0;
        if (paymentSourceTag is not null)
        {
            sheet.Cell(row, 5).Value = paymentSourceTag;
        }
    }
}
