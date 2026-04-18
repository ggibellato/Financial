using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Financial.Api.Tests;

internal static class ApiTestFactory
{
    public static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["Repository:Provider"] = "LocalJson",
                        ["DataJsonFile"] = TestDataPaths.DataJsonFile
                    };
                    config.AddInMemoryCollection(settings);
                });
            });
    }
}
