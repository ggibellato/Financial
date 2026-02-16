using System;
using System.IO;

namespace Financial.Infrastructure.Tests;

internal static class TestDataPaths
{
    public static string DataJsonPath =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "data.test.json");
}
