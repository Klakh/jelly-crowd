namespace Jellyfin.Plugin.JellyCrowd.Services;

/// <summary>
/// Checks whether a TMDB title already exists in the Jellyfin library.
/// </summary>
public interface ILibraryMatcher
{
  /// <summary>
  /// Determines whether a movie or show with the given TMDB id is present in the library.
  /// </summary>
  /// <param name="mediaType">The media type (<c>movie</c> or <c>tv</c>).</param>
  /// <param name="tmdbId">The TMDB identifier.</param>
  /// <returns><c>true</c> when a matching library item exists.</returns>
  bool Exists(string mediaType, int tmdbId);
}
