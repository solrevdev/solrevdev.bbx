using System.Text.Json;
using Bbx.Api;

namespace Bbx.Features.Users.ViewUserEmail;

public sealed class ViewUserEmailHandler(BitbucketClient client)
{
    public async Task<JsonElement> HandleAsync(ViewUserEmailRequest request, CancellationToken ct) =>
        await client.GetAsync<JsonElement>($"user/emails/{Uri.EscapeDataString(request.Email)}", ct);
}
