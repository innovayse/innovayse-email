namespace Innovayse.Email.Application.Messages.Queries.GetAttachment;

using Innovayse.Email.Domain.Models;

public sealed record GetAttachmentQuery(EmailFolder Folder, uint Uid, int AttachmentIndex);
