namespace Innovayse.Email.Domain.Models;

public sealed record EmailAddress(string Name, string Address);

public sealed record EmailMessageSummary(
    uint Uid,
    EmailFolder Folder,
    EmailAddress From,
    string Subject,
    string Snippet,
    DateTimeOffset Date,
    bool IsRead,
    bool HasAttachments);

public sealed record EmailMessageDetail(
    uint Uid,
    EmailFolder Folder,
    EmailAddress From,
    List<EmailAddress> To,
    List<EmailAddress> Cc,
    string Subject,
    string? BodyHtml,
    string? BodyPlain,
    DateTimeOffset Date,
    bool IsRead,
    bool HasAttachments,
    List<EmailAttachment> Attachments);
