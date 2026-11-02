using System.Globalization;

namespace ServiceRequests.Infrastructure.Persistence.Configurations;

internal static class CheckConstraints
{
    /// <summary>Allows only the values the enum defines, so the database and the code can never disagree.</summary>
    public static string InEnum<TEnum>(string column) where TEnum : struct, Enum =>
        $"[{column}] IN ({string.Join(", ", Enum.GetValues<TEnum>().Select(v => Convert.ToByte(v, CultureInfo.InvariantCulture)))})";
}
