namespace Innovayse.Email.Infrastructure;

using Innovayse.Email.Domain.Interfaces;
using Innovayse.Email.Infrastructure.Imap;
using Innovayse.Email.Infrastructure.Providers;
using Innovayse.Email.Infrastructure.Security;
using Innovayse.Email.Infrastructure.Settings;
using Innovayse.Email.Infrastructure.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ImapSettings>(config.GetSection("Imap"));
        services.Configure<SmtpSettings>(config.GetSection("Smtp"));
        services.Configure<MailcowSettings>(config.GetSection("Mailcow"));

        services.AddScoped<IImapService, ImapMailService>();
        services.AddScoped<ISmtpService, SmtpMailService>();
        services.AddScoped<IMailboxAuthenticator, ImapMailboxAuthenticator>();
        services.AddSingleton<ISessionCookieCodec>(_ => new SessionCookieCodec(config["EncryptionKey"] ?? ""));

        services.AddHttpClient<IMailboxQuotaProvider, MailcowQuotaProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        });

        return services;
    }
}
