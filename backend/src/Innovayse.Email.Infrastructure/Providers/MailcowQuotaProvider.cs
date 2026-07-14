namespace Innovayse.Email.Infrastructure.Providers;

using System.Net.Http.Json;
using System.Text.Json;
using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;
using Innovayse.Email.Infrastructure.Settings;
using Microsoft.Extensions.Options;

public sealed class MailcowQuotaProvider(
    HttpClient httpClient,
    IOptions<MailcowSettings> settings) : IMailboxQuotaProvider
{
    public async Task<QuotaInfo> GetQuotaAsync(string mailboxEmail, CancellationToken ct)
    {
        var opts = settings.Value;
        if (string.IsNullOrEmpty(opts.ApiUrl) || string.IsNullOrEmpty(opts.ApiKey))
            return new QuotaInfo(0, 0);

        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-API-Key", opts.ApiKey);
        var response = await httpClient.GetAsync(
            $"{opts.ApiUrl}/api/v1/get/mailbox/{Uri.EscapeDataString(mailboxEmail)}", ct);

        if (!response.IsSuccessStatusCode)
            return new QuotaInfo(0, 0);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var usedBytes = json.TryGetProperty("quota_used", out var used) ? used.GetInt64() : 0;
        var limitBytes = json.TryGetProperty("quota", out var limit) ? limit.GetInt64() * 1024 * 1024 : 0;

        return new QuotaInfo(usedBytes, limitBytes);
    }
}
