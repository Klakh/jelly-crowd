using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyCrowd.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyCrowd.Services;

/// <summary>
/// Default <see cref="IRequestReconciler"/>: marks approved requests available (and notifies) once
/// the matching title is found in the Jellyfin library, recording the library item id.
/// </summary>
public sealed class RequestReconciler : IRequestReconciler
{
  private readonly IRequestStore _store;
  private readonly ILibraryMatcher _libraryMatcher;
  private readonly INotificationService _notificationService;
  private readonly ILogger<RequestReconciler> _logger;

  /// <summary>
  /// Initializes a new instance of the <see cref="RequestReconciler"/> class.
  /// </summary>
  /// <param name="store">The request store.</param>
  /// <param name="libraryMatcher">The library matcher.</param>
  /// <param name="notificationService">The notification service.</param>
  /// <param name="logger">The logger.</param>
  public RequestReconciler(IRequestStore store, ILibraryMatcher libraryMatcher, INotificationService notificationService, ILogger<RequestReconciler> logger)
  {
    _store = store;
    _libraryMatcher = libraryMatcher;
    _notificationService = notificationService;
    _logger = logger;
  }

  /// <inheritdoc />
  public async Task<int> ReconcileAsync(CancellationToken cancellationToken)
  {
    var all = await _store.GetAllAsync(cancellationToken).ConfigureAwait(false);
    var resolved = 0;

    foreach (var request in all)
    {
      if (request.Status != RequestStatus.Approved)
      {
        continue;
      }

      cancellationToken.ThrowIfCancellationRequested();

      var itemId = _libraryMatcher.FindItemId(request.MediaType, request.TmdbId);
      if (itemId is not null)
      {
        await _store.MarkAvailableAsync(request.Id, itemId, cancellationToken).ConfigureAwait(false);
        await _notificationService.NotifyRequestEventAsync(request, NotificationEvent.Available, cancellationToken).ConfigureAwait(false);
        resolved++;
      }
    }

    if (resolved > 0)
    {
      _logger.LogInformation("Jelly Crowd reconcile: {Resolved} request(s) marked available.", resolved);
    }

    return resolved;
  }
}
