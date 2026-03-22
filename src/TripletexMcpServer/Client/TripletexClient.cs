using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TripletexMcpServer.Client;

/// <summary>
/// HTTP client for Tripletex API calls.
/// Credentials are passed per-call from tool parameters.
/// </summary>
public static class TripletexClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static HttpClient CreateClient(string baseUrl, string sessionToken)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"0:{sessionToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public static async Task<string> GetAsync(string baseUrl, string sessionToken, string path,
        Dictionary<string, string>? query = null)
    {
        using var client = CreateClient(baseUrl, sessionToken);
        var url = BuildPath(path, query);
        var response = await client.GetAsync(url);
        return await ReadAsync(response);
    }

    public static async Task<string> PostAsync(string baseUrl, string sessionToken, string path, object body)
    {
        using var client = CreateClient(baseUrl, sessionToken);
        var json = JsonSerializer.Serialize(body, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(path.TrimStart('/'), content);
        return await ReadAsync(response);
    }

    public static async Task<string> PutAsync(string baseUrl, string sessionToken, string path, object body,
        Dictionary<string, string>? query = null)
    {
        using var client = CreateClient(baseUrl, sessionToken);
        var url = BuildPath(path, query);
        var json = JsonSerializer.Serialize(body, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PutAsync(url, content);
        return await ReadAsync(response);
    }

    private static string BuildPath(string path, Dictionary<string, string>? query)
    {
        var p = path.TrimStart('/');
        if (query is { Count: > 0 })
        {
            var qs = string.Join("&", query
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            if (!string.IsNullOrEmpty(qs))
                p += "?" + qs;
        }
        return p;
    }

    private static async Task<string> ReadAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            return JsonSerializer.Serialize(new
            {
                error = true,
                statusCode = (int)response.StatusCode,
                message = $"HTTP {(int)response.StatusCode}: {body}"
            });
        }
        return body;
    }
}
