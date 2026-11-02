using System.Diagnostics.CodeAnalysis;

namespace ServiceRequests.Application.Requests;

/// <summary>Carries the 8-byte SQL Server rowversion to the client as Base64 text and back.</summary>
public static class RowVersionCodec
{
    private const int Length = 8;

    public static string Encode(byte[] rowVersion) => Convert.ToBase64String(rowVersion);

    public static bool TryDecode(string? value, [NotNullWhen(true)] out byte[]? rowVersion)
    {
        rowVersion = null;
        var buffer = new byte[Length];
        if (string.IsNullOrEmpty(value) || !Convert.TryFromBase64String(value, buffer, out var written) || written != Length)
        {
            return false;
        }

        rowVersion = buffer;
        return true;
    }
}
