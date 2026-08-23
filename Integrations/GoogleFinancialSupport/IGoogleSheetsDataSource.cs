using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financial.Integrations.GoogleFinancialSupport;

internal interface IGoogleSheetsDataSource
{
    Task<IList<IList<object>>> GetSpreadSheetDataAsync(string spreadSheetId, string range);
}
