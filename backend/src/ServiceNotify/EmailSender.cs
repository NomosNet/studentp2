using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ServiceNotify;

public sealed class EmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string email, string fullName, string subject, string message, CancellationToken cancellationToken)
    {
        var server = _configuration["SMTP_SERVER"] ?? _configuration["Smtp:Server"];
        var login = _configuration["SMTP_LOGIN"] ?? _configuration["Smtp:Login"];
        var password = _configuration["SMTP_PASSWORD"] ?? _configuration["Smtp:Password"];
        var port = int.TryParse(_configuration["SMTP_PORT"] ?? _configuration["Smtp:Port"], out var parsed) ? parsed : 587;

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("SMTP is not configured, printing email for {Email}: {Message}", email, message);
            return true;
        }

        try
        {
            var mime = new MimeMessage();
            mime.From.Add(MailboxAddress.Parse(login));
            mime.To.Add(MailboxAddress.Parse(email));
            mime.Subject = subject;
            mime.Body = new TextPart("plain")
            {
                Text = $"Добрый день, {fullName}!\n{message}"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(server, port, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(login, password, cancellationToken);
            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            _logger.LogInformation("Email sent to {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", email);
            return false;
        }
    }
}
