using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Bbx.Auth;

namespace Bbx.Api;

public class BitbucketClient : IDisposable
{
    private const string BaseUrl = "https://api.bitbucket.org/2.0/";

    private readonly HttpClient _client;
    private readonly IAuthProvider _auth;
    private readonly bool _ownsClient;

    public BitbucketClient(HttpClient client, IAuthProvider auth)
    {
        _client = client;
        _auth = auth;
        _ownsClient = false;
    }

    public BitbucketClient(BbxConfig config)
    {
        _client = CreateDefaultHttpClient();
        _auth = ResolveAuth(config);
        _ownsClient = true;
    }

    public BitbucketClient(string? accessToken = null, string? appPassword = null, string? username = null)
    {
        _client = CreateDefaultHttpClient();
        _auth = ResolveAuth(accessToken, appPassword, username);
        _ownsClient = true;
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Get, endpoint, null, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<string> GetStringAsync(string endpoint, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Get, endpoint, null, ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> GetRawAsync(string endpoint, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Get, endpoint, null, ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? body = null, CancellationToken ct = default)
    {
        var content = body != null
            ? new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
            : null;
        using var response = await SendAsync(HttpMethod.Post, endpoint, content, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<T?> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Post, endpoint, content, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<T?> PutAsync<T>(string endpoint, object body, CancellationToken ct = default)
    {
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await SendAsync(HttpMethod.Put, endpoint, content, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<T?> PutMultipartAsync<T>(string endpoint, MultipartFormDataContent content, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Put, endpoint, content, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, endpoint, null, ct);
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
                url = new Uri(url).PathAndQuery;
            }
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string endpoint,
        HttpContent? content,
        CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, NormalizeEndpoint(endpoint))
        {
            Content = content,
        };
        await _auth.ApplyAsync(request, ct);
        return await _client.SendAsync(request, ct);
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

    private static string NormalizeEndpoint(string endpoint)
    {
        if (endpoint.StartsWith("http")) return endpoint;
        return endpoint.TrimStart('/');
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30),
        };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("bbx-cli/1.0");
        return client;
    }

    private static IAuthProvider ResolveAuth(BbxConfig config)
    {
        if (!string.IsNullOrEmpty(config.Username))
        {
            var secret = config.ApiToken ?? config.AppPassword;
            if (!string.IsNullOrEmpty(secret))
            {
                return new BasicAuthProvider(config.Username, secret);
            }
        }
        return new NullAuthProvider();
    }

    private static IAuthProvider ResolveAuth(string? accessToken, string? appPassword, string? username)
    {
        // Bearer access tokens land in Phase 1 with OAuthAuthProvider; no
        // CLI flow today produces a config with AccessToken set, so the
        // accessToken parameter is preserved for ABI compatibility only.
        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(appPassword))
        {
            return new BasicAuthProvider(username, appPassword);
        }
        return new NullAuthProvider();
    }

    public void Dispose()
    {
        if (_ownsClient) _client.Dispose();
    }
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
