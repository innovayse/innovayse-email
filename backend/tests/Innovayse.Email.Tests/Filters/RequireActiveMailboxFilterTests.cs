namespace Innovayse.Email.Tests.Filters;

using Innovayse.Email.API.Filters;
using Innovayse.Email.Application.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

public class RequireActiveMailboxFilterTests
{
    private static ActionExecutingContext BuildContext(MailboxSessionHolder session)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
        return new ActionExecutingContext(
            actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: new object());
    }

    [Fact]
    public async Task OnActionExecutionAsync_NoActiveMailbox_ShortCircuitsWith401()
    {
        var session = new MailboxSessionHolder();
        var filter = new RequireActiveMailboxFilter(session);
        var context = BuildContext(session);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, context.Filters, context.Controller));
        });

        Assert.False(nextCalled);
        var result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task OnActionExecutionAsync_HasActiveMailbox_CallsNext()
    {
        var session = new MailboxSessionHolder
        {
            ActiveMailbox = new("user@example.com", "pw", "user@example.com", 0, "h", 993, "h", 587),
        };
        var filter = new RequireActiveMailboxFilter(session);
        var context = BuildContext(session);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, context.Filters, context.Controller));
        });

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }
}
