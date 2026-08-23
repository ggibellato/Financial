using Financial.Integrations.GoogleDrive.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financial.Integrations.GoogleDrive;

/// <summary>
/// Lists the spreadsheet files a Drive account holds. Consumed by the Investment spreadsheet
/// importer, which is why this sits alongside - but separate from - the storage contract:
/// listing files is not part of the swappable storage contract.
/// </summary>
public interface IGoogleDriveFileSource
{
    Task<List<SpreadSheetDTO>> GetFilesAsync();
}
