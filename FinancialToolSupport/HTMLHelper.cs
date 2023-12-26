using System.Net.Http;
using System;
using System.Threading.Tasks;

namespace FinancialToolSupport;

public static class HTMLHelper
{
    public async static Task<string> LoadPage(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Exception: {ex.Message}");
            }
        }
    }
}
