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

        return options.Provider switch
        {
            RepositoryProvider.LocalJson => new JSONRepository(new LocalJsonStorage(options.LocalDataPath), _serializer),
            RepositoryProvider.GoogleDriveJson => new JSONRepository(new GoogleDriveJsonStorage(options.GoogleDriveCredentialsPath, options.GoogleDriveFilePath), _serializer),
            _ => throw new ArgumentOutOfRangeException(nameof(options.Provider), options.Provider, "Unsupported repository provider.")
        };
    }
}
