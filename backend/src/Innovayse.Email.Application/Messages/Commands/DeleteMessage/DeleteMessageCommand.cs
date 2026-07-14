namespace Innovayse.Email.Application.Messages.Commands.DeleteMessage;

using Innovayse.Email.Domain.Models;

public sealed record DeleteMessageCommand(EmailFolder Folder, uint Uid);
