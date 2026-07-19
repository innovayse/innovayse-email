namespace Innovayse.Email.Tests.Infrastructure;

using Innovayse.Email.Domain.Models;
using Innovayse.Email.Infrastructure.Security;
using Xunit;

public class SessionCookieCodecTests
{
    private const string Key = "Uf2LNuJEOIDuLW1GBXu75paqpCYnEXyCtPoCxOlelK0="; // 32 bytes base64

    private static MailboxCredentials SampleCredentials() => new(
        Email: "user@example.com",
        Password: "s3cret!",
        DisplayName: "user@example.com",
        QuotaMb: 0,
        ImapHost: "mail.example.com",
        ImapPort: 993,
        SmtpHost: "mail.example.com",
        SmtpPort: 587);

    [Fact]
    public void EncodeThenDecode_RoundTripsExactCredentials()
    {
        var codec = new SessionCookieCodec(Key);
        var creds = SampleCredentials();

        var cookieValue = codec.Encode(creds);
        var decoded = codec.Decode(cookieValue);

        Assert.Equal(creds, decoded);
    }

    [Fact]
    public void Encode_ProducesDifferentCiphertextEachCall()
    {
        var codec = new SessionCookieCodec(Key);
        var creds = SampleCredentials();

        var first = codec.Encode(creds);
        var second = codec.Encode(creds);

        Assert.NotEqual(first, second); // random IV per call
    }

    [Fact]
    public void Decode_ReturnsNull_WhenValueIsTampered()
    {
        var codec = new SessionCookieCodec(Key);
        var cookieValue = codec.Encode(SampleCredentials());
        var tampered = cookieValue[..^4] + "abcd";

        var decoded = codec.Decode(tampered);

        Assert.Null(decoded);
    }

    [Fact]
    public void Decode_ReturnsNull_WhenValueIsNotBase64()
    {
        var codec = new SessionCookieCodec(Key);

        var decoded = codec.Decode("not-valid-base64!!");

        Assert.Null(decoded);
    }
}
