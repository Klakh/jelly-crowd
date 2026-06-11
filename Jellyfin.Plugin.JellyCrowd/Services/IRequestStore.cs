using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyCrowd.Models;

namespace Jellyfin.Plugin.JellyCrowd.Services;

/// <summary>
/// Persistence for media requests.
/// </summary>
public interface IRequestStore
{
  /// <summary>
  /// Creates a new request (assigns its id, timestamp and <see cref="RequestStatus.Pending"/> status).
  /// </summary>
  /// <param name="record">The request to create.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>The stored request.</returns>
  Task<RequestRecord> CreateAsync(RequestRecord record, CancellationToken cancellationToken);

  /// <summary>
  /// Gets every request, newest first.
  /// </summary>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>All requests.</returns>
  Task<IReadOnlyList<RequestRecord>> GetAllAsync(CancellationToken cancellationToken);

  /// <summary>
  /// Gets the requests made by a given user, newest first.
  /// </summary>
  /// <param name="userId">The user identifier.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>The user's requests.</returns>
  Task<IReadOnlyList<RequestRecord>> GetByUserAsync(Guid userId, CancellationToken cancellationToken);

  /// <summary>
  /// Gets a single request by id.
  /// </summary>
  /// <param name="id">The request identifier.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>The request, or <c>null</c> if not found.</returns>
  Task<RequestRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

  /// <summary>
  /// Updates the status of a request and stamps the deciding administrator and time.
  /// </summary>
  /// <param name="id">The request identifier.</param>
  /// <param name="status">The new status.</param>
  /// <param name="decidedBy">The administrator making the decision.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>The updated request, or <c>null</c> if not found.</returns>
  Task<RequestRecord?> UpdateStatusAsync(Guid id, RequestStatus status, Guid decidedBy, CancellationToken cancellationToken);

  /// <summary>
  /// Determines whether the user already has a non-denied request for the same title.
  /// </summary>
  /// <param name="userId">The user identifier.</param>
  /// <param name="tmdbId">The TMDB identifier.</param>
  /// <param name="mediaType">The media type.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns><c>true</c> when an active duplicate exists.</returns>
  Task<bool> ExistsActiveAsync(Guid userId, int tmdbId, string mediaType, CancellationToken cancellationToken);

  /// <summary>
  /// Counts the user's non-denied requests created at or after the given UTC instant (for rate limiting).
  /// </summary>
  /// <param name="userId">The user identifier.</param>
  /// <param name="sinceUtc">The window start (UTC).</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>The number of requests in the window.</returns>
  Task<int> CountUserRequestsSinceAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken);
}
