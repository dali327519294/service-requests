using ServiceRequests.Application.Requests;

namespace ServiceRequests.UnitTests.Application;

public class RowVersionCodecTests
{
    [Fact]
    public void Eight_bytes_survive_a_round_trip()
    {
        byte[] rowVersion = [0, 0, 0, 0, 0, 0, 0x07, 0xD1];

        Assert.True(RowVersionCodec.TryDecode(RowVersionCodec.Encode(rowVersion), out var decoded));
        Assert.Equal(rowVersion, decoded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]           // not Base64
    [InlineData("AAAAAAA=")]      // 5 bytes
    [InlineData("AAAAAAAAAAAA")]  // 9 bytes
    public void Invalid_text_is_rejected(string? value) =>
        Assert.False(RowVersionCodec.TryDecode(value, out _));
}
