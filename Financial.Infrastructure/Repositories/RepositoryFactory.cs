using Financial.Application.Interfaces;
using Financial.Infrastructure.Configuration;
using Financial.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Financial.Infrastructure.Repositories;

public sealed class RepositoryFactory
{
    private static readonly Dictionary<RepositoryProvider, Func<RepositorySelectionOptions, IJsonStorage>> StorageFactoryRegistry = new()
    {
        [RepositoryProvider.LocalJson] = opts => new LocalJsonStorage(opts.LocalDataPath),
        [RepositoryProvider.GoogleDriveJson] = opts => new GoogleDriveJsonStorage(opts.GoogleDriveCredentialsPath, opts.GoogleDriveFilePath),
    };

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

    public IRepository CreateFromConfiguration(IConfiguration configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var providerValue = configuration[RepositoryConfigurationKeys.Provider]
            ?? nameof(RepositoryProvider.LocalJson);

        if (!Enum.TryParse(providerValue, true, out RepositoryProvider provider))
        {
            throw new InvalidOperationException(
                $"Repository provider '{providerValue}' is not supported. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<RepositoryProvider>())}.");
        }

        var options = new RepositorySelectionOptions(
            provider,
            configuration[RepositoryConfigurationKeys.LocalJsonDataFile],
            configuration[RepositoryConfigurationKeys.GoogleDriveCredentialsPath],
            configuration[RepositoryConfigurationKeys.GoogleDriveFilePath]);

        return Create(options);
    }

    private static IJsonStorage CreateStorage(RepositorySelectionOptions options)
    {
        if (!StorageFactoryRegistry.TryGetValue(options.Provider, out var factory))
        {
            throw new ArgumentOutOfRangeException(nameof(options.Provider), options.Provider, "Unsupported repository provider.");
        }

        return factory(options);
    }
}
