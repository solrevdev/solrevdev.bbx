using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Bbx.Auth;

namespace Bbx.Api;

public class BitbucketClient : IDisposable
{
    private readonly HttpClient _client;
    private readonly IAuthProvider _auth;

    public BitbucketClient(HttpClient client, IAuthProvider auth)
    {
        _client = client;
        _auth = auth;
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
        using var response = await SendAsync(HttpMethod.Get, endpoint, null, ct, AnyMediaType);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> GetRawAsync(string endpoint, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Get, endpoint, null, ct, AnyMediaType);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<byte[]> GetByteArrayAsync(string endpoint, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Get, endpoint, null, ct, AnyMediaType);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsByteArrayAsync(ct);
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
        }
    }

    /// <summary>
    /// Accept value for endpoints that do not serve JSON. The client sends
    /// <c>Accept: application/json</c> as a default header, which some endpoints
    /// reject with HTTP 406 rather than falling back to their native type. The
    /// pipeline step log endpoint (<c>application/octet-stream</c>) is one.
    /// Setting Accept on the request suppresses the default for that call.
    /// </summary>
    private const string AnyMediaType = "*/*";

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string endpoint,
        HttpContent? content,
        CancellationToken ct,
        string? accept = null)
    {
        var request = new HttpRequestMessage(method, NormalizeEndpoint(endpoint))
        {
            Content = content,
        };
        if (accept is not null)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
        }
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

    public void Dispose()
    {
        // The HttpClient is owned by the DI container (singleton); do not
        // dispose it here.
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
