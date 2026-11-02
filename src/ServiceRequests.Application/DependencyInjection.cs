using Microsoft.Extensions.DependencyInjection;
using ServiceRequests.Application.Requests.Querying;

namespace ServiceRequests.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services.AddScoped<RequestQueryService>();
}
