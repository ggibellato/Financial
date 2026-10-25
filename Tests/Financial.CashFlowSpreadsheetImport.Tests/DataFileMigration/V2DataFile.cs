namespace Financial.CashFlowSpreadsheetImport.Tests.DataFileMigration;

internal static class V2DataFile
{
    public const string BarclaysId = "5db5c88d-0000-0000-0000-000000000001";
    public const string Visa8003Id = "5db5c88d-0000-0000-0000-000000000501";
    public const string Visa6007Id = "5db5c88d-0000-0000-0000-000000000502";
    public const string ChaseMasterId = "5db5c88d-0000-0000-0000-000000000503";
    public const string AmexId = "5db5c88d-0000-0000-0000-000000000504";
    public const string PaypalId = "5db5c88d-0000-0000-0000-000000000505";
    public const string Visa8003OctoberStatementId = "5db5c88d-0000-0000-0000-000000000401";
    public const string AmexOctoberStatementId = "5db5c88d-0000-0000-0000-000000000402";
    public const string UnpaidChargeId = "e0000000-0000-0000-0000-000000000001";
    public const string InvestmentAccountId = "5db5c88d-0000-0000-0000-000000000601";

    private const string CategoryId = "c0000000-0000-0000-0000-000000000001";

    public static string Json => Build(Visa8003Id, Visa8003Id, Visa8003Id);

    public static string WithExpenseCard(string cardId) => Build(cardId, Visa8003Id, Visa8003Id);

    public static string WithStatementCard(string cardId) => Build(Visa8003Id, cardId, Visa8003Id);

    public static string WithInvestmentAccountCard(string cardId) => Build(Visa8003Id, Visa8003Id, cardId);

    private static string Build(string expenseCardId, string statementCardId, string investmentAccountCardId) => $$"""
        {
          "ReserveBuckets": [],
          "Expenses": [
            { "Id": "{{UnpaidChargeId}}", "Date": "2026-10-03", "Description": "Taxi", "Value": 30, "CategoryId": "{{CategoryId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{expenseCardId}}", "ChargeDate": "2026-10-03", "InvoiceDate": "2026-10-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "e0000000-0000-0000-0000-000000000002", "Date": "2026-09-20", "Description": "Settled one", "Value": 10, "CategoryId": "{{CategoryId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": "{{Visa6007Id}}", "ChargeDate": "2026-09-02", "InvoiceDate": "2026-09-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "e0000000-0000-0000-0000-000000000003", "Date": "2026-09-20", "Description": "Settled two", "Value": 15, "CategoryId": "{{CategoryId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": "{{Visa6007Id}}", "ChargeDate": "2026-09-04", "InvoiceDate": "2026-09-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "e0000000-0000-0000-0000-000000000004", "Date": "2026-08-20", "Description": "Settled August", "Value": 20, "CategoryId": "{{CategoryId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": "{{Visa6007Id}}", "ChargeDate": "2026-08-02", "InvoiceDate": "2026-08-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "e0000000-0000-0000-0000-000000000005", "Date": "2026-08-05", "Description": "Unpaid August", "Value": 25, "CategoryId": "{{CategoryId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{Visa6007Id}}", "ChargeDate": "2026-08-05", "InvoiceDate": "2026-08-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "e0000000-0000-0000-0000-000000000006", "Date": "2026-10-06", "Description": "Amex shop", "Value": 40, "CategoryId": "{{CategoryId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-10-06", "InvoiceDate": "2026-10-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "e0000000-0000-0000-0000-000000000007", "Date": "2026-10-07", "Description": "Coffee", "Value": 3, "CategoryId": "{{CategoryId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": null, "CountsAsTithe": true }
          ],
          "ReserveMovements": [],
          "CardStatements": [
            { "Id": "{{Visa8003OctoberStatementId}}", "CreditCardId": "{{statementCardId}}", "Year": 2026, "Month": 10, "IsPaid": false },
            { "Id": "{{AmexOctoberStatementId}}", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 10, "IsPaid": false }
          ],
          "RecurringBills": [], "MaeLedgerEntries": [], "InvestmentSnapshots": [],
          "InvestmentAccounts": [
            { "Id": "{{InvestmentAccountId}}", "Name": "Visa balance", "IsActive": true, "IsLiability": true, "Source": "CreditCard", "CreditCardId": "{{investmentAccountCardId}}" }
          ],
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 100, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } }
          ],
          "IncomeSources": [], "Incomes": [],
          "CreditCards": [
            { "Id": "{{Visa8003Id}}", "Name": "Platinum Visa 8003", "IsActive": true, "NextInvoiceDueDate": "2026-11-18" },
            { "Id": "{{Visa6007Id}}", "Name": "Platinum Visa 6007", "IsActive": true, "NextInvoiceDueDate": null },
            { "Id": "{{ChaseMasterId}}", "Name": "Chase Master 4023", "IsActive": false, "NextInvoiceDueDate": null },
            { "Id": "{{AmexId}}", "Name": "BA Amex", "IsActive": true, "NextInvoiceDueDate": "2026-10-25" },
            { "Id": "{{PaypalId}}", "Name": "Paypal Credit", "IsActive": true, "NextInvoiceDueDate": null }
          ],
          "Categories": [{ "Id": "{{CategoryId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }],
          "Transfers": [], "BalanceAdjustments": [], "TitheCarryForwards": [],
          "TitheCarryForwardEffectiveFrom": null,
          "Version": 2
        }
        """;
}
