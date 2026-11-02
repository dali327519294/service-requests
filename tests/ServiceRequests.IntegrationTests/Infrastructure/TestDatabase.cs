using Microsoft.Data.SqlClient;

namespace ServiceRequests.IntegrationTests.Infrastructure;

/// <summary>Builds the connection string for a throwaway database, one per test class.</summary>
internal static class TestDatabase
{
    // CI points this at its SQL Server container; locally LocalDB is used
    private const string ServerVariable = "SERVICEREQUESTS_TEST_SQL";
    private const string LocalDb = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True";

    public static string CreateConnectionString() =>
        new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(ServerVariable) ?? LocalDb)
        {
            InitialCatalog = $"ServiceRequests_Test_{Guid.NewGuid():N}",
        }.ConnectionString;
}
