namespace SignalEngine.Infrastructure.Services.Email;

/// <summary>
/// Abstraction for sending emails.
/// Enables mocking in tests and decouples from SmtpClient.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends a plain-text email to a single recipient.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="body">Plain-text email body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="Exception">Throws on SMTP failure.</exception>
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
