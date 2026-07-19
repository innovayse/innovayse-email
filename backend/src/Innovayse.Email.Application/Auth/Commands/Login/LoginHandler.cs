namespace Innovayse.Email.Application.Auth.Commands.Login;

using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Domain.Models;

public sealed class LoginHandler(IMailboxAuthenticator authenticator)
{
    public Task<MailboxCredentials?> HandleAsync(LoginCommand command, CancellationToken ct)
        => authenticator.AuthenticateAsync(command.Email, command.Password, ct);
}
