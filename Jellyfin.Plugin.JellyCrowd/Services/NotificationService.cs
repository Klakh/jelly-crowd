using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyCrowd.Configuration;
using Jellyfin.Plugin.JellyCrowd.Models;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyCrowd.Services;

/// <summary>
/// Default <see cref="INotificationService"/> delivering to a Discord webhook and/or SMTP email.
/// Each channel is optional (enabled when configured) and failures are swallowed and logged.
/// </summary>
public sealed class NotificationService : INotificationService
{
  private const int SmtpTimeoutMs = 15000;

  private readonly IHttpClientFactory _httpClientFactory;
  private readonly ILogger<NotificationService> _logger;

  /// <summary>
  /// Initializes a new instance of the <see cref="NotificationService"/> class.
  /// </summary>
  /// <param name="httpClientFactory">The HTTP client factory (for Discord).</param>
  /// <param name="logger">The logger.</param>
  public NotificationService(IHttpClientFactory httpClientFactory, ILogger<NotificationService> logger)
  {
    _httpClientFactory = httpClientFactory;
    _logger = logger;
  }

  /// <inheritdoc />
  public async Task NotifyRequestEventAsync(RequestRecord request, NotificationEvent notificationEvent, CancellationToken cancellationToken)
  {
    var config = Plugin.Instance?.Configuration;
    if (config is null)
    {
      return;
    }

    var (subject, body) = NotificationMessages.Build(request, notificationEvent);

    await SendDiscordAsync(config, subject, body, cancellationToken).ConfigureAwait(false);
    await SendEmailAsync(config, subject, body, cancellationToken).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task SendTestAsync(string channel, CancellationToken cancellationToken)
  {
    var config = Plugin.Instance?.Configuration ?? throw new InvalidOperationException("Plugin is not initialized.");
    const string Subject = "Jelly Crowd test notification";
    const string Body = "This is a test notification from Jelly Crowd. If you can read this, the channel works.";

    if (string.Equals(channel, "discord", StringComparison.OrdinalIgnoreCase))
    {
      if (string.IsNullOrWhiteSpace(config.DiscordWebhookUrl))
      {
        throw new InvalidOperationException("The Discord webhook URL is not configured.");
      }

      await SendDiscordCoreAsync(config, Subject, Body, cancellationToken).ConfigureAwait(false);
    }
    else if (string.Equals(channel, "email", StringComparison.OrdinalIgnoreCase))
    {
      if (string.IsNullOrWhiteSpace(config.SmtpHost)
          || string.IsNullOrWhiteSpace(config.SmtpFromAddress)
          || string.IsNullOrWhiteSpace(config.NotificationEmailTo))
      {
        throw new InvalidOperationException("SMTP is not fully configured (host, from address and recipient are required).");
      }

      await SendEmailCoreAsync(config, Subject, Body, cancellationToken).ConfigureAwait(false);
    }
    else
    {
      throw new ArgumentException("Unknown notification channel.", nameof(channel));
    }
  }

  private async Task SendDiscordAsync(PluginConfiguration config, string subject, string body, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(config.DiscordWebhookUrl))
    {
      return;
    }

    try
    {
      await SendDiscordCoreAsync(config, subject, body, cancellationToken).ConfigureAwait(false);
    }
#pragma warning disable CA1031 // A notification failure must never affect the request flow.
    catch (Exception ex)
#pragma warning restore CA1031
    {
      _logger.LogWarning(ex, "Failed to send Discord notification.");
    }
  }

  private async Task SendEmailAsync(PluginConfiguration config, string subject, string body, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(config.SmtpHost)
        || string.IsNullOrWhiteSpace(config.SmtpFromAddress)
        || string.IsNullOrWhiteSpace(config.NotificationEmailTo))
    {
      return;
    }

    try
    {
      await SendEmailCoreAsync(config, subject, body, cancellationToken).ConfigureAwait(false);
    }
#pragma warning disable CA1031 // A notification failure must never affect the request flow.
    catch (Exception ex)
#pragma warning restore CA1031
    {
      _logger.LogWarning(ex, "Failed to send email notification.");
    }
  }

  private async Task SendDiscordCoreAsync(PluginConfiguration config, string subject, string body, CancellationToken cancellationToken)
  {
    var client = _httpClientFactory.CreateClient(NamedClient.Default);
    var payload = JsonSerializer.Serialize(new { content = "**" + subject + "**\n" + body });
    using var content = new StringContent(payload, Encoding.UTF8, "application/json");
    using var response = await client.PostAsync(new Uri(config.DiscordWebhookUrl), content, cancellationToken).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();
  }

  private async Task SendEmailCoreAsync(PluginConfiguration config, string subject, string body, CancellationToken cancellationToken)
  {
    using var message = new MailMessage(config.SmtpFromAddress, config.NotificationEmailTo, subject, body);
    using var smtp = new SmtpClient(config.SmtpHost, config.SmtpPort)
    {
      EnableSsl = config.SmtpUseSsl,
      Timeout = SmtpTimeoutMs
    };

    if (!string.IsNullOrWhiteSpace(config.SmtpUsername))
    {
      smtp.Credentials = new NetworkCredential(config.SmtpUsername, config.SmtpPassword);
    }

    await smtp.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
  }
}
