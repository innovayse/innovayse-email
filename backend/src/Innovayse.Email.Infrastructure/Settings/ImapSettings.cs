namespace Innovayse.Email.Infrastructure.Settings;

public sealed class ImapSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 993;
    public bool UseSsl { get; set; } = true;
}
