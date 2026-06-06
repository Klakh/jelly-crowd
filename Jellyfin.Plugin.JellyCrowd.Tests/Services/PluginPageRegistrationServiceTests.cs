using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyCrowd.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.JellyCrowd.Tests.Services;

/// <summary>
/// Tests for <see cref="PluginPageRegistrationService"/>.
/// </summary>
public class PluginPageRegistrationServiceTests
{
  [Fact]
  public async Task StartAsync_WithoutPluginPages_DoesNotThrow()
  {
    var provider = new ServiceCollection().BuildServiceProvider();
    var service = new PluginPageRegistrationService(provider, NullLogger<PluginPageRegistrationService>.Instance);

    // Plugin Pages is not registered (and its assembly is absent at test runtime): the service
    // must swallow that and let startup continue.
    await service.StartAsync(CancellationToken.None);
    await service.StopAsync(CancellationToken.None);

    Assert.True(true);
  }
}
