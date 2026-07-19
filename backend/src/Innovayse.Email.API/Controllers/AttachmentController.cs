namespace Innovayse.Email.API.Controllers;

using Innovayse.Email.Application.Messages.Queries.GetAttachment;
using Innovayse.Email.Application.Messages.Queries.GetMessage;
using Innovayse.Email.Domain.Models;
using Innovayse.Email.API.Filters;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/attachments")]
[ServiceFilter(typeof(RequireActiveMailboxFilter))]
public sealed class AttachmentController(
    GetAttachmentHandler getAttachment,
    GetMessageHandler getMessage) : ControllerBase
{
    [HttpGet("{folder}/{uid}/{index}")]
    public async Task<IActionResult> DownloadAttachment(
        string folder, uint uid, int index, CancellationToken ct)
    {
        if (!Enum.TryParse<EmailFolder>(folder, ignoreCase: true, out var folderEnum))
            return BadRequest(new { error = $"Unknown folder: {folder}" });

        // Fetch message to get attachment metadata (filename, content type)
        var message = await getMessage.HandleAsync(new GetMessageQuery(folderEnum, uid), ct);
        if (message is null) return NotFound();

        var attachment = message.Attachments.ElementAtOrDefault(index);
        if (attachment is null) return NotFound();

        var stream = await getAttachment.HandleAsync(new GetAttachmentQuery(folderEnum, uid, index), ct);

        return File(stream, attachment.ContentType, attachment.Filename);
    }
}
