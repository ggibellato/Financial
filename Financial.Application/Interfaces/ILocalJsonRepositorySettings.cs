namespace Financial.Application.Interfaces;

public interface ILocalJsonRepositorySettings : IRepositorySettings
{
    string? DataJsonFile { get; }
}
