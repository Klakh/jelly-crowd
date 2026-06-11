using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.JellyCrowd.Services;

/// <summary>
/// Default <see cref="ILibraryMatcher"/> backed by <see cref="ILibraryManager"/>, matching on the
/// TMDB provider id.
/// </summary>
public sealed class LibraryMatcher : ILibraryMatcher
{
  private readonly ILibraryManager _libraryManager;

  /// <summary>
  /// Initializes a new instance of the <see cref="LibraryMatcher"/> class.
  /// </summary>
  /// <param name="libraryManager">The Jellyfin library manager.</param>
  public LibraryMatcher(ILibraryManager libraryManager)
  {
    _libraryManager = libraryManager;
  }

  /// <inheritdoc />
  public bool Exists(string mediaType, int tmdbId)
  {
    var kind = MediaTypeToKind(mediaType);
    if (kind is null)
    {
      return false;
    }

    var query = new InternalItemsQuery
    {
      IncludeItemTypes = new[] { kind.Value },
      HasAnyProviderId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
      {
        [MetadataProvider.Tmdb.ToString()] = tmdbId.ToString(CultureInfo.InvariantCulture)
      },
      Recursive = true,
      Limit = 1
    };

    return _libraryManager.GetItemList(query).Count > 0;
  }

  /// <summary>
  /// Maps a Jelly Crowd media type to the corresponding Jellyfin item kind.
  /// </summary>
  /// <param name="mediaType">The media type (<c>movie</c> or <c>tv</c>).</param>
  /// <returns>The matching <see cref="BaseItemKind"/>, or <c>null</c> if unsupported.</returns>
  public static BaseItemKind? MediaTypeToKind(string mediaType)
  {
    if (string.Equals(mediaType, "movie", StringComparison.Ordinal))
    {
      return BaseItemKind.Movie;
    }

    if (string.Equals(mediaType, "tv", StringComparison.Ordinal))
    {
      return BaseItemKind.Series;
    }

    return null;
  }
}
