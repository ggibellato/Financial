using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface IDataQualityReportService
{
    DataQualityReportDTO GenerateReport();
}
