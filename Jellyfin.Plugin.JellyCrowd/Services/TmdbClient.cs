using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyCrowd.Models;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyCrowd.Services;

/// <summary>
/// Default <see cref="ITmdbClient"/> implementation backed by the TMDB v3 REST API.
/// </summary>
public class TmdbClient : ITmdbClient
{
  private const string BaseUrl = "https://api.themoviedb.org/3";

  private readonly IHttpClientFactory _httpClientFactory;
  private readonly ILogger<TmdbClient> _logger;

  /// <summary>
  /// Initializes a new instance of the <see cref="TmdbClient"/> class.
  /// </summary>
  /// <param name="httpClientFactory">The HTTP client factory.</param>
  /// <param name="logger">The logger.</param>
  public TmdbClient(IHttpClientFactory httpClientFactory, ILogger<TmdbClient> logger)
  {
    _httpClientFactory = httpClientFactory;
    _logger = logger;
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<CatalogItem>> GetTrendingAsync(string language, CancellationToken cancellationToken)
  {
    var json = await GetAsync($"/trending/all/week?language={Escape(language)}", cancellationToken).ConfigureAwait(false);
    return TmdbResponseParser.ParseResults(json);
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<CatalogItem>> SearchAsync(string query, string language, CancellationToken cancellationToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(query);

    var json = await GetAsync($"/search/multi?query={Escape(query)}&language={Escape(language)}&include_adult=false", cancellationToken).ConfigureAwait(false);
    return TmdbResponseParser.ParseResults(json);
  }

  /// <inheritdoc />
  public async Task<CatalogItem?> GetDetailsAsync(string mediaType, int tmdbId, string language, CancellationToken cancellationToken)
  {
    if (!string.Equals(mediaType, "movie", StringComparison.Ordinal)
        && !string.Equals(mediaType, "tv", StringComparison.Ordinal))
    {
      throw new ArgumentException("Media type must be 'movie' or 'tv'.", nameof(mediaType));
    }

    var id = tmdbId.ToString(CultureInfo.InvariantCulture);
    var json = await GetAsync($"/{mediaType}/{id}?language={Escape(language)}", cancellationToken).ConfigureAwait(false);
    return TmdbResponseParser.ParseDetails(json, mediaType);
  }

  private static string Escape(string value) => Uri.EscapeDataString(value ?? string.Empty);

  private async Task<string> GetAsync(string relativePathWithQuery, CancellationToken cancellationToken)
  {
    var apiKey = Plugin.Instance?.Configuration.TmdbApiKey ?? string.Empty;
    if (string.IsNullOrWhiteSpace(apiKey))
    {
      throw new InvalidOperationException("TMDB API key is not configured.");
    }

    _logger.LogDebug("Requesting TMDB {Path}", relativePathWithQuery);

    var separator = relativePathWithQuery.Contains('?', StringComparison.Ordinal) ? '&' : '?';
    var uri = new Uri($"{BaseUrl}{relativePathWithQuery}{separator}api_key={Escape(apiKey)}");

    var client = _httpClientFactory.CreateClient(NamedClient.Default);
    using var response = await client.GetAsync(uri, cancellationToken).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
  }
}
