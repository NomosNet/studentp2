using MailKit.Net.Smtp;
using MailKit.Security;
using Mailtrap;
using Mailtrap.Emails.Requests;
using Mailtrap.Emails.Responses;
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
        var apiToken = Config("MAILTRAP_API_TOKEN", "Mailtrap:ApiToken");
        if (!string.IsNullOrWhiteSpace(apiToken))
        {
            return await SendWithMailtrapAsync(apiToken, email, fullName, subject, message, cancellationToken);
        }

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

    private async Task<bool> SendWithMailtrapAsync(
        string apiToken,
        string email,
        string fullName,
        string subject,
        string message,
        CancellationToken cancellationToken)
    {
        var fromEmail = Config("MAILTRAP_FROM_EMAIL", "Mailtrap:FromEmail");
        var fromName = Config("MAILTRAP_FROM_NAME", "Mailtrap:FromName") ?? "Mailtrap Test";
        var category = Config("MAILTRAP_CATEGORY", "Mailtrap:Category") ?? "Integration Test";

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogError("Mailtrap From address is not configured");
            return false;
        }

        try
        {
            using var mailtrapClientFactory = new MailtrapClientFactory(apiToken);
            IMailtrapClient mailtrapClient = mailtrapClientFactory.CreateClient();
            SendEmailRequest request = SendEmailRequest
                .Create()
                .From(fromEmail, fromName)
                .To(email)
                .Subject(subject)
                .Category(category)
                .Text($"Добрый день, {fullName}!\n{message}");

            SendEmailResponse? response = await mailtrapClient
                .Email()
                .Send(request, cancellationToken);

            _logger.LogInformation("Email sent to {Email} via Mailtrap: {Response}", email, response);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending email to {Email}", email);
            return false;
        }
    }

    private string? Config(string environmentKey, string sectionKey)
    {
        var fromEnvironment = _configuration[environmentKey];
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var fromSection = _configuration[sectionKey];
        return string.IsNullOrWhiteSpace(fromSection) ? null : fromSection;
    }
}
