using System.Collections.ObjectModel;
using Jellyfin.Plugin.JellyCrowd.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.JellyCrowd.Configuration;

/// <summary>
/// Jelly Crowd plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
  /// <summary>
  /// Default per-user disk quota (in bytes) applied when no explicit override exists. 0 means unlimited.
  /// </summary>
  public const long DefaultQuotaBytes = 50L * 1024 * 1024 * 1024; // 50 GiB

  /// <summary>
  /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
  /// </summary>
  public PluginConfiguration()
  {
    TmdbApiKey = string.Empty;
    DefaultUserQuotaBytes = DefaultQuotaBytes;
    RequireApproval = true;
    EstimatedMovieSizeBytes = 4L * 1024 * 1024 * 1024; // 4 GiB
    EstimatedEpisodeSizeBytes = 1L * 1024 * 1024 * 1024; // 1 GiB
    MaxRequestsPerPeriod = 0;
    RequestPeriod = RequestPeriod.Week;
    DiscordWebhookUrl = string.Empty;
    SmtpHost = string.Empty;
    SmtpPort = 587;
    SmtpUseSsl = true;
    SmtpUsername = string.Empty;
    SmtpPassword = string.Empty;
    SmtpFromAddress = string.Empty;
    NotificationEmailTo = string.Empty;
  }

  /// <summary>
  /// Gets or sets the TMDB API key used to power the discovery catalog.
  /// </summary>
  public string TmdbApiKey { get; set; }

  /// <summary>
  /// Gets or sets the default per-user disk quota in bytes. 0 means unlimited.
  /// </summary>
  public long DefaultUserQuotaBytes { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether new requests require admin approval before fulfillment.
  /// </summary>
  public bool RequireApproval { get; set; }

  /// <summary>
  /// Gets or sets the estimated size (in bytes) of a movie, used for quota pre-checks before the real size is known.
  /// </summary>
  public long EstimatedMovieSizeBytes { get; set; }

  /// <summary>
  /// Gets or sets the estimated size (in bytes) of a single episode, used for quota pre-checks.
  /// </summary>
  public long EstimatedEpisodeSizeBytes { get; set; }

  /// <summary>
  /// Gets or sets the maximum number of requests a user may make per <see cref="RequestPeriod"/>. 0 means unlimited.
  /// </summary>
  public int MaxRequestsPerPeriod { get; set; }

  /// <summary>
  /// Gets or sets the rolling window for the request rate limit.
  /// </summary>
  public RequestPeriod RequestPeriod { get; set; }

  /// <summary>
  /// Gets the per-user quota overrides. A user not listed here uses <see cref="DefaultUserQuotaBytes"/>.
  /// </summary>
  public Collection<UserQuotaOverride> QuotaOverrides { get; } = new();

  /// <summary>
  /// Gets or sets the Discord webhook URL used for request notifications. Empty disables Discord.
  /// </summary>
  public string DiscordWebhookUrl { get; set; }

  /// <summary>
  /// Gets or sets the SMTP server host for email notifications. Empty disables email.
  /// </summary>
  public string SmtpHost { get; set; }

  /// <summary>
  /// Gets or sets the SMTP server port.
  /// </summary>
  public int SmtpPort { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether SMTP uses SSL/TLS.
  /// </summary>
  public bool SmtpUseSsl { get; set; }

  /// <summary>
  /// Gets or sets the SMTP username (empty for unauthenticated relays).
  /// </summary>
  public string SmtpUsername { get; set; }

  /// <summary>
  /// Gets or sets the SMTP password.
  /// </summary>
  public string SmtpPassword { get; set; }

  /// <summary>
  /// Gets or sets the "from" address for notification emails.
  /// </summary>
  public string SmtpFromAddress { get; set; }

  /// <summary>
  /// Gets or sets the recipient address for notification emails (typically the admin/ops mailbox).
  /// </summary>
  public string NotificationEmailTo { get; set; }
}
