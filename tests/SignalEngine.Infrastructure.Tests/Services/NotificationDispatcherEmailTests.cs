using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Services;
using SignalEngine.Infrastructure.Services.Email;
using Xunit;

namespace SignalEngine.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for NotificationDispatcher email functionality.
/// Tests focus on IEmailSender success/failure scenarios.
/// </summary>
public class NotificationDispatcherEmailTests
{
    private readonly ILookupRepository _lookupRepository;
    private readonly HttpClient _httpClient;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<NotificationDispatcher> _logger;
    private readonly NotificationDispatcher _dispatcher;

    public NotificationDispatcherEmailTests()
    {
        _lookupRepository = Substitute.For<ILookupRepository>();
        _httpClient = new HttpClient();
        _emailSender = Substitute.For<IEmailSender>();
        _logger = Substitute.For<ILogger<NotificationDispatcher>>();

        _dispatcher = new NotificationDispatcher(
            _lookupRepository,
            _httpClient,
            _emailSender,
            _logger);
    }

    [Fact]
    public async Task SendEmailAsync_WhenEmailSenderSucceeds_ReturnsTrue()
    {
        // Arrange
        var notification = CreateEmailNotification("user@example.com", "Test Subject", "Test Body");

        _lookupRepository
            .ResolveLookupCodeAsync(notification.ChannelTypeId, Arg.Any<CancellationToken>())
            .Returns(NotificationChannelTypeCodes.Email);

        _emailSender
            .SendAsync(
                notification.Recipient,
                notification.Subject,
                notification.Body,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _dispatcher.DispatchAsync(notification);

        // Assert
        result.Should().BeTrue();
        await _emailSender.Received(1).SendAsync(
            notification.Recipient,
            notification.Subject,
            notification.Body,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendEmailAsync_WhenEmailSenderThrows_ReturnsFalse()
    {
        // Arrange
        var notification = CreateEmailNotification("user@example.com", "Test Subject", "Test Body");

        _lookupRepository
            .ResolveLookupCodeAsync(notification.ChannelTypeId, Arg.Any<CancellationToken>())
            .Returns(NotificationChannelTypeCodes.Email);

        _emailSender
            .SendAsync(
                notification.Recipient,
                notification.Subject,
                notification.Body,
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP connection failed"));

        // Act
        var result = await _dispatcher.DispatchAsync(notification);

        // Assert
        result.Should().BeFalse();
    }

    private static Notification CreateEmailNotification(string recipient, string subject, string body)
    {
        return new Notification(
            tenantId: 1,
            signalId: 100,
            channelTypeId: 1, // Email channel
            recipient: recipient,
            subject: subject,
            body: body);
    }
}
