using System;
using System.IO;

namespace Financial.Api.Tests;

internal static class TestDataPaths
{
    public static string DataJsonFile =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "data.test.json");
}
