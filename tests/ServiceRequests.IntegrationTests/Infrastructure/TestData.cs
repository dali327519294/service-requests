using ServiceRequests.Domain.Requests;

namespace ServiceRequests.IntegrationTests.Infrastructure;

/// <summary>Creates valid rows with sensible defaults, so each test states only what it cares about.</summary>
internal static class TestData
{
    public static async Task<ServiceRequest> AddRequestAsync(ApiFactory factory, string title = "Test request",
        string organizationName = "Test organization", RequestStatus status = RequestStatus.New,
        RequestPriority priority = RequestPriority.Medium, string? assignedTo = null, DateTime? createdAt = null)
    {
        var request = new ServiceRequest(title, organizationName, priority, status, assignedTo, createdAt ?? DateTime.UtcNow);
        await factory.ExecuteDbAsync(async db =>
        {
            db.Requests.Add(request);
            await db.SaveChangesAsync();
        });
        return request;
    }
}
