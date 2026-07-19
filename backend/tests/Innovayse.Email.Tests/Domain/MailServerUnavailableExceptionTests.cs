namespace Innovayse.Email.Tests.Domain;

using Innovayse.Email.Domain.Exceptions;
using Xunit;

public class MailServerUnavailableExceptionTests
{
    [Fact]
    public void Constructor_SetsMessageAndInnerException()
    {
        var inner = new InvalidOperationException("connect refused");

        var ex = new MailServerUnavailableException("Could not reach mail server", inner);

        Assert.Equal("Could not reach mail server", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}
