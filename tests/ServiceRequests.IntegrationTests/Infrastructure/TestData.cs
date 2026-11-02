using ServiceRequests.Domain.Requests;

namespace ServiceRequests.IntegrationTests.Infrastructure;

/// <summary>Creates valid rows with sensible defaults, so each test states only what it cares about.</summary>
internal static class TestData
{
    public static async Task<ServiceRequest> AddRequestAsync(ApiFactory factory, Action<ServiceRequest>? configure = null)
    {
        var request = new ServiceRequest("Test request", "Test organization", RequestPriority.Medium, RequestStatus.New, assignedTo: null, DateTime.UtcNow);
        configure?.Invoke(request);
        await factory.ExecuteDbAsync(async db =>
        {
            db.Requests.Add(request);
            await db.SaveChangesAsync();
        });
        return request;
    }
}
