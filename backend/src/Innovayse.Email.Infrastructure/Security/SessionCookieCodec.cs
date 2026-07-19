namespace Innovayse.Email.Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class SessionCookieCodec(string base64Key) : ISessionCookieCodec
{
    public string Encode(MailboxCredentials credentials)
    {
        var key = Convert.FromBase64String(base64Key);
        var plainBytes = JsonSerializer.SerializeToUtf8Bytes(credentials);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var combined = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, combined, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(combined);
    }

    public MailboxCredentials? Decode(string cookieValue)
    {
        try
        {
            var key = Convert.FromBase64String(base64Key);
            var combined = Convert.FromBase64String(cookieValue);
            if (combined.Length < 17) return null;

            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.IV = combined[..16];

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(combined, 16, combined.Length - 16);

            return JsonSerializer.Deserialize<MailboxCredentials>(plainBytes);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or JsonException)
        {
            return null;
        }
    }
}
