namespace Innovayse.Email.Application.Messages.Commands.SendMessage;

using Microsoft.AspNetCore.Http;

public sealed record SendMessageCommand(
    List<string> To,
    List<string>? Cc,
    string Subject,
    string BodyHtml,
    List<IFormFile>? Attachments);
