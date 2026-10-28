namespace Financial.CashFlowSpreadsheetImport.Tests.DataFileMigration;

internal static class V1DataFile
{
    public const string BarclaysId = "5db5c88d-0000-0000-0000-000000000001";
    public const string Trading212Id = "5db5c88d-0000-0000-0000-000000000002";
    public const string ChaseId = "5db5c88d-0000-0000-0000-000000000003";
    public const string CardExpenseId = "e0000000-0000-0000-0000-000000000001";
    public const string BankExpenseId = "e0000000-0000-0000-0000-000000000002";
    public const string IncomeId = "e0000000-0000-0000-0000-000000000003";
    public const string TransferId = "e0000000-0000-0000-0000-000000000004";
    public const string AdjustmentId = "e0000000-0000-0000-0000-000000000005";

    private const string CategoryId = "c0000000-0000-0000-0000-000000000001";
    private const string ReservaCategoryId = "c0000000-0000-0000-0000-000000000002";
    private const string IncomeSourceId = "c0000000-0000-0000-0000-000000000002";

    public static string WithBankReference(string bankId) => Build(bankId);

    public static string Json => Build(ChaseId);

    private static string Build(string expenseBankId) => $$"""
        {
          "ReserveBuckets": [],
          "Expenses": [
            { "Id": "{{CardExpenseId}}", "Date": "2026-09-01", "Description": "Card", "Value": 10, "CategoryId": "{{CategoryId}}", "PaymentSourceBankId": null, "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "{{BankExpenseId}}", "Date": "2026-09-02", "Description": "Coffee", "Value": 2.30, "CategoryId": "{{CategoryId}}", "PaymentSourceBankId": "{{expenseBankId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": 0.70, "CountsAsTithe": true }
          ],
          "ReserveMovements": [], "CardStatements": [], "RecurringBills": [], "MaeLedgerEntries": [],
          "InvestmentSnapshots": [], "InvestmentAccounts": [],
          "Banks": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "RoundUpEnabled": false, "OpeningBalance": 0, "OpeningBalanceDate": "2026-09-01" },
            { "Id": "{{Trading212Id}}", "Name": "Trading212", "RoundUpEnabled": true },
            { "Id": "{{ChaseId}}", "Name": "Chase", "RoundUpEnabled": true, "OpeningBalance": 1250.70, "OpeningBalanceDate": "2026-09-01" }
          ],
          "IncomeSources": [{ "Id": "{{IncomeSourceId}}", "Name": "Gleison", "IsActive": true, "Group": "Salary", "AutoSplitToReserve": false }],
          "Incomes": [{ "Id": "{{IncomeId}}", "Date": "2026-09-01", "IncomeSourceId": "{{IncomeSourceId}}", "GrossValue": null, "NetValue": 174.27, "BankId": "{{BarclaysId}}", "Description": null, "SplitToReserve": false }],
          "CreditCards": [],
          "Categories": [{ "Id": "{{CategoryId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }, { "Id": "{{ReservaCategoryId}}", "Name": "Reserva", "Active": true, "IsInvestment": false, "IsTithe": false }],
          "Transfers": [{ "Id": "{{TransferId}}", "Date": "2026-09-04", "SourceBankId": "{{BarclaysId}}", "DestinationBankId": "{{Trading212Id}}", "Amount": 171.89, "Note": null }],
          "BalanceAdjustments": [{ "Id": "{{AdjustmentId}}", "Date": "2026-09-01", "BankId": "{{BarclaysId}}", "TargetBalance": 130.83, "Delta": 289.18, "Note": "Start" }],
          "TitheCarryForwards": [],
          "TitheCarryForwardEffectiveFrom": null,
          "Version": 1
        }
        """;
}
