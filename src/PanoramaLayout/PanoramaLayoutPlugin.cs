using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Sharp.Shared;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;

namespace PanoramaLayout;

/// <summary>
/// Public-ModSharp, entity-only probe for CS2's custom_hud_layout using a Valve-owned layout.
/// </summary>
public sealed class PanoramaLayoutPlugin : IModSharpModule, IGameListener
{
    private const string DesignerName = "custom_hud_layout";
    private const string LayoutTargetName = "panorama_layout_probe";
    private const string LayoutResource = "panorama/layout/btn_alert.vxml_c";
    private static readonly string BuildTimestampUtc = ResolveBuildTimestampUtc();

    private readonly IModSharp _modSharp;
    private readonly IEntityManager _entityManager;
    private readonly ILogger<PanoramaLayoutPlugin> _logger;
    private readonly bool _hotReload;

    private IBaseEntity? _layout;

    public PanoramaLayoutPlugin(
        ISharedSystem sharedSystem,
        string? dllPath,
        string? sharpPath,
        Version? version,
        IConfiguration? coreConfiguration,
        bool hotReload)
    {
        ArgumentNullException.ThrowIfNull(sharedSystem);

        _modSharp = sharedSystem.GetModSharp();
        _entityManager = sharedSystem.GetEntityManager();
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<PanoramaLayoutPlugin>();
        _hotReload = hotReload;
    }

    public string DisplayName => "Custom HUD Entity Probe";
    public string DisplayAuthor => "ModSharp example";

    public bool Init()
    {
        _logger.LogInformation(
            "Loading PanoramaLayout build {BuildTimestampUtc}; hotReload={HotReload}",
            BuildTimestampUtc,
            _hotReload);

        _modSharp.InstallGameListener(this);
        return true;
    }

    public void PostInit()
    {
        if (_hotReload)
            _modSharp.InvokeFrameAction(EnsureLayout);
    }

    public void OnAllModulesLoaded() { }
    public void OnLibraryConnected(string name) { }
    public void OnLibraryDisconnect(string name) { }

    public void Shutdown()
    {
        _modSharp.RemoveGameListener(this);
        ReleaseLayout("module shutdown");
    }

    void IGameListener.OnServerActivate()
        => EnsureLayout();

    void IGameListener.OnGameShutdown()
        => ReleaseLayout("game shutdown");

    int IGameListener.ListenerVersion => IGameListener.ApiVersion;
    int IGameListener.ListenerPriority => 0;

    private void EnsureLayout()
    {
        if (_layout is { IsValidEntity: true })
            return;

        try
        {
            var keyValues = new Dictionary<string, KeyValuesVariantValueItem>
            {
                ["origin"] = "0 0 0",
                ["targetname"] = LayoutTargetName,
                ["layout"] = LayoutResource,
            };

            // Equivalent to Swiftly's CreateEntityByDesignerName + DispatchSpawn path:
            // SpawnEntitySync applies the keyvalues and dispatches the entity synchronously.
            var layout = _entityManager.SpawnEntitySync(DesignerName, keyValues)
                ?? throw new InvalidOperationException($"ModSharp could not spawn {DesignerName}.");

            if (!layout.IsValidEntity)
                throw new InvalidOperationException($"ModSharp returned an invalid {DesignerName} entity.");

            _layout = layout;

            _logger.LogInformation(
                "Spawned public-API custom_hud_layout entity {Index}: build={BuildTimestampUtc}, target={TargetName}, layout={LayoutResource}, path=IEntityManager.SpawnEntitySync",
                layout.Index,
                BuildTimestampUtc,
                LayoutTargetName,
                LayoutResource);
        }
        catch (Exception exception)
        {
            _layout = null;
            _logger.LogError(
                exception,
                "Failed to spawn the custom HUD entity probe for {LayoutResource}",
                LayoutResource);
        }
    }

    private void ReleaseLayout(string reason)
    {
        var layout = _layout;
        _layout = null;

        if (layout is not { IsValidEntity: true })
            return;

        try
        {
            var entityIndex = layout.Index;
            layout.Kill();
            _logger.LogInformation("Removed custom_hud_layout entity {Index}: {Reason}", entityIndex, reason);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to remove custom_hud_layout: {Reason}", reason);
        }
    }

    private static string ResolveBuildTimestampUtc()
        => typeof(PanoramaLayoutPlugin)
            .Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "BuildTimestampUtc")
            ?.Value
            ?? "unknown";
}
