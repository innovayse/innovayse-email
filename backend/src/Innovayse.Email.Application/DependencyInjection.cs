namespace Innovayse.Email.Application;

using Innovayse.Email.Application.Auth.Commands.Login;
using Innovayse.Email.Application.Compose.Queries.GetTemplate;
using Innovayse.Email.Application.Messages.Commands.DeleteMessage;
using Innovayse.Email.Application.Messages.Commands.MarkAsRead;
using Innovayse.Email.Application.Messages.Commands.MoveMessage;
using Innovayse.Email.Application.Messages.Commands.SaveDraft;
using Innovayse.Email.Application.Messages.Commands.SendMessage;
using Innovayse.Email.Application.Messages.Queries.GetAttachment;
using Innovayse.Email.Application.Messages.Queries.GetFolderCounts;
using Innovayse.Email.Application.Messages.Queries.GetMessage;
using Innovayse.Email.Application.Messages.Queries.ListMessages;
using Innovayse.Email.Application.Messages.Queries.SearchMessages;
using Innovayse.Email.Application.Quota.Queries.GetQuota;
using Innovayse.Email.Application.Session;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<MailboxSessionHolder>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<ListMessagesHandler>();
        services.AddScoped<GetMessageHandler>();
        services.AddScoped<SearchMessagesHandler>();
        services.AddScoped<GetAttachmentHandler>();
        services.AddScoped<GetFolderCountsHandler>();
        services.AddScoped<SendMessageHandler>();
        services.AddScoped<SaveDraftHandler>();
        services.AddScoped<DeleteMessageHandler>();
        services.AddScoped<MoveMessageHandler>();
        services.AddScoped<MarkAsReadHandler>();
        services.AddScoped<GetTemplateHandler>();
        services.AddScoped<GetQuotaHandler>();
        return services;
    }
}
