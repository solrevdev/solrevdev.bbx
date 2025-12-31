using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Bbx.Auth;

namespace Bbx.Api;

public class BitbucketClient : IDisposable
{
    private readonly HttpClient _client;
    private const string BaseUrl = "https://api.bitbucket.org/2.0";

    public BitbucketClient(string? accessToken = null, string? appPassword = null, string? username = null)
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("bbx-cli/1.0");

        if (!string.IsNullOrEmpty(accessToken))
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        else if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(appPassword))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{appPassword}"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
    }

    public BitbucketClient(BbxConfig config) : this(config.AccessToken, config.AppPassword, config.Username)
    {
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        var url = endpoint.StartsWith("http") ? endpoint : endpoint;
        var response = await _client.GetAsync(url, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<string> GetStringAsync(string endpoint, CancellationToken ct = default)
    {
        var response = await _client.GetAsync(endpoint, ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> GetRawAsync(string endpoint, CancellationToken ct = default)
    {
        var response = await _client.GetAsync(endpoint, ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? body = null, CancellationToken ct = default)
    {
        var content = body != null
            ? new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
            : null;
        var response = await _client.PostAsync(endpoint, content, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<T?> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content, CancellationToken ct = default)
    {
        var response = await _client.PostAsync(endpoint, content, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<T?> PutAsync<T>(string endpoint, object body, CancellationToken ct = default)
    {
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        var response = await _client.PutAsync(endpoint, content, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<T?> PutMultipartAsync<T>(string endpoint, MultipartFormDataContent content, CancellationToken ct = default)
    {
        var response = await _client.PutAsync(endpoint, content, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        var response = await _client.DeleteAsync(endpoint, ct);
        await EnsureSuccessAsync(response);
    }

    public async IAsyncEnumerable<T> GetPaginatedAsync<T>(string endpoint, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var url = endpoint;
        while (!string.IsNullOrEmpty(url))
        {
            var response = await GetAsync<PaginatedResponse<T>>(url, ct);
            if (response?.Values == null) yield break;

            foreach (var item in response.Values)
            {
                yield return item;
            }

            url = response.Next;
            if (!string.IsNullOrEmpty(url) && url.StartsWith("http"))
            {
                // Extract relative path for subsequent requests
                url = new Uri(url).PathAndQuery;
            }
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var errorMessage = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";

            try
            {
                var error = JsonSerializer.Deserialize<BitbucketError>(content, JsonOptions);
                if (error?.Error?.Message != null)
                {
                    errorMessage = error.Error.Message;
                }
            }
            catch
            {
                // Ignore JSON parse errors
            }

            throw new HttpRequestException(errorMessage);
        }
    }

    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public void Dispose() => _client.Dispose();
}

public class PaginatedResponse<T>
{
    public int Size { get; set; }
    public int Page { get; set; }
    public int Pagelen { get; set; }
    public string? Next { get; set; }
    public string? Previous { get; set; }
    public List<T> Values { get; set; } = [];
}

public class BitbucketError
{
    public BitbucketErrorDetail? Error { get; set; }
}

public class BitbucketErrorDetail
{
    public string? Message { get; set; }
    public string? Detail { get; set; }
    public string? Id { get; set; }
}
