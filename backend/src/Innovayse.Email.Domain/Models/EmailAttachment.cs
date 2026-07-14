namespace Innovayse.Email.Domain.Models;

public sealed record EmailAttachment(
    int Index,
    string Filename,
    string ContentType,
    long Size,
    string? ContentId);
