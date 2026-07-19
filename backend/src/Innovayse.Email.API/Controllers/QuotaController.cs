namespace Innovayse.Email.API.Controllers;

using Innovayse.Email.API.Filters;
using Innovayse.Email.Application.Quota.Queries.GetQuota;
using Innovayse.Email.Application.Session;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/quota")]
[ServiceFilter(typeof(RequireActiveMailboxFilter))]
public sealed class QuotaController(
    GetQuotaHandler getQuota,
    MailboxSessionHolder session) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetQuota(CancellationToken ct)
    {
        var quota = await getQuota.HandleAsync(new GetQuotaQuery(session.ActiveMailbox!.Email), ct);
        return Ok(quota);
    }
}
