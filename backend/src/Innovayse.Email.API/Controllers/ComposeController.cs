namespace Innovayse.Email.API.Controllers;

using Innovayse.Email.Application.Compose.Queries.GetTemplate;
using Innovayse.Email.Application.Messages.Commands.SaveDraft;
using Innovayse.Email.Application.Messages.Commands.SendMessage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/compose")]
[Authorize]
public sealed class ComposeController(
    SendMessageHandler sendMessage,
    SaveDraftHandler saveDraft,
    GetTemplateHandler getTemplate) : ControllerBase
{
    [HttpPost("send")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Send(
        [FromForm] string to,
        [FromForm] string? cc,
        [FromForm] string subject,
        [FromForm] string bodyHtml,
        CancellationToken ct)
    {
        var toList = to.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var ccList = string.IsNullOrWhiteSpace(cc)
            ? null
            : cc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var attachments = Request.Form.Files.Count > 0
            ? Request.Form.Files.ToList()
            : null;

        var cmd = new SendMessageCommand(toList, ccList, subject, bodyHtml, attachments);
        await sendMessage.HandleAsync(cmd, ct);
        return Ok(new { success = true });
    }

    [HttpPost("draft")]
    public async Task<IActionResult> SaveDraft([FromBody] SaveDraftRequest request, CancellationToken ct)
    {
        var uid = await saveDraft.HandleAsync(
            new SaveDraftCommand(request.To, request.Subject, request.Body, request.ExistingDraftUid), ct);
        return Ok(new { uid });
    }

    [HttpGet("template/{uid}")]
    public async Task<IActionResult> GetTemplate(uint uid, CancellationToken ct)
    {
        var template = await getTemplate.HandleAsync(new GetTemplateQuery(uid), ct);
        if (template is null) return NotFound();
        return Ok(template);
    }
}

public sealed record SaveDraftRequest(string To, string Subject, string Body, uint? ExistingDraftUid);
