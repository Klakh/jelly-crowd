using System;
using System.Collections.Generic;
using System.Text.Json;
using Jellyfin.Plugin.JellyCrowd.Models;

namespace Jellyfin.Plugin.JellyCrowd.Services;

/// <summary>
/// Pure (network-free) parsing of TMDB JSON payloads into <see cref="CatalogItem"/> instances.
/// Kept separate from <see cref="TmdbClient"/> so it can be unit tested without HTTP.
/// </summary>
public static class TmdbResponseParser
{
  private const string MovieType = "movie";
  private const string TvType = "tv";

  /// <summary>
  /// Parses a TMDB list payload (a JSON object with a <c>results</c> array, e.g. trending or search).
  /// Person results and unknown media types are skipped.
  /// </summary>
  /// <param name="json">The raw TMDB JSON payload.</param>
  /// <param name="defaultMediaType">Media type to assume when the payload omits <c>media_type</c> (e.g. a movie-only search). May be <c>null</c>.</param>
  /// <returns>The parsed catalog items.</returns>
  public static IReadOnlyList<CatalogItem> ParseResults(string json, string? defaultMediaType = null)
  {
    ArgumentNullException.ThrowIfNull(json);

    var items = new List<CatalogItem>();
    using var doc = JsonDocument.Parse(json);
    if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
    {
      return items;
    }

    foreach (var element in results.EnumerateArray())
    {
      var item = ParseElement(element, defaultMediaType);
      if (item is not null)
      {
        items.Add(item);
      }
    }

    return items;
  }

  /// <summary>
  /// Parses a single TMDB detail payload (e.g. <c>/movie/{id}</c> or <c>/tv/{id}</c>).
  /// </summary>
  /// <param name="json">The raw TMDB JSON payload.</param>
  /// <param name="mediaType">The media type of the requested entity (<c>movie</c> or <c>tv</c>).</param>
  /// <returns>The parsed item, or <c>null</c> if the type is unsupported.</returns>
  public static CatalogItem? ParseDetails(string json, string mediaType)
  {
    ArgumentNullException.ThrowIfNull(json);

    using var doc = JsonDocument.Parse(json);
    return ParseElement(doc.RootElement, mediaType);
  }

  private static CatalogItem? ParseElement(JsonElement element, string? defaultMediaType)
  {
    var mediaType = GetString(element, "media_type") ?? defaultMediaType;
    if (!string.Equals(mediaType, MovieType, StringComparison.Ordinal)
        && !string.Equals(mediaType, TvType, StringComparison.Ordinal))
    {
      return null;
    }

    var isMovie = string.Equals(mediaType, MovieType, StringComparison.Ordinal);

    return new CatalogItem
    {
      TmdbId = GetInt(element, "id"),
      MediaType = mediaType!,
      Title = GetString(element, isMovie ? "title" : "name")
              ?? GetString(element, "title")
              ?? GetString(element, "name")
              ?? string.Empty,
      Overview = GetString(element, "overview"),
      PosterPath = GetString(element, "poster_path"),
      BackdropPath = GetString(element, "backdrop_path"),
      ReleaseDate = GetString(element, isMovie ? "release_date" : "first_air_date"),
      VoteAverage = GetDouble(element, "vote_average")
    };
  }

  private static string? GetString(JsonElement element, string property)
  {
    if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
    {
      var s = value.GetString();
      return string.IsNullOrEmpty(s) ? null : s;
    }

    return null;
  }

  private static int GetInt(JsonElement element, string property)
  {
    if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i))
    {
      return i;
    }

    return 0;
  }

  private static double GetDouble(JsonElement element, string property)
  {
    if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var d))
    {
      return d;
    }

    return 0d;
  }
}
