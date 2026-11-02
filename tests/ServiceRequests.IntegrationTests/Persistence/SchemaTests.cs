using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ServiceRequests.IntegrationTests.Infrastructure;

namespace ServiceRequests.IntegrationTests.Persistence;

public class SchemaTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("Status")]
    [InlineData("Priority")]
    public async Task Check_constraint_rejects_unknown_value(string column)
    {
        var request = await TestData.AddRequestAsync(factory);

        // The column comes from InlineData above, never from user input
        var sql = $"UPDATE Requests SET {column} = 9 WHERE Id = {request.Id}";
        await factory.ExecuteDbAsync(db => Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(sql)));
    }

    [Fact]
    public async Task RowVersion_changes_on_every_update()
    {
        var request = await TestData.AddRequestAsync(factory);
        var versions = new List<byte[]> { request.RowVersion };

        for (var i = 0; i < 2; i++)
        {
            var assignedTo = $"user{i}";
            await factory.ExecuteDbAsync(async db =>
            {
                await db.Database.ExecuteSqlAsync($"UPDATE Requests SET AssignedTo = {assignedTo} WHERE Id = {request.Id}");
                versions.Add(await db.Requests.Where(r => r.Id == request.Id).Select(r => r.RowVersion).SingleAsync());
            });
        }

        // byte[] compares by reference, so compare the hex text instead
        Assert.Equal(3, versions.Select(Convert.ToHexString).Distinct().Count());
    }

    [Fact]
    public async Task Dates_are_read_back_as_utc()
    {
        var request = await TestData.AddRequestAsync(factory);

        DateTime createdAt = default;
        await factory.ExecuteDbAsync(async db =>
            createdAt = await db.Requests.Where(r => r.Id == request.Id).Select(r => r.CreatedAt).SingleAsync());

        Assert.Equal(DateTimeKind.Utc, createdAt.Kind);
    }

    [Fact]
    public async Task Indexes_match_the_spec()
    {
        List<string> actual = [];
        await factory.ExecuteDbAsync(async db => actual = await db.Database.SqlQuery<string>($"""
            SELECT i.name + ': ' + STRING_AGG(c.name
                       + IIF(ic.is_descending_key = 1, ' DESC', '')
                       + IIF(ic.is_included_column = 1, ' INCLUDE', ''), ', ')
                   WITHIN GROUP (ORDER BY ic.is_included_column, ic.key_ordinal) AS Value
            FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.name LIKE 'IX[_]%'
            GROUP BY i.name
            """).ToListAsync());

        var expected = new HashSet<string>
        {
            "IX_Requests_CreatedAt: CreatedAt DESC",
            "IX_Requests_Status_CreatedAt: Status, CreatedAt DESC, Priority INCLUDE",
            "IX_Requests_AssignedTo_Status: AssignedTo, Status",
            "IX_Requests_OrganizationName: OrganizationName",
            "IX_RequestStatusHistory_RequestId_ChangedAt: RequestId, ChangedAt DESC",
        };
        Assert.Superset(expected, actual.ToHashSet());
    }
}
