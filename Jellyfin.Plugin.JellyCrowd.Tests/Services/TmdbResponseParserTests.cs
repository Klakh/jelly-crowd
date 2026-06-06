using Jellyfin.Plugin.JellyCrowd.Services;
using Xunit;

namespace Jellyfin.Plugin.JellyCrowd.Tests.Services;

/// <summary>
/// Tests for <see cref="TmdbResponseParser"/>.
/// </summary>
public class TmdbResponseParserTests
{
  private const string ListJson = """
  {
    "page": 1,
    "results": [
      { "id": 1, "media_type": "movie", "title": "Movie A", "overview": "o", "poster_path": "/p.jpg", "release_date": "2020-01-01", "vote_average": 7.5 },
      { "id": 2, "media_type": "tv", "name": "Show B", "first_air_date": "2019-05-05", "vote_average": 8.1 },
      { "id": 3, "media_type": "person", "name": "Someone" }
    ]
  }
  """;

  [Fact]
  public void ParseResults_SkipsPeople_AndMapsMovieAndTv()
  {
    var items = TmdbResponseParser.ParseResults(ListJson);

    Assert.Equal(2, items.Count);

    var movie = items[0];
    Assert.Equal(1, movie.TmdbId);
    Assert.Equal("movie", movie.MediaType);
    Assert.Equal("Movie A", movie.Title);
    Assert.Equal("2020-01-01", movie.ReleaseDate);
    Assert.Equal(7.5, movie.VoteAverage);
    Assert.False(movie.Available);

    var show = items[1];
    Assert.Equal(2, show.TmdbId);
    Assert.Equal("tv", show.MediaType);
    Assert.Equal("Show B", show.Title);
    Assert.Equal("2019-05-05", show.ReleaseDate);
  }

  [Fact]
  public void ParseResults_WithDefaultMediaType_AppliesWhenMissing()
  {
    const string json = """{ "results": [ { "id": 42, "title": "No Type", "release_date": "2022-02-02" } ] }""";

    var items = TmdbResponseParser.ParseResults(json, "movie");

    Assert.Single(items);
    Assert.Equal("movie", items[0].MediaType);
    Assert.Equal("No Type", items[0].Title);
  }

  [Fact]
  public void ParseResults_NoResultsArray_ReturnsEmpty()
  {
    Assert.Empty(TmdbResponseParser.ParseResults("""{ "status": "ok" }"""));
  }

  [Fact]
  public void ParseDetails_MapsSingleEntity()
  {
    const string json = """{ "id": 10, "title": "Detail Movie", "overview": "d", "release_date": "2021-02-02", "vote_average": 6.0 }""";

    var item = TmdbResponseParser.ParseDetails(json, "movie");

    Assert.NotNull(item);
    Assert.Equal(10, item!.TmdbId);
    Assert.Equal("Detail Movie", item.Title);
    Assert.Equal("2021-02-02", item.ReleaseDate);
  }
}
