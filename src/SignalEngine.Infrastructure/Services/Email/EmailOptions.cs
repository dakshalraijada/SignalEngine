namespace SignalEngine.Infrastructure.Services.Email;

/// <summary>
/// Configuration options for SMTP email delivery.
/// </summary>
public class EmailOptions
{
    /// <summary>
    /// SMTP server hostname (e.g., "smtp.sendgrid.net").
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port (e.g., 587 for TLS, 465 for SSL, 25 for plain).
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Whether to use SSL/TLS for the SMTP connection.
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// SMTP authentication username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP authentication password.
    /// This value MUST NOT be logged.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// The "From" email address for outgoing emails.
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Whether email sending is enabled.
    /// When false, emails will not be sent (useful for development).
    /// </summary>
    public bool Enabled { get; set; } = true;
}
