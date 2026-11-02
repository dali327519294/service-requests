using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ServiceRequests.Infrastructure.Persistence;

/// <summary>Marks dates read from the database as UTC; SQL Server stores no time zone.</summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    toDb => toDb,
    fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc));
