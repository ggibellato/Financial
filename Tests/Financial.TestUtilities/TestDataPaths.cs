namespace Financial.TestUtilities;

public static class TestDataPaths
{
    public static string DataJsonFile =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "data.test.json");
}
