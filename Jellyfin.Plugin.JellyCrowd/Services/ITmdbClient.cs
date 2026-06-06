using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyCrowd.Models;

namespace Jellyfin.Plugin.JellyCrowd.Services;

/// <summary>
/// Read access to the TMDB discovery catalog.
/// </summary>
public interface ITmdbClient
{
  /// <summary>
  /// Gets the items trending this week (movies and shows).
  /// </summary>
  /// <param name="language">The TMDB language code (e.g. <c>en-US</c>, <c>fr-FR</c>).</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>The trending catalog items.</returns>
  Task<IReadOnlyList<CatalogItem>> GetTrendingAsync(string language, CancellationToken cancellationToken);

  /// <summary>
  /// Searches movies and shows matching the given query.
  /// </summary>
  /// <param name="query">The free-text search query.</param>
  /// <param name="language">The TMDB language code.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>The matching catalog items.</returns>
  Task<IReadOnlyList<CatalogItem>> SearchAsync(string query, string language, CancellationToken cancellationToken);

  /// <summary>
  /// Gets the details for a single movie or show.
  /// </summary>
  /// <param name="mediaType">The media type (<c>movie</c> or <c>tv</c>).</param>
  /// <param name="tmdbId">The TMDB identifier.</param>
  /// <param name="language">The TMDB language code.</param>
  /// <param name="cancellationToken">The cancellation token.</param>
  /// <returns>The item details, or <c>null</c> if not found.</returns>
  Task<CatalogItem?> GetDetailsAsync(string mediaType, int tmdbId, string language, CancellationToken cancellationToken);
}
