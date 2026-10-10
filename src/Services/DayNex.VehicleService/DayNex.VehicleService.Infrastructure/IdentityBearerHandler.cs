using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace DayNex.VehicleService.Infrastructure;
public sealed class IdentityBearerHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var header = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(header, out var auth) || auth.Scheme != "Bearer")
            throw new InvalidOperationException("An authenticated customer is required.");
        request.Headers.Authorization = auth;
        return base.SendAsync(request, token);
    }
}
