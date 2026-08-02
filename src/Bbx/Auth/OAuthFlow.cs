using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bbx.Auth;

public sealed class OAuthFlow
{
    public const string AuthorizeUrl = "https://bitbucket.org/site/oauth2/authorize";
    public const string TokenUrl = "https://bitbucket.org/site/oauth2/access_token";
    public const int DefaultPort = 53682;
    public const string CallbackPath = "/callback";

    private readonly IBrowserLauncher _browser;
    private readonly HttpClient _http;

    public OAuthFlow(IBrowserLauncher browser, HttpClient http)
    {
        _browser = browser;
        _http = http;
    }

    public async Task<OAuthTokenResult> RunAsync(OAuthFlowOptions options, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId))
            throw new BbxUserException("Error: OAuth client_id is required.");
        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            throw new BbxUserException("Error: OAuth client_secret is required.");

        var port = options.Port > 0 ? options.Port : DefaultPort;
        var state = GenerateState();

        // Register both hostnames: the consumer's callback uses "localhost",
        // but tests / some browsers may resolve to 127.0.0.1. HttpListener
        // prefixes match on host header and require a trailing "/", so we
        // bind the root and route by path ourselves.
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex)
        {
            throw new BbxUserException(
                $"Error: Failed to bind loopback listener on port {port}: {ex.Message}");
        }

        var authUrl = BuildAuthorizeUrl(options.ClientId, state);
        if (options.NoBrowser)
        {
            Console.Error.WriteLine("→ Open this URL in your browser to authorize:");
            Console.Error.WriteLine($"  {authUrl}");
        }
        else
        {
            Console.Error.WriteLine("→ Opening browser… (Ctrl+C to cancel)");
            try
            {
                _browser.Launch(authUrl);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  Could not launch browser ({ex.Message}). Open this URL manually:");
                Console.Error.WriteLine($"  {authUrl}");
            }
        }
        Console.Error.WriteLine("→ Waiting for callback…");

        var timeout = options.Timeout > TimeSpan.Zero ? options.Timeout : TimeSpan.FromSeconds(120);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(timeout);

        HttpListenerContext context;
        try
        {
            context = await listener.GetContextAsync().WaitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new BbxUserException(
                $"Error: OAuth callback timed out after {timeout.TotalSeconds:0} seconds.");
        }

        var query = context.Request.QueryString;
        var receivedState = query.Get("state");
        var code = query.Get("code");
        var error = query.Get("error");
        var errorDescription = query.Get("error_description");

        try
        {
            if (!string.IsNullOrEmpty(error))
            {
                await WriteResponseAsync(context.Response, 400,
                    $"<html><body><h1>OAuth error</h1><p>{HtmlEncode(error)}</p></body></html>");
                throw new BbxUserException(
                    $"Error: OAuth provider returned error '{error}'" +
                    (string.IsNullOrEmpty(errorDescription) ? "." : $": {errorDescription}"));
            }

            // Reject mismatched state before accepting the code — defends against CSRF.
            if (string.IsNullOrEmpty(receivedState) || receivedState != state)
            {
                await WriteResponseAsync(context.Response, 400,
                    "<html><body><h1>OAuth state mismatch</h1></body></html>");
                throw new BbxUserException(
                    "Error: OAuth state mismatch — possible CSRF; aborting login.");
            }

            if (string.IsNullOrEmpty(code))
            {
                await WriteResponseAsync(context.Response, 400,
                    "<html><body><h1>OAuth callback missing authorization code</h1></body></html>");
                throw new BbxUserException("Error: OAuth callback missing authorization code.");
            }

            await WriteResponseAsync(context.Response, 200,
                "<html><body><h1>bbx authorized</h1><p>You can close this window and return to the terminal.</p></body></html>");
        }
        finally
        {
            try { listener.Stop(); } catch { }
        }

        return await ExchangeCodeAsync(options.ClientId, options.ClientSecret, code, ct).ConfigureAwait(false);
    }

    public static string BuildAuthorizeUrl(string clientId, string state)
        => $"{AuthorizeUrl}?client_id={Uri.EscapeDataString(clientId)}&response_type=code&state={Uri.EscapeDataString(state)}";

    private async Task<OAuthTokenResult> ExchangeCodeAsync(string clientId, string clientSecret, string code, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
            }),
        };
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new BbxUserException(
                $"Error: OAuth token exchange failed (HTTP {(int)response.StatusCode}): {body}");
        }

        var token = JsonSerializer.Deserialize<TokenResponse>(body)
                    ?? throw new BbxUserException("Error: OAuth token exchange returned an empty response.");
        if (string.IsNullOrEmpty(token.AccessToken))
            throw new BbxUserException("Error: OAuth token exchange response missing access_token.");

        var expiresIn = token.ExpiresIn > 0 ? TimeSpan.FromSeconds(token.ExpiresIn) : TimeSpan.FromHours(2);
        return new OAuthTokenResult
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.Add(expiresIn),
            Scopes = token.Scopes,
            TokenType = token.TokenType,
        };
    }

    private static string GenerateState()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, int status, string body)
    {
        try
        {
            response.StatusCode = status;
            response.ContentType = "text/html; charset=utf-8";
            var bytes = Encoding.UTF8.GetBytes(body);
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
        }
        finally
        {
            response.Close();
        }
    }

    private static string HtmlEncode(string? value)
        => value is null ? string.Empty : System.Net.WebUtility.HtmlEncode(value);

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("scopes")] public string? Scopes { get; set; }
        [JsonPropertyName("token_type")] public string? TokenType { get; set; }
    }
}

public sealed class OAuthFlowOptions
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public int Port { get; init; } = OAuthFlow.DefaultPort;
    public bool NoBrowser { get; init; }
    public string? Scopes { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(120);
}

public sealed class OAuthTokenResult
{
    public required string AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public string? Scopes { get; init; }
    public string? TokenType { get; init; }
}
