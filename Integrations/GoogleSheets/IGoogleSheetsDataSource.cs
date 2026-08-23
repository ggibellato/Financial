using Financial.Integrations.GoogleSheets.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financial.Integrations.GoogleSheets;

public interface IGoogleSheetsDataSource
{
    Task<List<SheetDTO>> GetSpreadSheetAsync(string spreadSheetId);

    Task<IList<IList<object>>> GetSpreadSheetDataAsync(string spreadSheetId, string range);
}
