-- The queries the API sends for each measured case, copied from EF Core's command log
-- (Microsoft.EntityFrameworkCore.Database.Command at Information). Each runs through sp_executesql
-- with typed parameters, as EF sends it, so SQL Server plans it the same way.
--
-- Every case has two variants:
--   after  - the query as the API runs it, with the indexes of the InitialCreate migration
--   before - the same query forced onto the clustered index (WITH (INDEX(1))): a full table scan,
--            the plan SQL Server would have without the nonclustered indexes. Nothing is dropped.
--
-- measure.ps1 runs each block with STATISTICS IO, TIME and XML on, and saves the actual plans.
-- A block starts with "-- @case <name> | <variant>".

-- @case 1 first page | after
EXEC sp_executesql N'SELECT COUNT(*) FROM [Requests] AS [r];
SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r]
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @p ROWS FETCH NEXT @p1 ROWS ONLY',
N'@p int, @p1 int', @p = 0, @p1 = 20;

-- @case 1 first page | before
EXEC sp_executesql N'SELECT COUNT(*) FROM [Requests] AS [r] WITH (INDEX(1));
SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r] WITH (INDEX(1))
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @p ROWS FETCH NEXT @p1 ROWS ONLY',
N'@p int, @p1 int', @p = 0, @p1 = 20;

-- @case 2 status filter | after
EXEC sp_executesql N'SELECT COUNT(*) FROM [Requests] AS [r] WHERE [r].[Status] = @statuses1;
SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r]
WHERE [r].[Status] = @statuses1
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @p ROWS FETCH NEXT @p2 ROWS ONLY',
N'@statuses1 tinyint, @p int, @p2 int', @statuses1 = 3, @p = 0, @p2 = 20;

-- @case 2 status filter | before
EXEC sp_executesql N'SELECT COUNT(*) FROM [Requests] AS [r] WITH (INDEX(1)) WHERE [r].[Status] = @statuses1;
SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r] WITH (INDEX(1))
WHERE [r].[Status] = @statuses1
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @p ROWS FETCH NEXT @p2 ROWS ONLY',
N'@statuses1 tinyint, @p int, @p2 int', @statuses1 = 3, @p = 0, @p2 = 20;

-- @case 3 organization + dates | after
EXEC sp_executesql N'SELECT COUNT(*) FROM [Requests] AS [r]
WHERE [r].[OrganizationName] LIKE @prefix_startswith ESCAPE N''\'' AND [r].[CreatedAt] >= @fromUtc AND [r].[CreatedAt] < @toUtc;
SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r]
WHERE [r].[OrganizationName] LIKE @prefix_startswith ESCAPE N''\'' AND [r].[CreatedAt] >= @fromUtc AND [r].[CreatedAt] < @toUtc
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @p ROWS FETCH NEXT @p3 ROWS ONLY',
N'@prefix_startswith nvarchar(200), @fromUtc datetime2(3), @toUtc datetime2(3), @p int, @p3 int',
@prefix_startswith = N'מוסך%', @fromUtc = '2026-01-01', @toUtc = '2026-04-01', @p = 0, @p3 = 20;

-- @case 3 organization + dates | before
EXEC sp_executesql N'SELECT COUNT(*) FROM [Requests] AS [r] WITH (INDEX(1))
WHERE [r].[OrganizationName] LIKE @prefix_startswith ESCAPE N''\'' AND [r].[CreatedAt] >= @fromUtc AND [r].[CreatedAt] < @toUtc;
SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r] WITH (INDEX(1))
WHERE [r].[OrganizationName] LIKE @prefix_startswith ESCAPE N''\'' AND [r].[CreatedAt] >= @fromUtc AND [r].[CreatedAt] < @toUtc
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @p ROWS FETCH NEXT @p3 ROWS ONLY',
N'@prefix_startswith nvarchar(200), @fromUtc datetime2(3), @toUtc datetime2(3), @p int, @p3 int',
@prefix_startswith = N'מוסך%', @fromUtc = '2026-01-01', @toUtc = '2026-04-01', @p = 0, @p3 = 20;

-- @case 4 text search | after
EXEC sp_executesql N'SELECT COUNT(*) FROM [Requests] AS [r]
WHERE [r].[Title] LIKE @search_contains ESCAPE N''\'' OR [r].[OrganizationName] LIKE @search_contains ESCAPE N''\'';
SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r]
WHERE [r].[Title] LIKE @search_contains ESCAPE N''\'' OR [r].[OrganizationName] LIKE @search_contains ESCAPE N''\''
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @p ROWS FETCH NEXT @p3 ROWS ONLY',
N'@search_contains nvarchar(200), @p int, @p3 int', @search_contains = N'%אסבסט%', @p = 0, @p3 = 20;

-- @case 4 text search | before
EXEC sp_executesql N'SELECT COUNT(*) FROM [Requests] AS [r] WITH (INDEX(1))
WHERE [r].[Title] LIKE @search_contains ESCAPE N''\'' OR [r].[OrganizationName] LIKE @search_contains ESCAPE N''\'';
SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r] WITH (INDEX(1))
WHERE [r].[Title] LIKE @search_contains ESCAPE N''\'' OR [r].[OrganizationName] LIKE @search_contains ESCAPE N''\''
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @p ROWS FETCH NEXT @p3 ROWS ONLY',
N'@search_contains nvarchar(200), @p int, @p3 int', @search_contains = N'%אסבסט%', @p = 0, @p3 = 20;

-- @case 5 page 500 | after
EXEC sp_executesql N'SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r]
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @p ROWS FETCH NEXT @p1 ROWS ONLY',
N'@p int, @p1 int', @p = 9980, @p1 = 20;

-- @case 5 page 500 | before
EXEC sp_executesql N'SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r] WITH (INDEX(1))
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @p ROWS FETCH NEXT @p1 ROWS ONLY',
N'@p int, @p1 int', @p = 9980, @p1 = 20;

-- @case 6 summary, no filter | after
EXEC sp_executesql N'SELECT [r].[Status] AS [Key], COUNT(*) AS [Count] FROM [Requests] AS [r] GROUP BY [r].[Status];
SELECT [r].[Priority] AS [Key], COUNT(*) AS [Count] FROM [Requests] AS [r] GROUP BY [r].[Priority]';

-- @case 6 summary, no filter | before
EXEC sp_executesql N'SELECT [r].[Status] AS [Key], COUNT(*) AS [Count] FROM [Requests] AS [r] WITH (INDEX(1)) GROUP BY [r].[Status];
SELECT [r].[Priority] AS [Key], COUNT(*) AS [Count] FROM [Requests] AS [r] WITH (INDEX(1)) GROUP BY [r].[Priority]';

-- @case 7 summary, status + priority | after
EXEC sp_executesql N'SELECT [r].[Status] AS [Key], COUNT(*) AS [Count] FROM [Requests] AS [r] WHERE [r].[Priority] = @priorities1 GROUP BY [r].[Status];
SELECT [r].[Priority] AS [Key], COUNT(*) AS [Count] FROM [Requests] AS [r] WHERE [r].[Status] = @statuses1 GROUP BY [r].[Priority]',
N'@priorities1 tinyint, @statuses1 tinyint', @priorities1 = 3, @statuses1 = 1;

-- @case 7 summary, status + priority | before
EXEC sp_executesql N'SELECT [r].[Status] AS [Key], COUNT(*) AS [Count] FROM [Requests] AS [r] WITH (INDEX(1)) WHERE [r].[Priority] = @priorities1 GROUP BY [r].[Status];
SELECT [r].[Priority] AS [Key], COUNT(*) AS [Count] FROM [Requests] AS [r] WITH (INDEX(1)) WHERE [r].[Status] = @statuses1 GROUP BY [r].[Priority]',
N'@priorities1 tinyint, @statuses1 tinyint', @priorities1 = 3, @statuses1 = 1;

-- @case 8 conditional update | after
-- Rolled back: the measurement leaves the data as it was. The UPDATE finds its row through the
-- primary key either way, so there is no "before" variant for it.
BEGIN TRANSACTION;
-- The rowversion the client would send; reading it adds 2-3 logical reads to this case
DECLARE @rowVersion binary(8) = (SELECT [RowVersion] FROM [Requests] WHERE [Id] = 1);
EXEC sp_executesql N'SET NOCOUNT ON;
INSERT INTO [RequestStatusHistory] ([ChangedAt], [ChangedBy], [NewStatus], [PreviousStatus], [RequestId])
OUTPUT INSERTED.[Id]
VALUES (@p0, @p1, @p2, @p3, @p4);
UPDATE [Requests] SET [Status] = @p5, [UpdatedAt] = @p6
OUTPUT INSERTED.[RowVersion]
WHERE [Id] = @p7 AND [RowVersion] = @p8;',
N'@p0 datetime2(3), @p1 nvarchar(100), @p2 tinyint, @p3 tinyint, @p4 int, @p5 tinyint, @p6 datetime2(3), @p7 int, @p8 binary(8)',
@p0 = '2026-11-05T09:00:00', @p1 = N'perf.check', @p2 = 2, @p3 = 1, @p4 = 1, @p5 = 2, @p6 = '2026-11-05T09:00:00', @p7 = 1,
@p8 = @rowVersion;
ROLLBACK TRANSACTION;
