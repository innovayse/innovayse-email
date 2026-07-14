namespace Innovayse.Email.Application.Messages.Commands.MoveMessage;

using Innovayse.Email.Domain.Models;

public sealed record MoveMessageCommand(EmailFolder SourceFolder, uint Uid, EmailFolder TargetFolder);
