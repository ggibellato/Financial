namespace Financial.Application.Interfaces;

public interface ILocalJsonRepositoryDiagnostics : IRepositoryDiagnostics
{
    string? DataJsonFile { get; }
}
