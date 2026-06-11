using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyCrowd.Services;

/// <summary>
/// Registers the Jelly Crowd user-facing catalog page with the Plugin Pages plugin at startup.
/// Uses reflection (no compile-time dependency) so the plugin builds and loads cleanly whether or
/// not Plugin Pages is installed.
/// </summary>
public sealed class PluginPageRegistrationService : IHostedService
{
  private const string ManagerTypeName = "Jellyfin.Plugin.PluginPages.Library.IPluginPagesManager";
  private const string PageTypeName = "Jellyfin.Plugin.PluginPages.Library.PluginPage";
  private const string RegisterMethodName = "RegisterPluginPage";

  // URLs must start with '/': Plugin Pages only resolves them via ApiClient.getUrl (honoring the
  // server base path) when they are root-relative; otherwise the fetch 404s against /web/.
  private static readonly (string Id, string Url, string DisplayText, string Icon)[] Pages =
  {
    ("jellycrowd", "/JellyCrowd/Web/catalog.html", "Jelly Crowd", "movie"),
    ("jellycrowd-requests", "/JellyCrowd/Web/requests.html", "My Requests", "playlist_add_check")
  };

  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<PluginPageRegistrationService> _logger;

  /// <summary>
  /// Initializes a new instance of the <see cref="PluginPageRegistrationService"/> class.
  /// </summary>
  /// <param name="serviceProvider">The service provider (used to resolve Plugin Pages, when present).</param>
  /// <param name="logger">The logger.</param>
  public PluginPageRegistrationService(IServiceProvider serviceProvider, ILogger<PluginPageRegistrationService> logger)
  {
    _serviceProvider = serviceProvider;
    _logger = logger;
  }

  /// <inheritdoc />
  public Task StartAsync(CancellationToken cancellationToken)
  {
    try
    {
      RegisterPage();
    }
#pragma warning disable CA1031 // Registration must never break server startup, whatever the failure.
    catch (Exception ex)
#pragma warning restore CA1031
    {
      _logger.LogWarning(ex, "Failed to register the Jelly Crowd page with Plugin Pages.");
    }

    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  private static Type? FindType(string fullName)
  {
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      var type = assembly.GetType(fullName, throwOnError: false);
      if (type is not null)
      {
        return type;
      }
    }

    return null;
  }

  private static void SetProperty(object target, string name, string value)
    => target.GetType().GetProperty(name)?.SetValue(target, value);

  private void RegisterPage()
  {
    var managerType = FindType(ManagerTypeName);
    var pageType = FindType(PageTypeName);
    if (managerType is null || pageType is null)
    {
      _logger.LogInformation(
        "Plugin Pages is not installed; the Jelly Crowd catalog page was not registered. "
        + "Install the 'Plugin Pages' and 'File Transformation' plugins to enable the user-facing page.");
      return;
    }

    var manager = _serviceProvider.GetService(managerType);
    if (manager is null)
    {
      _logger.LogInformation("Plugin Pages is present but its manager service is unavailable; pages not registered.");
      return;
    }

    var register = managerType.GetMethod(RegisterMethodName);
    if (register is null)
    {
      return;
    }

    foreach (var (id, url, displayText, icon) in Pages)
    {
      var page = Activator.CreateInstance(pageType);
      if (page is null)
      {
        continue;
      }

      SetProperty(page, "Id", id);
      SetProperty(page, "Url", url);
      SetProperty(page, "DisplayText", displayText);
      SetProperty(page, "Icon", icon);
      register.Invoke(manager, new[] { page });
    }

    _logger.LogInformation("Registered {Count} Jelly Crowd page(s) with Plugin Pages.", Pages.Length);
  }
}
