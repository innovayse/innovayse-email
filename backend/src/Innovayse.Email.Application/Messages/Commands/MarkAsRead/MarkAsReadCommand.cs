namespace Innovayse.Email.Application.Messages.Commands.MarkAsRead;

using Innovayse.Email.Domain.Models;

public sealed record MarkAsReadCommand(EmailFolder Folder, uint Uid);
public sealed record MarkAsUnreadCommand(EmailFolder Folder, uint Uid);
