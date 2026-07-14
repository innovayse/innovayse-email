namespace Innovayse.Email.Application.Mailboxes.Queries.ListMailboxes;

using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class ListMailboxesHandler(IMailboxCredentialProvider provider)
{
    public async Task<List<MailboxCredentials>> HandleAsync(ListMailboxesQuery query, CancellationToken ct)
        => await provider.GetMailboxesAsync(query.AccessToken, ct);
}
