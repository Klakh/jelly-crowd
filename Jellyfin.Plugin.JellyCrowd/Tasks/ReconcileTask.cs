using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyCrowd.Models;
using Jellyfin.Plugin.JellyCrowd.Services;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyCrowd.Tasks;

/// <summary>
/// Scheduled task that marks approved requests as <see cref="RequestStatus.Available"/> once the
/// corresponding media appears in the Jellyfin library.
/// </summary>
public sealed class ReconcileTask : IScheduledTask
{
  private readonly IRequestStore _store;
  private readonly ILibraryMatcher _libraryMatcher;
  private readonly INotificationService _notificationService;
  private readonly ILogger<ReconcileTask> _logger;

  /// <summary>
  /// Initializes a new instance of the <see cref="ReconcileTask"/> class.
  /// </summary>
  /// <param name="store">The request store.</param>
  /// <param name="libraryMatcher">The library matcher.</param>
  /// <param name="notificationService">The notification service.</param>
  /// <param name="logger">The logger.</param>
  public ReconcileTask(IRequestStore store, ILibraryMatcher libraryMatcher, INotificationService notificationService, ILogger<ReconcileTask> logger)
  {
    _store = store;
    _libraryMatcher = libraryMatcher;
    _notificationService = notificationService;
    _logger = logger;
  }

  /// <inheritdoc />
  public string Name => "Jelly Crowd: reconcile requests";

  /// <inheritdoc />
  public string Key => "JellyCrowdReconcileRequests";

  /// <inheritdoc />
  public string Description => "Marks approved Jelly Crowd requests as available once the media appears in the library.";

  /// <inheritdoc />
  public string Category => "Jelly Crowd";

  /// <inheritdoc />
  public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(progress);

    var all = await _store.GetAllAsync(cancellationToken).ConfigureAwait(false);
    var approved = all.Where(r => r.Status == RequestStatus.Approved).ToList();
    var resolved = 0;

    for (var i = 0; i < approved.Count; i++)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var request = approved[i];
      if (_libraryMatcher.Exists(request.MediaType, request.TmdbId))
      {
        await _store.UpdateStatusAsync(request.Id, RequestStatus.Available, request.DecidedBy ?? Guid.Empty, cancellationToken).ConfigureAwait(false);
        await _notificationService.NotifyRequestEventAsync(request, NotificationEvent.Available, cancellationToken).ConfigureAwait(false);
        resolved++;
      }

      progress.Report((double)(i + 1) / approved.Count * 100);
    }

    if (resolved > 0)
    {
      _logger.LogInformation("Jelly Crowd reconcile: {Resolved} request(s) marked available.", resolved);
    }

    progress.Report(100);
  }

  /// <inheritdoc />
  public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
  {
    return new[]
    {
      new TaskTriggerInfo
      {
        Type = TaskTriggerInfoType.IntervalTrigger,
        IntervalTicks = TimeSpan.FromHours(6).Ticks
      }
    };
  }
}
