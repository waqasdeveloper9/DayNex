using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace DayNex.VehicleService.Infrastructure;

public sealed class MotAuthorizationHandler(
    MotAccessTokenProvider tokens, IOptions<MotOptions> options) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            await tokens.GetAsync(cancellationToken));
        request.Headers.Remove("X-API-Key");
        request.Headers.Add("X-API-Key", options.Value.ApiKey);
        return await base.SendAsync(request, cancellationToken);
    }
}
