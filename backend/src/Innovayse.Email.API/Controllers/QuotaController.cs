namespace Innovayse.Email.API.Controllers;

using Innovayse.Email.Application.Quota.Queries.GetQuota;
using Innovayse.Email.Application.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/quota")]
[Authorize]
public sealed class QuotaController(
    GetQuotaHandler getQuota,
    MailboxSessionHolder session) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetQuota(CancellationToken ct)
    {
        var mailbox = session.ActiveMailbox;
        if (mailbox is null)
            return BadRequest(new { error = "No active mailbox selected" });

        var quota = await getQuota.HandleAsync(new GetQuotaQuery(mailbox.Email), ct);
        return Ok(quota);
    }
}
