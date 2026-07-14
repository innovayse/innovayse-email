namespace Innovayse.Email.Infrastructure.Providers;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;
using Microsoft.Extensions.Configuration;

public sealed class HostpanelCredentialProvider(
    HttpClient httpClient,
    IConfiguration config) : IMailboxCredentialProvider
{
    public async Task<List<MailboxCredentials>> GetMailboxesAsync(string accessToken, CancellationToken ct)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await httpClient.GetAsync("/api/my-email/mailboxes/credentials", ct);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<CredentialDto>>(ct) ?? [];
        var encryptionKey = config["EncryptionKey"] ?? "";

        return items.Select(i => new MailboxCredentials(
            i.Email,
            DecryptPassword(i.EncryptedPassword, encryptionKey),
            i.DisplayName,
            i.QuotaMb,
            i.ImapHost,
            i.ImapPort,
            i.SmtpHost,
            i.SmtpPort
        )).ToList();
    }

    private static string DecryptPassword(string encrypted, string base64Key)
    {
        if (string.IsNullOrEmpty(base64Key) || string.IsNullOrEmpty(encrypted))
            return encrypted;

        var key = Convert.FromBase64String(base64Key);
        var fullBytes = Convert.FromBase64String(encrypted);
        if (fullBytes.Length < 17) return encrypted;

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.IV = fullBytes[..16];
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(fullBytes, 16, fullBytes.Length - 16);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private sealed record CredentialDto(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("encryptedPassword")] string EncryptedPassword,
        [property: JsonPropertyName("displayName")] string DisplayName,
        [property: JsonPropertyName("quotaMb")] int QuotaMb,
        [property: JsonPropertyName("imapHost")] string ImapHost,
        [property: JsonPropertyName("imapPort")] int ImapPort,
        [property: JsonPropertyName("smtpHost")] string SmtpHost,
        [property: JsonPropertyName("smtpPort")] int SmtpPort);
}
