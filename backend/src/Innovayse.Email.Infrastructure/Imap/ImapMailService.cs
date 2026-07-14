namespace Innovayse.Email.Infrastructure.Imap;

using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Logging;
using MimeKit;

public sealed class ImapMailService(ILogger<ImapMailService> logger) : IImapService
{
    private async Task<ImapClient> ConnectAsync(MailboxCredentials creds, CancellationToken ct)
    {
        logger.LogInformation("IMAP connecting to {Host}:{Port} as {Email} (pwd length: {Len})",
            creds.ImapHost, creds.ImapPort, creds.Email, creds.Password?.Length ?? -1);
        var client = new ImapClient();
        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        await client.ConnectAsync(creds.ImapHost, creds.ImapPort, MailKit.Security.SecureSocketOptions.SslOnConnect, ct);
        await client.AuthenticateAsync(creds.Email, creds.Password, ct);
        return client;
    }

    private async Task<IMailFolder> GetFolderAsync(ImapClient client, EmailFolder folder, CancellationToken ct)
    {
        if (folder == EmailFolder.Inbox)
            return client.Inbox;

        if (folder == EmailFolder.Templates)
        {
            var personal = client.GetFolder(client.PersonalNamespaces[0]);
            return await personal.GetSubfolderAsync("Templates", ct);
        }

        var special = FolderMapping.ToSpecial[folder];
        if (special.HasValue)
            return client.GetFolder(special.Value);

        throw new InvalidOperationException($"Unknown folder: {folder}");
    }

    public async Task<List<EmailMessageSummary>> ListMessagesAsync(
        MailboxCredentials creds, EmailFolder folder, int page, int pageSize, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var mailFolder = await GetFolderAsync(client, folder, ct);
        await mailFolder.OpenAsync(FolderAccess.ReadOnly, ct);

        var total = mailFolder.Count;
        var start = Math.Max(0, total - (page * pageSize));
        var end = Math.Max(0, total - ((page - 1) * pageSize) - 1);
        if (start > end || total == 0)
            return [];

        var summaries = await mailFolder.FetchAsync(start, end,
            MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope |
            MessageSummaryItems.Flags | MessageSummaryItems.BodyStructure |
            MessageSummaryItems.PreviewText, ct);

        return summaries
            .OrderByDescending(s => s.Date)
            .Select(s => new EmailMessageSummary(
                s.UniqueId.Id,
                folder,
                new EmailAddress(
                    s.Envelope.From.Mailboxes.FirstOrDefault()?.Name ?? "",
                    s.Envelope.From.Mailboxes.FirstOrDefault()?.Address ?? ""),
                s.Envelope.Subject ?? "(no subject)",
                s.PreviewText ?? "",
                s.Date,
                s.Flags.HasValue && s.Flags.Value.HasFlag(MessageFlags.Seen),
                s.Body is BodyPartMultipart multi && multi.BodyParts.Any(p => p is BodyPartBasic b && b.IsAttachment)))
            .ToList();
    }

    public async Task<EmailMessageDetail?> GetMessageAsync(
        MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var mailFolder = await GetFolderAsync(client, folder, ct);
        await mailFolder.OpenAsync(FolderAccess.ReadWrite, ct);

        var message = await mailFolder.GetMessageAsync(new UniqueId(uid), ct);
        if (message is null) return null;

        await mailFolder.AddFlagsAsync(new UniqueId(uid), MessageFlags.Seen, true, ct);

        var attachments = message.Attachments
            .Select((a, i) => new EmailAttachment(
                i,
                a.ContentDisposition?.FileName ?? a.ContentType.Name ?? $"attachment-{i}",
                a.ContentType.MimeType,
                a is MimePart mp ? mp.Content?.Stream?.Length ?? 0 : 0,
                a.ContentId))
            .ToList();

        return new EmailMessageDetail(
            uid, folder,
            new EmailAddress(
                message.From.Mailboxes.FirstOrDefault()?.Name ?? "",
                message.From.Mailboxes.FirstOrDefault()?.Address ?? ""),
            message.To.Mailboxes.Select(m => new EmailAddress(m.Name ?? "", m.Address)).ToList(),
            message.Cc.Mailboxes.Select(m => new EmailAddress(m.Name ?? "", m.Address)).ToList(),
            message.Subject ?? "(no subject)",
            message.HtmlBody,
            message.TextBody,
            message.Date,
            true,
            attachments.Count > 0,
            attachments);
    }

    public async Task<List<EmailMessageSummary>> SearchAsync(
        MailboxCredentials creds, string query, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var results = new List<EmailMessageSummary>();

        foreach (var folderEnum in Enum.GetValues<EmailFolder>())
        {
            try
            {
                var mailFolder = await GetFolderAsync(client, folderEnum, ct);
                await mailFolder.OpenAsync(FolderAccess.ReadOnly, ct);

                var searchQuery = SearchQuery.Or(
                    SearchQuery.SubjectContains(query),
                    SearchQuery.Or(
                        SearchQuery.FromContains(query),
                        SearchQuery.BodyContains(query)));

                var uids = await mailFolder.SearchAsync(searchQuery, ct);
                if (uids.Count == 0) continue;

                var limitedUids = uids.TakeLast(50).ToList();
                var summaries = await mailFolder.FetchAsync(limitedUids,
                    MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope |
                    MessageSummaryItems.Flags | MessageSummaryItems.PreviewText, ct);

                results.AddRange(summaries.Select(s => new EmailMessageSummary(
                    s.UniqueId.Id, folderEnum,
                    new EmailAddress(
                        s.Envelope.From.Mailboxes.FirstOrDefault()?.Name ?? "",
                        s.Envelope.From.Mailboxes.FirstOrDefault()?.Address ?? ""),
                    s.Envelope.Subject ?? "(no subject)",
                    s.PreviewText ?? "",
                    s.Date,
                    s.Flags.HasValue && s.Flags.Value.HasFlag(MessageFlags.Seen),
                    false)));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to search folder {Folder}", folderEnum);
            }
        }

        return results.OrderByDescending(m => m.Date).Take(100).ToList();
    }

    public async Task MarkAsReadAsync(MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var mailFolder = await GetFolderAsync(client, folder, ct);
        await mailFolder.OpenAsync(FolderAccess.ReadWrite, ct);
        await mailFolder.AddFlagsAsync(new UniqueId(uid), MessageFlags.Seen, true, ct);
    }

    public async Task MarkAsUnreadAsync(MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var mailFolder = await GetFolderAsync(client, folder, ct);
        await mailFolder.OpenAsync(FolderAccess.ReadWrite, ct);
        await mailFolder.RemoveFlagsAsync(new UniqueId(uid), MessageFlags.Seen, true, ct);
    }

    public async Task MoveAsync(MailboxCredentials creds, EmailFolder sourceFolder, uint uid, EmailFolder targetFolder, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var source = await GetFolderAsync(client, sourceFolder, ct);
        var target = await GetFolderAsync(client, targetFolder, ct);
        await source.OpenAsync(FolderAccess.ReadWrite, ct);
        await source.MoveToAsync(new UniqueId(uid), target, ct);
    }

    public async Task DeleteAsync(MailboxCredentials creds, EmailFolder folder, uint uid, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var mailFolder = await GetFolderAsync(client, folder, ct);
        await mailFolder.OpenAsync(FolderAccess.ReadWrite, ct);

        if (folder == EmailFolder.Trash)
        {
            await mailFolder.AddFlagsAsync(new UniqueId(uid), MessageFlags.Deleted, true, ct);
            await mailFolder.ExpungeAsync(ct);
        }
        else
        {
            var trash = await GetFolderAsync(client, EmailFolder.Trash, ct);
            await mailFolder.MoveToAsync(new UniqueId(uid), trash, ct);
        }
    }

    public async Task<uint> SaveDraftAsync(MailboxCredentials creds, string to, string subject, string body, uint? existingDraftUid, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var drafts = await GetFolderAsync(client, EmailFolder.Drafts, ct);
        await drafts.OpenAsync(FolderAccess.ReadWrite, ct);

        if (existingDraftUid.HasValue)
        {
            await drafts.AddFlagsAsync(new UniqueId(existingDraftUid.Value), MessageFlags.Deleted, true, ct);
            await drafts.ExpungeAsync(ct);
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(creds.DisplayName, creds.Email));
        if (!string.IsNullOrWhiteSpace(to))
            message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject ?? "";
        message.Body = new TextPart("plain") { Text = body ?? "" };

        var request = new MailKit.AppendRequest(message, MessageFlags.Draft | MessageFlags.Seen);
        var uid = await drafts.AppendAsync(request, ct);
        return uid?.Id ?? 0;
    }

    public async Task<Stream> GetAttachmentAsync(MailboxCredentials creds, EmailFolder folder, uint uid, int attachmentIndex, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var mailFolder = await GetFolderAsync(client, folder, ct);
        await mailFolder.OpenAsync(FolderAccess.ReadOnly, ct);

        var message = await mailFolder.GetMessageAsync(new UniqueId(uid), ct);
        var attachment = message.Attachments.ElementAtOrDefault(attachmentIndex)
            ?? throw new InvalidOperationException("Attachment not found");

        var stream = new MemoryStream();
        if (attachment is MimePart part)
            await part.Content.DecodeToAsync(stream, ct);
        stream.Position = 0;
        return stream;
    }

    public async Task<List<FolderCount>> GetFolderCountsAsync(MailboxCredentials creds, CancellationToken ct)
    {
        using var client = await ConnectAsync(creds, ct);
        var counts = new List<FolderCount>();

        foreach (var folderEnum in Enum.GetValues<EmailFolder>())
        {
            try
            {
                var mailFolder = await GetFolderAsync(client, folderEnum, ct);
                await mailFolder.OpenAsync(FolderAccess.ReadOnly, ct);
                var unread = mailFolder.Unread;
                var total = mailFolder.Count;
                counts.Add(new FolderCount(folderEnum, unread, total));
            }
            catch
            {
                counts.Add(new FolderCount(folderEnum, 0, 0));
            }
        }

        return counts;
    }
}
