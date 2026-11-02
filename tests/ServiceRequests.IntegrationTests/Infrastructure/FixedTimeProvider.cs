namespace ServiceRequests.IntegrationTests.Infrastructure;

/// <summary>A clock that always returns the same moment, so time-based results are repeatable.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
