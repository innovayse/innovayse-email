namespace Innovayse.Email.Application.Messages.Queries.GetMessage;

using Innovayse.Email.Domain.Models;

public sealed record GetMessageQuery(EmailFolder Folder, uint Uid);
