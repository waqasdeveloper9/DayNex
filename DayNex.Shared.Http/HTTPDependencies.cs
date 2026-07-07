using DayNex.Shared.Http.Interface;
using Microsoft.Extensions.DependencyInjection;
namespace DayNex.Shared.Http
{
    public static class HTTPDependencies
    {
        public static IServiceCollection AddHttpService(this IServiceCollection services)
        {
            services.AddHttpClient<IApiClient, ApiClient>();
            return services;
        }
    }
}
