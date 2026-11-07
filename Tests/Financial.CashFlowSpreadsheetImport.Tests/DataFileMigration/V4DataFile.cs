namespace Financial.CashFlowSpreadsheetImport.Tests.DataFileMigration;

internal static class V4DataFile
{
    public const string BarclaysId = "7b0f0000-0000-0000-0000-000000000001";
    public const string PaidByYouId = "7b0f0000-0000-0000-0000-000000000101";
    public const string PaidByMaeId = "7b0f0000-0000-0000-0000-000000000102";
    public const string GbpEntryId = "7b0f0000-0000-0000-0000-000000000103";
    public const string PoundEntryId = "7b0f0000-0000-0000-0000-000000000104";
    public const string WholePoundsBrlEntryId = "7b0f0000-0000-0000-0000-000000000105";

    public const string Entries = $$"""
        [
          { "Id": "{{PaidByYouId}}", "Date": "2026-07-01", "Description": "School supplies", "Note": "Term start", "SourceCurrency": "BRL", "BrlValue": 350, "GbpValue": 51.23 },
          { "Id": "{{PaidByMaeId}}", "Date": "2026-07-02", "Description": "Insurance", "SourceCurrency": "BRL", "BrlValue": -186.1234, "GbpValue": null },
          { "Id": "{{GbpEntryId}}", "Date": "2026-07-03", "Description": "Doctor", "Note": "", "SourceCurrency": "GBP", "BrlValue": -300.5, "GbpValue": -40.12345678 },
          { "Id": "{{PoundEntryId}}", "Date": "2022-11-25", "Description": "Presente Samuel 100 libras", "Note": "libras", "SourceCurrency": "BRL", "BrlValue": 654.24, "GbpValue": 100 },
          { "Id": "{{WholePoundsBrlEntryId}}", "Date": "2023-01-10", "Description": "Transfer", "Note": "", "SourceCurrency": "BRL", "BrlValue": 600, "GbpValue": 100 }
        ]
        """;

    public const string Json = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 100, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null, "CategoryId": null }
          ],
          "MaeLedgerEntries": {{Entries}},
          "Version": 4
        }
        """;

    public const decimal TotalBrl = 350m - 186.1234m - 300.5m + 654.24m + 600m;
    public const decimal TotalGbp = 51.23m - 40.12345678m + 100m + 100m;
}
