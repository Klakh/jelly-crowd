using Jellyfin.Plugin.JellyCrowd.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.JellyCrowd;

/// <summary>
/// Registers the plugin's services into the Jellyfin dependency injection container at startup.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
  /// <inheritdoc />
  public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
  {
    serviceCollection.AddSingleton<ITmdbClient, TmdbClient>();
  }
}
