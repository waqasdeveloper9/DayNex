using System.Net;
using System.Net.Http.Headers;

namespace DayNex.UserInfoService.Infrastructure;

/// <summary>GOV.UK resolves postcode requests by redirecting to a canonical council resource.</summary>
public sealed class GovUkCouncilRedirectHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var response = await base.SendAsync(request, token);
        if (response.StatusCode is not (HttpStatusCode.MovedPermanently or HttpStatusCode.Redirect or
            HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect))
            return response;

        var location = response.Headers.Location;
        if (location is null || !Uri.TryCreate(request.RequestUri, location.ToString(), out var target) ||
            target.Scheme != "https" || target.Host != "www.gov.uk" || !target.IsDefaultPort ||
            target.UserInfo.Length != 0 || target.Query.Length != 0 || target.Fragment.Length != 0 ||
            !target.AbsolutePath.StartsWith("/api/local-authority/", StringComparison.Ordinal))
        {
            response.Dispose();
            throw new HttpRequestException("The council provider returned an unsupported redirect.", null, HttpStatusCode.BadGateway);
        }

        response.Dispose();
        // Follow exactly one public, same-host canonical GET; never copy an authorization header.
        using var canonical = new HttpRequestMessage(HttpMethod.Get, target);
        canonical.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return await base.SendAsync(canonical, token);
    }
}
