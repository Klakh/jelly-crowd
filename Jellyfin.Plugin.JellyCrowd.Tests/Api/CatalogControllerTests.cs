using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyCrowd.Api;
using Jellyfin.Plugin.JellyCrowd.Models;
using Jellyfin.Plugin.JellyCrowd.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.JellyCrowd.Tests.Api;

/// <summary>
/// Tests for <see cref="CatalogController"/>.
/// </summary>
public class CatalogControllerTests
{
  private static CatalogController CreateController(ITmdbClient client)
    => new(client, NullLogger<CatalogController>.Instance);

  [Fact]
  public async Task GetTrending_ReturnsOkWithItems()
  {
    var items = new List<CatalogItem> { new() { TmdbId = 1, MediaType = "movie", Title = "A" } };
    var controller = CreateController(new FakeTmdbClient { Results = items });

    var result = await controller.GetTrending(null, CancellationToken.None);

    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var payload = Assert.IsAssignableFrom<IReadOnlyList<CatalogItem>>(ok.Value);
    Assert.Single(payload);
  }

  [Fact]
  public async Task Search_WithEmptyQuery_ReturnsBadRequest()
  {
    var controller = CreateController(new FakeTmdbClient());

    var result = await controller.Search("   ", null, CancellationToken.None);

    Assert.IsType<BadRequestObjectResult>(result.Result);
  }

  [Fact]
  public async Task Search_WithQuery_ReturnsOk()
  {
    var controller = CreateController(new FakeTmdbClient { Results = new List<CatalogItem>() });

    var result = await controller.Search("matrix", "fr-FR", CancellationToken.None);

    Assert.IsType<OkObjectResult>(result.Result);
  }

  [Fact]
  public async Task GetDetails_WithInvalidMediaType_ReturnsBadRequest()
  {
    var controller = CreateController(new FakeTmdbClient());

    var result = await controller.GetDetails("book", 1, null, CancellationToken.None);

    Assert.IsType<BadRequestObjectResult>(result.Result);
  }

  [Fact]
  public async Task GetDetails_WhenNotFound_Returns404()
  {
    var controller = CreateController(new FakeTmdbClient { Detail = null });

    var result = await controller.GetDetails("movie", 99, null, CancellationToken.None);

    Assert.IsType<NotFoundResult>(result.Result);
  }

  [Fact]
  public async Task GetTrending_WhenNotConfigured_Returns503()
  {
    var controller = CreateController(new FakeTmdbClient { Throw = new InvalidOperationException("no key") });

    var result = await controller.GetTrending(null, CancellationToken.None);

    var obj = Assert.IsType<ObjectResult>(result.Result);
    Assert.Equal(StatusCodes.Status503ServiceUnavailable, obj.StatusCode);
  }

  private sealed class FakeTmdbClient : ITmdbClient
  {
    public IReadOnlyList<CatalogItem> Results { get; set; } = new List<CatalogItem>();

    public CatalogItem? Detail { get; set; } = new() { TmdbId = 1, MediaType = "movie", Title = "Detail" };

    public Exception? Throw { get; set; }

    public Task<IReadOnlyList<CatalogItem>> GetTrendingAsync(string language, CancellationToken cancellationToken)
    {
      if (Throw is not null)
      {
        throw Throw;
      }

      return Task.FromResult(Results);
    }

    public Task<IReadOnlyList<CatalogItem>> SearchAsync(string query, string language, CancellationToken cancellationToken)
    {
      if (Throw is not null)
      {
        throw Throw;
      }

      return Task.FromResult(Results);
    }

    public Task<CatalogItem?> GetDetailsAsync(string mediaType, int tmdbId, string language, CancellationToken cancellationToken)
    {
      if (Throw is not null)
      {
        throw Throw;
      }

      return Task.FromResult(Detail);
    }
  }
}
