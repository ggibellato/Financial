using FinancialModel.Application;
using System;

namespace FinancialModel.Infrastructure;

public sealed class RepositoryFactory : IRepositoryFactory
{
    public IRepository Create(RepositorySelectionOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return options.Provider switch
        {
            RepositoryProvider.LocalJson => new LocalJSONRepository(options.LocalDataPath),
            RepositoryProvider.GoogleDriveJson => new GoogleDriveJSONRepository(options.GoogleDriveCredentialsPath, options.GoogleDriveFilePath),
            _ => throw new ArgumentOutOfRangeException(nameof(options.Provider), options.Provider, "Unsupported repository provider.")
        };
    }
}
