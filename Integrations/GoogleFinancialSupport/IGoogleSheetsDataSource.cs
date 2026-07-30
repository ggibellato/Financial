using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;

internal interface IGoogleSheetsDataSource
{
    Task<IList<IList<object>>> GetSpreadSheetDataAsync(string spreadSheetId, string range);
}
