using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyCrowd.Models;

namespace Jellyfin.Plugin.JellyCrowd.Services;

/// <summary>
/// Delivers request notifications to the configured channels (Discord webhook, email).
/// </summary>
public interface INotificationService
{
  /// <summary>
  /// Sends a notification for a request lifecycle event. Never throws; delivery failures are logged.
  /// </summary>
  /// <param name="request">The request the event concerns.</param>
  /// <param name="notificationEvent">The lifecycle event.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>A task that completes when delivery has been attempted.</returns>
  Task NotifyRequestEventAsync(RequestRecord request, NotificationEvent notificationEvent, CancellationToken cancellationToken);
}
