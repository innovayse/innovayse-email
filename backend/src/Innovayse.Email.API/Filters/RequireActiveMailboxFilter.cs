namespace Innovayse.Email.API.Filters;

using Innovayse.Email.Application.Session;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public sealed class RequireActiveMailboxFilter(MailboxSessionHolder session) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (session.ActiveMailbox is null)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Not authenticated" });
            return;
        }

        await next();
    }
}
