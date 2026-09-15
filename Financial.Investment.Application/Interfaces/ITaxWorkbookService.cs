using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface ITaxWorkbookService
{
    IReadOnlyList<TaxWorkbookOptionDTO> GetWorkbookOptions();

    TaxWorkbookDTO GetWorkbook(string jurisdiction, string taxYear);
}
