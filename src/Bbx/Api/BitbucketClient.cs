using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Bbx.Auth;

namespace Bbx.Api;

public class BitbucketClient : IDisposable
{
    private readonly HttpClient _client;

    /// <summary>
    /// Held here rather than on <see cref="HttpClient.DefaultRequestHeaders"/>
    /// so a request can deliberately go out without it. A default header cannot
    /// be suppressed per request, which would leak credentials to a redirect
    /// target on another origin.
    /// </summary>
    private readonly AuthenticationHeaderValue? _authorization;

    private const string BaseUrl = "https://api.bitbucket.org/2.0/";

    public BitbucketClient(string? accessToken = null, string? appPassword = null, string? username = null)
    {
        // Redirects are followed in-process (see GetAsync) so credentials can be
        // re-applied; HttpClient drops them when it follows one itself.
        _client = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("bbx-cli/1.0");

        if (!string.IsNullOrEmpty(accessToken))
        {
            _authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        else if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(appPassword))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{appPassword}"));
            _authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

    }

    public BitbucketClient(BbxConfig config) : this(config.AccessToken, config.AppPassword, config.Username)
    {
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        var response = await GetAsync(endpoint, accept: null, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<string> GetStringAsync(string endpoint, CancellationToken ct = default)
    {
        var response = await GetAsync(endpoint, AnyMediaType, ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> GetRawAsync(string endpoint, CancellationToken ct = default)
    {
        var response = await GetAsync(endpoint, AnyMediaType, ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsStringAsync(ct);
    }

    /// <summary>
    /// Accept value for endpoints that do not serve JSON. The client sends
    /// <c>Accept: application/json</c> by default, which some endpoints reject
    /// with HTTP 406 rather than falling back to their native type. The pipeline
    /// step log endpoint (<c>application/octet-stream</c>) is one of them.
    /// </summary>
    private const string AnyMediaType = "*/*";

    private const int MaxRedirects = 5;

    /// <summary>
    /// GET, following redirects in-process so credentials survive them.
    /// </summary>
    /// <remarks>
    /// HttpClient drops the Authorization header when it follows a redirect,
    /// which turns an authenticated request into an anonymous one. The pull
    /// request diff and patch endpoints 302 to the underlying commit-range
    /// diff, so they failed with "You may not have access to this repository".
    /// Credentials are re-applied only on the same origin, so a redirect out to
    /// storage cannot leak them.
    /// </remarks>
    private async Task<HttpResponseMessage> GetAsync(string endpoint, string? accept, CancellationToken ct)
    {
        var current = new Uri(_client.BaseAddress!, NormalizeEndpoint(endpoint));
        var response = await SendGetAsync(current, accept, applyAuth: true, ct);

        var hops = 0;
        while (IsRedirect(response) && hops++ < MaxRedirects)
        {
            var location = response.Headers.Location;
            if (location is null) break;

            var target = location.IsAbsoluteUri ? location : new Uri(current, location);
            response.Dispose();

            var sameOrigin = Uri.Compare(target, current, UriComponents.SchemeAndServer,
                UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) == 0;

            current = target;
            response = await SendGetAsync(target, accept, sameOrigin, ct);
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendGetAsync(
        Uri url, string? accept, bool applyAuth, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (accept is not null)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
        }
        if (applyAuth)
        {
            request.Headers.Authorization = _authorization;
        }
        return await _client.SendAsync(request, ct);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string endpoint, HttpContent? content, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, NormalizeEndpoint(endpoint))
        {
            Content = content,
        };
        request.Headers.Authorization = _authorization;
        return await _client.SendAsync(request, ct);
    }

    private static bool IsRedirect(HttpResponseMessage response) => (int)response.StatusCode switch
    {
        301 or 302 or 303 or 307 or 308 => true,
        _ => false,
    };

    public async Task<T?> PostAsync<T>(string endpoint, object? body = null, CancellationToken ct = default)
    {
        var content = body != null
            ? new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
            : null;
        var response = await SendAsync(HttpMethod.Post, endpoint, content, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<T?> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content, CancellationToken ct = default)
    {
        var response = await SendAsync(HttpMethod.Post, endpoint, content, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<T?> PutAsync<T>(string endpoint, object body, CancellationToken ct = default)
    {
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        var response = await SendAsync(HttpMethod.Put, endpoint, content, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<T?> PutMultipartAsync<T>(string endpoint, MultipartFormDataContent content, CancellationToken ct = default)
    {
        var response = await SendAsync(HttpMethod.Put, endpoint, content, ct);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        var response = await SendAsync(HttpMethod.Delete, endpoint, null, ct);
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
                // unknown user selector with just the selector ("solrevdev"),
                // which reads as noise without the 404.
                errorMessage = $"{error!.Error!.Message} ({status})";

                // A 403 names the scopes the token is missing. Say which, so the
                // fix is "re-issue the token with these" rather than guesswork.
                var missing = error.Error.MissingScopes().ToList();
                if (missing.Count > 0)
                {
                    errorMessage += $" Missing token scopes: {string.Join(", ", missing)}.";
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
