namespace Innovayse.Email.API.Controllers;

using static Innovayse.Email.API.Controllers.TimeFormatter;
using Innovayse.Email.Application.Messages.Commands.DeleteMessage;
using Innovayse.Email.Application.Messages.Commands.MarkAsRead;
using Innovayse.Email.Application.Messages.Commands.MoveMessage;
using Innovayse.Email.Application.Messages.Queries.GetFolderCounts;
using Innovayse.Email.Application.Messages.Queries.GetMessage;
using Innovayse.Email.Application.Messages.Queries.ListMessages;
using Innovayse.Email.Application.Messages.Queries.SearchMessages;
using Innovayse.Email.Domain.Models;
using Innovayse.Email.API.Filters;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/messages")]
[ServiceFilter(typeof(RequireActiveMailboxFilter))]
public sealed class MessageController(
    ListMessagesHandler listMessages,
    GetMessageHandler getMessage,
    SearchMessagesHandler searchMessages,
    GetFolderCountsHandler getFolderCounts,
    DeleteMessageHandler deleteMessage,
    MoveMessageHandler moveMessage,
    MarkAsReadHandler markAsRead) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListMessages(
        [FromQuery] string folder = "inbox",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<EmailFolder>(folder, ignoreCase: true, out var folderEnum))
            return BadRequest(new { error = $"Unknown folder: {folder}" });

        var messages = await listMessages.HandleAsync(new ListMessagesQuery(folderEnum, page, pageSize), ct);
        var result = messages.Select(m => new
        {
            uid = m.Uid,
            folder = m.Folder.ToString().ToLowerInvariant(),
            sender = m.From.Name ?? m.From.Address,
            senderEmail = m.From.Address,
            subject = m.Subject,
            preview = m.Snippet,
            time = FormatTime(m.Date),
            date = m.Date,
            unread = !m.IsRead,
            hasAttachments = m.HasAttachments,
        });
        return Ok(result);
    }

    [HttpGet("{folder}/{uid}")]
    public async Task<IActionResult> GetMessage(string folder, uint uid, CancellationToken ct)
    {
        if (!Enum.TryParse<EmailFolder>(folder, ignoreCase: true, out var folderEnum))
            return BadRequest(new { error = $"Unknown folder: {folder}" });

        var m = await getMessage.HandleAsync(new GetMessageQuery(folderEnum, uid), ct);
        if (m is null) return NotFound();
        return Ok(new
        {
            uid = m.Uid,
            folder = m.Folder.ToString().ToLowerInvariant(),
            sender = m.From.Name ?? m.From.Address,
            senderEmail = m.From.Address,
            subject = m.Subject,
            bodyHtml = m.BodyHtml,
            bodyPlain = m.BodyPlain,
            time = FormatTime(m.Date),
            date = m.Date,
            unread = !m.IsRead,
            hasAttachments = m.HasAttachments,
            to = m.To.Select(a => new { name = a.Name, address = a.Address }),
            cc = m.Cc.Select(a => new { name = a.Name, address = a.Address }),
            attachments = m.Attachments.Select(a => new { a.Index, a.Filename, a.ContentType, a.Size }),
        });
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchMessages([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Search query cannot be empty" });

        var results = await searchMessages.HandleAsync(new SearchMessagesQuery(q), ct);
        return Ok(results);
    }

    [HttpGet("counts")]
    public async Task<IActionResult> GetCounts(CancellationToken ct)
    {
        var counts = await getFolderCounts.HandleAsync(new GetFolderCountsQuery(), ct);
        return Ok(counts.Select(c => new
        {
            folder = c.Folder.ToString().ToLowerInvariant(),
            unread = c.Unread,
            total = c.Total,
        }));
    }

    [HttpPut("{folder}/{uid}/read")]
    public async Task<IActionResult> MarkRead(string folder, uint uid, CancellationToken ct)
    {
        if (!Enum.TryParse<EmailFolder>(folder, ignoreCase: true, out var folderEnum))
            return BadRequest(new { error = $"Unknown folder: {folder}" });

        await markAsRead.HandleReadAsync(new MarkAsReadCommand(folderEnum, uid), ct);
        return NoContent();
    }

    [HttpPut("{folder}/{uid}/unread")]
    public async Task<IActionResult> MarkUnread(string folder, uint uid, CancellationToken ct)
    {
        if (!Enum.TryParse<EmailFolder>(folder, ignoreCase: true, out var folderEnum))
            return BadRequest(new { error = $"Unknown folder: {folder}" });

        await markAsRead.HandleUnreadAsync(new MarkAsUnreadCommand(folderEnum, uid), ct);
        return NoContent();
    }

    [HttpPost("{folder}/{uid}/move")]
    public async Task<IActionResult> MoveMessage(string folder, uint uid, [FromBody] MoveRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<EmailFolder>(folder, ignoreCase: true, out var sourceFolder))
            return BadRequest(new { error = $"Unknown source folder: {folder}" });

        if (!Enum.TryParse<EmailFolder>(request.TargetFolder, ignoreCase: true, out var targetFolder))
            return BadRequest(new { error = $"Unknown target folder: {request.TargetFolder}" });

        await moveMessage.HandleAsync(new MoveMessageCommand(sourceFolder, uid, targetFolder), ct);
        return NoContent();
    }

    [HttpDelete("{folder}/{uid}")]
    public async Task<IActionResult> DeleteMessage(string folder, uint uid, CancellationToken ct)
    {
        if (!Enum.TryParse<EmailFolder>(folder, ignoreCase: true, out var folderEnum))
            return BadRequest(new { error = $"Unknown folder: {folder}" });

        await deleteMessage.HandleAsync(new DeleteMessageCommand(folderEnum, uid), ct);
        return NoContent();
    }
}

public sealed record MoveRequest(string TargetFolder);

internal static class TimeFormatter
{
    internal static string FormatTime(DateTimeOffset date)
    {
        var now = DateTimeOffset.UtcNow;
        var diff = now - date;

        if (diff.TotalHours < 24 && date.Date == now.Date)
            return date.ToLocalTime().ToString("h:mm tt");
        if (diff.TotalHours < 48)
            return "Yesterday";
        if (diff.TotalDays < 7)
            return date.ToLocalTime().ToString("dddd");
        return date.ToLocalTime().ToString("MMM d");
    }
}
