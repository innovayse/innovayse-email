namespace Innovayse.Email.Domain.Interfaces;

using Innovayse.Email.Domain.Models;

public interface IMailboxQuotaProvider
{
    Task<QuotaInfo> GetQuotaAsync(string mailboxEmail, CancellationToken ct);
}
