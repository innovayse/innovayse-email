namespace Innovayse.Email.Application.Messages.Queries.ListMessages;

using Innovayse.Email.Domain.Models;

public sealed record ListMessagesQuery(EmailFolder Folder, int Page = 1, int PageSize = 50);
