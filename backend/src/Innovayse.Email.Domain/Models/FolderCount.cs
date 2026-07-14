namespace Innovayse.Email.Domain.Models;

public sealed record FolderCount(EmailFolder Folder, int Unread, int Total);
