using System.Collections.Generic;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.JellyCrowd.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JellyCrowd.Tests.Services;

/// <summary>
/// Tests for <see cref="LibraryMatcher"/>.
/// </summary>
public class LibraryMatcherTests
{
  [Fact]
  public void Exists_ReturnsTrue_WhenLibraryHasMatch()
  {
    var manager = new Mock<ILibraryManager>();
    manager.Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
      .Returns(new List<BaseItem> { new Movie() });

    var matcher = new LibraryMatcher(manager.Object);

    Assert.True(matcher.Exists("movie", 123));
  }

  [Fact]
  public void Exists_ReturnsFalse_WhenLibraryEmpty()
  {
    var manager = new Mock<ILibraryManager>();
    manager.Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
      .Returns(new List<BaseItem>());

    var matcher = new LibraryMatcher(manager.Object);

    Assert.False(matcher.Exists("tv", 99));
  }

  [Fact]
  public void Exists_ReturnsFalse_AndSkipsQuery_ForUnknownMediaType()
  {
    var manager = new Mock<ILibraryManager>();
    var matcher = new LibraryMatcher(manager.Object);

    Assert.False(matcher.Exists("book", 1));
    manager.Verify(m => m.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Never);
  }

  [Theory]
  [InlineData("movie", BaseItemKind.Movie)]
  [InlineData("tv", BaseItemKind.Series)]
  public void MediaTypeToKind_MapsKnownTypes(string mediaType, BaseItemKind expected)
  {
    Assert.Equal(expected, LibraryMatcher.MediaTypeToKind(mediaType));
  }

  [Fact]
  public void MediaTypeToKind_UnknownReturnsNull()
  {
    Assert.Null(LibraryMatcher.MediaTypeToKind("book"));
  }
}
