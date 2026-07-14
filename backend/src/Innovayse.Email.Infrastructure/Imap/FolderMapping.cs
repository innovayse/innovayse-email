namespace Innovayse.Email.Infrastructure.Imap;

using Innovayse.Email.Domain.Models;
using MailKit;

internal static class FolderMapping
{
    internal static readonly Dictionary<EmailFolder, SpecialFolder?> ToSpecial = new()
    {
        [EmailFolder.Inbox] = null, // Use client.Inbox
        [EmailFolder.Drafts] = SpecialFolder.Drafts,
        [EmailFolder.Sent] = SpecialFolder.Sent,
        [EmailFolder.Archive] = SpecialFolder.Archive,
        [EmailFolder.Junk] = SpecialFolder.Junk,
        [EmailFolder.Trash] = SpecialFolder.Trash,
        [EmailFolder.Templates] = null, // Custom folder by name
    };

    internal static readonly Dictionary<string, EmailFolder> FromName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["INBOX"] = EmailFolder.Inbox,
        ["Drafts"] = EmailFolder.Drafts,
        ["Sent"] = EmailFolder.Sent,
        ["Archive"] = EmailFolder.Archive,
        ["Junk"] = EmailFolder.Junk,
        ["Templates"] = EmailFolder.Templates,
        ["Trash"] = EmailFolder.Trash,
    };
}
