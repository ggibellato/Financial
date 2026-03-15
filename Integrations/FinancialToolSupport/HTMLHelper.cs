using System.Net.Http;
using System;
using System.Threading.Tasks;

namespace Financial.Infrastructure.Integrations.FinancialToolSupport;

public static class HTMLHelper
{
    private static readonly HttpClient HttpClient = new HttpClient();

    public static async Task<string> LoadPage(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Url must be provided.", nameof(url));
        }

        using var response = await HttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}

