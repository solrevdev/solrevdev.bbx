using System.Net;

namespace Bbx.Tests.TestKit;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responders = new();
    public List<HttpRequestMessage> Calls { get; } = new();
    public List<string?> CallBodies { get; } = new();

    public void Enqueue(HttpStatusCode status, string body, string contentType = "application/json")
    {
        _responders.Enqueue(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, contentType),
        });
    }

    public void EnqueueResponder(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responders.Enqueue(responder);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // HttpClient hands the token straight to its handler without checking it
        // first, so a fake that ignores it would answer a request a real one
        // would have refused.
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add(request);
        string? body = null;
        if (request.Content is not null)
        {
            body = await request.Content.ReadAsStringAsync(cancellationToken);
        }
        CallBodies.Add(body);
        if (_responders.Count == 0)
            throw new InvalidOperationException($"No scripted response for {request.Method} {request.RequestUri}");
        return _responders.Dequeue()(request);
    }
}
