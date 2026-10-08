using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using DayNex.Contracts.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DayNex.Shared.Http.Authorization;

public sealed class PermissionServiceUnavailableException : Exception
{
    public PermissionServiceUnavailableException(Exception? inner = null)
        : base("The permission service is temporarily unavailable.", inner) { }
}

public sealed class IdentityPermissionClient(HttpClient client, IHttpContextAccessor accessor) : IPermissionSource
{
    public async Task<UserPermissionsDto?> GetAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var header = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(header, out var authorization) ||
            !string.Equals(authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
            return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/identity/me/permissions");
        request.Headers.Authorization = authorization;
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserPermissionsDto>(cancellationToken)
                ?? throw new PermissionServiceUnavailableException();
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new PermissionServiceUnavailableException(ex);
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
        {
            throw new PermissionServiceUnavailableException(ex);
        }
    }
}

public static class IdentityPermissionDependencies
{
    public static IServiceCollection AddIdentityPermissionClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentityServiceOptions>().Bind(configuration.GetSection("IdentityService"))
            .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)),
                "IdentityService:BaseUrl must use HTTPS (HTTP is allowed only for local development).")
            .ValidateOnStart();
        services.AddHttpClient<IPermissionSource, IdentityPermissionClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityServiceOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(5);
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
        return services;
    }
}

public sealed class IdentityServiceOptions
{
    public string BaseUrl { get; set; } = "";
}
