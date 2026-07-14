namespace Innovayse.Email.Application.Messages.Commands.SaveDraft;

public sealed record SaveDraftCommand(string To, string Subject, string Body, uint? ExistingDraftUid);
