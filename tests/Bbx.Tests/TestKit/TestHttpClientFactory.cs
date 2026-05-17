using System.Net.Http.Headers;

namespace Bbx.Tests.TestKit;

internal static class TestHttpClientFactory
{
    public const string BaseUrl = "https://api.bitbucket.org/2.0/";

    public static HttpClient Create(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri(BaseUrl),
        };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("bbx-cli/1.0");
        return client;
    }
}
