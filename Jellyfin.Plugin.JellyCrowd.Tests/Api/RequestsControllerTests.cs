using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyCrowd.Api;
using Jellyfin.Plugin.JellyCrowd.Models;
using Jellyfin.Plugin.JellyCrowd.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Jellyfin.Plugin.JellyCrowd.Tests.Api;

/// <summary>
/// Tests for <see cref="RequestsController"/>.
/// </summary>
public class RequestsControllerTests
{
  private static readonly Guid User = Guid.NewGuid();

  private static RequestsController CreateController(IRequestStore store, Guid? userId = null)
  {
    var controller = new RequestsController(store, new FakeUserAccessor(userId ?? User))
    {
      ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };

    return controller;
  }

  private static CreateRequestDto ValidDto()
    => new() { TmdbId = 100, MediaType = "movie", Title = "Dune" };

  [Fact]
  public async Task Create_Valid_ReturnsOkPendingForCurrentUser()
  {
    var controller = CreateController(new FakeRequestStore());

    var result = await controller.Create(ValidDto(), CancellationToken.None);

    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var record = Assert.IsType<RequestRecord>(ok.Value);
    Assert.Equal(User, record.UserId);
    Assert.Equal(RequestStatus.Pending, record.Status);
  }

  [Fact]
  public async Task Create_InvalidMediaType_ReturnsBadRequest()
  {
    var controller = CreateController(new FakeRequestStore());

    var result = await controller.Create(new CreateRequestDto { TmdbId = 1, MediaType = "book", Title = "X" }, CancellationToken.None);

    Assert.IsType<BadRequestObjectResult>(result.Result);
  }

  [Fact]
  public async Task Create_MissingTitle_ReturnsBadRequest()
  {
    var controller = CreateController(new FakeRequestStore());

    var result = await controller.Create(new CreateRequestDto { TmdbId = 1, MediaType = "movie", Title = "  " }, CancellationToken.None);

    Assert.IsType<BadRequestObjectResult>(result.Result);
  }

  [Fact]
  public async Task Create_Duplicate_ReturnsConflict()
  {
    var store = new FakeRequestStore();
    var controller = CreateController(store);
    await controller.Create(ValidDto(), CancellationToken.None);

    var result = await controller.Create(ValidDto(), CancellationToken.None);

    Assert.IsType<ConflictObjectResult>(result.Result);
  }

  [Fact]
  public async Task Mine_ReturnsOnlyCurrentUserRequests()
  {
    var store = new FakeRequestStore();
    await CreateController(store, User).Create(ValidDto(), CancellationToken.None);
    await CreateController(store, Guid.NewGuid()).Create(new CreateRequestDto { TmdbId = 7, MediaType = "tv", Title = "Y" }, CancellationToken.None);

    var result = await CreateController(store, User).Mine(CancellationToken.None);

    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var items = Assert.IsAssignableFrom<IReadOnlyList<RequestRecord>>(ok.Value);
    Assert.Single(items);
    Assert.Equal(User, items[0].UserId);
  }

  [Fact]
  public async Task Approve_Existing_ReturnsApproved()
  {
    var store = new FakeRequestStore();
    var create = (OkObjectResult)(await CreateController(store).Create(ValidDto(), CancellationToken.None)).Result!;
    var id = ((RequestRecord)create.Value!).Id;

    var result = await CreateController(store).Approve(id, CancellationToken.None);

    var ok = Assert.IsType<OkObjectResult>(result.Result);
    Assert.Equal(RequestStatus.Approved, ((RequestRecord)ok.Value!).Status);
  }

  [Fact]
  public async Task Deny_Missing_ReturnsNotFound()
  {
    var result = await CreateController(new FakeRequestStore()).Deny(Guid.NewGuid(), CancellationToken.None);

    Assert.IsType<NotFoundResult>(result.Result);
  }

  private sealed class FakeUserAccessor : ICurrentUserAccessor
  {
    private readonly Guid _userId;

    public FakeUserAccessor(Guid userId) => _userId = userId;

    public Task<Guid> GetUserIdAsync(HttpRequest request) => Task.FromResult(_userId);
  }

  private sealed class FakeRequestStore : IRequestStore
  {
    private readonly List<RequestRecord> _items = new();

    public Task<RequestRecord> CreateAsync(RequestRecord record, CancellationToken cancellationToken)
    {
      record.Id = Guid.NewGuid();
      record.Status = RequestStatus.Pending;
      record.RequestedAt = DateTime.UtcNow;
      _items.Add(record);
      return Task.FromResult(record);
    }

    public Task<IReadOnlyList<RequestRecord>> GetAllAsync(CancellationToken cancellationToken)
      => Task.FromResult<IReadOnlyList<RequestRecord>>(_items.ToList());

    public Task<IReadOnlyList<RequestRecord>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
      => Task.FromResult<IReadOnlyList<RequestRecord>>(_items.Where(r => r.UserId == userId).ToList());

    public Task<RequestRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
      => Task.FromResult(_items.FirstOrDefault(r => r.Id == id));

    public Task<RequestRecord?> UpdateStatusAsync(Guid id, RequestStatus status, Guid decidedBy, CancellationToken cancellationToken)
    {
      var record = _items.FirstOrDefault(r => r.Id == id);
      if (record is not null)
      {
        record.Status = status;
        record.DecidedBy = decidedBy;
        record.DecidedAt = DateTime.UtcNow;
      }

      return Task.FromResult(record);
    }

    public Task<bool> ExistsActiveAsync(Guid userId, int tmdbId, string mediaType, CancellationToken cancellationToken)
      => Task.FromResult(_items.Any(r =>
        r.UserId == userId && r.TmdbId == tmdbId
        && string.Equals(r.MediaType, mediaType, StringComparison.Ordinal)
        && r.Status != RequestStatus.Denied));
  }
}
