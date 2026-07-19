namespace Innovayse.Email.Domain.Exceptions;

public sealed class MailServerUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
