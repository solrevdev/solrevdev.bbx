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
        // A PUT that succeeds with 204 has no body. Deserializing "" throws, which
        // is how `bbx snippet watch` failed against its 204.
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
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

    private const int MaxRedirects = 5;

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string endpoint,
        HttpContent? content,
        CancellationToken ct,
        string? accept = null)
    {
        var current = new Uri(_client.BaseAddress!, NormalizeEndpoint(endpoint));
        var response = await SendOnceAsync(method, current.AbsoluteUri, content, ct, accept);

        // Redirects are followed here rather than by HttpClient because
        // HttpClient drops the Authorization header when it follows one, which
        // turns an authenticated request into an anonymous one. The pull request
        // diff and patch endpoints 302 to the underlying commit-range diff, so
        // they failed with "You may not have access to this repository".
        // Only GET is followed: other verbs would need their body re-sent, and
        // no Bitbucket endpoint we call redirects them.
        var hops = 0;
        while (IsRedirect(response) && method == HttpMethod.Get && hops++ < MaxRedirects)
        {
            var location = response.Headers.Location;
            if (location is null) break;

            var target = location.IsAbsoluteUri ? location : new Uri(current, location);
            response.Dispose();

            // Re-apply credentials only when staying on the same origin, so a
            // redirect out to storage (downloads) cannot leak them.
            var sameOrigin = Uri.Compare(target, current, UriComponents.SchemeAndServer,
                UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) == 0;

            current = target;
            response = await SendOnceAsync(HttpMethod.Get, target.AbsoluteUri, null, ct, accept, applyAuth: sameOrigin);
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        HttpMethod method,
        string url,
        HttpContent? content,
        CancellationToken ct,
        string? accept,
        bool applyAuth = true)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = content,
        };
        if (accept is not null)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
        }
        if (applyAuth)
        {
            await _auth.ApplyAsync(request, ct);
        }
        return await _client.SendAsync(request, ct);
    }

    private static bool IsRedirect(HttpResponseMessage response) => (int)response.StatusCode switch
    {
        301 or 302 or 303 or 307 or 308 => true,
        _ => false,
    };

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var status = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
        var content = await response.Content.ReadAsStringAsync();
        var errorMessage = status;

        try
        {
            var error = JsonSerializer.Deserialize<BitbucketError>(content, JsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.Error?.Message))
            {
                // Keep the status alongside the API text. Bitbucket answers an
                // unknown username with just the username ("solrevdev"), which
                // reads as noise without the 404.
                errorMessage = $"{error!.Error!.Message} ({status})";

                // A 403 names the scopes the token is missing. Say which, so the
                // fix is "re-issue the token with these" rather than guesswork.
                var missing = error.Error.MissingScopes().ToList();
                if (missing.Count > 0)
                {
                    errorMessage += $" Missing token scopes: {string.Join(", ", missing)}."
                        + " Re-issue your token with those scopes at"
                        + " https://id.atlassian.com/manage-profile/security/api-tokens";
                }

                // Deprecation errors carry the changelog entry that explains them.
                var announcement = error.Error.Data?.AnnouncementUrl;
                if (!string.IsNullOrWhiteSpace(announcement))
                {
                    errorMessage += $" See {announcement}";
                }
            }
        }
        catch (JsonException)
        {
        }

        throw new HttpRequestException(errorMessage);
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

    /// <summary>
    /// Free-form. A string for most errors, but an object carrying
    /// <c>required</c> and <c>granted</c> arrays for scope failures, so this
    /// cannot be typed as a string: doing so made the whole payload fail to
    /// deserialize and reduced every 403 to a bare "HTTP 403 Forbidden".
    /// </summary>
    public JsonElement? Detail { get; set; }

    public string? Id { get; set; }
    public BitbucketErrorData? Data { get; set; }

    /// <summary>
    /// The scopes a 403 says the credentials are missing, if it named any.
    /// </summary>
    public IEnumerable<string> MissingScopes()
    {
        if (Detail is not { ValueKind: JsonValueKind.Object } detail
            || !detail.TryGetProperty("required", out var required)
            || required.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var granted = detail.TryGetProperty("granted", out var g) && g.ValueKind == JsonValueKind.Array
            ? g.EnumerateArray().Select(x => x.GetString()).ToHashSet()
            : [];

        return required.EnumerateArray()
            .Select(x => x.GetString())
            .Where(x => x is not null && !granted.Contains(x))
            .Select(x => x!)
            .ToList();
    }
}

public class BitbucketErrorData
{
    public string? AnnouncementUrl { get; set; }
}
