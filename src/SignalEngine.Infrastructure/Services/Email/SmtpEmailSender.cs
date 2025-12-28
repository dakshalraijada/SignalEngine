using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SignalEngine.Infrastructure.Services.Email;

/// <summary>
/// SMTP-based email sender implementation using System.Net.Mail.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<EmailOptions> options,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug(
                "Email sending is disabled. Would have sent to {Recipient}: {Subject}",
                to,
                subject);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            throw new InvalidOperationException("SMTP Host is not configured");
        }

        if (string.IsNullOrWhiteSpace(_options.From))
        {
            throw new InvalidOperationException("Email From address is not configured");
        }

        _logger.LogDebug(
            "Sending email to {Recipient} via SMTP {Host}:{Port}",
            to,
            _options.Host,
            _options.Port);

        using var message = new MailMessage
        {
            From = new MailAddress(_options.From),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(to));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        // Configure credentials if provided
        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        // Note: SmtpClient.SendMailAsync doesn't support CancellationToken directly
        // We use Task.Run to allow cancellation to abort the operation
        await Task.Run(async () => await client.SendMailAsync(message), cancellationToken);

        _logger.LogDebug(
            "Email sent successfully to {Recipient}",
            to);
    }
}
