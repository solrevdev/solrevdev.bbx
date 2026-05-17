using System.Net;

namespace Bbx.Tests.TestKit;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responders = new();
    public List<HttpRequestMessage> Calls { get; } = new();

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

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls.Add(request);
        if (_responders.Count == 0)
            throw new InvalidOperationException($"No scripted response for {request.Method} {request.RequestUri}");
        return Task.FromResult(_responders.Dequeue()(request));
    }
}
