namespace Innovayse.Email.Application.Quota.Queries.GetQuota;

using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class GetQuotaHandler(IMailboxQuotaProvider provider)
{
    public async Task<QuotaInfo> HandleAsync(GetQuotaQuery query, CancellationToken ct)
        => await provider.GetQuotaAsync(query.MailboxEmail, ct);
}
