using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.CommandCenter.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using System.Reflection;

namespace PanoramaLayout;

/// <summary>
/// Two-page, server-driven custom_hud_layout menu. The client layout contains only static
/// Panel/Label/Button declarations; commands, page transitions, actions, and input capture are
/// all owned by the server.
/// </summary>
public sealed class PanoramaLayoutPlugin : IModSharpModule, IGameListener, IClientListener
{
    private const string DesignerName = "custom_hud_layout";
    private const string LayoutTargetName = "panorama_server_menu";
    private const string LayoutResource =
        "panorama/layout/custom_game/server_menu.vxml_c";
    private const string GameDataKey = "panorama_layout_customhud";

    private const string MenuRootPanel = "ServerMenu";
    private const string HomePagePanel = "MenuHomePage";
    private const string ActionsPagePanel = "MenuActionsPage";
    private const string HomeResultPanel = "MenuHomeResult";
    private const string ActionResultPanel = "MenuActionResult";

    private const string OpenClass = "is-open";
    private const string ActiveClass = "is-active";
    private const string ResultClass = "is-visible";
    private const string AccentClass = "is-accent";

    private static readonly string BuildTimestampUtc = ResolveBuildTimestampUtc();

    private readonly ISharedSystem _sharedSystem;
    private readonly IModSharp _modSharp;
    private readonly IEntityManager _entityManager;
    private readonly IClientManager _clientManager;
    private readonly ILogger<PanoramaLayoutPlugin> _logger;
    private readonly IGameData _gameData;
    private readonly CustomHudNativeProbe _nativeProbe;
    private readonly CustomHudClickProbe _clickProbe;
    private readonly string _moduleIdentity;
    private readonly bool _hotReload;
    private readonly HashSet<int> _accentedSlots = [];
    private readonly HashSet<int> _openSlots = [];

    private IBaseEntity? _layout;
    private bool _gameDataRegistered;
    private bool _commandRegistered;

    public PanoramaLayoutPlugin(
        ISharedSystem sharedSystem,
        string? dllPath,
        string? sharpPath,
        Version? version,
        IConfiguration? coreConfiguration,
        bool hotReload)
    {
        ArgumentNullException.ThrowIfNull(sharedSystem);

        _sharedSystem = sharedSystem;
        _modSharp = sharedSystem.GetModSharp();
        _entityManager = sharedSystem.GetEntityManager();
        _clientManager = sharedSystem.GetClientManager();
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<PanoramaLayoutPlugin>();
        _gameData = _modSharp.GetGameData();
        _nativeProbe = new CustomHudNativeProbe(
            _gameData,
            sharedSystem.GetLoggerFactory().CreateLogger<CustomHudNativeProbe>());
        _clickProbe = new CustomHudClickProbe(
            sharedSystem,
            _gameData,
            sharedSystem.GetLoggerFactory().CreateLogger<CustomHudClickProbe>(),
            QueueMenuClick);
        _moduleIdentity = Path.GetFileNameWithoutExtension(dllPath)
            ?? typeof(PanoramaLayoutPlugin).Assembly.GetName().Name
            ?? "PanoramaLayout";
        _hotReload = hotReload;
    }

    public string DisplayName => "Interactive Custom HUD Menu";
    public string DisplayAuthor => "ModSharp example";

    public bool Init()
    {
        _logger.LogInformation(
            "Loading PanoramaLayout interactive menu build {BuildTimestampUtc}; hotReload={HotReload}",
            BuildTimestampUtc,
            _hotReload);

        try
        {
            _gameData.Register(GameDataKey);
            _gameDataRegistered = true;
            _logger.LogInformation("Registered gamedata {GameDataKey}.jsonc", GameDataKey);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to register gamedata {GameDataKey}.jsonc; interactive HUD features are unavailable",
                GameDataKey);
        }

        if (_gameDataRegistered)
            _clickProbe.TryInstall();

        _modSharp.InstallGameListener(this);
        _clientManager.InstallClientListener(this);
        return true;
    }

    public void PostInit()
    {
        if (_hotReload)
            _modSharp.InvokeFrameAction(EnsureLayout);
    }

    public void OnAllModulesLoaded()
    {
        if (_commandRegistered)
            return;

        var commandCenter = _sharedSystem
            .GetSharpModuleManager()
            .GetOptionalSharpModuleInterface<ICommandCenter>(ICommandCenter.Identity)
            ?.Instance;

        if (commandCenter is null)
        {
            _logger.LogWarning(
                "CommandCenter is not loaded; !menu/ms_menu cannot be registered");
            return;
        }

        var registry = commandCenter.GetRegistry(_moduleIdentity);
        registry.RegisterGenericCommand(
            "menu",
            OnMenuCommand,
            "Open the two-page server-driven Custom HUD menu.");

        _commandRegistered = true;
        _logger.LogInformation(
            "Registered !menu / ms_menu through CommandCenter");
    }

    public void OnLibraryConnected(string name) { }
    public void OnLibraryDisconnect(string name) { }

    public void Shutdown()
    {
        _clientManager.RemoveClientListener(this);
        _modSharp.RemoveGameListener(this);
        _clickProbe.Dispose();
        _accentedSlots.Clear();
        _openSlots.Clear();
        ReleaseLayout("module shutdown");

        if (!_gameDataRegistered)
            return;

        try
        {
            _gameData.Unregister(GameDataKey);
            _gameDataRegistered = false;
            _logger.LogInformation("Unregistered gamedata {GameDataKey}.jsonc", GameDataKey);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to unregister gamedata {GameDataKey}.jsonc", GameDataKey);
        }
    }

    void IGameListener.OnServerActivate()
        => EnsureLayout();

    void IGameListener.OnRoundRestart()
    {
        _accentedSlots.Clear();
        _openSlots.Clear();
    }

    void IGameListener.OnGameShutdown()
    {
        _accentedSlots.Clear();
        _openSlots.Clear();
        ReleaseLayout("game shutdown");
    }

    int IGameListener.ListenerVersion => IGameListener.ApiVersion;
    int IGameListener.ListenerPriority => 0;

    void IClientListener.OnClientDisconnected(
        IGameClient client,
        NetworkDisconnectionReason reason)
    {
        var numericSlot = (int)client.Slot;
        _accentedSlots.Remove(numericSlot);
        _openSlots.Remove(numericSlot);

        if (_layout is not { IsValidEntity: true } layout || !TryGetSlot(client, out var slot))
            return;

        _nativeProbe.TrySetInputCaptureEnabled(layout, slot, enabled: false);
        SetPlayerClass(layout, slot, MenuRootPanel, OpenClass, false);
    }

    int IClientListener.ListenerVersion => IClientListener.ApiVersion;
    int IClientListener.ListenerPriority => 0;

    private void OnMenuCommand(IGameClient? client, StringCommand command)
    {
        if (client is null)
        {
            Console.WriteLine("[PanoramaLayout] !menu must be called by a game client.");
            return;
        }

        ToggleMenu(client);
    }

    private void ToggleMenu(IGameClient client)
    {
        var numericSlot = (int)client.Slot;

        if (_openSlots.Contains(numericSlot)
            && _layout is { IsValidEntity: true } layout
            && TryGetSlot(client, out var slot))
        {
            CloseMenu(client, layout, slot);
        }
        else
        {
            OpenMenu(client);
        }
    }

    private void OpenMenu(IGameClient client)
    {
        EnsureLayout();

        if (_layout is not { IsValidEntity: true } layout)
        {
            client.Print(HudPrintChannel.Chat, "[Server Menu] Layout entity is unavailable.");
            return;
        }

        if (!TryGetSlot(client, out var slot))
            return;

        _accentedSlots.Remove((int)client.Slot);

        var stateApplied =
            SetPlayerClass(layout, slot, MenuRootPanel, OpenClass, true)
            & SetPlayerClass(layout, slot, MenuRootPanel, AccentClass, false)
            & SetPlayerClass(layout, slot, HomePagePanel, ActiveClass, true)
            & SetPlayerClass(layout, slot, ActionsPagePanel, ActiveClass, false)
            & SetPlayerClass(layout, slot, HomeResultPanel, ResultClass, false)
            & SetPlayerClass(layout, slot, ActionResultPanel, ResultClass, false);

        var inputApplied = _nativeProbe.TrySetInputCaptureEnabled(layout, slot, enabled: true);

        if (!stateApplied || !inputApplied)
        {
            _nativeProbe.TrySetInputCaptureEnabled(layout, slot, enabled: false);
            client.Print(HudPrintChannel.Chat, "[Server Menu] Failed to initialize interactive state.");
            _logger.LogWarning(
                "Failed to open menu for slot {Slot}: stateApplied={StateApplied}, inputApplied={InputApplied}",
                slot,
                stateApplied,
                inputApplied);
            return;
        }

        client.Print(HudPrintChannel.Chat, "[Server Menu] Opened. Every click is handled by the server.");
        _openSlots.Add((int)client.Slot);
        _logger.LogInformation("Opened interactive menu for slot {Slot} ({Name})", slot, client.Name);
    }

    private void QueueMenuClick(nint controller, nint layoutEntity, string buttonId)
    {
        if (_layout is not { IsValidEntity: true } layout || layout.GetAbsPtr() != layoutEntity)
            return;

        var slot = ResolveClientSlot(controller);
        if (slot is null)
        {
            _logger.LogWarning(
                "Received Custom HUD click {ButtonId} from unknown controller 0x{Controller:X}",
                buttonId,
                controller);
            return;
        }

        _logger.LogInformation(
            "Received Custom HUD click {ButtonId} from slot {Slot}",
            buttonId,
            slot.Value);

        _modSharp.InvokeFrameAction(
            () => HandleMenuClick(slot.Value, layoutEntity, buttonId));
    }

    private void HandleMenuClick(int slot, nint layoutEntity, string buttonId)
    {
        if (_layout is not { IsValidEntity: true } layout || layout.GetAbsPtr() != layoutEntity)
            return;

        var client = _clientManager
            .GetGameClients(true)
            .FirstOrDefault(candidate => candidate.Slot == slot);

        if (client is null || !TryGetSlot(client, out var nativeSlot))
            return;

        switch (buttonId)
        {
            case "MenuOpenActions":
                SetPlayerClass(layout, nativeSlot, HomePagePanel, ActiveClass, false);
                SetPlayerClass(layout, nativeSlot, ActionsPagePanel, ActiveClass, true);
                SetPlayerClass(layout, nativeSlot, ActionResultPanel, ResultClass, false);
                client.Print(HudPrintChannel.Chat, "[Server Menu] Page 2 selected by server state.");
                break;

            case "MenuQuickStatus":
                SetPlayerClass(layout, nativeSlot, HomeResultPanel, ResultClass, true);
                client.Print(HudPrintChannel.Chat, "[Server Menu] Server status: READY.");
                break;

            case "MenuPrintHello":
                SetPlayerClass(layout, nativeSlot, ActionResultPanel, ResultClass, true);
                client.Print(
                    HudPrintChannel.Chat,
                    $"[Server Menu] Hello {client.Name}; ModSharp received MenuPrintHello.");
                break;

            case "MenuToggleAccent":
                var accentEnabled = !_accentedSlots.Remove(slot);
                if (accentEnabled)
                    _accentedSlots.Add(slot);

                SetPlayerClass(layout, nativeSlot, MenuRootPanel, AccentClass, accentEnabled);
                client.Print(
                    HudPrintChannel.Chat,
                    $"[Server Menu] Cyan theme {(accentEnabled ? "enabled" : "disabled")}.");
                break;

            case "MenuBack":
                SetPlayerClass(layout, nativeSlot, ActionsPagePanel, ActiveClass, false);
                SetPlayerClass(layout, nativeSlot, HomePagePanel, ActiveClass, true);
                client.Print(HudPrintChannel.Chat, "[Server Menu] Returned to main page.");
                break;

            case "MenuClose":
            case "MenuCloseActions":
                CloseMenu(client, layout, nativeSlot);
                break;

            default:
                _logger.LogWarning("Ignored unknown menu button {ButtonId} from slot {Slot}", buttonId, slot);
                break;
        }
    }

    private void CloseMenu(IGameClient client, IBaseEntity layout, uint slot)
    {
        var inputApplied = _nativeProbe.TrySetInputCaptureEnabled(layout, slot, enabled: false);
        var hidden = SetPlayerClass(layout, slot, MenuRootPanel, OpenClass, false);
        _accentedSlots.Remove((int)client.Slot);
        _openSlots.Remove((int)client.Slot);

        client.Print(HudPrintChannel.Chat, "[Server Menu] Closed; mouse input released.");
        _logger.LogInformation(
            "Closed interactive menu for slot {Slot}: inputReleased={InputApplied}, hidden={Hidden}",
            slot,
            inputApplied,
            hidden);
    }

    private bool SetPlayerClass(
        IBaseEntity layout,
        uint slot,
        string panelId,
        string className,
        bool enabled)
    {
        var applied = _nativeProbe.TrySetHasClassForPlayer(
            layout,
            slot,
            panelId,
            className,
            enabled);

        _logger.LogInformation(
            "Set player panel class slot={Slot} {PanelId}/{ClassName}={Enabled}; nativeApplied={Applied}",
            slot,
            panelId,
            className,
            enabled,
            applied);
        return applied;
    }

    private int? ResolveClientSlot(nint controller)
    {
        foreach (var client in _clientManager.GetGameClients(true))
        {
            if (client.GetPlayerController() is { IsValidEntity: true } playerController
                && playerController.GetAbsPtr() == controller)
            {
                return client.Slot;
            }
        }

        return null;
    }

    private static bool TryGetSlot(IGameClient client, out uint slot)
    {
        var numericSlot = (int)client.Slot;
        if (numericSlot is < 0 or >= 64)
        {
            slot = 0;
            return false;
        }

        slot = (uint)numericSlot;
        return true;
    }

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

            var layout = _entityManager.SpawnEntitySync(DesignerName, keyValues)
                ?? throw new InvalidOperationException($"ModSharp could not spawn {DesignerName}.");

            if (!layout.IsValidEntity)
                throw new InvalidOperationException($"ModSharp returned an invalid {DesignerName} entity.");

            _layout = layout;
            _logger.LogInformation(
                "Spawned interactive custom_hud_layout entity {Index}: build={BuildTimestampUtc}, target={TargetName}, layout={LayoutResource}",
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
                "Failed to spawn the interactive Custom HUD for {LayoutResource}",
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
