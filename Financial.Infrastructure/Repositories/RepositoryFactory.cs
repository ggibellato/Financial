using Financial.Application.Interfaces;
using Financial.Infrastructure.Persistence;
using System;

namespace Financial.Infrastructure.Repositories;

public sealed class RepositoryFactory
{
    private readonly IInvestmentsSerializer _serializer;

    public RepositoryFactory(IInvestmentsSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public IRepository Create(RepositorySelectionOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var storage = CreateStorage(options);
        var investments = InvestmentsLoader.Load(storage, _serializer);
        return new JSONRepository(investments, storage, _serializer);
    }

    private static IJsonStorage CreateStorage(RepositorySelectionOptions options) =>
        options.Provider switch
        {
            RepositoryProvider.LocalJson => new LocalJsonStorage(options.LocalDataPath),
            RepositoryProvider.GoogleDriveJson => new GoogleDriveJsonStorage(options.GoogleDriveCredentialsPath, options.GoogleDriveFilePath),
            _ => throw new ArgumentOutOfRangeException(nameof(options.Provider), options.Provider, "Unsupported repository provider.")
        };
}
