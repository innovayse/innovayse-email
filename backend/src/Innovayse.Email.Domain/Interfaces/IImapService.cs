namespace Innovayse.Email.Domain.Interfaces;

using Innovayse.Email.Domain.Models;

public interface IImapService
{
    Task<List<EmailMessageSummary>> ListMessagesAsync(
        MailboxCredentials creds, EmailFolder folder, int page, int pageSize, CancellationToken ct);

    Task<EmailMessageDetail?> GetMessageAsync(
        MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct);

    Task<List<EmailMessageSummary>> SearchAsync(
        MailboxCredentials creds, string query, CancellationToken ct);

    Task MarkAsReadAsync(
        MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct);

    Task MarkAsUnreadAsync(
        MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct);

    Task MoveAsync(
        MailboxCredentials creds, EmailFolder sourceFolder, uint uid, EmailFolder targetFolder, CancellationToken ct);

    Task DeleteAsync(
        MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct);

    Task<uint> SaveDraftAsync(
        MailboxCredentials creds, string to, string subject, string body, uint? existingDraftUid, CancellationToken ct);

    Task<Stream> GetAttachmentAsync(
        MailboxCredentials creds, EmailFolder folder, uint uid, int attachmentIndex, CancellationToken ct);

    Task<List<FolderCount>> GetFolderCountsAsync(
        MailboxCredentials creds, CancellationToken ct);
}
