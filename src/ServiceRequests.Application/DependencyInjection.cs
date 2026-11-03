using System.Globalization;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceRequests.Application.Requests.Commands;
using ServiceRequests.Application.Requests.Querying;
using ServiceRequests.Application.Requests.Summary;

namespace ServiceRequests.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var summaryTtl = TimeSpan.FromSeconds(ReadPositiveInt(configuration, "Cache:SummaryTtlSeconds", 60));

        services.AddHybridCache();
        services.AddSingleton(sp => new SummaryCache(sp.GetRequiredService<HybridCache>(), summaryTtl));
        return services.AddScoped<RequestQueryService>().AddScoped<RequestSummaryService>().AddScoped<RequestStatusService>();
    }

    // Fails at startup on a bad value instead of silently using the default
    private static int ReadPositiveInt(IConfiguration configuration, string key, int defaultValue) =>
        configuration[key] switch
        {
            null => defaultValue,
            var text when int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0 => value,
            _ => throw new InvalidOperationException($"'{key}' must be a positive whole number."),
        };
}
