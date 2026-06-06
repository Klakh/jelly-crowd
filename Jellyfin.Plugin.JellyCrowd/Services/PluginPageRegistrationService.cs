using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.PluginPages.Library;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyCrowd.Services;

/// <summary>
/// Registers the Jelly Crowd user-facing catalog page with the Plugin Pages plugin at startup.
/// Tolerates Plugin Pages being absent (including its assembly not being loaded at all) so the
/// plugin always loads.
/// </summary>
public sealed class PluginPageRegistrationService : IHostedService
{
  private const string PageId = "jellycrowd";
  private const string PageUrl = "JellyCrowd/Web/catalog.html";
  private const string PageDisplayText = "Jelly Crowd";
  private const string PageIcon = "movie";

  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<PluginPageRegistrationService> _logger;

  /// <summary>
  /// Initializes a new instance of the <see cref="PluginPageRegistrationService"/> class.
  /// </summary>
  /// <param name="serviceProvider">The service provider (used to resolve Plugin Pages lazily/optionally).</param>
  /// <param name="logger">The logger.</param>
  public PluginPageRegistrationService(IServiceProvider serviceProvider, ILogger<PluginPageRegistrationService> logger)
  {
    _serviceProvider = serviceProvider;
    _logger = logger;
  }

  /// <inheritdoc />
  public Task StartAsync(CancellationToken cancellationToken)
  {
    // The try/catch must wrap the *call* to RegisterPage: if the Plugin Pages assembly is missing,
    // the type-load failure is raised when RegisterPage is JIT-compiled, i.e. at this call site,
    // not inside RegisterPage itself.
    try
    {
      RegisterPage();
    }
#pragma warning disable CA1031 // Registration must never break server startup, whatever the failure.
    catch (Exception ex)
#pragma warning restore CA1031
    {
      _logger.LogInformation(
        ex,
        "Could not register the Jelly Crowd page with Plugin Pages. "
        + "Install the 'Plugin Pages' and 'File Transformation' plugins to enable the user-facing page.");
    }

    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  private void RegisterPage()
  {
    var manager = _serviceProvider.GetService<IPluginPagesManager>();
    if (manager is null)
    {
      _logger.LogInformation("Plugin Pages is not installed; the Jelly Crowd catalog page was not registered.");
      return;
    }

    manager.RegisterPluginPage(new PluginPage
    {
      Id = PageId,
      Url = PageUrl,
      DisplayText = PageDisplayText,
      Icon = PageIcon
    });

    _logger.LogInformation("Registered the Jelly Crowd catalog page with Plugin Pages.");
  }
}
