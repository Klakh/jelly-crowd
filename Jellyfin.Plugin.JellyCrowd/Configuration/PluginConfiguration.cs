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
}
